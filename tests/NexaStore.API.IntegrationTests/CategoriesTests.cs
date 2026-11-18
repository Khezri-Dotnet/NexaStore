
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using NexaStore.API.IntegrationTests.Infrastructure;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.API.IntegrationTests;

public class CategoriesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CategoriesTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsCreated()
    {
        var request = new CreateCategoryRequest(
            "Integration Test Category",
            "Category created by integration test"
        );

        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            request
        );

        var errorBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"CATEGORY ERROR: {errorBody}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.True(document.RootElement.TryGetProperty("id", out var id));
        Assert.NotEqual(Guid.Empty, id.GetGuid());
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateCategoryRequest(
            "",
            "Invalid category"
        );

        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            request
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsConflict()
    {
        var request = new CreateCategoryRequest(
            "Duplicate Test Category",
            "Duplicate test"
        );

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            request
        );

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            request
        );

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
