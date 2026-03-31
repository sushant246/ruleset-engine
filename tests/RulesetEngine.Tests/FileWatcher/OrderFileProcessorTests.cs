using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;
using RulesetEngine.FileWatcher;
using RulesetEngine.FileWatcher.Services;
using Xunit;

namespace RulesetEngine.Tests.FileWatcher;

public class OrderFileProcessorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IRuleEvaluationService> _mockEvaluationService;
    private readonly OrderFileProcessor _processor;

    public OrderFileProcessorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ruleset-filewatcher-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _mockEvaluationService = new Mock<IRuleEvaluationService>();

        var options = Options.Create(new FileWatcherOptions
        {
            WatchFolder = Path.Combine(_tempDir, "incoming"),
            ArchiveFolder = Path.Combine(_tempDir, "archive"),
            ErrorFolder = Path.Combine(_tempDir, "error")
        });

        _processor = new OrderFileProcessor(
            _mockEvaluationService.Object,
            options,
            NullLogger<OrderFileProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessZipAsync_ValidOrder_CallsEvaluationService()
    {
        // Arrange
        var order = CreateOrder("ZIP-001", "99999");
        var zipPath = CreateZipWithOrder(_tempDir, "test-order.zip", "order1.json", order);

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<OrderDto>()))
            .ReturnsAsync(new EvaluationResultDto { Matched = true, ProductionPlant = "US" });

        // Act
        await _processor.ProcessZipAsync(zipPath);

        // Assert
        _mockEvaluationService.Verify(s => s.EvaluateAsync(It.Is<OrderDto>(o => o.OrderId == "ZIP-001")), Times.Once);
    }

    [Fact]
    public async Task ProcessZipAsync_MultipleOrders_CallsEvaluationServiceForEach()
    {
        // Arrange
        var zipPath = Path.Combine(_tempDir, "multi.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            AddOrderEntry(archive, "order1.json", CreateOrder("MULTI-001", "A"));
            AddOrderEntry(archive, "order2.json", CreateOrder("MULTI-002", "B"));
            AddOrderEntry(archive, "order3.json", CreateOrder("MULTI-003", "C"));
        }

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<OrderDto>()))
            .ReturnsAsync(new EvaluationResultDto { Matched = false });

        // Act
        await _processor.ProcessZipAsync(zipPath);

        // Assert: evaluated three times
        _mockEvaluationService.Verify(s => s.EvaluateAsync(It.IsAny<OrderDto>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessZipAsync_AfterProcessing_ZipMovedToArchive()
    {
        // Arrange
        var order = CreateOrder("ARCHIVE-001", "99999");
        var zipPath = CreateZipWithOrder(_tempDir, "archive-test.zip", "order.json", order);

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<OrderDto>()))
            .ReturnsAsync(new EvaluationResultDto { Matched = true, ProductionPlant = "US" });

        var archiveFolder = Path.Combine(_tempDir, "archive");

        // Act
        await _processor.ProcessZipAsync(zipPath);

        // Assert: source no longer exists
        Assert.False(File.Exists(zipPath));
        // Archive folder should have the file
        Assert.True(Directory.Exists(archiveFolder));
        Assert.Single(Directory.GetFiles(archiveFolder, "*.zip"));
    }

    [Fact]
    public async Task ProcessZipAsync_InvalidZip_MovesToErrorFolder()
    {
        // Arrange: write a file that is not a valid ZIP
        var badZipPath = Path.Combine(_tempDir, "corrupted.zip");
        await File.WriteAllTextAsync(badZipPath, "this is not a zip file");

        var errorFolder = Path.Combine(_tempDir, "error");

        // Act
        await _processor.ProcessZipAsync(badZipPath);

        // Assert: moved to error folder, not crashed
        Assert.False(File.Exists(badZipPath));
        Assert.True(Directory.Exists(errorFolder));
        Assert.Single(Directory.GetFiles(errorFolder, "*.zip"));
    }

    [Fact]
    public async Task ProcessZipAsync_EmptyZip_DoesNotCallEvaluationService()
    {
        // Arrange: ZIP with no JSON entries
        var zipPath = Path.Combine(_tempDir, "empty.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("readme.txt"); // non-JSON file
        }

        // Act
        await _processor.ProcessZipAsync(zipPath);

        // Assert
        _mockEvaluationService.Verify(s => s.EvaluateAsync(It.IsAny<OrderDto>()), Times.Never);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static OrderDto CreateOrder(string orderId, string publisherNumber) => new()
    {
        OrderId = orderId,
        PublisherNumber = publisherNumber,
        OrderMethod = "POD"
    };

    private static string CreateZipWithOrder(string baseDir, string zipName, string entryName, OrderDto order)
    {
        var zipPath = Path.Combine(baseDir, zipName);
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddOrderEntry(archive, entryName, order);
        return zipPath;
    }

    private static void AddOrderEntry(ZipArchive archive, string entryName, OrderDto order)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, order);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
