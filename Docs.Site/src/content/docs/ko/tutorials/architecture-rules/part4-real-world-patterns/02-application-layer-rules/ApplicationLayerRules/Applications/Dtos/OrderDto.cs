namespace ApplicationLayerRules.Applications.Dtos;

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}
