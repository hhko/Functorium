using Functorium.Applications.Queries;

namespace QueryPortInterface;

/// <summary>
/// Product 전용 Query Port.
/// IQueryPort&lt;Product, ProductDto&gt;를 확장하여 Product 도메인의 읽기 전용 인터페이스를 정의합니다.
///
/// - TEntity = Product: Specification&lt;Product&gt;으로 필터링 조건을 표현
/// - TDto = ProductDto: 클라이언트에 반환할 읽기 전용 프로젝션
///
/// 세 가지 조회 방식:
/// 1. Search       → PagedResult&lt;ProductDto&gt;  (Offset 기반 페이지네이션)
/// 2. SearchByCursor → CursorPagedResult&lt;ProductDto&gt; (Keyset 기반 페이지네이션)
/// 3. Stream       → IAsyncEnumerable&lt;ProductDto&gt; (대량 데이터 스트리밍)
/// </summary>
public interface IProductQuery : IQueryPort<Product, ProductDto>
{
}
