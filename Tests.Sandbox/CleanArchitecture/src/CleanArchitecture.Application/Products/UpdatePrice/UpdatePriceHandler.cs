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

        var newPrice = Money.Create(command.NewPrice, command.Currency)
            .IfFail(error => throw new ApplicationException(error.Message));

        var result = product.UpdatePrice(newPrice);
        result.IfFail(error => throw new ApplicationException(error.Message));

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
