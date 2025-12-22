using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Functorium.Testing.SourceGenerators;

/// <summary>
/// 소스 생성기 테스트를 위한 유틸리티 클래스.
/// IIncrementalGenerator를 테스트 환경에서 실행하고 결과를 반환합니다.
/// </summary>
public static class SourceGeneratorTestRunner
{
    // 테스트에서 항상 참조해야 하는 필수 어셈블리 타입 목록
    // LanguageExt 타입을 명시적으로 포함하여 타입 해석의 일관성 보장
    private static readonly Type[] RequiredTypes =
    [
        typeof(object),                                        // System.Runtime
        typeof(LanguageExt.IO),                                // LanguageExt.Core
        typeof(LanguageExt.FinT<,>),                           // LanguageExt.Core (generic)
        typeof(Microsoft.Extensions.Logging.ILogger),          // Microsoft.Extensions.Logging
    ];

    /// <summary>
    /// 소스 생성기를 실행하고 생성된 코드를 반환합니다.
    /// </summary>
    /// <typeparam name="TGenerator">실행할 소스 생성기 타입</typeparam>
    /// <param name="generator">소스 생성기 인스턴스</param>
    /// <param name="sourceCode">입력 소스 코드</param>
    /// <returns>생성된 코드 문자열 (생성되지 않은 경우 null)</returns>
    public static string? Generate<TGenerator>(this TGenerator generator, string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 소스 코드에서 Syntax Tree 생성
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 필수 어셈블리를 먼저 추가 (순서 보장)
        var requiredReferences = RequiredTypes
            .Select(t => t.Assembly)
            .Distinct()
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        // 현재 로드된 어셈블리 중 동적이 아닌 것들을 참조로 변환
        var otherReferences = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Where(assembly => !RequiredTypes.Any(t => t.Assembly == assembly))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        // 필수 참조를 먼저, 그 다음 나머지 참조
        var references = requiredReferences.Concat(otherReferences);

        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTests",     // 생성할 어셈블리 이름
            [syntaxTree],               // 소스
            references,                 // 참조
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 컴파일: IIncrementalGenerator 소스 생성기 호출
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,          // 소스 생성기 결과: 소스
                out var diagnostics);               // 소스 생성기 진단: 경고, 에러

        // 소스 생성기 진단(컴파일러 에러)
        diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty();

        // 소스 생성기 결과(컴파일러 결과)
        return outputCompilation
            .SyntaxTrees
            .Skip(1)                // [0] 원본 소스 SyntaxTree 제외
            .LastOrDefault()?
            .ToString();
    }
}
