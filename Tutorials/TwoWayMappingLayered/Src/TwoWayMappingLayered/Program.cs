using TwoWayMappingLayered.Adapters.Infrastructure.Abstractions.Registrations;
using TwoWayMappingLayered.Adapters.Persistence.Abstractions.Registrations;
using TwoWayMappingLayered.Adapters.Presentation.Abstractions.Registrations;

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// Two-Way Mapping Tutorial
//
// HappyCoders 문서 원문:
// "In my experience, this variant is the most suitable."
// (제 경험상, 이 방식이 가장 적합합니다.) - Sven Woltmann
//
// 핵심 특징:
// - Domain과 Adapter에 별도 모델 존재
// - ProductMapper로 양방향 변환
// - Domain이 기술 의존성으로부터 완전히 자유로움
// - 비즈니스 메서드(FormattedPrice 등) 즉시 사용 가능
// =================================================================

// =================================================================
// 레이어별 서비스 등록
// =================================================================
builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence()
    .RegisterAdapterInfrastructure(builder.Configuration);

// =================================================================
// App 빌드 및 미들웨어 설정
// =================================================================
var app = builder.Build();

app.UseAdapterInfrastructure();
app.UseAdapterPersistence();
app.UseAdapterPresentation();

app.Run();

// 테스트를 위한 partial class 노출
public partial class Program { }
