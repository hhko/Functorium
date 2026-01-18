using LayeredArch.Adapters.Infrastructure.Abstractions.Registrations;
using LayeredArch.Adapters.Persistence.Abstractions.Registrations;
using LayeredArch.Adapters.Presentation.Abstractions.Registrations;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAdapterInfrastructure()
   .UseAdapterPersistence()
   .UseAdapterPresentation();

app.Run();

// 테스트를 위한 partial class 노출
public partial class Program { }
