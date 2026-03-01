using Functorium.Applications.Queries;
using QueryPortInterface;

// ---------------------------------------------------------------
// Chapter 9: IQueryPort Interface
// ---------------------------------------------------------------
// IQueryPort<TEntity, TDto>는 CQRS의 Query 측 포트입니다.
// - TEntity: 도메인 엔터티 (Specification 필터링 대상)
// - TDto: 읽기 전용 프로젝션 (클라이언트 반환용)
//
// 세 가지 조회 메서드:
// 1. Search        → Offset 기반 페이지네이션 (PagedResult<TDto>)
// 2. SearchByCursor → Keyset 기반 페이지네이션 (CursorPagedResult<TDto>)
// 3. Stream        → 대량 데이터 스트리밍 (IAsyncEnumerable<TDto>)
// ---------------------------------------------------------------

Console.WriteLine("=== Chapter 9: IQueryPort Interface ===");
Console.WriteLine();

// ProductDto 생성 예시
var dto = new ProductDto(
    Id: ProductId.New().ToString(),
    Name: "Mechanical Keyboard",
    Price: 89_000m,
    Stock: 50,
    Category: "Electronics");

Console.WriteLine($"ProductDto: {dto.Name} / {dto.Price:N0}원 / 재고: {dto.Stock}");
Console.WriteLine();

// IQueryPort<Product, ProductDto>의 메서드 시그니처 설명
Console.WriteLine("[IQueryPort<Product, ProductDto> 메서드]");
Console.WriteLine("  Search(spec, page, sort)       → FinT<IO, PagedResult<ProductDto>>");
Console.WriteLine("  SearchByCursor(spec, cursor, sort) → FinT<IO, CursorPagedResult<ProductDto>>");
Console.WriteLine("  Stream(spec, sort)             → IAsyncEnumerable<ProductDto>");
Console.WriteLine();

// 각 반환 타입의 의미
Console.WriteLine("[반환 타입 비교]");
Console.WriteLine("  PagedResult<T>       : Offset 기반 — TotalCount, TotalPages, HasNext/PreviousPage");
Console.WriteLine("  CursorPagedResult<T> : Keyset 기반 — NextCursor, PrevCursor, HasMore");
Console.WriteLine("  IAsyncEnumerable<T>  : 스트리밍 — 메모리에 전체 적재 없이 yield");
