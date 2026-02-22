namespace Functorium.Domains.Services;

/// <summary>
/// 도메인 서비스의 마커 인터페이스.
/// 여러 Aggregate에 걸친 순수 도메인 로직을 표현합니다.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>순수 함수로 구현 (외부 I/O 없음)</item>
/// <item>여러 Aggregate를 참조하는 비즈니스 로직 배치</item>
/// <item>IPort 의존성 없음 (Port/Adapter는 Usecase에서 사용)</item>
/// <item>Domain Layer에 배치</item>
/// </list>
/// </remarks>
public interface IDomainService { }
