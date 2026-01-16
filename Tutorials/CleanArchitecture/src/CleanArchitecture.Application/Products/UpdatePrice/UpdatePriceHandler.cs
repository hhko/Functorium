using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Application.Products.UpdatePrice;

public class UpdatePriceHandler : ICommandHandler<UpdatePriceCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePriceHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(UpdatePriceCommand command, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(command.ProductId, ct);

        if (product is null)
            return false;

        var newPrice = new Money(command.NewPrice, command.Currency);
        product.UpdatePrice(newPrice);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
