using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Application.Services;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Domain.Enums;
using Moq;

namespace AsisyaApi.UnitTests.Services;

public class BulkLoadServiceTests
{
    private readonly Mock<IBulkJobRepository> _jobs = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IBulkLoadQueue> _queue = new();
    private readonly Mock<IBulkProductWriter> _writer = new();
    private readonly BulkLoadService _sut;

    public BulkLoadServiceTests()
    {
        _sut = new BulkLoadService(_jobs.Object, _categories.Object, _queue.Object, _writer.Object);
    }

    [Fact]
    public async Task StartAsync_CreatesPendingJob_AndEnqueuesAllCategories()
    {
        _categories.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Category { Id = 1, Name = "SERVIDORES" },
            new Category { Id = 2, Name = "CLOUD" }
        ]);
        BulkLoadWorkItem? enqueued = null;
        _queue.Setup(q => q.EnqueueAsync(It.IsAny<BulkLoadWorkItem>(), It.IsAny<CancellationToken>()))
            .Callback<BulkLoadWorkItem, CancellationToken>((item, _) => enqueued = item)
            .Returns(ValueTask.CompletedTask);

        var response = await _sut.StartAsync(new BulkProductRequest { Count = 100_000 });

        Assert.Equal(nameof(BulkJobStatus.Pending), response.Status);
        Assert.Equal(100_000, response.TotalRecords);
        Assert.Equal(0, response.ProcessedRecords);
        _jobs.Verify(r => r.AddAsync(It.Is<BulkJob>(j => j.Id == response.JobId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(enqueued);
        Assert.Equal(response.JobId, enqueued!.JobId);
        Assert.Equal([1, 2], enqueued.CategoryIds);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenThereAreNoCategories()
    {
        _categories.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<BusinessValidationException>(() => _sut.StartAsync(new BulkProductRequest { Count = 10 }));
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<BulkLoadWorkItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenRequestedCategoryDoesNotExist()
    {
        _categories.Setup(r => r.GetExistingIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1]);

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.StartAsync(new BulkProductRequest { Count = 10, CategoryIds = [1, 7] }));
        Assert.Contains("7", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(BulkProductRequest.MaxCount + 1)]
    public async Task StartAsync_RejectsOutOfRangeCount(int count)
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() => _sut.StartAsync(new BulkProductRequest { Count = count }));
    }

    [Fact]
    public async Task ProcessAsync_WritesInBatches_ReportsProgress_AndCompletes()
    {
        var job = new BulkJob { Id = Guid.NewGuid(), Status = BulkJobStatus.Pending, TotalRecords = 12_000 };
        _jobs.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var batchSizes = new List<int>();
        _writer.Setup(w => w.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<Product>, CancellationToken>((products, _) =>
            {
                var count = products.Count();
                batchSizes.Add(count);
                return Task.FromResult((ulong)count);
            });

        var progress = new List<(BulkJobStatus Status, int Processed)>();
        _jobs.Setup(r => r.UpdateAsync(job, It.IsAny<CancellationToken>()))
            .Callback<BulkJob, CancellationToken>((j, _) => progress.Add((j.Status, j.ProcessedRecords)))
            .Returns(Task.CompletedTask);

        await _sut.ProcessAsync(new BulkLoadWorkItem(job.Id, 12_000, [1, 2]));

        Assert.Equal([5_000, 5_000, 2_000], batchSizes);
        Assert.Equal(
            [
                (BulkJobStatus.Processing, 0),
                (BulkJobStatus.Processing, 5_000),
                (BulkJobStatus.Processing, 10_000),
                (BulkJobStatus.Processing, 12_000),
                (BulkJobStatus.Completed, 12_000)
            ],
            progress);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public async Task ProcessAsync_MarksJobFailed_WhenWriterThrows()
    {
        var job = new BulkJob { Id = Guid.NewGuid(), Status = BulkJobStatus.Pending, TotalRecords = 8_000 };
        _jobs.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _writer.SetupSequence(w => w.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5_000UL)
            .ThrowsAsync(new InvalidOperationException("conexión perdida"));

        await _sut.ProcessAsync(new BulkLoadWorkItem(job.Id, 8_000, [1]));

        Assert.Equal(BulkJobStatus.Failed, job.Status);
        Assert.Equal(5_000, job.ProcessedRecords);
        Assert.Equal("conexión perdida", job.ErrorMessage);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public async Task ProcessAsync_IgnoresJobsThatAreNotPending()
    {
        var job = new BulkJob { Id = Guid.NewGuid(), Status = BulkJobStatus.Completed };
        _jobs.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);

        await _sut.ProcessAsync(new BulkLoadWorkItem(job.Id, 10, [1]));

        _writer.Verify(w => w.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GenerateProducts_DistributesCategoriesRoundRobin_WithValidValues()
    {
        var products = BulkLoadService.GenerateProducts(startIndex: 0, count: 6, categoryIds: [1, 2]).ToList();

        Assert.Equal([1, 2, 1, 2, 1, 2], products.Select(p => p.CategoryId));
        Assert.All(products, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.InRange(p.Price, 10m, 10_000m);
            Assert.InRange(p.Stock, 0, 500);
        });
    }

    [Fact]
    public async Task GetStatusAsync_ThrowsNotFound_ForUnknownJob()
    {
        _jobs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((BulkJob?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetStatusAsync(Guid.NewGuid()));
    }
}
