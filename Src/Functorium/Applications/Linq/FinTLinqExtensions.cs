namespace Functorium.Applications.Linq;

/// <summary>
/// FinT 모나드 트랜스포머를 위한 LINQ 확장 메서드
///
/// 목적: 다양한 타입(Fin, IO, Validation)을 FinT로 통합하여 LINQ 쿼리 표현식 지원
///
/// 핵심 패턴:
///   - Fin&lt;A&gt; → FinT&lt;M, B&gt;: 순수 결과 값을 모나드 트랜스포머로 승격
///   - IO&lt;A&gt; → FinT&lt;IO, B&gt;: IO 효과를 성공으로 가정하고 FinT로 변환
///   - Validation&lt;Error, A&gt; → FinT&lt;M, B&gt;: 검증 결과를 FinT로 변환
///   - Filter: 조건부 필터링 지원
///   - TraverseSerial: 순차 처리 보장
///
/// 파일 구조 (partial class by source type):
///   - FinTLinqExtensions.cs: 클래스 정의 및 문서
///   - FinTLinqExtensions.Fin.cs: Fin&lt;A&gt; 확장 (SelectMany, Filter)
///   - FinTLinqExtensions.IO.cs: IO&lt;A&gt; 확장 (SelectMany)
///   - FinTLinqExtensions.Validation.cs: Validation&lt;Error, A&gt; 확장 (SelectMany)
///   - FinTLinqExtensions.FinT.cs: FinT&lt;M, A&gt; 확장 (Filter, TraverseSerial)
/// </summary>
public static partial class FinTLinqExtensions
{
}
