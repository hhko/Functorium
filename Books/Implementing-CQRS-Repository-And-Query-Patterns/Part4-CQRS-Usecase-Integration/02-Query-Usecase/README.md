# Part 4 - Chapter 15: Query Usecase

> **Part 4: CQRS Usecase 통합** | [← 이전: 14장 Command Usecase](../01-Command-Usecase/) | [다음: 16장 FinT to FinResponse →](../03-FinT-To-FinResponse/)

---

## 개요

Query Usecase는 CQRS의 Query 측면에서 읽기 전용 비즈니스 로직을 실행하는 패턴입니다. Command가 상태를 변경하는 반면, Query는 데이터를 조회만 합니다. `IQueryRequest<TSuccess>`로 요청을 정의하고, `IQueryUsecase<TQuery, TSuccess>`로 처리 로직을 구현합니다.

---

## 학습 목표

- **IQueryRequest / IQueryUsecase** 인터페이스의 역할 이해
- **Query 전용 인터페이스**: Repository가 아닌 Query Port를 통한 읽기 분리
- **DTO 기반 응답**: 도메인 엔티티 대신 DTO를 반환하는 패턴
- **Command와 Query의 구조적 차이** 이해

---

## 핵심 개념

### Command vs Query Usecase

| 구분 | Command | Query |
|------|---------|-------|
| 목적 | 상태 변경 | 데이터 조회 |
| 인터페이스 | `ICommandRequest<T>` | `IQueryRequest<T>` |
| 핸들러 | `ICommandUsecase` | `IQueryUsecase` |
| 데이터 소스 | Repository (Aggregate) | Query Port (DTO) |
| 트랜잭션 | SaveChanges 자동 호출 | 트랜잭션 없음 |

### Query Port 패턴

```csharp
// Query 전용 인터페이스 - Repository와 분리
public interface IProductQuery
{
    FinT<IO, List<ProductDto>> SearchByName(string keyword);
    FinT<IO, ProductDto> GetById(ProductId id);
}
```

Query Port는 도메인 엔티티가 아닌 DTO를 직접 반환합니다. 이는 읽기 성능 최적화와 관심사 분리를 동시에 달성합니다.

---

## 프로젝트 설명

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot 기반 상품 엔티티 |
| `ProductDto.cs` | 조회 전용 DTO |
| `IProductQuery.cs` | Query Port 인터페이스 |
| `InMemoryProductQuery.cs` | InMemory Query 어댑터 구현 |
| `SearchProductsQuery.cs` | Query Usecase 패턴 (Request, Response, Usecase) |
| `Program.cs` | 실행 데모 |

---

## 한눈에 보는 정리

| 개념 | 설명 |
|------|------|
| `IQueryRequest<T>` | Query 요청 마커 (Mediator IQuery 확장) |
| `IQueryUsecase<TQuery, T>` | Query 핸들러 (Mediator IQueryHandler 확장) |
| Query Port | 읽기 전용 데이터 접근 인터페이스 |
| DTO | 도메인 엔티티 대신 반환하는 조회 전용 데이터 |

---

## FAQ

**Q: 왜 Repository 대신 별도의 Query Port를 사용하나요?**
A: Repository는 Aggregate Root 단위의 CRUD에 초점을 맞추지만, Query는 여러 테이블을 조인하거나 집계하는 등 읽기에 최적화된 별도의 경로가 필요합니다. CQRS의 핵심은 이 읽기/쓰기 경로의 분리입니다.

**Q: Query Usecase에서도 FinT를 사용하는 이유는?**
A: 데이터 조회도 실패할 수 있기 때문입니다 (not found, DB 연결 오류 등). FinT를 사용하면 Command와 동일한 합성 패턴으로 에러를 처리할 수 있습니다.
