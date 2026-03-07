---
title: "유스케이스 패턴"
---

## 개요

Specification을 정의하고, Expression으로 변환하고, Repository와 통합하는 방법까지 배웠습니다. 그렇다면 실제 애플리케이션에서 Specification은 어디에서, 어떻게 사용될까요? 이 장에서는 CQRS 패턴에서 Specification이 활용되는 두 가지 핵심 시나리오 — Command의 존재 검사와 Query의 동적 필터 — 를 살펴봅니다.

## 학습 목표

1. **Command에서 Specification 활용** - `Exists(spec)`를 통한 중복 검사 패턴을 적용할 수 있습니다
2. **Query에서 Specification 활용** - `Specification<T>.All`을 초기값으로 사용하는 동적 필터 조합을 구현할 수 있습니다
3. **CQRS에서의 역할 분리** - Command(존재 검사)와 Query(검색 필터)에서 Specification의 역할 차이를 설명할 수 있습니다

## 핵심 개념

### Command: 존재 검사

먼저 Command 쪽부터 살펴보겠습니다. Command Usecase에서는 비즈니스 규칙 검증을 위해 Specification을 사용합니다. 예를 들어, 상품 생성 시 동일한 이름의 상품이 이미 존재하는지 확인합니다.

```csharp
var uniqueSpec = new ProductNameUniqueSpec(command.Name);
if (_repository.Exists(uniqueSpec))
    return false; // 이미 존재
```

### Query: 검색 필터

이번에는 Query 쪽입니다. Query Usecase에서는 `Specification<T>.All`을 초기값으로 사용하여 선택적 필터를 점진적으로 조합합니다.

```csharp
var spec = Specification<Product>.All;

if (query.Category is not null)
    spec &= new ProductCategorySpec(query.Category);
if (query.MinPrice.HasValue && query.MaxPrice.HasValue)
    spec &= new ProductPriceRangeSpec(query.MinPrice.Value, query.MaxPrice.Value);

return _repository.FindAll(spec);
```

## 프로젝트 설명

### 프로젝트 구조

```
UsecasePatterns/
├── Product.cs                              # 상품 레코드
├── IProductRepository.cs                   # Repository 인터페이스
├── InMemoryProductRepository.cs            # InMemory 구현
├── Specifications/
│   ├── ProductNameUniqueSpec.cs             # 이름 중복 검사
│   ├── ProductInStockSpec.cs                      # 재고 있음
│   ├── ProductPriceRangeSpec.cs                   # 가격 범위
│   └── ProductCategorySpec.cs                     # 카테고리 필터
├── Usecases/
│   ├── CreateProductCommand.cs             # Command: 상품 생성
│   └── SearchProductsQuery.cs              # Query: 상품 검색
└── Program.cs                              # 데모 실행
```

## 한눈에 보는 정리

Command와 Query에서 Specification이 활용되는 방식을 비교합니다.

| 구분 | Command | Query |
|------|---------|-------|
| **목적** | 비즈니스 규칙 검증 | 데이터 검색/필터링 |
| **Repository 메서드** | `Exists(spec)` | `FindAll(spec)` |
| **Specification 사용** | 단일 Spec | `All` + `&=` 조합 |
| **예시** | 이름 중복 검사 | 카테고리 + 가격 + 재고 필터 |

## FAQ

### Q1: 왜 Command에서 직접 이름을 비교하지 않고 Specification을 사용하나요?
**A**: Specification으로 캡슐화하면 동일한 중복 검사 로직을 다른 Usecase에서도 재사용할 수 있습니다. 또한 Repository 인터페이스에 `ExistsByName` 같은 특수 메서드를 추가하지 않아도 되어, Repository 메서드 폭발을 방지합니다.

### Q2: Query에서 `null` 대신 `Specification<T>.All`을 초기값으로 사용하는 이유는 무엇인가요?
**A**: `All`을 사용하면 null 체크 없이 `&=` 연산자로 점진적 조합이 가능합니다. `All & X = X`의 항등원 성질 덕분에, 필터가 하나도 없으면 `All`이 그대로 반환되어 전체 조회로 동작합니다.

### Q3: Command와 Query에서 같은 Specification을 공유해도 되나요?
**A**: 네, 동일한 도메인 조건이라면 공유할 수 있습니다. Specification은 도메인 레이어에 속하며, Command와 Query 모두 같은 도메인 조건을 참조합니다.

---

이 장에서 본 Query의 `All &= ...` 패턴은 간단한 경우에는 충분하지만, 필터 조건이 많아지면 Usecase 코드가 복잡해집니다. 다음 장에서는 이 패턴을 독립적인 빌더 클래스로 발전시켜, 필터 로직을 깔끔하게 캡슐화하는 방법을 다룹니다.

→ [14장: 동적 필터 빌더](../02-Dynamic-Filter-Builder/)
