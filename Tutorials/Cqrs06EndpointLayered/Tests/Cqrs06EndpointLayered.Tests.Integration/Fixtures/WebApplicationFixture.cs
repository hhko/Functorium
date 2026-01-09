using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs06EndpointLayered.Tests.Integration.Fixtures;

public class WebApplicationFixture : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;

    public WebApplicationFixture(WebApplicationFactory<Program> factory)
    {
        Client = factory.CreateClient();
    }
}
