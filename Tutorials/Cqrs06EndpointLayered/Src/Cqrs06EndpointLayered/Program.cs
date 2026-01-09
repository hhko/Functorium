using Cqrs06EndpointLayered.Adapters.Infrastructure.Abstractions.Registrations;
using Cqrs06EndpointLayered.Adapters.Persistence.Abstractions.Registrations;
using Cqrs06EndpointLayered.Adapters.Presentation.Abstractions.Registrations;
using Mediator;

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// Mediator 등록 (Scoped - WebApi에서 요청당 Scope 생성)
// Note: Mediator.SourceGenerator가 Entry Point에서 실행되어야
//       internal Usecase에 접근 가능하므로 AddMediator()는 여기에 위치
// =================================================================
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

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
