namespace Functorium.Applications.Linq;

// =============================================================================
// FinTLinqExtensions - FinT 모나드 트랜스포머를 위한 LINQ 확장 메서드
// =============================================================================
//
// 목적: 다양한 타입(Fin, IO, Validation)을 FinT로 통합하여 LINQ 쿼리 표현식 지원
//
// -----------------------------------------------------------------------------
// SelectMany 확장 메서드 (LINQ 체이닝)
// -----------------------------------------------------------------------------
//
// | File             | Source Type           | Selector Return Type   | Result Type    | Description                       |
// |------------------|-----------------------|------------------------|----------------|-----------------------------------|
// | .Fin.cs          | Fin<A>                | FinT<M, B>             | FinT<M, C>     | Fin → FinT 승격 후 체이닝         |
// | .Fin.cs          | FinT<M, A>            | Fin<B>                 | FinT<M, C>     | FinT 체인 중간에 Fin 사용         |
// | .IO.cs           | IO<A>                 | B (Map)                | FinT<IO, B>    | IO → FinT 단순 변환               |
// | .IO.cs           | IO<A>                 | FinT<IO, B>            | FinT<IO, C>    | IO → FinT 승격 후 체이닝          |
// | .IO.cs           | FinT<IO, A>           | IO<B>                  | FinT<IO, C>    | FinT 체인 중간에 IO 사용          |
// | .Validation.cs   | Validation<Error, A>  | FinT<M, B>             | FinT<M, C>     | Validation → FinT (제네릭)        |
// | .Validation.cs   | Validation<Error, A>  | B (Map)                | FinT<M, B>     | Validation → FinT 단순 변환       |
// | .Validation.cs   | FinT<M, A>            | Validation<Error, B>   | FinT<M, C>     | FinT 체인 중간에 Validation 사용  |
//
// -----------------------------------------------------------------------------
// Filter 확장 메서드 (조건부 필터링)
// -----------------------------------------------------------------------------
//
// | File      | Target Type  | Return Type  | Description                  |
// |-----------|--------------|--------------|------------------------------|
// | .Fin.cs   | Fin<A>       | Fin<A>       | 조건 불만족 시 Fail 반환     |
// | .FinT.cs  | FinT<M, A>   | FinT<M, A>   | 조건 불만족 시 Fail 반환     |
//
// -----------------------------------------------------------------------------
// TraverseSerial 확장 메서드 (순차 처리)
// -----------------------------------------------------------------------------
//
// | File      | Target Type | Return Type       | Description                             |
// |-----------|-------------|-------------------|-----------------------------------------|
// | .FinT.cs  | Seq<A>      | FinT<M, Seq<B>>   | 순차 처리 (제네릭 모나드)               |
// | .FinT.cs  | Seq<A>      | FinT<IO, Seq<B>>  | 순차 처리 + OpenTelemetry Activity 추적 |
//
// -----------------------------------------------------------------------------
// IO.cs와 Validation.cs의 메서드 수가 다른 이유
// -----------------------------------------------------------------------------
//
// IO.cs (3개) vs Validation.cs (3개)
//
// | Direction              | IO.cs     | Validation.cs |
// |------------------------|-----------|---------------|
// | Source -> FinT (Map)   | IO only   | Generic M     |
// | Source -> FinT<M, B>   | IO only   | Generic M     |
// | FinT<M, A> -> Source   | IO only   | Generic M     |
//
// 이유:
//   - IO는 그 자체가 특정 모나드이므로 제네릭 M 버전이 불필요합니다.
//     IO<A>는 이미 IO 모나드에 종속되어 있습니다.
//     FinT<M, A> → IO<B>는 M이 IO일 때만 의미가 있습니다.
//
//   - Validation은 모나드가 아닌 데이터 타입이므로 어떤 모나드 M과도 조합 가능합니다.
//     IO 역시 Monad<IO>를 만족하므로, 제네릭 M 버전이 FinT<IO, _> 컨텍스트까지
//     타입 추론으로 커버합니다. 별도의 IO 특화 오버로드는 중복이므로 제거되었습니다.
//
// -----------------------------------------------------------------------------
// 파일 구조 (partial class by source type)
// -----------------------------------------------------------------------------
//
//   - FinTLinqExtensions.cs:            클래스 정의 및 문서
//   - FinTLinqExtensions.Fin.cs:        Fin<A> 확장 (SelectMany, Filter)
//   - FinTLinqExtensions.IO.cs:         IO<A> 확장 (SelectMany)
//   - FinTLinqExtensions.Validation.cs: Validation<Error, A> 확장 (SelectMany)
//   - FinTLinqExtensions.FinT.cs:       FinT<M, A> 확장 (Filter, TraverseSerial)
//
// =============================================================================

public static partial class FinTLinqExtensions
{
}
