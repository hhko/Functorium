using Microsoft.CodeAnalysis;

namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 타입 표시를 위한 SymbolDisplayFormat 정의
/// </summary>
internal static class SymbolDisplayFormats
{
    /// <summary>
    /// 항상 global:: 접두사를 포함하는 완전한 타입 표시 형식
    ///
    /// <para>
    /// <strong>문제:</strong> <c>SymbolDisplayFormat.FullyQualifiedFormat</c>는
    /// using 문의 존재 여부에 따라 <c>global::</c> 접두사를 생략할 수 있어
    /// 비결정적 동작을 유발합니다.
    /// </para>
    ///
    /// <para>
    /// <strong>예시:</strong>
    /// <code>
    /// // using LanguageExt; 가 있는 경우
    /// FinT&lt;IO, string&gt;
    ///
    /// // using LanguageExt; 가 없는 경우
    /// global::LanguageExt.FinT&lt;global::LanguageExt.IO, string&gt;
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>해결:</strong> 이 형식은 항상 <c>global::</c>을 포함하여
    /// 소스 생성 결과의 일관성을 보장합니다.
    /// </para>
    /// </summary>
    public static readonly SymbolDisplayFormat GlobalQualifiedFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.None,
        parameterOptions: SymbolDisplayParameterOptions.None,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.None,
        kindOptions: SymbolDisplayKindOptions.None,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
}
