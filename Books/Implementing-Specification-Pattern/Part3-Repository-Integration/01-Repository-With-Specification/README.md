# Part 3 - Chapter 9: Repository와 Specification

> **Part 3: Repository 연동** | [다음: 10장 InMemory 구현 ->](../02-InMemory-Implementation/README.md)

---

## 개요

전통적인 Repository 패턴에서는 조회 조건이 추가될 때마다 새로운 메서드를 만들어야 합니다. `FindByCategory`, `FindByPriceRange`, `FindInStock`, `FindByCategoryAndPriceRange`... 조건의 조합이 늘어날수록 메서드 수가 기하급수적으로 폭발합니다.

Specification 패턴은 이 문제를 근본적으로 해결합니다. Repository에는 `FindAll(Specification<T> spec)` 단 하나의 메서드만 두고, **무엇을 찾을지(WHAT)**는 Specification에게 위임합니다.

## 학습 목표

### 핵심 학습 목표
1. **메서드 폭발 문제 인식** - 조건 조합마다 메서드를 추가하는 방식의 한계 이해
2. **관심사 분리** - Repository는 HOW(어디서 찾을지), Specification은 WHAT(무엇을 찾을지)을 담당
3. **IProductRepository 인터페이스 설계** - Specification을 매개변수로 받는 범용 Repository 인터페이스

### 실습을 통해 확인할 내용
- 메서드 폭발 문제의 구체적인 예시
- `FindAll(Specification<Product> spec)` 메서드 하나로 모든 조회 조건 처리
- `Exists(Specification<Product> spec)` 메서드로 존재 여부 확인

## 핵심 개념

### 메서드 폭발 (Method Explosion)

전통적인 Repository에서 조건 3개(카테고리, 가격, 재고)만 있어도 조합 가능한 메서드 수는 급격히 증가합니다.

```csharp
// Before: 조건마다 메서드 추가
public interface IProductRepository
{
    IEnumerable<Product> FindByCategory(string category);
    IEnumerable<Product> FindByPriceRange(decimal min, decimal max);
    IEnumerable<Product> FindInStock();
    IEnumerable<Product> FindByCategoryAndPriceRange(string category, decimal min, decimal max);
    IEnumerable<Product> FindInStockByCategory(string category);
    // ... 조건이 늘어날수록 메서드가 기하급수적으로 증가!
}
```

### Specification으로 해결

```csharp
// After: 단 두 개의 메서드로 모든 조건 처리
public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
```

새로운 조건이 추가되면 **새로운 Specification 클래스만 만들면 됩니다**. Repository 인터페이스는 변경할 필요가 없습니다.

## 프로젝트 설명

### 프로젝트 구조
```
RepositorySpec/                          # 메인 프로젝트
├── Product.cs                           # 도메인 모델
├── IProductRepository.cs                # Repository 인터페이스
├── Specifications/
│   ├── InStockSpec.cs                   # 재고 있는 상품
│   ├── PriceRangeSpec.cs                # 가격 범위 상품
│   └── CategorySpec.cs                  # 카테고리별 상품
├── Program.cs                           # Before/After 비교 데모
└── RepositorySpec.csproj
RepositorySpec.Tests.Unit/               # 테스트 프로젝트
├── RepositorySpecTests.cs               # 인터페이스 계약 + Spec 테스트
└── ...
```

### 핵심 코드

#### IProductRepository.cs
```csharp
public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
```

Repository는 Specification이 어떤 조건을 표현하는지 전혀 알 필요가 없습니다. Specification의 `IsSatisfiedBy` 메서드에 위임하기만 하면 됩니다.

## 한눈에 보는 정리

### Before vs After 비교
| 구분 | Before (전통적) | After (Specification) |
|------|----------------|----------------------|
| **새 조건 추가** | Repository에 메서드 추가 | Specification 클래스 추가 |
| **조건 조합** | 조합마다 별도 메서드 | 연산자(`&`, `\|`, `!`)로 조합 |
| **Repository 변경** | 조건마다 변경 필요 | 변경 불필요 |
| **테스트** | 메서드마다 테스트 | Specification 단위 테스트 |
| **Open-Closed** | 위반 (수정 필요) | 준수 (확장만 필요) |

### 관심사 분리
| 역할 | 담당 | 예시 |
|------|------|------|
| **Repository** | HOW (어디서 찾을지) | InMemory, DB, API |
| **Specification** | WHAT (무엇을 찾을지) | 재고 있는 상품, 1만원 이하 |

## FAQ

### Q1: Repository에 FindAll과 Exists 외에 다른 메서드가 필요하지 않나요?
**A**: 실제 프로젝트에서는 `Count(spec)`, `FindFirst(spec)` 등을 추가할 수 있습니다. 핵심은 **조회 조건을 메서드 시그니처가 아닌 Specification 객체로 표현**한다는 점입니다. 어떤 메서드를 추가하든 매개변수는 항상 `Specification<T>`입니다.

### Q2: 기존 Repository 패턴과 함께 사용할 수 있나요?
**A**: 네. `FindById(int id)` 같은 단순 조회는 기존 방식으로 유지하고, 복잡한 조건 조합이 필요한 조회만 Specification으로 전환하는 것이 실용적입니다.

### Q3: Specification 패턴이 과도한 설계(Over-Engineering)가 되는 경우는?
**A**: 조회 조건이 1~2개뿐이고 조합이 필요 없다면, 전통적인 메서드 방식이 더 간단합니다. 조건이 3개 이상이거나 조합이 필요할 때 Specification의 가치가 드러납니다.
