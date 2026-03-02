# Chapter 9: 불변성 규칙 (Immutability Rule)

## 소개

도메인 클래스의 **불변성(Immutability)은** 함수형 프로그래밍과 안전한 동시성 처리의 핵심입니다.
이 챕터에서는 `RequireImmutable()` 메서드를 사용하여 클래스의 불변성을 종합적으로 검증하는 방법을 학습합니다.

## 학습 목표

- `RequireImmutable()`이 검증하는 6가지 차원 이해
- 올바른 불변 클래스 설계 패턴 학습
- 읽기 전용 컬렉션을 활용한 불변 클래스 구현

## 도메인 코드

### Temperature - 기본 불변 클래스

private 생성자, getter-only 속성, 팩토리 메서드 패턴을 사용한 불변 클래스입니다.

```csharp
public sealed class Temperature
{
    public double Value { get; }
    public string Unit { get; }

    private Temperature(double value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Temperature Create(double value, string unit)
        => new(value, unit);

    public Temperature ToCelsius()
        => Unit == "F" ? Create((Value - 32) * 5 / 9, "C") : this;

    public override string ToString() => $"{Value}°{Unit}";
}
```

`ToCelsius()` 메서드는 기존 객체를 변경하지 않고 새로운 `Temperature` 인스턴스를 반환합니다.
이것이 불변 객체의 핵심 패턴입니다.

### Palette - 읽기 전용 컬렉션을 사용한 불변 클래스

```csharp
public sealed class Palette
{
    public string Name { get; }
    public IReadOnlyList<string> Colors { get; }

    private Palette(string name, IReadOnlyList<string> colors)
    {
        Name = name;
        Colors = colors;
    }

    public static Palette Create(string name, params string[] colors)
        => new(name, colors.ToList().AsReadOnly());
}
```

`IReadOnlyList<string>`을 사용하여 컬렉션의 불변성을 보장합니다.
`List<string>`을 직접 노출하면 `ImmutabilityRule`의 가변 컬렉션 검증에 위반됩니다.

## 테스트 코드

### RequireImmutable()의 6가지 검증 차원

`RequireImmutable()`은 내부적으로 `ImmutabilityRule`을 적용하며, 다음 6가지 차원에서 클래스의 불변성을 검증합니다:

1. **기본 Writability 검증** - 멤버가 immutable인지 확인
2. **생성자 검증** - 모든 생성자가 private인지 확인
3. **프로퍼티 검증** - public setter가 없는지 확인
4. **필드 검증** - public 필드가 없는지 확인
5. **가변 컬렉션 타입 검증** - `List<>`, `Dictionary<>` 등 가변 컬렉션 사용 금지
6. **상태 변경 메서드 검증** - 허용된 메서드(팩토리, getter, `ToString` 등) 외 금지

### 전체 도메인 클래스 불변성 검증

```csharp
[Fact]
public void DomainClasses_ShouldBe_Immutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Immutability Rule");
}
```

### 개별 클래스 검증 (Sealed + Immutable)

```csharp
[Fact]
public void Temperature_ShouldBe_SealedAndImmutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveName("Temperature")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireSealed()
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Temperature Sealed Immutability Rule");
}
```

`RequireSealed()`과 `RequireImmutable()`을 체이닝하여 sealed이면서 불변인 클래스를 검증합니다.

## 핵심 정리

| 개념 | 설명 |
|------|------|
| `RequireImmutable()` | 6가지 차원에서 클래스 불변성을 종합 검증 |
| private 생성자 | 외부에서 직접 인스턴스 생성 방지 |
| getter-only 속성 | 속성 값 변경 방지 |
| `IReadOnlyList<T>` | 가변 컬렉션 대신 읽기 전용 컬렉션 사용 |
| 팩토리 메서드 | `Create` 정적 메서드로 인스턴스 생성 |
| 변환 메서드 | 기존 객체를 변경하지 않고 새 인스턴스 반환 |

---

[이전: Part 2 - Method and Property Validation](../../Part2-Method-And-Property-Validation/) | [다음: Chapter 10 - Nested Class Validation](../02-Nested-Class-Validation/)
