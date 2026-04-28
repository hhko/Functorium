using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Products;
using CleanArchitecture.Application.Products.Create;
using CleanArchitecture.Application.Products.GetAll;
using CleanArchitecture.Application.Products.GetById;
using CleanArchitecture.Application.Products.UpdatePrice;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.WebAPI.Models;

using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandHandler<CreateProductCommand, ProductId> _createHandler;
    private readonly ICommandHandler<UpdatePriceCommand, bool> _updatePriceHandler;
    private readonly IQueryHandler<GetProductByIdQuery, ProductDto?> _getByIdHandler;
    private readonly IQueryHandler<GetAllProductsQuery, IEnumerable<ProductDto>> _getAllHandler;

    public ProductsController(
        ICommandHandler<CreateProductCommand, ProductId> createHandler,
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

        return CreatedAtAction(nameof(GetById), new { id = productId.ToString() }, new { id = productId.ToString() });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var productId = ProductId.Create(id);
        var query = new GetProductByIdQuery(productId);
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

    [HttpPut("{id}/price")]
    public async Task<IActionResult> UpdatePrice(string id, [FromBody] UpdatePriceRequest request)
    {
        var productId = ProductId.Create(id);
        var command = new UpdatePriceCommand(productId, request.NewPrice, request.Currency);
        var success = await _updatePriceHandler.HandleAsync(command);

        return success ? NoContent() : NotFound();
    }
}
