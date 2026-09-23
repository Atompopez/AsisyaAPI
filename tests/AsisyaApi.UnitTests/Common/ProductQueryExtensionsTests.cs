using AsisyaApi.Application.Common;
using AsisyaApi.Domain.Entities;

namespace AsisyaApi.UnitTests.Common;

public class ProductQueryExtensionsTests
{
    private static readonly List<Product> Products =
    [
        new() { Id = 1, Name = "Servidor Dell", Description = "Rack 2U", Price = 5000, CategoryId = 1 },
        new() { Id = 2, Name = "Servidor HP", Description = "Blade", Price = 3000, CategoryId = 1 },
        new() { Id = 3, Name = "VM Small", Description = "Instancia dell cloud", Price = 20, CategoryId = 2 },
        new() { Id = 4, Name = "VM Large", Description = "Instancia", Price = 200, CategoryId = 2 },
        new() { Id = 5, Name = "Storage", Description = "S3 compatible", Price = 50, CategoryId = 2 }
    ];

    private static ProductFilter Filter(
        string? search = null, int? categoryId = null, decimal? min = null, decimal? max = null,
        ProductSort sort = ProductSort.Id, int page = 1, int pageSize = 10) =>
        new(page, pageSize, search, categoryId, min, max, sort);

    [Fact]
    public void Search_IsCaseInsensitive_AndMatchesNameOrDescription()
    {
        var ids = Products.AsQueryable().ApplyFilter(Filter(search: "DELL")).Select(p => p.Id).ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void CategoryAndPriceRange_AreCombined()
    {
        var ids = Products.AsQueryable()
            .ApplyFilter(Filter(categoryId: 2, min: 30, max: 200))
            .Select(p => p.Id)
            .ToList();

        Assert.Equal([4, 5], ids);
    }

    [Fact]
    public void Sort_PriceDesc_OrdersByPriceDescending()
    {
        var ids = Products.AsQueryable().ApplySort(ProductSort.PriceDesc).Select(p => p.Id).ToList();

        Assert.Equal([1, 2, 4, 5, 3], ids);
    }

    [Fact]
    public void Paging_SkipsPreviousPages()
    {
        var ids = Products.AsQueryable().ApplySort(ProductSort.Id).ApplyPaging(page: 2, pageSize: 2).Select(p => p.Id).ToList();

        Assert.Equal([3, 4], ids);
    }
}
