using Functorium.Applications.Observabilities;

namespace LayeredArch.Application.Usecases.RootContexts;

[LogEnricherRoot]
public interface ICustomerRequest { string CustomerId { get; } }
