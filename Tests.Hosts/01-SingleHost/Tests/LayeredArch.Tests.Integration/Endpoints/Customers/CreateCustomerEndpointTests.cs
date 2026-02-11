using LayeredArch.Application.Usecases.Customers;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Customers;

public class CreateCustomerEndpointTests : IntegrationTestBase
{
    public CreateCustomerEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateCustomer_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"Customer {Guid.NewGuid()}",
            Email = $"{Guid.NewGuid()}@example.com",
            CreditLimit = 5000.00m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateCustomerCommand.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(request.Name);
        result.Email.ShouldBe(request.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = "",
            Email = $"{Guid.NewGuid()}@example.com",
            CreditLimit = 5000.00m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturn400BadRequest_WhenDuplicateEmail()
    {
        // Arrange
        var email = $"{Guid.NewGuid()}@example.com";
        var request = new
        {
            Name = $"Customer {Guid.NewGuid()}",
            Email = email,
            CreditLimit = 5000.00m
        };

        // Create first customer
        var firstResponse = await Client.PostAsJsonAsync("/api/customers", request, TestContext.Current.CancellationToken);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act - try to create duplicate
        var duplicateRequest = new
        {
            Name = $"Customer {Guid.NewGuid()}",
            Email = email,
            CreditLimit = 3000.00m
        };
        var response = await Client.PostAsJsonAsync("/api/customers", duplicateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
