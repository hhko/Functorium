---
title: "Sealed Record Union 패턴 참조"
---

## 기본 구조

```csharp
public abstract record UnionType
{
    public sealed record Case1(/* 데이터 */) : UnionType;
    public sealed record Case2(/* 데이터 */) : UnionType;
    public sealed record Case3(/* 데이터 */) : UnionType;

    private UnionType() { } // 외부 상속 차단
}
```

## 핵심 규칙

1. **기본 record는 `abstract`** — 직접 인스턴스화 방지
2. **각 케이스는 `sealed record`** — 추가 상속 방지
3. **생성자는 `private`** — 외부에서 새로운 케이스 추가 방지
4. **중첩 정의** — 모든 케이스가 한 파일에 모여 있어 전체 상태 공간을 한눈에 파악

## 패턴 매칭

```csharp
var result = union switch
{
    UnionType.Case1 c1 => HandleCase1(c1),
    UnionType.Case2 c2 => HandleCase2(c2),
    UnionType.Case3 c3 => HandleCase3(c3),
    _ => throw new InvalidOperationException("알 수 없는 케이스")
};
```

## 상태 기계 확장

```csharp
public abstract record State
{
    public sealed record Initial() : State;
    public sealed record Processing(DateTime StartedAt) : State;
    public sealed record Completed(DateTime CompletedAt, string Result) : State;
    public sealed record Failed(DateTime FailedAt, string Error) : State;

    private State() { }

    public static Fin<State> Start(State state) => state switch
    {
        Initial => new Processing(DateTime.UtcNow),
        _ => Fin<State>.Fail(Error.New("Invalid transition"))
    };
}
```

## 제약 사항

- C#의 sealed record union은 F#의 DU처럼 완전한 exhaustiveness check를 제공하지 않습니다
- `_ =>` 기본 케이스가 항상 필요합니다
- record의 `with` 식은 같은 타입 내에서만 동작합니다
