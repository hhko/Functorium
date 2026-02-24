# Part 2 - Chapter 5: Expression Tree 기초

> **Part 2: Expression Specification** | [← 목차로](../../../README.md)

---

## 개요

이 장에서는 C#의 `Expression<Func<T, bool>>`과 일반 `Func<T, bool>`의 차이를 학습합니다. Expression Tree는 코드를 데이터로 표현하는 구조로, ORM(Entity Framework Core 등)이 C# 조건식을 SQL로 변환할 수 있게 해주는 핵심 기술입니다.

> **Func는 실행 가능한 블랙박스이고, Expression은 검사 가능한 트리 구조입니다.**

## 학습 목표

### 핵심 학습 목표
1. **Func vs Expression의 근본적 차이 이해**
   - `Func<T, bool>`은 컴파일된 델리게이트로 내부 구조를 알 수 없음
   - `Expression<Func<T, bool>>`은 코드의 구조를 트리 형태로 보존함
   - Expression은 Body, Parameters, NodeType 등을 통해 검사 가능

2. **Expression Tree가 ORM에 필요한 이유**
   - EF Core는 LINQ 쿼리를 SQL로 번역해야 함
   - Func는 불투명하여 SQL로 변환할 수 없음
   - Expression은 트리를 순회하며 SQL 절로 변환 가능

3. **Expression의 컴파일과 실행**
   - `Expression.Compile()`로 Func로 변환하여 메모리에서 실행 가능
   - 컴파일된 결과는 캐싱하여 재사용하는 것이 효율적

### 실습을 통해 확인할 내용
- Expression의 Body, Parameters 속성 접근
- Expression을 Compile하여 Func로 변환 후 실행
- AsQueryable()에서 Expression 기반 Where 사용

## 핵심 개념

### Func: 불투명한 블랙박스

`Func<Product, bool>`은 컴파일된 델리게이트입니다. 런타임에 호출하여 결과를 얻을 수 있지만, "어떤 조건인지"를 프로그래밍적으로 알아낼 수 없습니다.

```csharp
Func<Product, bool> func = p => p.Price > 1000;
// func의 내부를 검사할 방법이 없음
// EF Core가 이것을 SQL로 번역할 수 없음
```

### Expression: 검사 가능한 트리 구조

`Expression<Func<Product, bool>>`은 동일한 람다식을 트리 구조로 보존합니다. 컴파일러가 람다식을 코드가 아닌 데이터 구조로 변환합니다.

```csharp
Expression<Func<Product, bool>> expr = p => p.Price > 1000;

// 트리 구조 검사 가능
Console.WriteLine(expr.Body);        // (p.Price > 1000)
Console.WriteLine(expr.Parameters);  // p
Console.WriteLine(expr.Body.NodeType); // GreaterThan
```

### Expression → Func 컴파일

Expression은 `Compile()` 메서드를 통해 실행 가능한 Func로 변환할 수 있습니다. 이 과정은 비용이 있으므로 결과를 캐싱하는 것이 좋습니다.

```csharp
var compiled = expr.Compile();
var result = compiled(product); // true/false
```

### ORM이 Expression을 필요로 하는 이유

| 구분 | Func | Expression |
|------|------|------------|
| **내부 구조** | 불투명 (블랙박스) | 검사 가능 (트리) |
| **SQL 변환** | 불가능 | 가능 |
| **IQueryable** | 지원 불가 | Where 절에 사용 가능 |
| **실행 위치** | 항상 메모리 | DB 서버 또는 메모리 |

## 프로젝트 설명

### 프로젝트 구조
```
ExpressionIntro/                          # 메인 프로젝트
├── Program.cs                            # Expression Tree 데모
├── Product.cs                            # 상품 레코드
├── ExpressionIntro.csproj                # 프로젝트 파일
ExpressionIntro.Tests.Unit/               # 테스트 프로젝트
├── ExpressionBasicsTests.cs              # Expression 기초 테스트
├── Using.cs                              # 글로벌 using
├── xunit.runner.json                     # xUnit 설정
├── ExpressionIntro.Tests.Unit.csproj     # 테스트 프로젝트 파일
README.md                                 # 이 문서
```

### 핵심 코드

#### Product.cs
```csharp
public record Product(string Name, decimal Price, int Stock, string Category);
```

#### Expression 생성과 검사
```csharp
// Func - 불투명한 블랙박스
Func<Product, bool> func = p => p.Price > 1000;

// Expression - 검사 가능한 트리
Expression<Func<Product, bool>> expr = p => p.Price > 1000;
Console.WriteLine($"Body: {expr.Body}");
Console.WriteLine($"Parameters: {string.Join(", ", expr.Parameters)}");

// Expression → Func 컴파일
var compiled = expr.Compile();
var product = new Product("노트북", 1_500_000, 10, "전자제품");
Console.WriteLine($"Result: {compiled(product)}");
```

## 한눈에 보는 정리

### Func vs Expression 비교
| 구분 | `Func<T, bool>` | `Expression<Func<T, bool>>` |
|------|-----------------|------------------------------|
| **본질** | 컴파일된 코드 | 코드의 데이터 표현 |
| **검사** | 불가능 | Body, Parameters 등 접근 가능 |
| **SQL 변환** | 불가능 | ORM이 트리를 순회하여 변환 |
| **실행** | 직접 호출 | Compile() 후 호출 |
| **IEnumerable** | Where에 사용 가능 | Compile 필요 |
| **IQueryable** | 사용 불가 | Where에 직접 사용 가능 |

### 핵심 포인트
1. **Expression은 코드를 데이터로 표현한 것**으로, 프로그래밍적으로 분석하고 변환할 수 있습니다.
2. **ORM은 Expression이 있어야 SQL을 생성**할 수 있습니다. Func만으로는 전체 데이터를 메모리에 로드해야 합니다.
3. **Compile()은 비용이 있으므로 캐싱**하는 것이 좋습니다.

## FAQ

### Q1: Expression Tree는 어디에서 사용되나요?
**A**: 주로 ORM(Entity Framework Core), LINQ to SQL, 동적 쿼리 빌더 등에서 사용됩니다. C# 코드로 작성한 조건식을 SQL이나 다른 쿼리 언어로 변환해야 할 때 Expression Tree가 필수입니다.

### Q2: Func 대신 항상 Expression을 사용해야 하나요?
**A**: 아닙니다. 메모리 내 컬렉션을 필터링할 때는 Func가 더 효율적입니다. Expression은 SQL 변환이 필요한 경우에만 사용하면 됩니다. Specification 패턴에서는 두 가지 시나리오를 모두 지원하기 위해 Expression 기반을 사용합니다.

### Q3: Expression.Compile()의 성능 비용은 얼마나 되나요?
**A**: Compile()은 Expression Tree를 IL 코드로 변환하는 과정이므로 상대적으로 비용이 큽니다. 따라서 한 번 컴파일한 결과를 캐싱하여 재사용하는 것이 좋습니다. Functorium의 `ExpressionSpecification`은 내부적으로 이 캐싱을 자동으로 수행합니다.

### Q4: AsQueryable()은 무엇인가요?
**A**: `AsQueryable()`은 `IEnumerable<T>`을 `IQueryable<T>`로 변환합니다. `IQueryable`은 Expression 기반의 Where를 지원하므로, 메모리 내 컬렉션에서도 Expression 기반 필터링을 테스트할 수 있습니다. 실제 프로젝트에서는 EF Core의 `DbSet<T>`이 `IQueryable<T>`을 구현합니다.
