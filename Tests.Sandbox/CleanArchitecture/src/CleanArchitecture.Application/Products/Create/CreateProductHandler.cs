using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Application.Products.Create;

public class CreateProductHandler : ICommandHandler<CreateProductCommand, ProductId>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductId> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        if (await _productRepository.ExistsAsync(command.Sku, ct))
            throw new ApplicationException($"Product with SKU '{command.Sku}' already exists");

        var price = Money.Create(command.Price, command.Currency)
            .IfFail(error => throw new ApplicationException(error.Message));

        var product = Product.Create(command.Name, command.Sku, price)
            .IfFail(error => throw new ApplicationException(error.Message));

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return product.Id;
    }
}
