using Functorium.Applications.Observabilities;

namespace ObservabilityHost.Usecases;

[LogEnricherRoot]
public interface ICustomerRequest { string CustomerId { get; } }
