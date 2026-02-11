using LayeredArch.Application.Usecases.Customers;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Customers;

public class GetCustomerByIdEndpointTests : IntegrationTestBase
{
    public GetCustomerByIdEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetCustomerById_ShouldReturn200Ok_WhenCustomerExists()
    {
        // Arrange - Create a customer first
        var createRequest = new
        {
            Name = $"Customer {Guid.NewGuid()}",
            Email = $"{Guid.NewGuid()}@example.com",
            CreditLimit = 5000.00m
        };
        var createResponse = await Client.PostAsJsonAsync("/api/customers", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateCustomerCommand.Response>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/customers/{created.CustomerId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetCustomerByIdQuery.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturnNotFoundOrBadRequest_WhenCustomerDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/customers/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }
}
