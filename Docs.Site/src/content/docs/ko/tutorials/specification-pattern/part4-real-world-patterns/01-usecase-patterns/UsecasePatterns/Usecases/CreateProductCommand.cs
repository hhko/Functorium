namespace UsecasePatterns.Usecases;

public record CreateProductCommand(string Name, decimal Price, int Stock, string Category);

public class CreateProductCommandHandler
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
        => _repository = repository;

    public bool Handle(CreateProductCommand command)
    {
        // 중복 검사: Specification으로 존재 여부 확인
        var uniqueSpec = new Specifications.ProductNameUniqueSpec(command.Name);
        if (_repository.Exists(uniqueSpec))
            return false; // 이미 존재

        // 실제로는 여기서 Product 생성 및 저장
        return true;
    }
}
