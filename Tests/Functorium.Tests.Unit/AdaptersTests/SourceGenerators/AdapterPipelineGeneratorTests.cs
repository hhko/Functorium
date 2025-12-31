using Functorium.Adapters.SourceGenerator;
using Functorium.Testing.Actions.SourceGenerators;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

// # AdapterPipelineGenerator 소스 생성기 테스트
//
// [GeneratePipeline] 속성이 붙은 어댑터 클래스에 대해
// 파이프라인 래퍼 클래스가 올바르게 생성되는지 검증합니다.
//
// ## 테스트 시나리오
//
// ### 1. 기본 생성 테스트
// - GeneratePipelineAttribute 자동 생성 확인
//
// ### 2. 기본 어댑터 시나리오
// - 단일 메서드 어댑터: IAdapter 구현, 1개 메서드
// - 다중 메서드 어댑터: 여러 메서드 포함
// - 메서드 없는 어댑터: IAdapter만 구현, 파이프라인 미생성
//
// ### 3. 파라미터 시나리오
// - 파라미터 0개: LoggerMessage.Define 사용 (4개 파라미터)
// - 파라미터 2개: LoggerMessage.Define 사용 (6개 파라미터)
// - 파라미터 3개: logger.LogDebug() 폴백 (7개 파라미터)
// - 컬렉션 파라미터: Count 필드 추가
// - Nullable 파라미터: int?, string? 처리
// - 단순 튜플 입력 파라미터: (int Id, string Name) - Count 미생성
// - 컬렉션 포함 튜플 입력 파라미터: (int Id, List<string> Tags) - Count 미생성
// - 배열 포함 튜플 입력 파라미터: (string Name, int[] Scores) - Length 미생성
//
// ### 4. 반환 타입 시나리오
// - 단순 반환 타입: FinT<IO, int>
// - 컬렉션 반환 타입: FinT<IO, List<T>>
// - 복잡한 제네릭: FinT<IO, Dictionary<string, List<int>>>
// - 단순 값 튜플: FinT<IO, (int Id, string Name)>
// - 컬렉션 포함 튜플: FinT<IO, (int Id, List<string> Tags)> - Count 미생성
// - 배열 포함 튜플: FinT<IO, (string Name, int[] Scores)> - Length 미생성
//
// ### 5. 생성자 시나리오
// - Primary Constructor: C# 12+ 문법
// - 다중 생성자: 가장 많은 파라미터 선택
// - 파라미터명 충돌: logger → baseLogger 리네이밍
// - 부모 클래스 생성자: 상속 체인 처리
//
// ### 6. 인터페이스 시나리오
// - IAdapter 직접 구현
// - IAdapter 상속 인터페이스: IUserRepository : IAdapter
// - 다중 인터페이스: IAdapter + IDisposable
//
// ### 7. 네임스페이스 시나리오
// - 단순 네임스페이스: MyApp
// - 깊은 네임스페이스: Company.Domain.Adapters.Infrastructure.Repositories
//
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class AdapterPipelineGeneratorTests
{
    private readonly AdapterPipelineGenerator _sut;

    public AdapterPipelineGeneratorTests()
    {
        _sut = new AdapterPipelineGenerator();
    }

    #region 1. 기본 생성 테스트

    /// <summary>
    /// 시나리오: 소스 생성기가 [GeneratePipeline] Attribute를 자동으로 생성하는지 확인합니다.
    /// 이 Attribute는 어댑터 클래스에 붙여 파이프라인 생성을 지시하는 마커입니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_GeneratePipelineAttribute()
    {
        // Arrange
        string input = string.Empty;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 2 기본 어댑터 시나리오

    /// <summary>
    /// 시나리오: 단일 메서드 어댑터
    /// IAdapter를 구현하고 단일 메서드를 가진 어댑터에 대해 파이프라인 클래스가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSingleMethod()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITestAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class TestAdapter : ITestAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 다중 메서드 어댑터
    /// 여러 메서드를 가진 어댑터에 대해 모든 메서드가 오버라이드되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleMethods()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IMultiMethodAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
                FinT<IO, string> GetName();
                FinT<IO, bool> IsValid();
            }

            [GeneratePipeline]
            public class MultiMethodAdapter : IMultiMethodAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
                public virtual FinT<IO, string> GetName() => FinT<IO, string>.Succ("test");
                public virtual FinT<IO, bool> IsValid() => FinT<IO, bool>.Succ(true);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 메서드 없는 어댑터
    /// IAdapter를 직접 구현하지만 추가 메서드가 없는 경우 파이프라인이 생성되지 않아야 합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldNotGenerate_PipelineClass_WhenNoMethods()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;

            namespace TestNamespace;

            [GeneratePipeline]
            public class EmptyAdapter : IAdapter
            {
                public string RequestCategory => "Test";
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 3 파라미터 시나리오

    /// <summary>
    /// 시나리오: 파라미터 0개
    /// 파라미터가 없는 메서드에서 LoggerMessage.Define이 사용되는지 확인합니다.
    /// (기본 4개 파라미터만 사용)
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_LoggerMessageDefine_WithZeroParameters()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IZeroParamAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class ZeroParamAdapter : IZeroParamAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 파라미터 2개 (총 6개 = 4 + 2)
    /// LoggerMessage.Define 제한(6개)에 맞는 경우 고성능 로깅이 사용되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_LoggerMessageDefine_WithTwoParameters()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITwoParamAdapter : IAdapter
            {
                FinT<IO, string> GetData(int id, string name);
            }

            [GeneratePipeline]
            public class TwoParamAdapter : ITwoParamAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, string> GetData(int id, string name) => FinT<IO, string>.Succ($"{id}:{name}");
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 파라미터 3개 (총 7개 = 4 + 3)
    /// LoggerMessage.Define 제한(6개)을 초과하여 logger.LogDebug()로 폴백되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_LogDebugFallback_WithThreeParameters()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IThreeParamAdapter : IAdapter
            {
                FinT<IO, string> GetData(int id, string name, bool isActive);
            }

            [GeneratePipeline]
            public class ThreeParamAdapter : IThreeParamAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, string> GetData(int id, string name, bool isActive) => FinT<IO, string>.Succ($"{id}:{name}:{isActive}");
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 컬렉션 파라미터 (List, 배열)
    /// 컬렉션 타입 파라미터에 대해 Count 필드가 추가되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_CollectionCountFields_WithCollectionParameters()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ICollectionParamAdapter : IAdapter
            {
                FinT<IO, int> ProcessItems(List<string> items);
            }

            [GeneratePipeline]
            public class CollectionParamAdapter : ICollectionParamAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> ProcessItems(List<string> items) => FinT<IO, int>.Succ(items?.Count ?? 0);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: Nullable 파라미터
    /// nullable 타입 파라미터가 올바르게 처리되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithNullableParameters()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface INullableParamAdapter : IAdapter
            {
                FinT<IO, string> GetData(int? id, string? name);
            }

            [GeneratePipeline]
            public class NullableParamAdapter : INullableParamAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, string> GetData(int? id, string? name) => FinT<IO, string>.Succ($"{id}:{name}");
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 단순 튜플 입력 파라미터
    /// (int Id, string Name) 형태의 튜플 입력 파라미터가 컬렉션으로 인식되지 않는지 확인합니다.
    /// 튜플은 컬렉션으로 취급되지 않으므로 Count 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputParameter()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITupleInputAdapter : IAdapter
            {
                FinT<IO, string> ProcessUser((int Id, string Name) user);
            }

            [GeneratePipeline]
            public class TupleInputAdapter : ITupleInputAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, string> ProcessUser((int Id, string Name) user)
                    => FinT<IO, string>.Succ($"{user.Id}:{user.Name}");
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 컬렉션 포함 튜플 입력 파라미터
    /// (int Id, List&lt;string&gt; Tags) 형태의 튜플 입력 파라미터가 컬렉션으로 인식되지 않는지 확인합니다.
    /// 튜플 내부에 List가 있더라도 튜플 전체는 컬렉션으로 취급되지 않으므로 Count 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputContainingCollection()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ITupleWithCollectionInputAdapter : IAdapter
            {
                FinT<IO, int> ProcessUserWithTags((int Id, List<string> Tags) user);
            }

            [GeneratePipeline]
            public class TupleWithCollectionInputAdapter : ITupleWithCollectionInputAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> ProcessUserWithTags((int Id, List<string> Tags) user)
                    => FinT<IO, int>.Succ(user.Tags?.Count ?? 0);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 배열 포함 튜플 입력 파라미터
    /// (string Name, int[] Scores) 형태의 튜플 입력 파라미터가 컬렉션으로 인식되지 않는지 확인합니다.
    /// 튜플 내부에 배열이 있더라도 튜플 전체는 컬렉션으로 취급되지 않으므로 Length 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputContainingArray()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITupleWithArrayInputAdapter : IAdapter
            {
                FinT<IO, double> CalculateAverage((string Name, int[] Scores) student);
            }

            [GeneratePipeline]
            public class TupleWithArrayInputAdapter : ITupleWithArrayInputAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, double> CalculateAverage((string Name, int[] Scores) student)
                    => FinT<IO, double>.Succ(student.Scores?.Length > 0 ? student.Scores.Average() : 0);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 4 반환 타입 시나리오

    /// <summary>
    /// 시나리오: 단순 반환 타입
    /// FinT&lt;IO, int&gt; 형태의 단순 반환 타입이 올바르게 추출되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleReturnType()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ISimpleReturnAdapter : IAdapter
            {
                FinT<IO, int> GetNumber();
                FinT<IO, string> GetText();
                FinT<IO, bool> GetFlag();
            }

            [GeneratePipeline]
            public class SimpleReturnAdapter : ISimpleReturnAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetNumber() => FinT<IO, int>.Succ(42);
                public virtual FinT<IO, string> GetText() => FinT<IO, string>.Succ("hello");
                public virtual FinT<IO, bool> GetFlag() => FinT<IO, bool>.Succ(true);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 컬렉션 반환 타입
    /// FinT&lt;IO, List&lt;User&gt;&gt; 형태의 컬렉션 반환 타입이 올바르게 처리되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithCollectionReturnType()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System.Collections.Generic;

            namespace TestNamespace;

            public record User(int Id, string Name);

            public interface ICollectionReturnAdapter : IAdapter
            {
                FinT<IO, List<User>> GetUsers();
                FinT<IO, string[]> GetNames();
            }

            [GeneratePipeline]
            public class CollectionReturnAdapter : ICollectionReturnAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, List<User>> GetUsers() => FinT<IO, List<User>>.Succ(new List<User>());
                public virtual FinT<IO, string[]> GetNames() => FinT<IO, string[]>.Succ(Array.Empty<string>());
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 복잡한 제네릭 반환 타입
    /// FinT&lt;IO, Dictionary&lt;string, List&lt;int&gt;&gt;&gt; 형태의 복잡한 제네릭이 올바르게 추출되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithComplexGenericReturnType()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface IComplexGenericAdapter : IAdapter
            {
                FinT<IO, Dictionary<string, List<int>>> GetComplexData();
            }

            [GeneratePipeline]
            public class ComplexGenericAdapter : IComplexGenericAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, Dictionary<string, List<int>>> GetComplexData()
                    => FinT<IO, Dictionary<string, List<int>>>.Succ(new Dictionary<string, List<int>>());
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 단순 값 튜플 반환 타입
    /// FinT&lt;IO, (int Id, string Name)&gt; 형태의 튜플이 올바르게 처리되는지 확인합니다.
    /// 튜플은 컬렉션으로 인식되지 않으므로 Count 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleTupleReturnType()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITupleAdapter : IAdapter
            {
                FinT<IO, (int Id, string Name)> GetUserInfo();
            }

            [GeneratePipeline]
            public class TupleAdapter : ITupleAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, (int Id, string Name)> GetUserInfo()
                    => FinT<IO, (int Id, string Name)>.Succ((1, "Test"));
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 컬렉션 포함 튜플 반환 타입
    /// FinT&lt;IO, (int Id, List&lt;string&gt; Tags)&gt; 형태의 튜플이 올바르게 처리되는지 확인합니다.
    /// 튜플 내부의 List는 감지되지 않으므로 Count 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleContainingCollection()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ITupleWithCollectionAdapter : IAdapter
            {
                FinT<IO, (int Id, List<string> Tags)> GetUserWithTags();
            }

            [GeneratePipeline]
            public class TupleWithCollectionAdapter : ITupleWithCollectionAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, (int Id, List<string> Tags)> GetUserWithTags()
                    => FinT<IO, (int Id, List<string> Tags)>.Succ((1, new List<string> { "tag1", "tag2" }));
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 배열 포함 튜플 반환 타입
    /// FinT&lt;IO, (string Name, int[] Scores)&gt; 형태의 튜플이 올바르게 처리되는지 확인합니다.
    /// 튜플 내부의 배열은 감지되지 않으므로 Length 필드가 생성되지 않습니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleContainingArray()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITupleWithArrayAdapter : IAdapter
            {
                FinT<IO, (string Name, int[] Scores)> GetStudentScores();
            }

            [GeneratePipeline]
            public class TupleWithArrayAdapter : ITupleWithArrayAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, (string Name, int[] Scores)> GetStudentScores()
                    => FinT<IO, (string Name, int[] Scores)>.Succ(("Student", new[] { 90, 85, 95 }));
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 5 생성자 시나리오

    /// <summary>
    /// 시나리오: Primary Constructor (C# 12+)
    /// Primary constructor 문법을 사용하는 클래스가 올바르게 처리되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithPrimaryConstructor()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IPrimaryCtorAdapter : IAdapter
            {
                FinT<IO, string> GetConnectionString();
            }

            [GeneratePipeline]
            public class PrimaryCtorAdapter(string connectionString, int timeout) : IPrimaryCtorAdapter
            {
                public string RequestCategory => "Database";
                public virtual FinT<IO, string> GetConnectionString() => FinT<IO, string>.Succ(connectionString);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 다중 생성자
    /// 여러 생성자 중 파라미터가 가장 많은 생성자가 선택되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleConstructors()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IMultiCtorAdapter : IAdapter
            {
                FinT<IO, string> GetInfo();
            }

            [GeneratePipeline]
            public class MultiCtorAdapter : IMultiCtorAdapter
            {
                private readonly string _connectionString;
                private readonly int _timeout;
                private readonly bool _useCache;

                public MultiCtorAdapter() : this("default", 30, false) { }
                public MultiCtorAdapter(string connectionString) : this(connectionString, 30, false) { }
                public MultiCtorAdapter(string connectionString, int timeout, bool useCache)
                {
                    _connectionString = connectionString;
                    _timeout = timeout;
                    _useCache = useCache;
                }

                public string RequestCategory => "Database";
                public virtual FinT<IO, string> GetInfo() => FinT<IO, string>.Succ(_connectionString);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 파라미터명 충돌
    /// 예약된 파라미터명(logger, parentContext 등)과 충돌 시 리네이밍되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithParameterNameConflict()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using Microsoft.Extensions.Logging;

            namespace TestNamespace;

            public interface IConflictParamAdapter : IAdapter
            {
                FinT<IO, string> GetData();
            }

            [GeneratePipeline]
            public class ConflictParamAdapter : IConflictParamAdapter
            {
                private readonly ILogger _logger;

                public ConflictParamAdapter(ILogger logger)
                {
                    _logger = logger;
                }

                public string RequestCategory => "Test";
                public virtual FinT<IO, string> GetData() => FinT<IO, string>.Succ("data");
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 부모 클래스 생성자
    /// 상속 체인에서 부모 클래스의 생성자 파라미터가 올바르게 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithBaseClassConstructor()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public abstract class BaseAdapter
            {
                protected readonly string ConnectionString;

                protected BaseAdapter(string connectionString)
                {
                    ConnectionString = connectionString;
                }
            }

            public interface IDerivedAdapter : IAdapter
            {
                FinT<IO, string> GetConnection();
            }

            [GeneratePipeline]
            public class DerivedAdapter : BaseAdapter, IDerivedAdapter
            {
                public DerivedAdapter(string connectionString) : base(connectionString) { }

                public string RequestCategory => "Database";
                public virtual FinT<IO, string> GetConnection() => FinT<IO, string>.Succ(ConnectionString);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 6 인터페이스 시나리오

    /// <summary>
    /// 시나리오 16: IAdapter 직접 구현
    /// IAdapter를 직접 구현하는 경우 파이프라인이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithDirectIAdapterImplementation()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IDirectAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class DirectAdapter : IDirectAdapter
            {
                public string RequestCategory => "Direct";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(1);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: IAdapter 상속 인터페이스
    /// IAdapter를 상속하는 커스텀 인터페이스를 구현하는 경우 처리되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithInheritedIAdapterInterface()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface IUserRepository : IAdapter
            {
                FinT<IO, string> GetUserById(int id);
                FinT<IO, bool> UpdateUser(int id, string name);
            }

            [GeneratePipeline]
            public class UserRepository : IUserRepository
            {
                public string RequestCategory => "Repository";
                public virtual FinT<IO, string> GetUserById(int id) => FinT<IO, string>.Succ($"User_{id}");
                public virtual FinT<IO, bool> UpdateUser(int id, string name) => FinT<IO, bool>.Succ(true);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 다중 인터페이스 구현
    /// 여러 인터페이스를 구현하는 경우 IAdapter 관련 메서드만 파이프라인에 포함되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleInterfaces()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;
            using System;

            namespace TestNamespace;

            public interface IDisposableAdapter : IAdapter, IDisposable
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class DisposableAdapter : IDisposableAdapter
            {
                public string RequestCategory => "Disposable";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
                public void Dispose() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion

    #region 7 네임스페이스 시나리오

    /// <summary>
    /// 시나리오: 단순 네임스페이스
    /// 단순 네임스페이스에서 파일명이 올바르게 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleNamespace()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace MyApp;

            public interface ISimpleNamespaceAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class SimpleNamespaceAdapter : ISimpleNamespaceAdapter
            {
                public string RequestCategory => "Simple";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(1);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    /// <summary>
    /// 시나리오: 깊은 네임스페이스
    /// 깊은 네임스페이스에서 마지막 부분만 파일명에 포함되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithDeepNamespace()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace Company.Domain.Adapters.Infrastructure.Repositories;

            public interface IDeepNamespaceAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class DeepNamespaceAdapter : IDeepNamespaceAdapter
            {
                public string RequestCategory => "Repository";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(1);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    #endregion
}
