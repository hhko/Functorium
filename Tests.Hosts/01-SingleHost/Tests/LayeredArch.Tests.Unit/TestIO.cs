namespace LayeredArch.Tests.Unit;

/// <summary>
/// FinT&lt;IO, T&gt; 반환값 생성 헬퍼
/// Application 레이어 Usecase 테스트에서 Mock 반환값 생성에 사용
/// </summary>
internal static class TestIO
{
    public static FinT<IO, T> Succ<T>(T value) => FinT.lift(IO.pure(Fin.Succ(value)));
    public static FinT<IO, T> Fail<T>(Error error) => FinT.lift(IO.pure(Fin.Fail<T>(error)));
}
