using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Application.Services;
using AsisyaApi.Domain.Entities;
using Moq;

namespace AsisyaApi.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categories.Object);
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsDto()
    {
        _categories.Setup(r => r.ExistsByNameAsync("CLOUD", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categories.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => c.Id = 2)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateCategoryRequest { Name = " CLOUD ", PhotoUrl = "https://img/cloud.png" });

        Assert.Equal(new CategoryResponse(2, "CLOUD", "https://img/cloud.png"), result);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenNameExists()
    {
        _categories.Setup(r => r.ExistsByNameAsync("SERVIDORES", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(new CreateCategoryRequest { Name = "SERVIDORES", PhotoUrl = "https://img/s.png" }));
        _categories.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
