using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Functorium.SourceGenerators.Abstractions;
using Functorium.SourceGenerators.Generators.CtxEnricherGenerator;
using Functorium.SourceGenerators.Generators.ObservablePortGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;

namespace Functorium.SourceGenerators.Generators.DomainEventCtxEnricherGenerator;

/// <summary>
/// IDomainEventHandler&lt;T&gt; 구현 클래스를 감지하여 T(이벤트 타입)에 대한
/// IDomainEventCtxEnricher 구현체를 생성하는 소스 생성기.
/// [CtxIgnore]를 이벤트 record에 적용하면 생성을 제외합니다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DomainEventCtxEnricherGenerator()
    : IncrementalGeneratorBase<DomainEventCtxEnricherInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string DomainEventHandlerInterfaceFullName =
        "Functorium.Applications.Events.IDomainEventHandler<TEvent>";
    private const string IgnoreAttributeFullName = "Functorium.Applications.Observabilities.CtxIgnoreAttribute";
    private const string RootAttributeFullName = "Functorium.Applications.Observabilities.CtxRootAttribute";
    private const string TargetAttributeFullName = "Functorium.Applications.Observabilities.CtxTargetAttribute";
    private const string ValueObjectInterfaceFullName = "Functorium.Domains.ValueObjects.IValueObject";
    private const string EntityIdInterfacePrefix = "Functorium.Domains.Entities.IEntityId<";

    private static readonly HashSet<string> DomainEventBasePropertyNames =
        ["OccurredAt", "EventId", "CorrelationId", "CausationId"];

    private static readonly DiagnosticDescriptor InaccessibleEventDiagnostic = new(
        id: "FUNCTORIUM004",
        title: "Event type is not accessible for DomainEventCtxEnricher generation",
        messageFormat: "'{0}' implements IDomainEvent but DomainEventCtxEnricher cannot be generated because '{1}' is {2}. Apply [CtxIgnore] to the event record to suppress this warning.",
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

    private static IncrementalValuesProvider<DomainEventCtxEnricherInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsClassDeclaration,
                transform: MapToDomainEventCtxEnricherInfo)
            .Where(x => x.EventTypeName.Length > 0);
    }

    private static bool IsClassDeclaration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }

    private static DomainEventCtxEnricherInfo MapToDomainEventCtxEnricherInfo(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken)
            is not INamedTypeSymbol handlerSymbol)
            return DomainEventCtxEnricherInfo.None;

        INamedTypeSymbol? eventSymbol = FindDomainEventType(handlerSymbol);
        if (eventSymbol is null)
            return DomainEventCtxEnricherInfo.None;

        if (eventSymbol.IsAbstract)
            return DomainEventCtxEnricherInfo.None;

        if (HasClassLevelIgnoreAttribute(eventSymbol))
            return DomainEventCtxEnricherInfo.None;

        var inaccessibleType = FindInaccessibleType(eventSymbol);
        if (inaccessibleType != null)
        {
            return DomainEventCtxEnricherInfo.Inaccessible(
                eventSymbol.ToDisplayString(),
                inaccessibleType.Name,
                inaccessibleType.DeclaredAccessibility.ToString().ToLowerInvariant(),
                context.Node.GetLocation());
        }

        cancellationToken.ThrowIfCancellationRequested();

        string @namespace = handlerSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : handlerSymbol.ContainingNamespace.ToString();

        var containingTypeNames = GetContainingTypeNames(eventSymbol);

        string ctxPrefix;
        if (containingTypeNames.Length > 0)
        {
            string containingPart = string.Join(".",
                containingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));
            ctxPrefix = $"{containingPart}.{SnakeCaseConverter.ToSnakeCase(eventSymbol.Name)}";
        }
        else
        {
            ctxPrefix = SnakeCaseConverter.ToSnakeCase(eventSymbol.Name);
        }

        string eventTypeName = eventSymbol.Name;
        string eventTypeFullName = GetGlobalFullName(eventSymbol);

        var eventProperties = CollectProperties(eventSymbol, "domainEvent", ctxPrefix);

        string enricherClassName = string.Concat(containingTypeNames.Select(n => n))
            + eventTypeName + "CtxEnricher";

        Location? location = context.Node.GetLocation();

        return new DomainEventCtxEnricherInfo(
            @namespace,
            containingTypeNames,
            eventTypeName,
            eventTypeFullName,
            eventProperties,
            enricherClassName,
            location);
    }

    private static INamedTypeSymbol? FindDomainEventType(INamedTypeSymbol handlerSymbol)
    {
        foreach (var iface in handlerSymbol.AllInterfaces)
        {
            if (iface.IsGenericType && iface.TypeArguments.Length == 1)
            {
                string ifaceName = iface.OriginalDefinition.ToDisplayString();
                if (ifaceName == DomainEventHandlerInterfaceFullName)
                {
                    return iface.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }
        return null;
    }

    private static bool ImplementsValueObjectOrEntityId(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        foreach (var iface in namedType.AllInterfaces)
        {
            string ifaceFullName = iface.ToDisplayString();
            if (ifaceFullName == ValueObjectInterfaceFullName)
                return true;
            if (ifaceFullName.StartsWith(EntityIdInterfacePrefix))
                return true;
        }

        return false;
    }

    private static string[] GetContainingTypeNames(INamedTypeSymbol symbol)
    {
        var names = new List<string>();
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

    private static CtxPropertyInfo[] CollectProperties(
        INamedTypeSymbol typeSymbol,
        string variablePrefix,
        string ctxSegmentPrefix)
    {
        var properties = new List<CtxPropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            if (property.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (property.IsStatic || property.IsIndexer)
                continue;

            if (DomainEventBasePropertyNames.Contains(property.Name))
                continue;

            if (HasIgnoreAttribute(property))
                continue;

            string typeFullName = property.Type.ToDisplayString();
            bool isCollection = CollectionTypeHelper.IsCollectionType(typeFullName);

            bool needsToString = !isCollection && ImplementsValueObjectOrEntityId(property.Type);

            if (!isCollection && !needsToString && IsComplexType(property.Type))
                continue;

            string snakeName = SnakeCaseConverter.ToSnakeCase(property.Name);
            string ctxFieldName;
            string? countExpression = null;
            string openSearchTypeGroup = needsToString
                ? "keyword"
                : GetOpenSearchTypeGroup(property.Type, isCollection);

            bool hasDirectRootAttribute = HasRootAttribute(property);
            var (declaringInterface, isRootFromInterface) = FindPropertyInterface(typeSymbol, property);
            bool isRoot = hasDirectRootAttribute || isRootFromInterface;

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
                needsToString,
                targetPillars));
        }

        return properties.ToArray();
    }

    private static int ResolveTargetPillars(IPropertySymbol property, INamedTypeSymbol? declaringInterface)
    {
        int? directTarget = GetTargetPillarsFromAttributes(property.GetAttributes());

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

        if (declaringInterface != null)
        {
            int? interfaceTarget = GetTargetPillarsFromAttributes(declaringInterface.GetAttributes());
            if (interfaceTarget != null)
                return interfaceTarget.Value;
        }

        return CtxPropertyInfo.PillarDefault;
    }

    private static int? GetTargetPillarsFromAttributes(ImmutableArray<AttributeData> attributes)
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
        ImmutableArray<DomainEventCtxEnricherInfo> enricherInfos)
    {
        var uniqueInfos = enricherInfos
            .GroupBy(x => x.EventTypeFullName)
            .Select(g => g.First())
            .ToImmutableArray();

        DetectCtxFieldTypeConflicts(context, uniqueInfos);

        foreach (var info in uniqueInfos)
        {
            if (info.SkipReason.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InaccessibleEventDiagnostic,
                    info.Location,
                    info.EventTypeFullName,
                    info.EventTypeName == "SKIP" ? info.EventTypeFullName : info.EventTypeName,
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
        ImmutableArray<DomainEventCtxEnricherInfo> enricherInfos)
    {
        var ctxFieldMap = new Dictionary<string,
            (string OpenSearchTypeGroup, string TypeFullName, string EnricherClassName, Location? Location)>();

        foreach (var info in enricherInfos)
        {
            foreach (var prop in info.EventProperties)
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

    private static string GenerateEnricherSource(DomainEventCtxEnricherInfo info, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};")
                .AppendLine();
        }

        string eventFullType = info.EventTypeFullName;
        int eventCount = info.EventProperties.Length;

        string ctxPrefix;
        if (info.ContainingTypeNames.Length > 0)
        {
            string containingPart = string.Join(".",
                info.ContainingTypeNames.Select(n => SnakeCaseConverter.ToSnakeCase(n)));
            ctxPrefix = $"{containingPart}.{SnakeCaseConverter.ToSnakeCase(info.EventTypeName)}";
        }
        else
        {
            ctxPrefix = SnakeCaseConverter.ToSnakeCase(info.EventTypeName);
        }

        sb.AppendLine($"public partial class {info.EnricherClassName}");
        sb.AppendLine($"    : global::Functorium.Applications.Observabilities.IDomainEventCtxEnricher<{eventFullType}>");
        sb.AppendLine("{");

        // Enrich
        sb.AppendLine($"    public global::System.IDisposable? Enrich({eventFullType} domainEvent)");
        sb.AppendLine("    {");
        if (eventCount > 0)
        {
            sb.AppendLine($"        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>({eventCount});");
            foreach (var prop in info.EventProperties)
            {
                string pillarArg = GetPillarArgument(prop);
                if (prop.IsCollection)
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", {prop.CountExpression}{pillarArg}));");
                }
                else if (prop.NeedsToString)
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", domainEvent.{prop.PropertyName}.ToString(){pillarArg}));");
                }
                else
                {
                    sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(\"{prop.CtxFieldName}\", domainEvent.{prop.PropertyName}{pillarArg}));");
                }
            }
            sb.AppendLine("        OnEnrich(domainEvent, disposables);");
            sb.AppendLine("        return new GeneratedCompositeDisposable(disposables);");
        }
        else
        {
            sb.AppendLine("        var disposables = new global::System.Collections.Generic.List<global::System.IDisposable>();");
            sb.AppendLine("        OnEnrich(domainEvent, disposables);");
            sb.AppendLine("        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // partial void extension point
        sb.AppendLine($"    partial void OnEnrich(");
        sb.AppendLine($"        {eventFullType} domainEvent,");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables);");
        sb.AppendLine();

        // Helper: PushEventCtx
        sb.AppendLine($"    private static void PushEventCtx(");
        sb.AppendLine($"        global::System.Collections.Generic.List<global::System.IDisposable> disposables,");
        sb.AppendLine($"        string fieldName,");
        sb.AppendLine($"        object? value,");
        sb.AppendLine($"        global::Functorium.Applications.Observabilities.CtxPillar pillars = global::Functorium.Applications.Observabilities.CtxPillar.Default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        disposables.Add(global::Functorium.Applications.Observabilities.CtxEnricherContext.Push(");
        sb.AppendLine($"            \"ctx.{ctxPrefix}.\" + fieldName, value, pillars));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Helper: PushRootCtx (root 속성이 있을 때만 생성)
        bool hasRootProperties = info.EventProperties.Any(p => p.IsRoot);

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

    private static string GetPillarArgument(CtxPropertyInfo prop)
    {
        if (prop.IsDefault)
            return "";

        return $", (global::Functorium.Applications.Observabilities.CtxPillar){prop.TargetPillars}";
    }
}
