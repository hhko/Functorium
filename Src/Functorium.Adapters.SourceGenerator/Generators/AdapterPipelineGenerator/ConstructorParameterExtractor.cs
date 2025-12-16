using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 클래스의 생성자 파라미터를 추출하는 유틸리티 클래스
/// </summary>
internal static class ConstructorParameterExtractor
{
    /// <summary>
    /// 타겟 클래스 또는 부모 클래스에서 생성자 파라미터를 추출합니다.
    ///
    /// 우선순위:
    /// 1. 타겟 클래스 자체의 생성자 (파라미터가 있는 경우)
    /// 2. 부모 클래스의 생성자 (타겟 클래스에 파라미터 생성자가 없는 경우)
    /// </summary>
    /// <param name="classSymbol">타겟 클래스 심볼</param>
    /// <returns>생성자 파라미터 목록</returns>
    public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
    {
        // 1. 타겟 클래스의 생성자 확인 (우선순위)
        var targetConstructorParams = TryExtractFromTargetClass(classSymbol);
        if (targetConstructorParams.Count > 0)
        {
            return targetConstructorParams;
        }

        // 2. 부모 클래스의 생성자 확인
        return TryExtractFromBaseClass(classSymbol);
    }

    /// <summary>
    /// 타겟 클래스의 생성자에서 파라미터를 추출합니다.
    /// </summary>
    private static List<ParameterInfo> TryExtractFromTargetClass(INamedTypeSymbol classSymbol)
    {
        var constructors = GetPublicConstructors(classSymbol);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    /// <summary>
    /// 부모 클래스의 생성자에서 파라미터를 추출합니다.
    /// </summary>
    private static List<ParameterInfo> TryExtractFromBaseClass(INamedTypeSymbol classSymbol)
    {
        // System.Object는 제외
        if (classSymbol.BaseType == null || classSymbol.BaseType.SpecialType == SpecialType.System_Object)
        {
            return new List<ParameterInfo>();
        }

        var constructors = GetPublicConstructors(classSymbol.BaseType);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    /// <summary>
    /// Public 생성자 목록을 가져옵니다.
    /// </summary>
    private static List<IMethodSymbol> GetPublicConstructors(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor &&
                       m.DeclaredAccessibility == Accessibility.Public)
            .ToList();
    }

    /// <summary>
    /// 가장 적절한 생성자를 선택합니다.
    ///
    /// 우선순위:
    /// 1. Primary constructor (C# 12+)
    /// 2. 파라미터가 가장 많은 생성자
    /// </summary>
    /// <remarks>
    /// 향후 개선:
    /// - 특정 애트리뷰트로 표시된 생성자 우선 선택
    /// </remarks>
    private static IMethodSymbol? SelectBestConstructor(List<IMethodSymbol> constructors)
    {
        // 1순위: Primary constructor
        var primaryConstructor = constructors.FirstOrDefault(IsPrimaryConstructor);
        if (primaryConstructor != null)
        {
            return primaryConstructor;
        }

        // 2순위: 파라미터가 가장 많은 생성자
        return constructors
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();
    }

    /// <summary>
    /// Primary constructor인지 확인합니다.
    ///
    /// Primary constructor는 C# 12에서 도입된 기능으로,
    /// 클래스 선언에 직접 파라미터를 정의합니다.
    /// 예: public class MyClass(string name, int age)
    /// </summary>
    /// <param name="constructor">확인할 생성자</param>
    /// <returns>Primary constructor이면 true, 아니면 false</returns>
    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        // DeclaringSyntaxReferences가 없으면 Primary constructor가 아님
        var syntaxReferences = constructor.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0)
        {
            return false;
        }

        var syntax = syntaxReferences[0].GetSyntax();

        // Primary constructor는 TypeDeclarationSyntax에 ParameterList가 있음
        // 일반 생성자는 ConstructorDeclarationSyntax로 표현됨
        return syntax is TypeDeclarationSyntax typeDecl && typeDecl.ParameterList != null;
    }

    /// <summary>
    /// Roslyn의 IParameterSymbol 목록을 ParameterInfo 목록으로 변환합니다.
    /// </summary>
    private static List<ParameterInfo> ConvertToParameterInfoList(ImmutableArray<IParameterSymbol> parameters)
    {
        return parameters
            .Select(p => new ParameterInfo(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                p.RefKind))
            .ToList();
    }
}
