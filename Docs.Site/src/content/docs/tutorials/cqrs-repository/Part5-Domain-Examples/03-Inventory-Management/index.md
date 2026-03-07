---
title: "재고 관리"
---
## 개요

상품을 삭제하면 관련 주문 이력도 함께 사라질까요? 물리적으로 삭제하면 참조 무결성이 깨지고, 과거 데이터를 복구할 수 없습니다. **데이터를 보존하면서 삭제된 것처럼 동작하게** 만들 수는 없을까요?

이 장에서는 재고(Inventory) 도메인을 통해 ISoftDeletable 패턴과 Cursor 기반 페이지네이션을 구현합니다. 논리 삭제/복원 메커니즘과 ActiveProductSpec을 사용한 필터링을 보여주며, Cursor 페이지네이션으로 대규모 데이터 처리 패턴을 학습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **ISoftDeletable** 인터페이스로 논리 삭제를 구현할 수 있습니다
2. **Delete() / Restore()** 메서드로 삭제/복원 상태를 관리할 수 있습니다
3. **ActiveProductSpec**으로 삭제된 항목을 필터링할 수 있습니다
4. **Cursor 기반 페이지네이션** (SearchByCursor)을 구현할 수 있습니다
5. **Stream** 비동기 열거로 대량 데이터를 처리할 수 있습니다

---

## 핵심 개념

### ISoftDeletable 패턴

물리적 DELETE 대신 `DeletedAt`을 설정하면 데이터를 보존하면서 삭제 상태를 표현할 수 있습니다. 복원도 간단합니다.

```csharp
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    public Option<DateTime> DeletedAt { get; private set; }
    // ISoftDeletable.IsDeleted는 DeletedAt.IsSome으로 자동 계산

    public Fin<Unit> Delete()  { DeletedAt = DateTime.UtcNow; ... }
    public Fin<Unit> Restore() { DeletedAt = None; ... }
}
```

`ActiveProductSpec`으로 삭제되지 않은 상품만 조회하면, 애플리케이션 코드에서는 삭제 여부를 의식하지 않아도 됩니다.

### Cursor 페이지네이션 흐름

대규모 데이터를 페이지 단위로 조회할 때, Cursor 방식은 페이지 깊이와 무관하게 일정한 성능을 보장합니다. 첫 페이지는 커서 없이 시작하고, 이후 `NextCursor`를 전달하여 다음 페이지를 가져옵니다.

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

각 파일이 소프트 삭제와 페이지네이션에서 어떤 역할을 하는지 확인하세요.

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

이 예제에서 사용된 소프트 삭제와 페이지네이션 패턴을 정리하면 다음과 같습니다.

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

---

재고 관리와 소프트 삭제를 구현했습니다. 마지막으로, 같은 데이터를 Offset, Cursor, Stream 세 가지 방식으로 조회해야 한다면 어떤 것을 선택해야 할까요? 다음 장에서 **세 가지 페이지네이션을 비교**합니다.
