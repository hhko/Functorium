using Functorium.Abstractions.Observabilities;

namespace ObservabilityHost.Usecases;

[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }
