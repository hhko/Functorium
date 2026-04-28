using DtoSeparation;

// ---------------------------------------------------------------
// Chapter 10: DTO Separation
// ---------------------------------------------------------------
// CQRS에서 Command DTO와 Query DTO를 분리하는 이유:
// - Command DTO: 쓰기 연산에 필요한 데이터만 포함
// - Query DTO: 읽기 연산에 최적화된 프로젝션
// - 목적에 따라 서로 다른 DTO를 사용
// ---------------------------------------------------------------

Console.WriteLine("=== Chapter 10: DTO Separation ===");
Console.WriteLine();

// 1. Command DTO — 생성 요청
var request = new CreateProductRequest(
    Name: "Mechanical Keyboard",
    Description: "Cherry MX Brown switches, RGB backlight",
    Price: 89_000m,
    Stock: 50,
    Category: "Electronics");

Console.WriteLine("[Command DTO - 입력]");
Console.WriteLine($"  CreateProductRequest: {request.Name} / {request.Price:N0}원");
Console.WriteLine();

// 2. Command DTO — 생성 응답
var response = new CreateProductResponse(
    Id: ProductId.New().ToString(),
    Name: request.Name,
    CreatedAt: DateTime.UtcNow);

Console.WriteLine("[Command DTO - 출력]");
Console.WriteLine($"  CreateProductResponse: Id={response.Id[..8]}... / {response.Name}");
Console.WriteLine();

// 3. Query DTO — 목록 조회 (최소 필드)
var listDto = new ProductListDto(
    Id: response.Id,
    Name: "Mechanical Keyboard",
    Price: 89_000m,
    Category: "Electronics");

Console.WriteLine("[Query DTO - 목록]");
Console.WriteLine($"  ProductListDto: {listDto.Name} / {listDto.Price:N0}원 / {listDto.Category}");
Console.WriteLine("  -> Description, Stock, CreatedAt 등 불필요한 필드 제외");
Console.WriteLine();

// 4. Query DTO — 상세 조회 (전체 필드)
var detailDto = new ProductDetailDto(
    Id: response.Id,
    Name: "Mechanical Keyboard",
    Description: "Cherry MX Brown switches, RGB backlight",
    Price: 89_000m,
    Stock: 50,
    Category: "Electronics",
    CreatedAt: DateTime.UtcNow,
    UpdatedAt: null);

Console.WriteLine("[Query DTO - 상세]");
Console.WriteLine($"  ProductDetailDto: {detailDto.Name}");
Console.WriteLine($"  Description: {detailDto.Description}");
Console.WriteLine($"  Stock: {detailDto.Stock} / CreatedAt: {detailDto.CreatedAt:yyyy-MM-dd}");
