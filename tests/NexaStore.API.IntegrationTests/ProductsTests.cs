
using System.Net;
using System.Net.Http.Json;

using NexaStore.API.IntegrationTests.Infrastructure;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.API.IntegrationTests;

public class ProductsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        var request = new CreateProductRequest(
            "Test Product",
            "Integration Test",
            100,
            10,
            Guid.NewGuid());

        var response = await _client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProducts_InvalidPageNumber_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/api/products?pageNumber=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProducts_InvalidPageSize_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/api/products?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProducts_InvalidPriceRange_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/api/products?minPrice=500&maxPrice=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
