---
title: "Part 5 - Chapter 22: 카탈로그 검색"
---

> **Part 5: 도메인별 실전 예제** | [← 이전: 21장 Inventory Management →](../03-Inventory-Management) | [다음: 부록 →](../../../Appendix/)

---

## 개요

카탈로그(Catalog) 검색 도메인을 통해 3가지 페이지네이션 방식을 비교합니다. 동일한 Specification으로 Offset, Cursor, Stream 조회를 수행하며, 각 방식의 특성과 적합한 사용 시나리오를 학습합니다.

---

## 학습 목표

- **3가지 페이지네이션** 비교: Offset, Cursor, Stream
- **Specification 조합**으로 다양한 필터 조건 구현
- **동일 Specification**이 모든 Query 메서드에서 동작하는 것 확인
- 각 방식의 **성능 특성**과 **적합한 사용 시나리오** 이해

---

## 핵심 개념

### 3가지 페이지네이션 비교표

| 특성 | Search (Offset) | SearchByCursor (Keyset) | Stream |
|------|:---:|:---:|:---:|
| Total Count | O | X | X |
| 임의 페이지 접근 | O | X | X |
| Deep Page 성능 | O(N) | O(1) | N/A |
| 메모리 사용 | 페이지 단위 | 페이지 단위 | 항목 단위 |
| 적합한 시나리오 | UI 목록 | 무한 스크롤 | 배치 처리 |

### Specification 조합

```csharp
// 재고 있음 AND 가격 30,000~100,000
var spec = new InStockSpec() & new PriceRangeSpec(30_000m, 100_000m);

// 동일한 Specification으로 3가지 조회 수행
await query.Search(spec, page, sort);           // Offset
await query.SearchByCursor(spec, cursor, sort); // Cursor
query.Stream(spec, sort);                       // Stream
```

### 각 방식의 호출 패턴

```csharp
// 1. Offset: TotalCount 제공, 페이지 번호로 접근
var paged = await query.Search(spec, new PageRequest(1, 20), sort);
// paged.TotalCount, paged.TotalPages, paged.HasNextPage

// 2. Cursor: HasMore + NextCursor로 다음 페이지
var cursor = await query.SearchByCursor(spec, new CursorPageRequest(pageSize: 20), sort);
// cursor.HasMore, cursor.NextCursor → 다음 요청의 after로 전달

// 3. Stream: await foreach로 하나씩 소비
await foreach (var item in query.Stream(spec, sort, ct))
{
    Process(item); // 메모리 부담 없이 대량 처리
}
```

---

## 프로젝트 설명

### 파일 구조

| 파일 | 역할 |
|------|------|
| `ProductId.cs` | Ulid 기반 상품 식별자 |
| `Product.cs` | 카탈로그 상품 Aggregate |
| `ProductDto.cs` | Query 측 DTO |
| `InStockSpec.cs` | 재고 > 0 Specification |
| `PriceRangeSpec.cs` | 가격 범위 Specification |
| `InMemoryCatalogQuery.cs` | 3가지 Query 메서드 구현 |

---

## 한눈에 보는 정리

| 개념 | 구현 |
|------|------|
| Offset 페이지네이션 | `Search(spec, PageRequest, SortExpression)` → `PagedResult<T>` |
| Cursor 페이지네이션 | `SearchByCursor(spec, CursorPageRequest, SortExpression)` → `CursorPagedResult<T>` |
| 비동기 스트림 | `Stream(spec, SortExpression)` → `IAsyncEnumerable<T>` |
| Specification 조합 | `new InStockSpec() & new PriceRangeSpec(min, max)` |
| 통합 Query Adapter | `InMemoryCatalogQuery : InMemoryQueryBase<Product, ProductDto>` |

---

## FAQ

**Q: Offset과 Cursor를 동시에 제공하는 이유는?**
A: UI 요구사항에 따라 다릅니다. 관리자 목록(페이지 번호 필요)에는 Offset, 모바일 무한 스크롤에는 Cursor가 적합합니다. `InMemoryQueryBase`가 두 방식을 모두 구현하므로 UseCase에서 선택하면 됩니다.

**Q: Stream은 언제 사용하나요?**
A: CSV 내보내기, 데이터 마이그레이션, 통계 집계 등 전체 데이터를 순회해야 하는 배치 작업에 적합합니다. `IAsyncEnumerable<T>`로 한 건씩 yield하므로 메모리에 전체를 올리지 않습니다.

**Q: 같은 Specification이 세 방식 모두에서 동작하는 이유는?**
A: Specification은 "무엇을 필터링할 것인가"의 관심사이고, 페이지네이션은 "어떻게 결과를 나눌 것인가"의 관심사입니다. 두 관심사를 분리했기 때문에 동일한 조건을 다른 방식으로 자유롭게 조합할 수 있습니다.
