using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Functorium.SourceGenerators.Abstractions;
using Functorium.SourceGenerators.Generators.ObservablePortGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;

namespace Functorium.SourceGenerators.Generators.CtxEnricherGenerator;

/// <summary>
/// ICommandRequest&lt;T&gt; 또는 IQueryRequest&lt;T&gt;를 구현하는 Request record를 자동 감지하여
/// IUsecaseCtxEnricher 구현체를 생성하는 소스 생성기.
/// [CtxIgnore]를 Request record에 적용하면 생성을 제외합니다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CtxEnricherGenerator()
    : IncrementalGeneratorBase<CtxEnricherInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string IgnoreAttributeFullName = "Functorium.Applications.Observabilities.CtxIgnoreAttribute";
    private const string RootAttributeFullName = "Functorium.Applications.Observabilities.CtxRootAttribute";
    private const string TargetAttributeFullName = "Functorium.Applications.Observabilities.CtxTargetAttribute";

    private static readonly DiagnosticDescriptor InaccessibleRequestDiagnostic = new(
        id: "FUNCTORIUM003",
        title: "Request type is not accessible for CtxEnricher generation",
        messageFormat: "'{0}' implements ICommandRequest/IQueryRequest but CtxEnricher cannot be generated because '{1}' is {2}. Apply [CtxIgnore] to the Request record to suppress this warning.",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CtxFieldTypeConflictDiagnostic = new(
        id: "FUNCTORIUM002",
        title: "ctx field type conflict across CtxEnrichers",
        messageFormat: "ctx field '{0}' has conflicting types: '{1}' ({2}) in '{3}' vs '{4}' ({5}) in '{6}'. OpenSearch dynamic mapping will reject one of them.",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor HighCardinalityMetricsTagDiagnostic = new(
        id: "FUNCTORIUM005",
        title: "High-cardinality type used as MetricsTag",
        messageFormat: "ctx field '{0}' ({1}) is marked as MetricsTag but has potentially high cardinality. This may cause metrics cardinality explosion. Use MetricsValue for numeric types or ensure the string has bounded values.",
        category: "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonNumericMetricsValueDiagnostic = new(
        id: "FUNCTORIUM006",
        title: "Non-numeric type used as MetricsValue",
        messageFormat: "ctx field '{0}' ({1}) is marked as MetricsValue but is not a numeric type. Only long/double types can be recorded as metric values.",
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MetricsTagAndValueDiagnostic = new(
        id: "FUNCTORIUM007",
        title: "MetricsTag and MetricsValue both specified",
        messageFormat: "ctx field '{0}' has both MetricsTag and MetricsValue specified. A property should typically serve one role.",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static IncrementalValuesProvider<CtxEnricherInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsRecordDeclaration,
                transform: MapToCtxEnricherInfo)
            .Where(x => x.RequestTypeName.Length > 0);
    }

    private static bool IsRecordDeclaration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is RecordDeclarationSyntax;
    }

    private static CtxEnricherInfo MapToCtxEnricherInfo(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken)
            is not INamedTypeSymbol requestSymbol)
            return CtxEnricherInfo.None;

        // ICommandRequest<T> 또는 IQueryRequest<T> 구현 여부 확인
        if (FindResponseType(requestSymbol) is null)
            return CtxEnricherInfo.None;

        // [CtxIgnore] 클래스 레벨 옵트아웃 확인
        if (HasClassLevelIgnoreAttribute(requestSymbol))
            return CtxEnricherInfo.None;

        // 네임스페이스 레벨에서 접근 불가능한 타입은 진단 경고 후 건너뜀
        var inaccessibleType = FindInaccessibleType(requestSymbol);
        if (inaccessibleType != null)
        {
            return CtxEnricherInfo.Inaccessible(
                requestSymbol.ToDisplayString(),
                inaccessibleType.Name,
                inaccessibleType.DeclaredAccessibility.ToString().ToLowerInvariant(),
                context.Node.GetLocation());
        }

        cancellationToken.ThrowIfCancellationRequested();

        string @namespace = requestSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : requestSymbol.ContainingNamespace.ToString();

        var containingTypeNames = GetContainingTypeNames(requestSymbol);

        string ctxTypePrefix = string.Join(".",
            containingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));

        string requestTypeName = requestSymbol.Name;
        string requestTypeFullName = GetGlobalFullName(requestSymbol);

        var requestProperties = CollectProperties(requestSymbol, "request", $"{ctxTypePrefix}.request");

        INamedTypeSymbol? responseSymbol = FindResponseType(requestSymbol);
        string responseTypeName = string.Empty;
        string responseTypeFullName = string.Empty;
        CtxPropertyInfo[] responseProperties = [];

        if (responseSymbol != null)
        {
            responseTypeName = GetMinimalName(responseSymbol, requestSymbol);
            responseTypeFullName = GetGlobalFullName(responseSymbol);
            responseProperties = CollectProperties(responseSymbol, "r", $"{ctxTypePrefix}.response");
        }

        // Enricher 클래스 이름: {ContainingTypes}{RequestTypeName}CtxEnricher
        string enricherClassName = string.Concat(containingTypeNames.Select(n => n))
            + requestTypeName + "CtxEnricher";

        Location? location = context.Node.GetLocation();

        return new CtxEnricherInfo(
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

    private static CtxPropertyInfo[] CollectProperties(
        INamedTypeSymbol typeSymbol,
        string variablePrefix,
        string ctxSegmentPrefix)
    {
        var properties = new System.Collections.Generic.List<CtxPropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            if (property.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (property.IsStatic || property.IsIndexer)
                continue;

            // [CtxIgnore] 제외
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

            // CtxTarget pillar 결정
            int targetPillars = ResolveTargetPillars(property, declaringInterface);

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

            properties.Add(new CtxPropertyInfo(
                property.Name,
                ctxFieldName,
                typeFullName,
                isCollection,
                countExpression,
                openSearchTypeGroup,
                isRoot,
                needsToString: false,
                targetPillars));
        }

        return properties.ToArray();
    }

    /// <summary>
    /// [CtxTarget] 어트리뷰트를 파싱하여 CtxPillar 값을 결정합니다.
    /// 우선순위: 프로퍼티/파라미터 > 인터페이스 > 기본값(Default)
    /// </summary>
    private static int ResolveTargetPillars(IPropertySymbol property, INamedTypeSymbol? declaringInterface)
    {
        // 1. 프로퍼티 직접 어트리뷰트 확인
        int? directTarget = GetTargetPillarsFromAttributes(property.GetAttributes());

        // record 생성자 파라미터의 어트리뷰트도 확인
        if (directTarget == null)
        {
            var containingType = property.ContainingType;
            foreach (var ctor in containingType.Constructors)
            {
                foreach (var param in ctor.Parameters)
                {
                    if (param.Name == property.Name)
                    {
                        directTarget = GetTargetPillarsFromAttributes(param.GetAttributes());
                        if (directTarget != null) break;
                    }
                }
                if (directTarget != null) break;
            }
        }

        if (directTarget != null)
            return directTarget.Value;

        // 2. 인터페이스 수준 어트리뷰트 확인
        if (declaringInterface != null)
        {
            int? interfaceTarget = GetTargetPillarsFromAttributes(declaringInterface.GetAttributes());
            if (interfaceTarget != null)
                return interfaceTarget.Value;
        }

        // 3. 기본값: CtxPillar.Default (Logging | Tracing)
        return CtxPropertyInfo.PillarDefault;
    }

    private static int? GetTargetPillarsFromAttributes(
        System.Collections.Immutable.ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() == TargetAttributeFullName
                && attr.ConstructorArguments.Length == 1
                && attr.ConstructorArguments[0].Value is int pillars)
            {
                return pillars;
            }
        }
        return null;
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
        if (property.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == IgnoreAttributeFullName))
            return true;

        if (property.DeclaringSyntaxReferences.Length > 0)
        {
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
        if (property.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == RootAttributeFullName))
            return true;

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

    private static string GetInterfaceCtxPrefix(INamedTypeSymbol interfaceSymbol)
    {
        string name = interfaceSymbol.Name;
        if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);
        return SnakeCaseConverter.ToSnakeCase(name);
    }

    private static bool IsComplexType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.SpecialType != SpecialType.None)
            return false;

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return false;

        string fullName = typeSymbol.ToDisplayString();

        if (fullName == "System.Guid" || fullName == "System.DateTime"
            || fullName == "System.DateTimeOffset" || fullName == "System.TimeSpan"
            || fullName == "System.DateOnly" || fullName == "System.TimeOnly"
            || fullName == "decimal" || fullName == "System.Decimal"
            || fullName == "System.Uri")
            return false;

        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return IsComplexType(namedType.TypeArguments[0]);
        }

        if (fullName.Contains("LanguageExt.Option<"))
            return false;

        if (CollectionTypeHelper.IsCollectionType(fullName))
            return false;

        if (typeSymbol.TypeKind == TypeKind.Class
            || typeSymbol.TypeKind == TypeKind.Struct
            || typeSymbol.TypeKind == TypeKind.Interface)
            return true;

        return false;
    }

    private static string GetOpenSearchTypeGroup(ITypeSymbol typeSymbol, bool isCollection)
    {
        if (isCollection) return "long";

        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return GetOpenSearchTypeGroup(namedType.TypeArguments[0], false);
        }

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

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return "keyword";

        string fullName = typeSymbol.ToDisplayString();
        if (fullName is "System.Guid" or "System.DateTime" or "System.DateTimeOffset"
            or "System.TimeSpan" or "System.DateOnly" or "System.TimeOnly" or "System.Uri")
            return "keyword";

        if (fullName.Contains("LanguageExt.Option<"))
            return "keyword";

        return "keyword";
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<CtxEnricherInfo> enricherInfos)
    {
        // ctx 필드 타입 충돌 감지
        DetectCtxFieldTypeConflicts(context, enricherInfos);

        // Metrics 진단 검사
        DetectMetricsDiagnostics(context, enricherInfos);

        foreach (var info in enricherInfos)
        {
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
        ImmutableArray<CtxEnricherInfo> enricherInfos)
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

    private static void DetectMetricsDiagnostics(
        SourceProductionContext context,
        ImmutableArray<CtxEnricherInfo> enricherInfos)
    {
        foreach (var info in enricherInfos)
        {
            if (info.SkipReason.Length > 0) continue;

            var allProperties = info.RequestProperties.Concat(info.ResponseProperties);
            foreach (var prop in allProperties)
            {
                // FUNCTORIUM005: 고카디널리티 타입 + MetricsTag
                if (prop.HasMetricsTag && prop.OpenSearchTypeGroup != "boolean")
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        HighCardinalityMetricsTagDiagnostic,
                        info.Location,
                        prop.CtxFieldName,
                        prop.TypeFullName));
                }

                // FUNCTORIUM006: 비수치 타입 + MetricsValue
                if (prop.HasMetricsValue && prop.OpenSearchTypeGroup is not "long" and not "double")
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        NonNumericMetricsValueDiagnostic,
                        info.Location,
                        prop.CtxFieldName,
                        prop.TypeFullName));
                }

                // FUNCTORIUM007: MetricsTag + MetricsValue 동시
                if (prop.HasMetricsTag && prop.HasMetricsValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MetricsTagAndValueDiagnostic,
                        info.Location,
                        prop.CtxFieldName));
                }
            }
        }
    }

    private static string GenerateEnricherSource(CtxEnricherInfo info, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};")
                .AppendLine();
        }

        string ctxTypePrefix = string.Join(".",
            info.ContainingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));

        string finResponseType = $"global::Functorium.Applications.Usecases.FinResponse<{info.ResponseTypeFullName}>";
        string requestFullType = info.RequestTypeFullName;

        int requestCount = info.RequestProperties.Length;
        int responseCount = info.ResponseProperties.Length;

        sb.AppendLine($"public partial class {info.EnricherClassName}");
        sb.AppendLine($"    : global::Functorium.Applications.Observabilities.IUsecaseCtxEnricher<{requestFullType}, {finResponseType}>");
        sb.AppendLine("{");

        // EnrichRequest
        sb.AppendLine($"    public global::System.IDisposable? EnrichRequest({requestFullType} request)");
        sb.AppendLine("    {");
        if (requestCount > 0)
        {
            sb.AppendLine($"        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>({requestCount});");
            foreach (var prop in info.RequestProperties)
            {
                string pillarArg = GetPillarArgument(prop);
                if (prop.IsCollection)
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", {prop.CountExpression}{pillarArg}));");
                }
                else
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", request.{prop.PropertyName}{pillarArg}));");
                }
            }
            sb.AppendLine("        OnEnrichRequest(request, disposables);");
            sb.AppendLine("        return new GeneratedCompositeDisposable(disposables);");
        }
        else
        {
            sb.AppendLine("        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>();");
            sb.AppendLine("        OnEnrichRequest(request, disposables);");
            sb.AppendLine("        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // EnrichResponse
        sb.AppendLine($"    public global::System.IDisposable? EnrichResponse(");
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
                string pillarArg = GetPillarArgument(prop);
                if (prop.IsCollection)
                {
                    sb.AppendLine($"            disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", {prop.CountExpression}{pillarArg}));");
                }
                else
                {
                    sb.AppendLine($"            disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", r.{prop.PropertyName}{pillarArg}));");
                }
            }
            sb.AppendLine("        }");
        }
        sb.AppendLine("        OnEnrichResponse(request, response, disposables);");
        sb.AppendLine("        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // partial void extension points
        sb.AppendLine($"    partial void OnEnrichRequest(");
        sb.AppendLine($"        {requestFullType} request,");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables);");
        sb.AppendLine();
        sb.AppendLine($"    partial void OnEnrichResponse(");
        sb.AppendLine($"        {requestFullType} request,");
        sb.AppendLine($"        {finResponseType} response,");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables);");
        sb.AppendLine();

        // Helper: PushRequestCtx / PushResponseCtx
        sb.AppendLine($"    private static void PushRequestCtx(");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
        sb.AppendLine($"        string fieldName,");
        sb.AppendLine($"        object? value,");
        sb.AppendLine($"        global::Functorium.Applications.Observabilities.CtxPillar pillars = global::Functorium.Applications.Observabilities.CtxPillar.Default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(");
        sb.AppendLine($"            \"ctx.{ctxTypePrefix}.request.\" + fieldName, value, pillars));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private static void PushResponseCtx(");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
        sb.AppendLine($"        string fieldName,");
        sb.AppendLine($"        object? value,");
        sb.AppendLine($"        global::Functorium.Applications.Observabilities.CtxPillar pillars = global::Functorium.Applications.Observabilities.CtxPillar.Default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(");
        sb.AppendLine($"            \"ctx.{ctxTypePrefix}.response.\" + fieldName, value, pillars));");
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
            sb.AppendLine($"        object? value,");
            sb.AppendLine($"        global::Functorium.Applications.Observabilities.CtxPillar pillars = global::Functorium.Applications.Observabilities.CtxPillar.Default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(");
            sb.AppendLine($"            \"ctx.\" + fieldName, value, pillars));");
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

    /// <summary>
    /// CtxPillar 인자 문자열을 생성합니다.
    /// Default (Logging | Tracing)이면 생략 (기본값 사용), 아니면 명시적으로 캐스트합니다.
    /// </summary>
    private static string GetPillarArgument(CtxPropertyInfo prop)
    {
        if (prop.IsDefault)
            return ""; // 기본값 사용 — Push의 default parameter

        return $", (global::Functorium.Applications.Observabilities.CtxPillar){prop.TargetPillars}";
    }
}
