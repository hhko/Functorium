---
title: "Why Source Generator"
---

## 개요

앞 장에서 소스 생성기의 정의와 동작 원리를 살펴보았습니다. 하지만 "이런 기술이 있다"는 것과 "이 기술을 선택해야 한다"는 것은 다른 문제입니다. 리플렉션, T4 템플릿, Expression Trees 등 기존 코드 생성 기법이 이미 존재하는데, 왜 굳이 소스 생성기를 사용해야 할까요? 이 장에서는 소스 생성기가 제공하는 구체적인 장점과 적합한 사용 시나리오를 다룹니다.

## 학습 목표

### 핵심 학습 목표
1. **소스 생성기의 장점 이해**
   - 성능, 타입 안전성, 디버깅, AOT 지원의 구체적 근거 파악
2. **기존 코드 생성 기법과의 차이점 파악**
   - T4 템플릿, Reflection.Emit, Expression Trees와의 비교
3. **적합한 사용 시나리오 판단**
   - 소스 생성기가 적합한 경우와 부적합한 경우 구분

---

## 소스 생성기의 장점

### 1. 성능

런타임 비용은 애플리케이션 성능에 직접적인 영향을 미칩니다. 소스 생성기는 **컴파일 타임**에 코드를 생성하므로 런타임 오버헤드가 없습니다.

```csharp
// 리플렉션 기반 (런타임 비용 발생)
public void LogWithReflection(object obj)
{
    var properties = obj.GetType().GetProperties();  // 매번 리플렉션 호출
    foreach (var prop in properties)
    {
        var value = prop.GetValue(obj);  // 런타임 비용
        Console.WriteLine($"{prop.Name}: {value}");
    }
}

// 소스 생성기 기반 (런타임 비용 없음)
public void LogWithSourceGenerator(User user)
{
    // 컴파일 타임에 생성된 코드 - 직접 프로퍼티 접근
    Console.WriteLine($"Id: {user.Id}");
    Console.WriteLine($"Name: {user.Name}");
    Console.WriteLine($"Email: {user.Email}");
}
```

```
성능 비교 (예시)
===============

리플렉션:     ~1,000 ns/호출
소스 생성기:     ~10 ns/호출

→ 약 100배 성능 향상
```

### 2. 타입 안전성

성능뿐 아니라 안전성도 중요합니다. 컴파일 타임에 코드가 생성되므로 **타입 오류를 빌드 시점에 발견**할 수 있습니다.

```csharp
// 리플렉션 - 런타임에 오류 발생
var value = prop.GetValue(obj);  // obj가 null이면 런타임 예외

// 소스 생성기 - 컴파일 타임에 오류 발견
public void Process(User user)
{
    var id = user.Id;  // User에 Id가 없으면 컴파일 에러
}
```

### 3. 디버깅 용이성

생성된 코드는 일반 C# 코드이므로 **디버거로 스텝 인(step-in)할** 수 있습니다.

```
Visual Studio에서 생성된 코드 보기
=================================

1. 솔루션 탐색기 → Dependencies → Analyzers
2. 소스 생성기 프로젝트 확장
3. 생성된 .g.cs 파일 확인 및 디버깅 가능
```

### 4. AOT 컴파일 지원

.NET 10의 Native AOT와 완벽히 호환됩니다.

```xml
<!-- .NET 10 프로젝트에서 AOT 활성화 -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

리플렉션 기반 코드는 AOT에서 제약이 있지만, 소스 생성기로 생성된 코드는 **정적으로 컴파일**되어 제약이 없습니다.

### 5. 인텔리센스 지원

생성된 코드는 IDE의 인텔리센스에서 **즉시 인식**됩니다.

```csharp
// 소스 생성기가 UserPipeline 클래스를 생성하면
var pipeline = new UserPipeline();
pipeline.  // ← 인텔리센스가 생성된 메서드 표시
```

지금까지 소스 생성기의 개별 장점을 살펴보았습니다. 이제 기존 코드 생성 기술과 직접 비교하여, 각 기술이 어떤 상황에서 적합한지 구체적으로 판단해 보겠습니다.

---

## 기존 기술과의 비교

### T4 템플릿

| 항목 | T4 템플릿 | 소스 생성기 |
|------|----------|-------------|
| 실행 시점 | 디자인 타임 (수동) | 컴파일 타임 (자동) |
| 입력 변경 감지 | 수동 재실행 필요 | 자동 감지 및 재생성 |
| 소스 제어 | 생성 파일 포함 필요 | 포함 불필요 |
| IDE 통합 | 제한적 | 완전 통합 |
| .NET 10 지원 | 레거시 | 공식 지원 |

### Reflection.Emit

| 항목 | Reflection.Emit | 소스 생성기 |
|------|-----------------|-------------|
| 생성물 | IL 코드 | C# 소스 코드 |
| 디버깅 | 매우 어려움 | 쉬움 (일반 C#) |
| AOT 지원 | 제한적 | 완전 지원 |
| 학습 곡선 | 높음 (IL 지식 필요) | 낮음 (C# 지식) |

### Expression Trees

| 항목 | Expression Trees | 소스 생성기 |
|------|------------------|-------------|
| 실행 시점 | 런타임 | 컴파일 타임 |
| 표현 범위 | 람다 표현식 | 전체 C# 문법 |
| 성능 | 캐싱 필요 | 오버헤드 없음 |
| 복잡도 | 중간 | 중간 |

비교를 통해 소스 생성기의 위치를 확인했으니, 이제 실제 프로젝트에서 소스 생성기를 선택해야 하는 경우와 그렇지 않은 경우를 정리해 보겠습니다.

---

## 적합한 사용 시나리오

### 소스 생성기가 적합한 경우

```
✓ 반복적인 보일러플레이트 코드 제거
  예: DTO 매핑, INotifyPropertyChanged 구현

✓ 특성(Attribute) 기반 코드 생성
  예: [Serialize], [Validate], [Log]

✓ 인터페이스 구현 자동화
  예: Repository 패턴, CQRS 핸들러

✓ 성능이 중요한 직렬화/역직렬화
  예: JSON, MessagePack, Protocol Buffers

✓ AOT 배포가 필요한 프로젝트
  예: iOS/Android, 웹어셈블리, 서버리스
```

### 소스 생성기가 부적합한 경우

```
✗ 런타임에 동적으로 타입이 결정되는 경우
  예: 플러그인 시스템, 스크립팅 엔진

✗ 매우 단순한 코드 생성
  예: 한두 줄의 래퍼 메서드

✗ 외부 데이터 소스 의존
  예: 데이터베이스 스키마 기반 생성 (컴파일 시점에 접근 불가)
```

이러한 판단 기준을 바탕으로, .NET 생태계에서 소스 생성기가 실제로 어떻게 활용되고 있는지 대표적인 사례를 살펴보겠습니다.

---

## 실제 활용 사례

### 1. System.Text.Json (Microsoft)

.NET의 공식 JSON 라이브러리는 소스 생성기로 **컴파일 타임 직렬화**를 지원합니다.

```csharp
// .NET 10 - JSON 소스 생성기
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Order))]
public partial class AppJsonContext : JsonSerializerContext;

// 사용
var json = JsonSerializer.Serialize(user, AppJsonContext.Default.User);
```

### 2. LoggerMessage (Microsoft)

고성능 로깅을 위한 소스 생성기입니다.

```csharp
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information,
                   Message = "User {UserId} logged in at {LoginTime}")]
    public static partial void UserLoggedIn(
        ILogger logger, int userId, DateTime loginTime);
}
```

### 3. Functorium ObservablePortGenerator

이 튜토리얼에서 구현할 소스 생성기로, **어댑터 파이프라인 코드**를 자동 생성합니다.

```csharp
// 입력 - 개발자가 작성
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// 출력 - 소스 생성기가 자동 생성
public partial class UserRepositoryObservable
{
    private readonly ILogger<UserRepository> _logger;

    public FinT<IO, User> GetUserAsync(int id)
    {
        // 로깅, 메트릭, 추적 코드 자동 포함
    }
}
```

---

## 한눈에 보는 정리

소스 생성기는 런타임 오버헤드 없는 성능, 컴파일 타임 오류 검출을 통한 타입 안전성, 생성된 C# 코드에 대한 직접 디버깅, .NET 10 Native AOT 완벽 호환, 그리고 인텔리센스와 리팩터링을 지원하는 IDE 통합이라는 다섯 가지 핵심 장점을 제공합니다. 특히 반복적인 보일러플레이트 코드 생성과 AOT 배포가 필요한 시나리오에서 기존 기술 대비 확실한 우위를 가집니다.

---

## FAQ

### Q1: 소스 생성기가 T4 템플릿보다 나은 점은 무엇인가요?
**A**: T4 템플릿은 디자인 타임에 수동으로 실행해야 하고, 생성된 파일을 소스 제어에 포함해야 합니다. 소스 생성기는 컴파일 타임에 자동 실행되어 입력 변경을 자동 감지하며, 생성 파일을 소스 제어에 넣을 필요가 없어 유지보수 비용이 크게 줄어듭니다.

### Q2: 소스 생성기가 적합하지 않은 경우는 어떤 상황인가요?
**A**: 런타임에 타입이 동적으로 결정되는 플러그인 시스템이나, 컴파일 시점에 접근할 수 없는 데이터베이스 스키마 기반 생성에는 부적합합니다. 또한 한두 줄의 단순한 래퍼 메서드라면 소스 생성기의 설정 비용 대비 이점이 적습니다.

### Q3: AOT 환경에서 소스 생성기가 중요한 이유는 무엇인가요?
**A**: Native AOT는 런타임 코드 생성과 동적 리플렉션을 제한합니다. 소스 생성기는 컴파일 타임에 모든 코드를 정적으로 생성하므로, AOT 컴파일러가 생성된 코드를 일반 코드처럼 최적화할 수 있어 AOT 배포 환경에서 유일한 대안이 됩니다.

---

소스 생성기를 선택해야 하는 이유를 확인했으니, 이제 이 튜토리얼에서 실제로 구현할 ObservablePortGenerator 프로젝트의 전체 구조와 설계 목표를 살펴보겠습니다.

→ [03. 프로젝트 개요](03-project-overview.md)
