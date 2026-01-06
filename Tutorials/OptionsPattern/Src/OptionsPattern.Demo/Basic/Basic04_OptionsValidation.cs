using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Basic;

/// <summary>
/// Basic04: Options 검증 (FluentValidation)
/// 
/// 학습 목표:
/// - ValidateOnStart() 사용법
///   * ValidateOnStart()는 애플리케이션 시작 시 Options 값을 검증합니다
///   * 검증 실패 시 OptionsValidationException이 발생하여 애플리케이션이 시작되지 않습니다
///   * 잘못된 설정으로 인한 런타임 오류를 방지할 수 있습니다
///   * IHost를 사용하는 경우 호스트 시작 시 자동으로 검증이 실행됩니다
/// - FluentValidation을 사용한 검증 규칙 작성
///   * RuleFor() 메서드로 각 속성에 대한 검증 규칙을 정의합니다
///   * NotEmpty(), InclusiveBetween(), Must() 등 다양한 검증 메서드를 사용할 수 있습니다
///   * When() 메서드로 조건부 검증을 구현할 수 있습니다
///   * WithMessage()로 사용자 정의 오류 메시지를 지정할 수 있습니다
/// - 검증 실패 시 동작 이해
///   * 검증 실패 시 OptionsValidationException이 발생합니다
///   * 예외의 Failures 속성에 모든 검증 오류 메시지가 포함됩니다
///   * 애플리케이션 시작이 중단되므로, 설정 파일을 수정해야 합니다
///   * 검증 오류는 로그에 기록되므로 문제를 쉽게 파악할 수 있습니다
/// - Validator 클래스 패턴
///   * Options 클래스 내부에 중첩 클래스로 Validator를 정의하는 것이 일반적입니다
///   * AbstractValidator<TOptions>를 상속받아 검증 규칙을 정의합니다
///   * 생성자에서 검증 규칙을 설정합니다
///   * Options와 Validator가 항상 함께 유지되어 관리가 용이합니다
/// </summary>
public static class Basic04_OptionsValidation
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic04: Options Validation");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 1. 유효한 설정으로 검증 성공 예제
        Console.WriteLine("Example 1: Valid Options (Validation Success)");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddValidatorsFromAssemblyContaining<DatabaseOptions>();

            // ValidateOnStart()로 시작 시 검증
            services.AddOptions<DatabaseOptions>()
                .BindConfiguration(DatabaseOptions.SectionName)
                .ValidateFluentValidation()
                .ValidateOnStart();

            try
            {
                var serviceProvider = services.BuildServiceProvider();
                
                // BuildServiceProvider() 시점에 ValidateOnStart()가 실행됩니다
                // 여기서 검증이 실패하면 OptionsValidationException이 발생합니다
                var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
                
                Console.WriteLine("✅ Validation passed!");
                OptionsViewer.PrintOptions(options.Value, "Valid DatabaseOptions");
            }
            catch (OptionsValidationException ex)
            {
                Console.WriteLine("❌ Validation failed:");
                foreach (var failure in ex.Failures)
                {
                    Console.WriteLine($"   - {failure}");
                }
            }
            Console.WriteLine();
        }

        // 2. 잘못된 설정으로 검증 실패 예제
        Console.WriteLine("Example 2: Invalid Options (Validation Failure)");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<DatabaseOptions>();

            // 잘못된 값으로 설정
            services.AddOptions<DatabaseOptions>()
                .Configure(options =>
                {
                    options.ConnectionString = ""; // 필수 값이 비어있음
                    options.ConnectionTimeout = 500; // 범위를 벗어남 (1-300)
                    options.RetryCount = 15; // 범위를 벗어남 (0-10)
                })
                .ValidateFluentValidation()
                .ValidateOnStart();

            try
            {
                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
                
                Console.WriteLine("✅ Validation passed!");
                OptionsViewer.PrintOptions(options.Value, "DatabaseOptions");
            }
            catch (OptionsValidationException ex)
            {
                Console.WriteLine("❌ Validation failed:");
                foreach (var failure in ex.Failures)
                {
                    Console.WriteLine($"   - {failure}");
                }
            }
            Console.WriteLine();
        }

        // 3. CacheOptions의 조건부 검증 예제
        Console.WriteLine("Example 3: Conditional Validation (CacheOptions)");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<CacheOptions>();

            // Redis 타입인데 ConnectionString이 없는 경우
            services.AddOptions<CacheOptions>()
                .Configure(options =>
                {
                    options.CacheType = "Redis";
                    options.RedisConnectionString = null; // 필수인데 없음
                })
                .ValidateFluentValidation()
                .ValidateOnStart();

            try
            {
                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetRequiredService<IOptions<CacheOptions>>();
                
                Console.WriteLine("✅ Validation passed!");
                OptionsViewer.PrintOptions(options.Value, "CacheOptions");
            }
            catch (OptionsValidationException ex)
            {
                Console.WriteLine("❌ Validation failed:");
                foreach (var failure in ex.Failures)
                {
                    Console.WriteLine($"   - {failure}");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - ValidateOnStart()는 서비스 프로바이더 빌드 시 검증을 실행합니다");
        Console.WriteLine("   - 검증 실패 시 OptionsValidationException이 발생합니다");
        Console.WriteLine("   - FluentValidation의 When()을 사용하여 조건부 검증이 가능합니다");
        Console.WriteLine("   - Validator는 중첩 클래스로 정의하는 것이 일반적입니다");
        Console.WriteLine();
    }
}

// ValidateFluentValidation 확장 메서드
internal static class OptionsBuilderExtensions
{
    public static OptionsBuilder<TOptions> ValidateFluentValidation<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(provider =>
        {
            var validatorType = typeof(TOptions).GetNestedType("Validator");
            if (validatorType == null || !typeof(IValidator<TOptions>).IsAssignableFrom(validatorType))
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TOptions).Name} must have a nested Validator class that implements IValidator<{typeof(TOptions).Name}>");
            }

            var validator = (IValidator<TOptions>)Activator.CreateInstance(validatorType)!;
            return new FluentValidationOptions<TOptions>(optionsBuilder.Name, validator);
        });

        return optionsBuilder;
    }

    private sealed class FluentValidationOptions<TOptions> : IValidateOptions<TOptions>
        where TOptions : class
    {
        private readonly string? _name;
        private readonly IValidator<TOptions> _validator;

        public FluentValidationOptions(string? name, IValidator<TOptions> validator)
        {
            _name = name;
            _validator = validator;
        }

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (_name != null && _name != name)
            {
                return ValidateOptionsResult.Skip;
            }

            ArgumentNullException.ThrowIfNull(options);

            var result = _validator.Validate(options);
            if (result.IsValid)
            {
                return ValidateOptionsResult.Success;
            }

            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            return ValidateOptionsResult.Fail(errors);
        }
    }
}
