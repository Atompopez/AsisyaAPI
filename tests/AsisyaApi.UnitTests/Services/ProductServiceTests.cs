using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Application.Services;
using AsisyaApi.Domain.Entities;
using Moq;

namespace AsisyaApi.UnitTests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly ProductService _sut;

    private static readonly Category Servidores = new() { Id = 1, Name = "SERVIDORES", PhotoUrl = "https://img/servidores.png" };

    public ProductServiceTests()
    {
        _sut = new ProductService(_products.Object, _categories.Object);
    }

    [Theory]
    [InlineData(0, 0, 1, ProductQueryDefaults.DefaultPageSize)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(3, 1000, 3, ProductQueryDefaults.MaxPageSize)]
    public void BuildFilter_NormalizesPaging(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var filter = ProductService.BuildFilter(new ProductQueryParameters { Page = page, PageSize = pageSize });

        Assert.Equal(expectedPage, filter.Page);
        Assert.Equal(expectedPageSize, filter.PageSize);
    }

    [Theory]
    [InlineData(null, ProductSort.Id)]
    [InlineData("price_desc", ProductSort.PriceDesc)]
    [InlineData("NAME", ProductSort.Name)]
    [InlineData("createdAt_desc", ProductSort.CreatedAtDesc)]
    public void BuildFilter_ParsesSort(string? sortBy, ProductSort expected)
    {
        var filter = ProductService.BuildFilter(new ProductQueryParameters { SortBy = sortBy });

        Assert.Equal(expected, filter.Sort);
    }

    [Fact]
    public void BuildFilter_TrimsSearchAndDropsBlank()
    {
        Assert.Equal("dell", ProductService.BuildFilter(new ProductQueryParameters { Search = "  dell " }).Search);
        Assert.Null(ProductService.BuildFilter(new ProductQueryParameters { Search = "   " }).Search);
    }

    [Fact]
    public void BuildFilter_RejectsUnknownSort()
    {
        Assert.Throws<BusinessValidationException>(() =>
            ProductService.BuildFilter(new ProductQueryParameters { SortBy = "stock" }));
    }

    [Fact]
    public void BuildFilter_RejectsInvertedPriceRange()
    {
        Assert.Throws<BusinessValidationException>(() =>
            ProductService.BuildFilter(new ProductQueryParameters { MinPrice = 100, MaxPrice = 10 }));
    }

    [Fact]
    public async Task GetPagedAsync_MapsItemsAndPagingMetadata()
    {
        var items = new List<Product>
        {
            new() { Id = 7, Name = "Rack", Price = 10, CategoryId = 1, Category = Servidores },
            new() { Id = 8, Name = "Blade", Price = 20, CategoryId = 1, Category = Servidores }
        };
        _products
            .Setup(r => r.GetPagedAsync(It.Is<ProductFilter>(f => f.Page == 2 && f.PageSize == 2 && f.CategoryId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 5));

        var result = await _sut.GetPagedAsync(new ProductQueryParameters { Page = 2, PageSize = 2, CategoryId = 1 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("SERVIDORES", result.Items[0].CategoryName);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCategoryPhoto()
    {
        _products.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = 7, Name = "Rack", CategoryId = 1, Category = Servidores });

        var result = await _sut.GetByIdAsync(7);

        Assert.Equal("SERVIDORES", result.Category.Name);
        Assert.Equal("https://img/servidores.png", result.Category.PhotoUrl);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenMissing()
    {
        _products.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_PersistsProduct_WhenCategoryExists()
    {
        _categories.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Servidores);
        Product? saved = null;
        _products.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => { p.Id = 42; saved = p; })
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateProductRequest
        {
            Name = "  Dell R740 ",
            Description = "2x Xeon",
            Price = 8500.5m,
            Stock = 3,
            CategoryId = 1
        });

        Assert.NotNull(saved);
        Assert.Equal("Dell R740", saved!.Name);
        Assert.Equal(42, result.Id);
        Assert.Equal(8500.5m, result.Price);
        Assert.Equal("SERVIDORES", result.Category.Name);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCategoryDoesNotExist()
    {
        _categories.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.CreateAsync(new CreateProductRequest { Name = "X1", CategoryId = 9 }));
        _products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges()
    {
        var cloud = new Category { Id = 2, Name = "CLOUD", PhotoUrl = "https://img/cloud.png" };
        var product = new Product { Id = 5, Name = "Old", Price = 1, Stock = 1, CategoryId = 1, Category = Servidores };
        _products.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _categories.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(cloud);

        var result = await _sut.UpdateAsync(5, new UpdateProductRequest
        {
            Name = "New",
            Description = "desc",
            Price = 99,
            Stock = 7,
            CategoryId = 2
        });

        _products.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("New", product.Name);
        Assert.Equal(2, product.CategoryId);
        Assert.Equal("CLOUD", result.Category.Name);
        Assert.Equal(99, result.Price);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenMissing()
    {
        _products.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(3));
        _products.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingProduct()
    {
        var product = new Product { Id = 3 };
        _products.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.DeleteAsync(3);

        _products.Verify(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}
