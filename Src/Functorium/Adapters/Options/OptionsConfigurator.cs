using FluentValidation;
using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Options;

public static class OptionsConfigurator
{
    public static TOptions GetOptions<TOptions>(this IServiceCollection services)
        where TOptions : class, new()
    {
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
    }

    //public static TOptions GetOptions<TOptions>(this IApplicationBuilder app)
    //    where TOptions : class, new()
    //{
    //    return app.ApplicationServices.GetRequiredService<IOptions<TOptions>>().Value;
    //}

    // IServiceCollection 확장 메서드: RegisterConfigureOptions
    //  - appsettings.json을 configurationSectionName 기준으로 TOptions 연결
    //  - OptionsBuilder<TOptions> 확장 메서드 호출
    //    - IValidateOptions<TOptions> 인터페이스 구현체 FluentValidationOptions<TOptions> 클래스 의존성 주입
    //      - FluentValidationOptions<TOptions> 클래스 호출
    //        - 개별 IValidator<TOptions> 상속 클래스의 Validate 메서드 호출
    //  - 프로그램 시작 시 유효성 감사

    // TOptions 유효성 검사 등록
    public static OptionsBuilder<TOptions> RegisterConfigureOptions<TOptions, TValidator>(
        this IServiceCollection services,
        string configurationSectionName)
            where TOptions : class
            where TValidator : class, IValidator<TOptions>
    {
        // IValidator 인터페이스 구현체 등록
        services.AddScoped<IValidator<TOptions>, TValidator>();

        var builder = services.AddOptions<TOptions>()                         // TOptions과
                              .BindConfiguration(configurationSectionName)    //  - 옵션 데이터 연결: appsettings.json -> configurationSectionName 이름으로 TOptions 연결
                              .AddValidateFluentValidation()                  //  - 옵션 유효성 연결: TOptions -> IValidateOptions<TOptions>으로 연결
                              .ValidateOnStart();                             //  - 옵션 유효성 검사: 프로그램 시작 시 유효성 감사

        // TOptions가 IStartupOptionsLogger를 구현하면 자동으로 IStartupOptionsLogger로 등록
        // StartupLogger가 IEnumerable<IStartupOptionsLogger>을 통해 자동으로 수집할 수 있도록 함
        if (typeof(IStartupOptionsLogger).IsAssignableFrom(typeof(TOptions)))
        {
            services.AddSingleton<IStartupOptionsLogger>(sp => (IStartupOptionsLogger)sp.GetRequiredService<IOptions<TOptions>>().Value);
        }

        return builder;
    }

    // 옵션 유효성 연결: TOptions -> IValidateOptions<TOptions>으로 연결
    private static OptionsBuilder<TOptions> AddValidateFluentValidation<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(provider =>
            new FluentValidationOptions<TOptions>(
                optionsBuilder.Name,
                provider));

        return optionsBuilder;
    }

    // IValidator<TOptions> 호출
    private sealed class FluentValidationOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
    {
        private readonly string? _name;
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationOptions(
            string? name,
            IServiceProvider serviceProvider)
        {
            _name = name;
            _serviceProvider = serviceProvider;
        }

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            // 유효성 검사 제외: 기본 옵션 값을 사용할 때
            if (_name != null && _name != name)
            {
                return ValidateOptionsResult.Skip;
            }

            ArgumentNullException.ThrowIfNull(options);

            // Scoped 란?
            //  일반적으로 Scoped로 등록된 서비스 는 하나의 HTTP 요청 내에서는 동일한 인스턴스를 공유하지만, 다른 요청에서는 새로운 인스턴스가 생성됩니다.
            //  IServiceProvider에서 직접 Scoped 서비스(예: Validator)를 가져오면 문제가 발생할 수 있습니다(HTTP 요청이 아니기 때문에).
            //  루트 스코프에서 Scoped 서비스에 접근하면, 스코프가 적절히 관리되지 않아 메모리 누수나 예기치 않은 동작이 발생할 수 있습니다.
            //
            // 루트 스코프에서 Scoped 서비스 접근하기
            //  명시적으로 IServiceScopeFactory를 사용하여 새로운 DI 스코프 를 만들고, 그 안에서 Scoped 서비스를 가져와 사용해야 합니다.
            //  (HTTP 요청에 따른 Scoped 서비스 라이프사이클을 직접 관리해야 합니다)
            using IServiceScope scope = _serviceProvider.CreateScope();

            // IValidator<TOptions> 인스턴스를 반환합니다(없을 때 예외 발생: GetRequiredService).
            IValidator<TOptions> validator = scope.ServiceProvider.GetRequiredService<IValidator<TOptions>>();

            // IValidator TOptions 유효성 검사
            FluentValidation.Results.ValidationResult result = validator.Validate(options);

            // 옵션 유효성 검사 성공일 때
            if (result.IsValid)
            {
                return ValidateOptionsResult.Success;
            }

            // 옵션 유효성 검사 실패일 때
            //
            // Microsoft.Extensions.Options.OptionsValidationException:
            //      'Fluent validation failed for
            //      'ExampleOptions.Retries'                                        // <- {typeName}.{error.PropertyName}
            //          with the error:
            //      'Retries'은(는) 1 이상 9 이하여야 합니다. 입력한 값은 -1입니다.'     // <- {error.ErrorMessage}
            string typeName = options.GetType().Name;
            IEnumerable<string> errors = result
                .Errors
                .Select(error => $"Option Validation failed for '{typeName}.{error.PropertyName}': {error.ErrorMessage}");

            return ValidateOptionsResult.Fail(string.Join(Environment.NewLine, errors));
        }
    }
}
