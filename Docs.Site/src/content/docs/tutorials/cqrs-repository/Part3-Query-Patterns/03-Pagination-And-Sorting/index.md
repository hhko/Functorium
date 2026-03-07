---
title: "페이지네이션과 정렬"
---
## 개요

상품 10만 건을 한 번에 반환하면 어떻게 될까요? 클라이언트는 메모리가 부족해지고, 네트워크는 병목이 되며, 사용자는 끝없이 스크롤해야 합니다. 데이터를 적절한 크기로 잘라서 전달해야 합니다. 이 장에서는 Offset 기반과 Cursor(Keyset) 기반 페이지네이션의 차이, SortExpression을 사용한 다중 필드 정렬을 학습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. PageRequest와 PagedResult로 Offset 기반 페이지네이션을 구성할 수 있습니다
2. CursorPageRequest와 CursorPagedResult로 Keyset 기반 페이지네이션을 구성할 수 있습니다
3. SortExpression의 fluent API로 다중 필드 정렬을 표현할 수 있습니다
4. Offset과 Cursor 페이지네이션의 trade-off를 판단하여 적합한 방식을 선택할 수 있습니다

---

## 핵심 개념

### "왜 필요한가?" — 전체 데이터를 한 번에 반환하면

페이지네이션 없이 전체 데이터를 반환하면, 데이터가 1,000건일 때는 괜찮지만 10만 건이 되면 응답 시간이 수십 초로 늘어납니다. 두 가지 접근법이 있습니다: **Offset은** 페이지 번호로 이동하고, **Cursor는** "이 항목 다음부터" 방식으로 탐색합니다. 각각의 장단점을 살펴볼까요?

### Offset 기반 페이지네이션

Offset 방식은 "몇 번째부터 몇 개"로 데이터를 요청합니다.

```
PageRequest(page: 2, pageSize: 10) → OFFSET 10 LIMIT 10
```

| 타입 | 속성 |
|------|------|
| `PageRequest` | Page, PageSize, Skip |
| `PagedResult<T>` | Items, TotalCount, TotalPages, HasPreviousPage, HasNextPage |

- 장점: 특정 페이지로 바로 이동 가능, UI에서 페이지 번호 표시 가능
- 단점: deep page에서 성능 저하 (OFFSET이 클수록 느림)

### Cursor(Keyset) 기반 페이지네이션

Cursor 방식은 "이 커서 다음부터 몇 개"로 데이터를 요청합니다.

```
CursorPageRequest(after: "cursor-value", pageSize: 10) → WHERE id > 'cursor-value' LIMIT 10
```

| 타입 | 속성 |
|------|------|
| `CursorPageRequest` | After, Before, PageSize |
| `CursorPagedResult<T>` | Items, NextCursor, PrevCursor, HasMore |

- 장점: deep page에서도 O(1) 성능, 실시간 데이터에 적합
- 단점: 특정 페이지로 바로 이동 불가, "다음/이전"만 가능

### SortExpression

페이지네이션과 함께 정렬도 제어해야 합니다. SortExpression은 fluent API로 다중 필드 정렬을 표현합니다.

```csharp
// 단일 필드 정렬
SortExpression.By("Name")

// 다중 필드 정렬 (fluent API)
SortExpression.By("Category").ThenBy("Price", SortDirection.Descending)

// 빈 정렬 (기본 정렬 사용)
SortExpression.Empty
```

### Offset vs Cursor 비교

어떤 방식을 선택할지는 데이터 특성과 UI 요구사항에 따라 달라집니다.

| 기준 | Offset | Cursor |
|------|--------|--------|
| Deep Page 성능 | O(N) | O(1) |
| 특정 페이지 이동 | 가능 | 불가능 |
| 실시간 데이터 | 중복/누락 가능 | 안정적 |
| SQL | LIMIT/OFFSET | WHERE + LIMIT |
| UI | 페이지 번호 | "더 보기" 버튼 |

---

## 프로젝트 설명

### PaginationDemo

PagedResult과 CursorPagedResult를 생성하는 헬퍼 메서드와 SortExpression을 적용하는 정렬 메서드를 제공합니다. InMemoryQueryBase의 동작을 단순화하여 보여줍니다.

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| PageRequest | Offset 기반 페이지네이션 요청 (Page, PageSize) |
| PagedResult | Offset 기반 결과 (TotalCount, TotalPages, HasNext/Prev) |
| CursorPageRequest | Keyset 기반 페이지네이션 요청 (After, Before, PageSize) |
| CursorPagedResult | Keyset 기반 결과 (NextCursor, PrevCursor, HasMore) |
| SortExpression | 다중 필드 정렬 표현 (By/ThenBy fluent API) |

---

## FAQ

**Q: Offset과 Cursor 중 어떤 것을 사용해야 하나요?**
A: 대부분의 관리자 페이지(게시판, 상품 목록)에서는 Offset이 적합합니다. 무한 스크롤, 실시간 피드, 대규모 데이터셋에서는 Cursor가 적합합니다. Functorium의 IQueryPort는 두 가지를 모두 지원합니다.

**Q: PageRequest에서 PageSize의 최대값이 있나요?**
A: MaxPageSize(10,000)로 제한됩니다. 이보다 큰 값을 요청하면 자동으로 MaxPageSize로 조정됩니다.

**Q: SortExpression.Empty는 언제 사용하나요?**
A: 클라이언트가 정렬을 지정하지 않은 경우 Empty를 전달하면, Query Adapter의 DefaultSortField가 적용됩니다.

---

페이지네이션과 정렬을 정의했습니다. 이제 이 인터페이스를 실제로 구현해야 합니다. Dapper 연동 전에 먼저 테스트할 수 있을까요? 다음 장에서는 InMemory Query Adapter로 DB 없이 빠르게 검증하는 방법을 살펴봅니다.
