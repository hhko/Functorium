using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Functorium.SourceGenerators.Abstractions;
using Functorium.SourceGenerators.Generators.ObservablePortGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;

namespace Functorium.SourceGenerators.Generators.LogEnricherGenerator;

/// <summary>
/// ICommandRequest&lt;T&gt; 또는 IQueryRequest&lt;T&gt;를 구현하는 Request record를 자동 감지하여
/// IUsecaseLogEnricher 구현체를 생성하는 소스 생성기.
/// [LogEnricherIgnore]를 Request record에 적용하면 생성을 제외합니다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class LogEnricherGenerator()
    : IncrementalGeneratorBase<LogEnricherInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string IgnoreAttributeFullName = "Functorium.Applications.Usecases.LogEnricherIgnoreAttribute";
    private const string RootAttributeFullName = "Functorium.Applications.Observabilities.LogEnricherRootAttribute";

    private static readonly DiagnosticDescriptor InaccessibleRequestDiagnostic = new(
        id: "FUNCTORIUM003",
        title: "Request type is not accessible for LogEnricher generation",
        messageFormat: "'{0}' implements ICommandRequest/IQueryRequest but LogEnricher cannot be generated because '{1}' is {2}. Apply [LogEnricherIgnore] to the Request record to suppress this warning.",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CtxFieldTypeConflictDiagnostic = new(
        id: "FUNCTORIUM002",
        title: "ctx field type conflict across LogEnrichers",
        messageFormat: "ctx field '{0}' has conflicting types: '{1}' ({2}) in '{3}' vs '{4}' ({5}) in '{6}'. OpenSearch dynamic mapping will reject one of them.",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static IncrementalValuesProvider<LogEnricherInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsRecordDeclaration,
                transform: MapToLogEnricherInfo)
            .Where(x => x.RequestTypeName.Length > 0);
    }

    private static bool IsRecordDeclaration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is RecordDeclarationSyntax;
    }

    private static LogEnricherInfo MapToLogEnricherInfo(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken)
            is not INamedTypeSymbol requestSymbol)
            return LogEnricherInfo.None;

        // ICommandRequest<T> 또는 IQueryRequest<T> 구현 여부 확인
        if (FindResponseType(requestSymbol) is null)
            return LogEnricherInfo.None;

        // [LogEnricherIgnore] 클래스 레벨 옵트아웃 확인
        if (HasClassLevelIgnoreAttribute(requestSymbol))
            return LogEnricherInfo.None;

        // 네임스페이스 레벨에서 접근 불가능한 타입은 진단 경고 후 건너뜀
        var inaccessibleType = FindInaccessibleType(requestSymbol);
        if (inaccessibleType != null)
        {
            return LogEnricherInfo.Inaccessible(
                requestSymbol.ToDisplayString(),
                inaccessibleType.Name,
                inaccessibleType.DeclaredAccessibility.ToString().ToLowerInvariant(),
                context.Node.GetLocation());
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 네임스페이스
        string @namespace = requestSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : requestSymbol.ContainingNamespace.ToString();

        // 포함 타입 이름 목록 (바깥 → 안쪽)
        var containingTypeNames = GetContainingTypeNames(requestSymbol);

        // ctx 타입 접두사 계산
        string ctxTypePrefix = string.Join(".",
            containingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));

        string requestTypeName = requestSymbol.Name;

        // Request 전체 한정 이름
        string requestTypeFullName = GetGlobalFullName(requestSymbol);

        // Request 속성 수집
        var requestProperties = CollectProperties(requestSymbol, "request", $"{ctxTypePrefix}.request");

        // ICommandRequest<TSuccess> 또는 IQueryRequest<TSuccess>에서 TSuccess 타입 발견
        INamedTypeSymbol? responseSymbol = FindResponseType(requestSymbol);
        string responseTypeName = string.Empty;
        string responseTypeFullName = string.Empty;
        LogEnricherPropertyInfo[] responseProperties = [];

        if (responseSymbol != null)
        {
            responseTypeName = GetMinimalName(responseSymbol, requestSymbol);
            responseTypeFullName = GetGlobalFullName(responseSymbol);
            responseProperties = CollectProperties(responseSymbol, "r", $"{ctxTypePrefix}.response");
        }

        // Enricher 클래스 이름: {ContainingTypes}{RequestTypeName}LogEnricher
        string enricherClassName = string.Concat(containingTypeNames.Select(n => n))
            + requestTypeName + "LogEnricher";

        Location? location = context.Node.GetLocation();

        return new LogEnricherInfo(
            @namespace,
            containingTypeNames,
            requestTypeName,
            requestProperties,
            responseTypeName,
            responseTypeFullName,
            responseProperties,
            enricherClassName,
            requestTypeFullName,
            location);
    }

    private static string[] GetContainingTypeNames(INamedTypeSymbol symbol)
    {
        var names = new System.Collections.Generic.List<string>();
        var current = symbol.ContainingType;
        while (current != null)
        {
            names.Insert(0, current.Name);
            current = current.ContainingType;
        }
        return names.ToArray();
    }

    private static string GetGlobalFullName(INamedTypeSymbol symbol)
    {
        return "global::" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .RemoveGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters))
            .Replace("global::", "");
    }

    private static string GetMinimalName(INamedTypeSymbol symbol, INamedTypeSymbol contextSymbol)
    {
        // 같은 네임스페이스 + 같은 containing type이면 짧은 이름 사용
        if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, contextSymbol.ContainingType)
            && SymbolEqualityComparer.Default.Equals(symbol.ContainingNamespace, contextSymbol.ContainingNamespace))
        {
            if (symbol.ContainingType != null)
                return symbol.ContainingType.Name + "." + symbol.Name;
            return symbol.Name;
        }

        return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static INamedTypeSymbol? FindResponseType(INamedTypeSymbol requestSymbol)
    {
        foreach (var iface in requestSymbol.AllInterfaces)
        {
            if (iface.IsGenericType && iface.TypeArguments.Length == 1)
            {
                string ifaceName = iface.OriginalDefinition.ToDisplayString();
                if (ifaceName == "Functorium.Applications.Usecases.ICommandRequest<TSuccess>"
                    || ifaceName == "Functorium.Applications.Usecases.IQueryRequest<TSuccess>")
                {
                    return iface.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }
        return null;
    }

    private static LogEnricherPropertyInfo[] CollectProperties(
        INamedTypeSymbol typeSymbol,
        string variablePrefix,
        string ctxSegmentPrefix)
    {
        var properties = new System.Collections.Generic.List<LogEnricherPropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            if (property.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (property.IsStatic || property.IsIndexer)
                continue;

            // [LogEnricherIgnore] 제외
            if (HasIgnoreAttribute(property))
                continue;

            string typeFullName = property.Type.ToDisplayString();
            bool isCollection = CollectionTypeHelper.IsCollectionType(typeFullName);

            // 복합 타입(클래스/레코드) 건너뜀 — 스칼라와 컬렉션만 처리
            if (!isCollection && IsComplexType(property.Type))
                continue;

            string snakeName = SnakeCaseConverter.ToSnakeCase(property.Name);
            string ctxFieldName;
            string? countExpression = null;
            string openSearchTypeGroup = GetOpenSearchTypeGroup(property.Type, isCollection);

            bool hasDirectRootAttribute = HasRootAttribute(property);
            var (declaringInterface, isRootFromInterface) = FindPropertyInterface(typeSymbol, property);
            bool isRoot = hasDirectRootAttribute || isRootFromInterface;

            if (isCollection)
            {
                if (isRoot)
                    ctxFieldName = $"ctx.{snakeName}_count";
                else if (declaringInterface != null)
                    ctxFieldName = $"ctx.{GetInterfaceCtxPrefix(declaringInterface)}.{snakeName}_count";
                else
                    ctxFieldName = $"ctx.{ctxSegmentPrefix}.{snakeName}_count";
                countExpression = CollectionTypeHelper.GetCountExpression(
                    $"{variablePrefix}.{property.Name}", typeFullName);
            }
            else
            {
                if (isRoot)
                    ctxFieldName = $"ctx.{snakeName}";
                else if (declaringInterface != null)
                    ctxFieldName = $"ctx.{GetInterfaceCtxPrefix(declaringInterface)}.{snakeName}";
                else
                    ctxFieldName = $"ctx.{ctxSegmentPrefix}.{snakeName}";
            }

            properties.Add(new LogEnricherPropertyInfo(
                property.Name,
                ctxFieldName,
                typeFullName,
                isCollection,
                countExpression,
                openSearchTypeGroup,
                isRoot));
        }

        return properties.ToArray();
    }

    private static bool HasClassLevelIgnoreAttribute(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == IgnoreAttributeFullName);
    }

    private static INamedTypeSymbol? FindInaccessibleType(INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? current = symbol;
        while (current != null)
        {
            if (current.DeclaredAccessibility is Accessibility.Private
                or Accessibility.Protected
                or Accessibility.ProtectedAndInternal)
                return current;
            current = current.ContainingType;
        }
        return null;
    }

    private static bool HasIgnoreAttribute(IPropertySymbol property)
    {
        // 프로퍼티 자체의 어트리뷰트 확인
        if (property.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == IgnoreAttributeFullName))
            return true;

        // record 생성자 파라미터의 어트리뷰트 확인
        if (property.DeclaringSyntaxReferences.Length > 0)
        {
            // record의 primary constructor parameter에서도 확인
            var containingType = property.ContainingType;
            foreach (var ctor in containingType.Constructors)
            {
                foreach (var param in ctor.Parameters)
                {
                    if (param.Name == property.Name
                        && param.GetAttributes().Any(a =>
                            a.AttributeClass?.ToDisplayString() == IgnoreAttributeFullName))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool HasRootAttribute(IPropertySymbol property)
    {
        // 프로퍼티 자체의 어트리뷰트 확인
        if (property.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == RootAttributeFullName))
            return true;

        // record 생성자 파라미터의 어트리뷰트 확인
        if (property.DeclaringSyntaxReferences.Length > 0)
        {
            var containingType = property.ContainingType;
            foreach (var ctor in containingType.Constructors)
            {
                foreach (var param in ctor.Parameters)
                {
                    if (param.Name == property.Name
                        && param.GetAttributes().Any(a =>
                            a.AttributeClass?.ToDisplayString() == RootAttributeFullName))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 프로퍼티가 선언된 인터페이스를 찾는다.
    /// [LogEnricherRoot] 인터페이스를 우선 반환한다.
    /// </summary>
    private static (INamedTypeSymbol? declaringInterface, bool isRoot) FindPropertyInterface(
        INamedTypeSymbol typeSymbol, IPropertySymbol property)
    {
        INamedTypeSymbol? firstNonRoot = null;

        foreach (var iface in typeSymbol.AllInterfaces)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member is IPropertySymbol interfaceProperty
                    && interfaceProperty.Name == property.Name)
                {
                    bool isRoot = iface.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString() == RootAttributeFullName);

                    if (isRoot) return (iface, true);
                    firstNonRoot ??= iface;
                    break;
                }
            }
        }

        return firstNonRoot != null ? (firstNonRoot, false) : (null, false);
    }

    /// <summary>
    /// 인터페이스 이름에서 ctx prefix를 계산한다.
    /// 규칙: 'I' prefix 제거(표준 네이밍) → snake_case
    /// IX → x, ICustomerRequest → customer_request
    /// </summary>
    private static string GetInterfaceCtxPrefix(INamedTypeSymbol interfaceSymbol)
    {
        string name = interfaceSymbol.Name;
        if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);
        return SnakeCaseConverter.ToSnakeCase(name);
    }

    private static bool IsComplexType(ITypeSymbol typeSymbol)
    {
        // 스칼라 타입: primitive, string, decimal, DateTime, Guid, enum, Option<T> 등
        if (typeSymbol.SpecialType != SpecialType.None)
            return false; // string, int, bool, etc.

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return false;

        string fullName = typeSymbol.ToDisplayString();

        // 잘 알려진 스칼라 타입들
        if (fullName == "System.Guid" || fullName == "System.DateTime"
            || fullName == "System.DateTimeOffset" || fullName == "System.TimeSpan"
            || fullName == "System.DateOnly" || fullName == "System.TimeOnly"
            || fullName == "decimal" || fullName == "System.Decimal"
            || fullName == "System.Uri")
            return false;

        // Nullable<T>는 내부 T에 위임
        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return IsComplexType(namedType.TypeArguments[0]);
        }

        // LanguageExt Option<T>는 스칼라로 처리
        if (fullName.Contains("LanguageExt.Option<"))
            return false;

        // 컬렉션은 이미 별도 처리되므로 여기서는 복합 타입이 아님
        if (CollectionTypeHelper.IsCollectionType(fullName))
            return false;

        // 그 외 class/record/struct(커스텀)는 복합 타입
        if (typeSymbol.TypeKind == TypeKind.Class
            || typeSymbol.TypeKind == TypeKind.Struct
            || typeSymbol.TypeKind == TypeKind.Interface)
            return true;

        return false;
    }

    private static string GetOpenSearchTypeGroup(ITypeSymbol typeSymbol, bool isCollection)
    {
        if (isCollection) return "long"; // count는 항상 정수

        // Nullable<T>는 내부 T에 위임
        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return GetOpenSearchTypeGroup(namedType.TypeArguments[0], false);
        }

        // SpecialType 기반 분류
        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "boolean";
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return "long";
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return "double";
            case SpecialType.System_String:
                return "keyword";
        }

        // enum → keyword
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return "keyword";

        // 잘 알려진 스칼라 타입 → keyword
        string fullName = typeSymbol.ToDisplayString();
        if (fullName is "System.Guid" or "System.DateTime" or "System.DateTimeOffset"
            or "System.TimeSpan" or "System.DateOnly" or "System.TimeOnly" or "System.Uri")
            return "keyword";

        // LanguageExt Option<T> → keyword
        if (fullName.Contains("LanguageExt.Option<"))
            return "keyword";

        // 기본값: keyword (CollectProperties에서 스칼라/컬렉션만 남으므로)
        return "keyword";
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<LogEnricherInfo> enricherInfos)
    {
        // ctx 필드 타입 충돌 감지
        DetectCtxFieldTypeConflicts(context, enricherInfos);

        foreach (var info in enricherInfos)
        {
            // 접근 불가능한 타입: 진단 경고만 출력하고 코드 생성 건너뜀
            if (info.SkipReason.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InaccessibleRequestDiagnostic,
                    info.Location,
                    info.RequestTypeFullName,
                    info.ResponseTypeFullName,
                    info.SkipReason));
                continue;
            }

            StringBuilder sb = new();
            string source = GenerateEnricherSource(info, sb);

            // 네임스페이스의 마지막 부분 추출
            string namespaceSuffix = string.Empty;
            if (!string.IsNullOrEmpty(info.Namespace))
            {
                var lastDotIndex = info.Namespace.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    namespaceSuffix = info.Namespace.Substring(lastDotIndex + 1) + ".";
                }
            }

            context.AddSource(
                $"{namespaceSuffix}{info.EnricherClassName}.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void DetectCtxFieldTypeConflicts(
        SourceProductionContext context,
        ImmutableArray<LogEnricherInfo> enricherInfos)
    {
        var ctxFieldMap = new System.Collections.Generic.Dictionary<string,
            (string OpenSearchTypeGroup, string TypeFullName, string EnricherClassName, Location? Location)>();

        foreach (var info in enricherInfos)
        {
            var allProperties = info.RequestProperties.Concat(info.ResponseProperties);

            foreach (var prop in allProperties)
            {
                if (ctxFieldMap.TryGetValue(prop.CtxFieldName, out var existing))
                {
                    if (existing.OpenSearchTypeGroup != prop.OpenSearchTypeGroup)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            CtxFieldTypeConflictDiagnostic,
                            info.Location,
                            prop.CtxFieldName,
                            prop.TypeFullName, prop.OpenSearchTypeGroup, info.EnricherClassName,
                            existing.TypeFullName, existing.OpenSearchTypeGroup, existing.EnricherClassName));
                    }
                }
                else
                {
                    ctxFieldMap[prop.CtxFieldName] = (
                        prop.OpenSearchTypeGroup, prop.TypeFullName,
                        info.EnricherClassName, info.Location);
                }
            }
        }
    }

    private static string GenerateEnricherSource(LogEnricherInfo info, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};")
                .AppendLine();
        }

        // Request의 display 형식: ContainingTypes.RequestTypeName
        string requestDisplayType = string.Concat(info.ContainingTypeNames.Select(n => n + "."))
            + info.RequestTypeName;

        // ctx 타입 접두사 계산 (PushRequestCtx/PushResponseCtx 헬퍼용)
        string ctxTypePrefix = string.Join(".",
            info.ContainingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));

        // FinResponse<Response> 형식
        string finResponseType = $"global::Functorium.Applications.Usecases.FinResponse<{info.ResponseTypeFullName}>";

        // Request의 전체 한정 형식 (using 없이도 동작하도록)
        string requestFullType = info.RequestTypeFullName;

        int requestCount = info.RequestProperties.Length;
        int responseCount = info.ResponseProperties.Length;

        sb.AppendLine($"public partial class {info.EnricherClassName}");
        sb.AppendLine($"    : global::Functorium.Applications.Observabilities.IUsecaseLogEnricher<{requestFullType}, {finResponseType}>");
        sb.AppendLine("{");

        // EnrichRequestLog
        sb.AppendLine($"    public global::System.IDisposable? EnrichRequestLog({requestFullType} request)");
        sb.AppendLine("    {");
        if (requestCount > 0)
        {
            sb.AppendLine($"        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>({requestCount});");
            foreach (var prop in info.RequestProperties)
            {
                if (prop.IsCollection)
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(\"{prop.CtxFieldName}\", {prop.CountExpression}));");
                }
                else
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(\"{prop.CtxFieldName}\", request.{prop.PropertyName}));");
                }
            }
            sb.AppendLine("        OnEnrichRequestLog(request, disposables);");
            sb.AppendLine("        return new GeneratedCompositeDisposable(disposables);");
        }
        else
        {
            sb.AppendLine("        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>();");
            sb.AppendLine("        OnEnrichRequestLog(request, disposables);");
            sb.AppendLine("        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // EnrichResponseLog
        sb.AppendLine($"    public global::System.IDisposable? EnrichResponseLog(");
        sb.AppendLine($"        {requestFullType} request,");
        sb.AppendLine($"        {finResponseType} response)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>({responseCount});");
        if (responseCount > 0)
        {
            sb.AppendLine($"        if (response is {finResponseType}.Succ {{ Value: var r }})");
            sb.AppendLine("        {");
            foreach (var prop in info.ResponseProperties)
            {
                if (prop.IsCollection)
                {
                    sb.AppendLine($"            disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(\"{prop.CtxFieldName}\", {prop.CountExpression}));");
                }
                else
                {
                    sb.AppendLine($"            disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(\"{prop.CtxFieldName}\", r.{prop.PropertyName}));");
                }
            }
            sb.AppendLine("        }");
        }
        sb.AppendLine("        OnEnrichResponseLog(request, response, disposables);");
        sb.AppendLine("        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // partial void extension points
        sb.AppendLine($"    partial void OnEnrichRequestLog(");
        sb.AppendLine($"        {requestFullType} request,");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables);");
        sb.AppendLine();
        sb.AppendLine($"    partial void OnEnrichResponseLog(");
        sb.AppendLine($"        {requestFullType} request,");
        sb.AppendLine($"        {finResponseType} response,");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables);");
        sb.AppendLine();

        // Helper: PushRequestCtx / PushResponseCtx
        sb.AppendLine($"    private static void PushRequestCtx(");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
        sb.AppendLine($"        string fieldName,");
        sb.AppendLine($"        object? value)");
        sb.AppendLine("    {");
        sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(");
        sb.AppendLine($"            \"ctx.{ctxTypePrefix}.request.\" + fieldName, value));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private static void PushResponseCtx(");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
        sb.AppendLine($"        string fieldName,");
        sb.AppendLine($"        object? value)");
        sb.AppendLine("    {");
        sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(");
        sb.AppendLine($"            \"ctx.{ctxTypePrefix}.response.\" + fieldName, value));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Helper: PushRootCtx (root 속성이 있을 때만 생성)
        bool hasRootProperties = info.RequestProperties.Any(p => p.IsRoot)
            || info.ResponseProperties.Any(p => p.IsRoot);

        if (hasRootProperties)
        {
            sb.AppendLine($"    private static void PushRootCtx(");
            sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
            sb.AppendLine($"        string fieldName,");
            sb.AppendLine($"        object? value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.LogEnricherContext.PushProperty(");
            sb.AppendLine($"            \"ctx.\" + fieldName, value));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // GeneratedCompositeDisposable
        sb.AppendLine("    private sealed class GeneratedCompositeDisposable(global::System.Collections.Generic.List<global::System.IDisposable> disposables) : global::System.IDisposable");
        sb.AppendLine("    {");
        sb.AppendLine("        public void Dispose()");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int i = disposables.Count - 1; i >= 0; i--)");
        sb.AppendLine("                disposables[i].Dispose();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }
}
