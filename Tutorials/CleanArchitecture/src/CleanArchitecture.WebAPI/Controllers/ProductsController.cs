using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Products;
using CleanArchitecture.Application.Products.Create;
using CleanArchitecture.Application.Products.GetAll;
using CleanArchitecture.Application.Products.GetById;
using CleanArchitecture.Application.Products.UpdatePrice;
using CleanArchitecture.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandHandler<CreateProductCommand, Guid> _createHandler;
    private readonly ICommandHandler<UpdatePriceCommand, bool> _updatePriceHandler;
    private readonly IQueryHandler<GetProductByIdQuery, ProductDto?> _getByIdHandler;
    private readonly IQueryHandler<GetAllProductsQuery, IEnumerable<ProductDto>> _getAllHandler;

    public ProductsController(
        ICommandHandler<CreateProductCommand, Guid> createHandler,
        ICommandHandler<UpdatePriceCommand, bool> updatePriceHandler,
        IQueryHandler<GetProductByIdQuery, ProductDto?> getByIdHandler,
        IQueryHandler<GetAllProductsQuery, IEnumerable<ProductDto>> getAllHandler)
    {
        _createHandler = createHandler;
        _updatePriceHandler = updatePriceHandler;
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Sku,
            request.Price,
            request.Currency);

        var productId = await _createHandler.HandleAsync(command);

        return CreatedAtAction(nameof(GetById), new { id = productId }, new { id = productId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _getByIdHandler.HandleAsync(query);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
    {
        var query = new GetAllProductsQuery(onlyActive);
        var products = await _getAllHandler.HandleAsync(query);

        return Ok(products);
    }

    [HttpPut("{id:guid}/price")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest request)
    {
        var command = new UpdatePriceCommand(id, request.NewPrice, request.Currency);
        var success = await _updatePriceHandler.HandleAsync(command);

        return success ? NoContent() : NotFound();
    }
}
