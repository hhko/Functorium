namespace Functorium.Domains.Services;

/// <summary>
/// 도메인 서비스의 마커 인터페이스.
/// 여러 Aggregate에 걸친 도메인 로직을 표현합니다.
/// </summary>
/// <remarks>
/// Evans Blue Book Ch.9: Domain Service는 Stateless(호출 간 가변 상태 없음)를 요구합니다.
/// <list type="bullet">
/// <item>기본 패턴: 순수 함수로 구현 (외부 I/O 없음)</item>
/// <item>Evans Ch.9 패턴: Repository 인터페이스 의존 허용 (대규모 교차 데이터 시)</item>
/// <item>여러 Aggregate를 참조하는 비즈니스 로직 배치</item>
/// <item>IObservablePort 의존성 없음 (Port/Adapter는 Usecase에서 사용)</item>
/// <item>Domain Layer에 배치</item>
/// </list>
/// </remarks>
public interface IDomainService { }
