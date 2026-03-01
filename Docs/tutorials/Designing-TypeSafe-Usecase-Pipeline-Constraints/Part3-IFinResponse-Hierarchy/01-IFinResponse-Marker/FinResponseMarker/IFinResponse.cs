namespace FinResponseMarker;

/// <summary>
/// 비제네릭 마커 인터페이스.
/// Pipeline에서 모든 응답의 성공/실패를 확인할 수 있습니다.
/// </summary>
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
