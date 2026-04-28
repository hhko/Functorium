using LayerDependencyRules.Domains.Ports;

namespace LayerDependencyRules.Applications;

public sealed class GetProduct
{
    public sealed record Request(string Name);
    public sealed record Response(string Name);

    public sealed class Usecase
    {
        private readonly IProductRepository _repository;

        public Usecase(IProductRepository repository) => _repository = repository;

        public async Task<Response?> ExecuteAsync(Request request)
        {
            var product = await _repository.GetByNameAsync(request.Name);
            return product is null ? null : new Response(product.Name);
        }
    }
}
