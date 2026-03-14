using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;

namespace Functorium.SourceGenerators.Generators.UnionTypeGenerator;

/// <summary>
/// [UnionType] 속성이 붙은 abstract partial record에 대해
/// Match/Switch 메서드를 자동 생성하는 소스 생성기.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class UnionTypeGenerator()
    : IncrementalGeneratorBase<UnionTypeInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string AttributeName = "UnionType";
    private const string AttributeNamespace = "Functorium.Domains.ValueObjects";
    private const string FullyQualifiedAttributeName = $"{AttributeNamespace}.{AttributeName}Attribute";

    private static IncrementalValuesProvider<UnionTypeInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: IsRecordDeclaration,
                transform: MapToUnionTypeInfo)
            .Where(x => x != UnionTypeInfo.None);
    }

    private static bool IsRecordDeclaration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is RecordDeclarationSyntax;
    }

    private static UnionTypeInfo MapToUnionTypeInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return UnionTypeInfo.None;

        cancellationToken.ThrowIfCancellationRequested();

        // abstract record만 대상
        if (!typeSymbol.IsAbstract)
            return UnionTypeInfo.None;

        string typeName = typeSymbol.Name;
        string @namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToString();

        // 내부 sealed record 케이스 수집 (직접 상속하는 sealed 타입만)
        var caseNames = typeSymbol
            .GetTypeMembers()
            .Where(m => m.IsSealed && m.IsRecord && InheritsFrom(m, typeSymbol))
            .Select(m => m.Name)
            .ToArray();

        if (caseNames.Length == 0)
            return UnionTypeInfo.None;

        Location? location = context.TargetNode.GetLocation();

        return new UnionTypeInfo(@namespace, typeName, caseNames, location);
    }

    private static bool InheritsFrom(INamedTypeSymbol derived, INamedTypeSymbol baseType)
    {
        var current = derived.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<UnionTypeInfo> unionTypeInfos)
    {
        foreach (var info in unionTypeInfos)
        {
            StringBuilder sb = new();
            string source = GenerateUnionTypeSource(info, sb);

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
                $"{namespaceSuffix}{info.TypeName}.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateUnionTypeSource(UnionTypeInfo info, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};")
                .AppendLine();
        }

        sb.AppendLine($"public abstract partial record {info.TypeName}")
            .AppendLine("{");

        // Match<TResult> 메서드 생성
        GenerateMatchMethod(sb, info);

        sb.AppendLine();

        // Switch 메서드 생성
        GenerateSwitchMethod(sb, info);

        sb.AppendLine();

        // Is{CaseName} 속성 생성
        GenerateIsProperties(sb, info);

        sb.AppendLine();

        // As{CaseName}() 메서드 생성
        GenerateAsMethods(sb, info);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateMatchMethod(StringBuilder sb, UnionTypeInfo info)
    {
        sb.AppendLine("    public TResult Match<TResult>(");

        for (int i = 0; i < info.CaseNames.Length; i++)
        {
            var caseName = info.CaseNames[i];
            var paramName = ToCamelCase(caseName);
            var separator = i < info.CaseNames.Length - 1 ? "," : ")";
            sb.AppendLine($"        global::System.Func<{caseName}, TResult> {paramName}{separator}");
        }

        sb.AppendLine("    {");
        sb.AppendLine("        return this switch");
        sb.AppendLine("        {");

        foreach (var caseName in info.CaseNames)
        {
            var paramName = ToCamelCase(caseName);
            sb.AppendLine($"            {caseName} __case => {paramName}(__case),");
        }

        sb.AppendLine($"            _ => throw new global::Functorium.Domains.ValueObjects.UnreachableCaseException(this)");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
    }

    private static void GenerateSwitchMethod(StringBuilder sb, UnionTypeInfo info)
    {
        sb.AppendLine("    public void Switch(");

        for (int i = 0; i < info.CaseNames.Length; i++)
        {
            var caseName = info.CaseNames[i];
            var paramName = ToCamelCase(caseName);
            var separator = i < info.CaseNames.Length - 1 ? "," : ")";
            sb.AppendLine($"        global::System.Action<{caseName}> {paramName}{separator}");
        }

        sb.AppendLine("    {");
        sb.AppendLine("        switch (this)");
        sb.AppendLine("        {");

        foreach (var caseName in info.CaseNames)
        {
            var paramName = ToCamelCase(caseName);
            sb.AppendLine($"            case {caseName} __case: {paramName}(__case); break;");
        }

        sb.AppendLine($"            default: throw new global::Functorium.Domains.ValueObjects.UnreachableCaseException(this);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateIsProperties(StringBuilder sb, UnionTypeInfo info)
    {
        foreach (var caseName in info.CaseNames)
        {
            sb.AppendLine($"    public bool Is{caseName} => this is {caseName};");
        }
    }

    private static void GenerateAsMethods(StringBuilder sb, UnionTypeInfo info)
    {
        foreach (var caseName in info.CaseNames)
        {
            sb.AppendLine($"    public {caseName}? As{caseName}() => this as {caseName};");
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
