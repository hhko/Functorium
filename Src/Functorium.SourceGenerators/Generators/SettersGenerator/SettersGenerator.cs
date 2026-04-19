using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;
using static Functorium.SourceGenerators.Abstractions.Selectors;

namespace Functorium.SourceGenerators.Generators.SettersGenerator;

/// <summary>
/// [GenerateSetters] 속성이 붙은 EF Core 모델 클래스에 대해
/// ExecuteUpdate용 ToSetters 정적 메서드를 자동 생성하는 소스 생성기.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SettersGenerator()
    : IncrementalGeneratorBase<SettersInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string AttributeName = "GenerateSetters";
    private const string AttributeNamespace = "Functorium.Adapters.SourceGenerators";
    private const string FullyQualifiedAttributeName = $"{AttributeNamespace}.{AttributeName}Attribute";

    private const string IgnoreAttributeName = "SetterIgnore";
    private const string FullyQualifiedIgnoreAttributeName = $"{AttributeNamespace}.{IgnoreAttributeName}Attribute";

    private static IncrementalValuesProvider<SettersInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: IsClass,
                transform: MapToSettersInfo)
            .Where(x => x != SettersInfo.None);
    }

    private static SettersInfo MapToSettersInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return SettersInfo.None;

        cancellationToken.ThrowIfCancellationRequested();

        string className = classSymbol.Name;
        string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToString();

        var propertyNames = new List<string>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            // public settable 프로퍼티만 대상
            if (property.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (property.SetMethod is null)
                continue;
            if (property.IsReadOnly || property.IsStatic)
                continue;

            // Id 프로퍼티 자동 제외 (PK)
            if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                continue;

            // 네비게이션 프로퍼티 제외 (ICollection<T>, IEnumerable<T> 등)
            if (IsNavigationProperty(property.Type))
                continue;

            // [SetterIgnore] 속성이 있으면 제외
            bool hasIgnore = property.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == FullyQualifiedIgnoreAttributeName);
            if (hasIgnore)
                continue;

            propertyNames.Add(property.Name);
        }

        if (propertyNames.Count == 0)
            return SettersInfo.None;

        Location? location = context.TargetNode.GetLocation();

        return new SettersInfo(
            @namespace,
            className,
            propertyNames.ToArray(),
            location);
    }

    /// <summary>
    /// 네비게이션 프로퍼티 여부 판별. ICollection&lt;T&gt;, IList&lt;T&gt;, IEnumerable&lt;T&gt; (string 제외) 등.
    /// </summary>
    private static bool IsNavigationProperty(ITypeSymbol type)
    {
        // string은 IEnumerable<char>이지만 네비게이션이 아님
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // 배열 타입 제외
        if (type is IArrayTypeSymbol)
            return true;

        if (type is INamedTypeSymbol namedType)
        {
            // ICollection<T>, IList<T>, List<T>, HashSet<T> 등 컬렉션 인터페이스 검사
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    (iface.Name == "ICollection" || iface.Name == "IList" || iface.Name == "ISet"))
                    return true;
            }

            // 타입 자체가 제네릭 컬렉션인 경우 (List<T>, Collection<T>)
            if (namedType.IsGenericType &&
                (namedType.Name == "List" || namedType.Name == "Collection" ||
                 namedType.Name == "HashSet" || namedType.Name == "ICollection" ||
                 namedType.Name == "IList"))
                return true;
        }

        return false;
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<SettersInfo> infos)
    {
        foreach (var info in infos)
        {
            StringBuilder sb = new();
            string source = GenerateSettersSource(info, sb);

            string namespaceSuffix = string.Empty;
            if (!string.IsNullOrEmpty(info.Namespace))
            {
                var lastDotIndex = info.Namespace.LastIndexOf('.');
                if (lastDotIndex >= 0)
                    namespaceSuffix = info.Namespace.Substring(lastDotIndex + 1) + ".";
            }

            context.AddSource(
                $"{namespaceSuffix}{info.ClassName}.Setters.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateSettersSource(SettersInfo info, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine()
            .AppendLine("using Microsoft.EntityFrameworkCore.Query;")
            .AppendLine()
            .AppendLine($"namespace {info.Namespace};")
            .AppendLine()
            .AppendLine($"partial class {info.ClassName}")
            .AppendLine("{")
            .AppendLine("    /// <summary>")
            .AppendLine("    /// ExecuteUpdateAsync용 SetProperty를 UpdateSettersBuilder에 적용합니다.")
            .AppendLine("    /// Source Generator가 자동 생성한 메서드입니다.")
            .AppendLine("    /// </summary>")
            .AppendLine($"    public static void ApplySetters(UpdateSettersBuilder<{info.ClassName}> setters, {info.ClassName} model)")
            .AppendLine("    {");

        for (int i = 0; i < info.PropertyNames.Length; i++)
        {
            string propName = info.PropertyNames[i];
            sb.AppendLine($"        setters.SetProperty(m => m.{propName}, model.{propName});");
        }

        sb.AppendLine("    }")
            .AppendLine("}")
            .AppendLine();

        return sb.ToString();
    }
}
