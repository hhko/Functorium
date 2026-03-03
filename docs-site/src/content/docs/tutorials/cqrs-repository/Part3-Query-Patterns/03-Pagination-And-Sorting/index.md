---
title: "페이지네이션과 정렬"
---

> **Part 3: Query 측 — 읽기 전용 패턴** | [← 이전: 10장 DTO Separation →](../02-DTO-Separation/) | [다음: 12장 InMemory Query Adapter →](../04-InMemory-Query-Adapter/)

---

## 개요

IQueryPort의 Search와 SearchByCursor 메서드는 각각 PageRequest와 CursorPageRequest를 받아 페이지네이션된 결과를 반환합니다. 이 장에서는 Offset 기반과 Cursor(Keyset) 기반 페이지네이션의 차이, SortExpression을 사용한 다중 필드 정렬을 학습합니다.

---

## 학습 목표

- PageRequest와 PagedResult의 Offset 기반 페이지네이션 동작 이해
- CursorPageRequest와 CursorPagedResult의 Keyset 기반 페이지네이션 동작 이해
- SortExpression의 fluent API로 다중 필드 정렬 표현
- Offset vs Cursor 페이지네이션의 trade-off 파악

---

## 핵심 개념

### Offset 기반 페이지네이션

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

```csharp
// 단일 필드 정렬
SortExpression.By("Name")

// 다중 필드 정렬 (fluent API)
SortExpression.By("Category").ThenBy("Price", SortDirection.Descending)

// 빈 정렬 (기본 정렬 사용)
SortExpression.Empty
```

### Offset vs Cursor 비교

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
