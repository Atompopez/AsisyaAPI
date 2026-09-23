using System.Net;
using System.Net.Http.Json;
using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;

namespace AsisyaApi.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ProductsEndToEndTests(ApiFactory factory)
{
    // Nombres únicos por ejecución: la tabla Categories tiene índice único por nombre.
    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    [Fact]
    public async Task CreateCategory_CreateProduct_ListReturnsProductWithCategory()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // POST /Category
        var categoryName = UniqueName("SERVIDORES");
        var categoryResponse = await client.PostAsJsonAsync("/Category",
            new { name = categoryName, photoUrl = "https://picsum.photos/seed/servidores/640/480" });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // POST /Product
        var productResponse = await client.PostAsJsonAsync("/Product", new
        {
            name = "Servidor Dell PowerEdge R740",
            description = "2x Intel Xeon, 256 GB RAM",
            price = 8500.50m,
            stock = 4,
            categoryId = category!.Id
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var created = await productResponse.Content.ReadFromJsonAsync<ProductDetailResponse>();

        // GET /Products
        var page = await client.GetFromJsonAsync<PagedResult<ProductResponse>>($"/Products?categoryId={category.Id}");
        var listed = Assert.Single(page!.Items);
        Assert.Equal(created!.Id, listed.Id);
        Assert.Equal("Servidor Dell PowerEdge R740", listed.Name);
        Assert.Equal(category.Id, listed.CategoryId);
        Assert.Equal(categoryName, listed.CategoryName);

        // GET /Products/{id} incluye la foto de la categoría
        var detail = await client.GetFromJsonAsync<ProductDetailResponse>($"/Products/{created.Id}");
        Assert.Equal("https://picsum.photos/seed/servidores/640/480", detail!.Category.PhotoUrl);
    }

    [Fact]
    public async Task UpdateAndDeleteProduct()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var category = await (await client.PostAsJsonAsync("/Category",
            new { name = UniqueName("CLOUD"), photoUrl = "https://picsum.photos/seed/cloud/640/480" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();
        var created = await (await client.PostAsJsonAsync("/Product",
            new { name = "VM pequeña", description = "", price = 20m, stock = 10, categoryId = category!.Id }))
            .Content.ReadFromJsonAsync<ProductDetailResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/Products/{created!.Id}",
            new { name = "VM mediana", description = "4 vCPU", price = 45m, stock = 5, categoryId = category.Id });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDetailResponse>();
        Assert.Equal("VM mediana", updated!.Name);
        Assert.Equal(45m, updated.Price);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Products/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Products/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task BulkLoad_InsertsProductsInBackground_AndReportsCompletion()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var category = await (await client.PostAsJsonAsync("/Category",
            new { name = UniqueName("BULK"), photoUrl = "https://picsum.photos/seed/bulk/640/480" }))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        const int count = 12_000;
        var accepted = await client.PostAsJsonAsync("/Product/bulk", new { count, categoryIds = new[] { category!.Id } });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var job = await accepted.Content.ReadFromJsonAsync<BulkJobResponse>();
        Assert.NotNull(accepted.Headers.Location);

        var deadline = DateTime.UtcNow.AddSeconds(60);
        BulkJobResponse? status;
        do
        {
            await Task.Delay(250);
            status = await client.GetFromJsonAsync<BulkJobResponse>($"/Product/bulk/{job!.JobId}");
        }
        while (status!.Status is "Pending" or "Processing" && DateTime.UtcNow < deadline);

        Assert.Equal("Completed", status.Status);
        Assert.Equal(count, status.ProcessedRecords);

        var page = await client.GetFromJsonAsync<PagedResult<ProductResponse>>($"/Products?categoryId={category.Id}&pageSize=5");
        Assert.Equal(count, page!.TotalCount);
        Assert.Equal(5, page.Items.Count);
    }

    [Theory]
    [InlineData("GET", "/Products")]
    [InlineData("GET", "/Products/1")]
    [InlineData("POST", "/Category")]
    [InlineData("POST", "/Product")]
    [InlineData("POST", "/Product/bulk")]
    public async Task ProtectedEndpoints_RequireJwt(string method, string url)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(new { }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = ApiFactory.AdminUser, password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
