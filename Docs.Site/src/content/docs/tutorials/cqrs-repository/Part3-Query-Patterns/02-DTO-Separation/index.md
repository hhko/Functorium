---
title: "DTO 분리"
---
## 개요

하나의 `ProductDto`를 Command와 Query에서 공유하면 어떻게 될까요? 쓰기에서는 `Id`와 `CreatedAt`이 필요 없고, 목록 조회에서는 `Description` 같은 무거운 필드가 필요 없습니다. 하나의 DTO에 모든 용도를 욱여넣으면, 한 쪽의 변경이 다른 쪽에 불필요한 영향을 미칩니다. 이 장에서는 같은 도메인 엔터티에 대해 용도별로 서로 다른 DTO를 설계하는 방법을 학습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Command DTO와 Query DTO의 역할 차이를 설명할 수 있습니다
2. 입력(Request)과 출력(Response) DTO를 분리하는 이유를 이해할 수 있습니다
3. 목록(List)과 상세(Detail) Query DTO의 설계 기준을 적용할 수 있습니다
4. 도메인 엔터티를 직접 반환하지 않는 이유를 설명할 수 있습니다

---

## 핵심 개념

### "왜 필요한가?" — 하나의 DTO로 모든 것을 처리하면

```csharp
// 하나의 DTO를 Command와 Query에서 공유하면?
public record ProductDto(
    string Id,              // Command 입력에는 불필요 (서버가 생성)
    string Name,
    decimal Price,
    string Description,     // 목록 조회에는 불필요 (무거운 필드)
    string Category,
    int Stock,
    DateTime CreatedAt      // Command 입력에는 불필요 (서버가 생성)
);
```

Command 입력에서는 `Id`와 `CreatedAt`을 빈 값으로 보내야 하고, 목록 조회에서는 매번 `Description`을 전송합니다. 용도가 다르면 DTO도 달라야 합니다.

### DTO 분류 체계

용도별로 DTO를 분리하면 각각 필요한 필드만 포함할 수 있습니다.

| 분류 | DTO | 역할 |
|------|-----|------|
| Command 입력 | `CreateProductRequest` | 클라이언트 → 서버 (쓰기 데이터) |
| Command 출력 | `CreateProductResponse` | 서버 → 클라이언트 (생성 확인) |
| Query 목록 | `ProductListDto` | 리스트 뷰 (최소 필드) |
| Query 상세 | `ProductDetailDto` | 디테일 뷰 (전체 필드) |

### Command DTO vs Query DTO

두 DTO 그룹은 데이터 흐름 방향과 목적이 다릅니다.

| 구분 | Command DTO | Query DTO |
|------|------------|-----------|
| 방향 | 클라이언트 → 서버 / 서버 → 클라이언트 | 서버 → 클라이언트 |
| 목적 | 상태 변경에 필요한 데이터 운반 | 읽기에 최적화된 프로젝션 |
| 예시 | CreateProductRequest, CreateProductResponse | ProductListDto, ProductDetailDto |
| 필드 | 쓰기에 필요한 필드만 | 읽기에 필요한 필드만 |

### 목록 DTO vs 상세 DTO

- **ProductListDto는** Name, Price, Category만 포함합니다. Description 같은 무거운 필드를 제외하여 네트워크 비용을 절감합니다.
- **ProductDetailDto는** 모든 필드를 포함합니다. 단일 상품 조회 시 사용합니다.

---

## 프로젝트 설명

### Product (도메인 엔터티)

비즈니스 로직(ChangePrice, DecreaseStock)을 포함하는 Aggregate Root입니다. 이 엔터티는 클라이언트에 직접 반환되지 않습니다.

### CreateProductRequest / CreateProductResponse

Command DTO입니다. Request에는 서버가 생성하는 Id, CreatedAt이 없고, Response에는 생성 확인에 필요한 최소 필드만 포함합니다.

### ProductListDto / ProductDetailDto

Query DTO입니다. 같은 Product에 대해 목록과 상세 조회에서 서로 다른 프로젝션을 사용합니다.

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| Command 입력 DTO | 쓰기에 필요한 필드만 (서버 생성 필드 제외) |
| Command 출력 DTO | 생성 확인 최소 정보 (Id, Name, CreatedAt) |
| Query 목록 DTO | 리스트에 필요한 최소 필드 (무거운 필드 제외) |
| Query 상세 DTO | 디테일에 필요한 전체 필드 |
| 원칙 | 도메인 엔터티를 직접 반환하지 않음 |

---

## FAQ

### Q1: 하나의 DTO로 통일하면 안 되나요?
**A**: 가능하지만 비효율적입니다. 목록 조회에서 Description 같은 큰 필드를 매번 전송하면 네트워크 비용이 증가하고, Command 입력에 Id를 포함하면 클라이언트가 불필요한 값을 보내야 합니다.

### Q2: DTO가 너무 많아지면 관리가 어렵지 않나요?
**A**: DTO는 단순한 record이므로 유지보수 부담이 적습니다. 오히려 하나의 거대한 DTO를 여러 용도로 사용할 때 변경의 부수효과가 더 문제됩니다.

### Q3: 도메인 엔터티를 직접 반환하면 안 되는 이유는?
**A**: (1) 도메인 로직(ChangePrice, DecreaseStock)이 클라이언트에 노출되고, (2) ORM 프록시 객체의 지연 로딩 문제가 발생하며, (3) 읽기 최적화(필요한 컬럼만 SELECT)가 불가능합니다.

---

Command DTO와 Query DTO를 분리했습니다. 그런데 상품 10만 건을 한 번에 반환하면 클라이언트는 어떻게 될까요? 다음 장에서는 페이지네이션과 정렬을 통해 대량 데이터를 제어하는 방법을 살펴봅니다.
