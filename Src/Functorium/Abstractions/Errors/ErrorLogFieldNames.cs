namespace Functorium.Abstractions.Errors;

/// <summary>
/// Serilog destructurer가 구조화 로그 출력에 사용하는 필드 이름 상수.
/// </summary>
/// <remarks>
/// <para>
/// 이 상수들은 로그 JSON/텍스트 출력에서 키로 쓰이며, 대시보드·알림 쿼리가
/// 이 이름을 참조합니다. 상수 값을 변경하면 하위 관측성 소비자(Seq·Grafana·
/// Elastic)의 쿼리도 함께 마이그레이션해야 합니다.
/// </para>
/// <para>
/// <c>Kind</c>는 에러 레코드의 분류(<c>ExpectedError</c>·<c>ExceptionalError</c>·
/// <c>ManyErrors</c>)를 담습니다. 이전 이름(<c>ErrorType</c>)은 <c>ErrorKind</c>
/// 추상 record와 혼동되어 <c>Kind</c>로 단축·명확화되었습니다.
/// </para>
/// </remarks>
internal static class ErrorLogFieldNames
{
    public const string ErrorCode = nameof(ExpectedError.ErrorCode);
    public const string Message = nameof(ExpectedError.Message);
    public const string ErrorCurrentValue = nameof(ExpectedError.ErrorCurrentValue);
    public const string NumericCode = nameof(ExpectedError.NumericCode);

    public const string Kind = nameof(Kind);
    public const string Count = nameof(Count);
    public const string Errors = nameof(Errors);
    public const string InnerError = nameof(InnerError);
    public const string ExceptionDetails = nameof(ExceptionDetails);

    public const string UnknownErrorCode = "UNKNOWN.ERROR-CODE";
    public const string UnknownErrorMessage = "UNKNOWN.ERROR-MESSAGE";
}
