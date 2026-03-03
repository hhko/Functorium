---
title: "Part 5 - Chapter 21: 재고 관리"
---

> **Part 5: 도메인별 실전 예제** | [← 이전: 20장 Customer Management →](../02-Customer-Management) | [다음: 22장 Catalog Search →](../04-Catalog-Search)

---

## 개요

재고(Inventory) 도메인을 통해 ISoftDeletable 패턴과 Cursor 기반 페이지네이션을 구현합니다. 논리 삭제/복원 메커니즘과 ActiveProductSpec을 사용한 필터링을 보여주며, Cursor 페이지네이션으로 대규모 데이터 처리 패턴을 학습합니다.

---

## 학습 목표

- **ISoftDeletable** 인터페이스로 논리 삭제 구현
- **Delete() / Restore()** 메서드로 삭제/복원 상태 관리
- **ActiveProductSpec**으로 삭제된 항목 필터링
- **Cursor 기반 페이지네이션** (SearchByCursor)
- **Stream** 비동기 열거로 대량 데이터 처리

---

## 핵심 개념

### ISoftDeletable 패턴

```csharp
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    public Option<DateTime> DeletedAt { get; private set; }
    // ISoftDeletable.IsDeleted는 DeletedAt.IsSome으로 자동 계산

    public Fin<Unit> Delete()  { DeletedAt = DateTime.UtcNow; ... }
    public Fin<Unit> Restore() { DeletedAt = None; ... }
}
```

### Cursor 페이지네이션 흐름

```csharp
// 1페이지: 커서 없이 시작
var page1 = await query.SearchByCursor(spec,
    new CursorPageRequest(pageSize: 20),
    SortExpression.By("Name")).Run().RunAsync();

// 2페이지: NextCursor를 after에 전달
if (page1.HasMore)
{
    var page2 = await query.SearchByCursor(spec,
        new CursorPageRequest(after: page1.NextCursor, pageSize: 20),
        SortExpression.By("Name")).Run().RunAsync();
}
```

---

## 프로젝트 설명

### 파일 구조

| 파일 | 역할 |
|------|------|
| `ProductId.cs` | Ulid 기반 상품 식별자 |
| `Product.cs` | 상품 Aggregate Root (ISoftDeletable) |
| `ProductDto.cs` | Query 측 DTO |
| `ActiveProductSpec.cs` | 비삭제 상품 Specification |
| `IProductRepository.cs` | Repository 인터페이스 |
| `InMemoryProductRepository.cs` | InMemory Repository 구현 |
| `InMemoryProductQuery.cs` | InMemory Query Adapter (Cursor 포함) |

---

## 한눈에 보는 정리

| 개념 | 구현 |
|------|------|
| Soft Delete | `ISoftDeletable` → `DeletedAt`, `IsDeleted` |
| 삭제/복원 | `Delete()` / `Restore()` → `Fin<Unit>` |
| 활성 필터 | `ActiveProductSpec` (IsDeleted == false) |
| Cursor 페이지네이션 | `SearchByCursor(spec, cursor, sort)` |
| 비동기 스트림 | `Stream(spec, sort)` → `IAsyncEnumerable<T>` |

---

## FAQ

**Q: Soft Delete와 Hard Delete 중 어떤 것을 선택해야 하나요?**
A: 비즈니스 요구에 따라 다릅니다. 감사 추적이 필요하거나 실수로 삭제한 데이터를 복구해야 하면 Soft Delete, 개인정보 보호법(GDPR 등)에 의해 완전 삭제가 필요하면 Hard Delete를 사용합니다.

**Q: Cursor 페이지네이션이 Offset보다 좋은 이유는?**
A: Offset은 `SKIP N`으로 N개를 건너뛰므로 deep page에서 O(N) 비용이 발생합니다. Cursor는 WHERE 조건으로 시작 지점을 직접 지정하므로 페이지 깊이와 무관하게 O(1) 성능입니다.

**Q: ActiveProductSpec을 Repository가 아닌 Query에서 사용하는 이유는?**
A: Repository는 Aggregate 단위 CRUD에 집중하고, 필터링/검색은 Query 측의 역할입니다. CQRS에서 Command와 Query의 관심사를 분리하는 핵심 원칙입니다.
