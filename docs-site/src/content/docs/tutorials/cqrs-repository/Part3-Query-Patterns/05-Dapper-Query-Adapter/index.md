---
title: "Dapper 쿼리 어댑터"
---
## 개요

DapperQueryBase<TEntity, TDto>는 Dapper를 사용한 SQL 기반 Query Adapter의 공통 인프라입니다. InMemoryQueryBase가 메모리 내 데이터를 LINQ로 처리하는 것과 달리, DapperQueryBase는 SQL 쿼리를 조합하여 데이터베이스에 직접 실행합니다. 이 장에서는 실제 DB 없이 SqlQueryBuilder를 통해 SQL 생성 패턴을 학습합니다.

---

## 학습 목표

- DapperQueryBase의 Template Method 패턴과 서브클래스가 구현할 항목 이해
- Offset 기반과 Cursor 기반 페이지네이션의 SQL 차이 파악
- AllowedSortColumns를 사용한 SQL Injection 방지 방법 이해
- InMemoryQueryBase와 DapperQueryBase의 구조적 대칭 이해

---

## 핵심 개념

### DapperQueryBase 서브클래스 구현 항목

| 추상 멤버 | 역할 | 예시 |
|-----------|------|------|
| `SelectSql` | SELECT 쿼리 본문 | `SELECT p.id, p.name FROM products p` |
| `CountSql` | COUNT 쿼리 본문 | `SELECT COUNT(*) FROM products p` |
| `DefaultOrderBy` | 기본 정렬 절 | `p.name ASC` |
| `AllowedSortColumns` | 허용된 정렬 컬럼 매핑 | `{ "Name": "p.name" }` |
| `BuildWhereClause` | Specification → SQL WHERE | `WHERE p.stock > 0` |

### Offset vs Cursor SQL 패턴

```sql
-- Offset 기반
SELECT * FROM products WHERE stock > 0 ORDER BY name ASC LIMIT 10 OFFSET 20

-- Cursor 기반
SELECT * FROM products WHERE stock > 0 AND id > @CursorValue ORDER BY id LIMIT 10
```

### AllowedSortColumns의 역할

클라이언트가 보내는 정렬 필드명(예: "Name")을 실제 SQL 컬럼명(예: "p.name")으로 매핑합니다. 매핑에 없는 필드는 거부되어 SQL Injection을 방지합니다.

### InMemoryQueryBase vs DapperQueryBase

| 구분 | InMemoryQueryBase | DapperQueryBase |
|------|-------------------|-----------------|
| 데이터 소스 | ConcurrentDictionary (메모리) | IDbConnection (SQL DB) |
| 필터링 | Specification.IsSatisfiedBy (C#) | BuildWhereClause (SQL WHERE) |
| 정렬 | SortSelector (C# 함수) | AllowedSortColumns (SQL ORDER BY) |
| 프로젝션 | LINQ Select (C#) | SelectSql (SQL SELECT) |
| 용도 | 테스트, 프로토타이핑 | 프로덕션 |

---

## 프로젝트 설명

### SqlQueryBuilder

DapperQueryBase가 내부적으로 수행하는 SQL 조합 패턴을 단순화하여 보여줍니다. 실제 DapperQueryBase는 Dapper의 DynamicParameters와 QueryMultipleAsync를 사용하지만, 이 장에서는 SQL 문자열 생성에 집중합니다.

- `BuildSelectWithPagination`: Offset 기반 SELECT 쿼리 (LIMIT/OFFSET)
- `BuildSelectWithCursor`: Cursor 기반 SELECT 쿼리 (WHERE + LIMIT)
- `BuildCount`: COUNT 쿼리
- `BuildOrderBy`: AllowedSortColumns 매핑을 적용한 ORDER BY 절

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| DapperQueryBase | SQL 기반 Query Adapter의 공통 베이스 |
| SelectSql / CountSql | 서브클래스가 선언하는 SQL 본문 |
| BuildWhereClause | Specification → SQL WHERE 변환 |
| AllowedSortColumns | 클라이언트 필드명 → SQL 컬럼명 매핑 (Injection 방지) |
| PaginationClause | LIMIT/OFFSET (오버라이드로 DB 방언 지원) |

---

## FAQ

**Q: DapperQueryBase를 직접 사용하지 않고 SqlQueryBuilder로 학습하는 이유는?**
A: DapperQueryBase는 실제 DB 연결(IDbConnection)이 필요합니다. 이 장에서는 DB 없이 SQL 생성 패턴의 핵심 개념을 학습합니다. 실제 프로덕션 구현은 DapperQueryBase를 상속하여 사용합니다.

**Q: BuildWhereClause에서 Specification을 SQL로 어떻게 변환하나요?**
A: DapperSpecTranslator를 주입하면 자동으로 변환됩니다. 또는 서브클래스에서 BuildWhereClause를 직접 오버라이드하여 Specification 타입별로 SQL을 생성할 수 있습니다.

**Q: SQL Server에서도 사용할 수 있나요?**
A: PaginationClause와 CursorPaginationClause를 오버라이드하면 됩니다. 기본값은 PostgreSQL/SQLite 스타일(LIMIT/OFFSET)이지만, SQL Server의 OFFSET FETCH나 TOP으로 교체할 수 있습니다.
