using Functorium.Abstractions.Observabilities;

namespace LayeredArch.Application.Usecases.RootContexts;

[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }
