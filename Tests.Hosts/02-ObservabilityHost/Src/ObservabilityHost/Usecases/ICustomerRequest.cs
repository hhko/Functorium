using Functorium.Applications.Observabilities;

namespace ObservabilityHost.Usecases;

[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }
