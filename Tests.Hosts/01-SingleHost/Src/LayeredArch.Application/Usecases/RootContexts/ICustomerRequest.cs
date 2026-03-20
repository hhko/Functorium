using Functorium.Applications.Usecases;

namespace LayeredArch.Application.Usecases.RootContexts;

[LogEnricherRoot]
public interface ICustomerRequest { string CustomerId { get; } }
