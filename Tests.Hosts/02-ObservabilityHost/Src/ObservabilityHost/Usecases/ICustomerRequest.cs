using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

[LogEnricherRoot]
public interface ICustomerRequest { string CustomerId { get; } }
