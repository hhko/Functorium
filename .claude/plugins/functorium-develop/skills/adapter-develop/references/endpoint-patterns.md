# Endpoint 패턴 레퍼런스

## FastEndpoints Endpoint 기본 패턴

### Create (201 Created)

```csharp
using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Commands;

public sealed class CreateProductEndpoint
    : Endpoint<CreateProductEndpoint.Request, CreateProductEndpoint.Response>
{
    private readonly IMediator _mediator;

    public CreateProductEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 생성";
            s.Description = "새로운 상품을 생성합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Endpoint Request → Usecase Request 변환
        var usecaseRequest = new CreateProductCommand.Request(
            req.Name, req.Description, req.Price, req.StockQuantity);

        // Mediator로 Usecase 호출
        var result = await _mediator.Send(usecaseRequest, ct);

        // Usecase Response → Endpoint Response 매핑
        var mapped = result.Map(r => new Response(
            r.ProductId, r.Name, r.Description, r.Price, r.StockQuantity, r.CreatedAt));

        // FinResponse를 HTTP Response로 변환
        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity);

    public new sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);
}
```

### Search (200 OK + Query Params)

```csharp
public sealed class SearchProductsEndpoint
    : Endpoint<SearchProductsEndpoint.Request, SearchProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchProductsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/products/search");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 검색";
            s.Description = "가격 범위, 페이지네이션, 정렬을 지원하는 상품 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchProductsQuery.Request(
            req.Name ?? "",
            req.MinPrice ?? 0,
            req.MaxPrice ?? 0,
            req.Page ?? 1,
            req.PageSize ?? PageRequest.DefaultPageSize,
            req.SortBy ?? "",
            req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Products.ToList(), r.TotalCount, r.Page, r.PageSize,
            r.TotalPages, r.HasNextPage, r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? Name = null,
        [property: QueryParam] decimal? MinPrice = null,
        [property: QueryParam] decimal? MaxPrice = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<ProductSummaryDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
```

## FinResponse HTTP 매핑 규칙

### 확장 메서드

```csharp
public static class FinResponseExtensions
{
    // 성공: 200 OK / 실패: 400 Bad Request
    public static async Task SendFinResponseAsync<TResponse>(
        this IEndpoint ep, FinResponse<TResponse> result, CancellationToken ct = default);

    // 성공: 201 Created / 실패: 400 Bad Request
    public static async Task SendCreatedFinResponseAsync<TResponse>(
        this IEndpoint ep, FinResponse<TResponse> result, CancellationToken ct = default);

    // 성공: 200 OK / 실패: 404 Not Found or 400 Bad Request
    public static async Task SendFinResponseWithNotFoundAsync<TResponse>(
        this IEndpoint ep, FinResponse<TResponse> result, CancellationToken ct = default);
}
```

### HTTP 상태 코드 매핑

| 시나리오 | 메서드 | 성공 코드 | 실패 코드 |
|---------|--------|----------|----------|
| 조회/수정/삭제 | `SendFinResponseAsync` | 200 OK | 400 Bad Request |
| 생성 | `SendCreatedFinResponseAsync` | 201 Created | 400 Bad Request |
| 조회 (NotFound 분기) | `SendFinResponseWithNotFoundAsync` | 200 OK | 404/400 |

### ErrorResponse DTO

```csharp
public sealed record ErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; }

    public ErrorResponse(Error error)
    {
        StatusCode = 400;
        Message = error.Message;

        if (error is ManyErrors manyErrors)
            Errors = manyErrors.Errors.Select(e => e.Message).ToList();
        else
            Errors = new[] { error.Message };
    }
}
```

## 핵심 패턴

### Endpoint → Usecase 흐름

```
1. Configure()     : Route, Auth, Summary 설정
2. HandleAsync()   : Request → Mediator.Send() → FinResponse.Map() → SendXxxFinResponseAsync()
3. Request record  : Endpoint 전용 DTO (sealed record)
4. Response record : Endpoint 전용 DTO (new sealed record)
```

### Request/Response 규칙

- **Request** - `sealed record` (Endpoint 클래스 내부 중첩)
- **Response** - `new sealed record` (Endpoint 클래스 내부 중첩, `new` 키워드로 base 숨김)
- **GET 요청** - `[property: QueryParam]` 어트리뷰트로 쿼리 파라미터 바인딩
- **POST/PUT 요청** - JSON body 자동 바인딩

### Configure() 패턴

```csharp
public override void Configure()
{
    Post("api/products");           // HTTP 메서드 + 경로
    AllowAnonymous();               // 또는 Roles("Admin")
    Summary(s =>
    {
        s.Summary = "상품 생성";     // Swagger 요약
        s.Description = "...";      // Swagger 상세
    });
}
```
