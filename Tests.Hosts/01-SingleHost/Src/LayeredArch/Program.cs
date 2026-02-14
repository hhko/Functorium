using LayeredArch.Adapters.Infrastructure.Abstractions.Registrations;
using LayeredArch.Adapters.Persistence.Abstractions.Registrations;
using LayeredArch.Adapters.Presentation.Abstractions.Registrations;
using Functorium.Abstractions.Diagnostics;
using LayeredArch.CrashDiagnostics;

// =================================================================
// 크래시 덤프 핸들러 초기화 (가장 먼저 실행)
// AccessViolationException 같은 CSE를 처리하여 덤프 파일을 생성합니다
// =================================================================
CrashDumpHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// 레이어별 서비스 등록
// =================================================================
builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence(builder.Configuration)
    .RegisterAdapterInfrastructure(builder.Configuration);

// =================================================================
// App 빌드 및 미들웨어 설정
// =================================================================
var app = builder.Build();

app.UseAdapterInfrastructure()
   .UseAdapterPersistence()
   .UseAdapterPresentation();

// =================================================================
// 크래시 진단 엔드포인트 (개발 전용)
// ⚠️ 프로덕션에서는 비활성화해야 합니다
// =================================================================
if (app.Environment.IsDevelopment())
{
    app.MapCrashDiagnosticsEndpoints();
}

app.Run();

// 테스트를 위한 partial class 노출
public partial class Program { }
