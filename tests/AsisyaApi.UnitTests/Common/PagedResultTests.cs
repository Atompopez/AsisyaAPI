using AsisyaApi.Application.Common;

namespace AsisyaApi.UnitTests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0, false)]
    [InlineData(20, 20, 1, false)]
    [InlineData(21, 20, 2, true)]
    [InlineData(100_000, 20, 5_000, true)]
    public void ComputesTotalPagesAndNextPage(int total, int pageSize, int expectedPages, bool expectedHasNext)
    {
        var result = new PagedResult<int>([], page: 1, pageSize, total);

        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Equal(expectedHasNext, result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }
}
