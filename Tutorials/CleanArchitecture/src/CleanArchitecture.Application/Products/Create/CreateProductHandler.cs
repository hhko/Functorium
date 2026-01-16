using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Application.Products.Create;

public class CreateProductHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        if (await _productRepository.ExistsAsync(command.Sku, ct))
            throw new ApplicationException($"Product with SKU '{command.Sku}' already exists");

        var price = new Money(command.Price, command.Currency);
        var product = Product.Create(command.Name, command.Sku, price);

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return product.Id;
    }
}
