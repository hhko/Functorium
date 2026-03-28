using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Basic;

public class Basic04_OptionsValidationTests
{
    [Fact]
    public void Should_Pass_Validation_With_Valid_Options()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<DatabaseOptions>();

        services.AddOptions<DatabaseOptions>()
            .Configure(options =>
            {
                options.ConnectionString = "Server=localhost;Database=Test";
                options.ConnectionTimeout = 30;
                options.RetryCount = 3;
                options.MaxPoolSize = 100;
            })
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Act & Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

        options.ShouldNotBeNull();
        options.Value.ConnectionString.ShouldBe("Server=localhost;Database=Test");
    }

    [Fact]
    public void Should_Fail_Validation_With_Invalid_Options()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<DatabaseOptions>();

        services.AddOptions<DatabaseOptions>()
            .Configure(options =>
            {
                options.ConnectionString = ""; // 필수 값이 비어있음
                options.ConnectionTimeout = 500; // 범위를 벗어남
                options.RetryCount = 15; // 범위를 벗어남
            })
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Act & Assert
        // ValidateOnStart()는 호스트 시작 시 검증을 실행합니다
        // 여기서는 검증 로직이 등록되었는지 확인합니다
        var serviceProvider = services.BuildServiceProvider();
        
        // 검증 로직이 등록되었는지 확인
        var validateOptions = serviceProvider.GetServices<IValidateOptions<DatabaseOptions>>();
        validateOptions.ShouldNotBeEmpty();
        
        // 직접 검증 실행
        var options = new DatabaseOptions
        {
            ConnectionString = "",
            ConnectionTimeout = 500,
            RetryCount = 15
        };
        
        var result = validateOptions.First().Validate("", options);
        result.ShouldNotBe(ValidateOptionsResult.Success);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Should_Fail_Validation_With_Conditional_Rule()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<CacheOptions>();

        services.AddOptions<CacheOptions>()
            .Configure(options =>
            {
                options.CacheType = "Redis";
                options.RedisConnectionString = null; // Redis일 때 필수인데 없음
            })
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Act & Assert
        // ValidateOnStart()는 호스트 시작 시 검증을 실행합니다
        // 여기서는 검증 로직이 등록되었는지 확인합니다
        var serviceProvider = services.BuildServiceProvider();
        
        // 검증 로직이 등록되었는지 확인
        var validateOptions = serviceProvider.GetServices<IValidateOptions<CacheOptions>>();
        validateOptions.ShouldNotBeEmpty();
        
        // 직접 검증 실행
        var options = new CacheOptions
        {
            CacheType = "Redis",
            RedisConnectionString = null
        };
        
        var result = validateOptions.First().Validate("", options);
        result.ShouldNotBe(ValidateOptionsResult.Success);
        result.Failed.ShouldBeTrue();
    }
}

// ValidateFluentValidation 확장 메서드 (테스트용)
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
