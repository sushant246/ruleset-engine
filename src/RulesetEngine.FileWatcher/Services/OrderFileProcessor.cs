using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;

namespace RulesetEngine.FileWatcher.Services;

/// <summary>
/// Processes a single ZIP file: extracts every *.json entry, evaluates each
/// order, then moves the ZIP to the archive (or error) folder.
/// </summary>
public interface IOrderFileProcessor
{
    Task ProcessZipAsync(string zipPath, CancellationToken cancellationToken = default);
}

public class OrderFileProcessor : IOrderFileProcessor
{
    private readonly IRuleEvaluationService _evaluationService;
    private readonly FileWatcherOptions _options;
    private readonly ILogger<OrderFileProcessor> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderFileProcessor(
        IRuleEvaluationService evaluationService,
        IOptions<FileWatcherOptions> options,
        ILogger<OrderFileProcessor> logger)
    {
        _evaluationService = evaluationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessZipAsync(string zipPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing ZIP: {ZipPath}", zipPath);
        var zipName = Path.GetFileName(zipPath);
        var destFolder = _options.ArchiveFolder;

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);

            var jsonEntries = archive.Entries
                .Where(e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!jsonEntries.Any())
            {
                _logger.LogWarning("ZIP {ZipName} contains no JSON files", zipName);
            }

            foreach (var entry in jsonEntries)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await ProcessEntryAsync(entry, zipName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ZIP: {ZipPath}", zipPath);
            destFolder = _options.ErrorFolder;
        }
        finally
        {
            MoveFile(zipPath, destFolder);
        }
    }

    private async Task ProcessEntryAsync(
        ZipArchiveEntry entry,
        string zipName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = entry.Open();
            var order = await JsonSerializer.DeserializeAsync<OrderDto>(stream, JsonOpts, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Entry {EntryName} in {ZipName} deserialized to null; skipping", entry.Name, zipName);
                return;
            }

            var result = await _evaluationService.EvaluateAsync(order);

            _logger.LogInformation(
                "Processed {EntryName} from {ZipName}: OrderId={OrderId} → Plant={Plant} (Matched={Matched}, FallbackUsed={FallbackUsed})",
                entry.Name, zipName,
                order.OrderId ?? "?",
                result.ProductionPlant ?? "NONE",
                result.Matched,
                result.FallbackUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing entry {EntryName} in {ZipName}", entry.Name, zipName);
        }
    }

    private void MoveFile(string sourcePath, string destinationFolder)
    {
        try
        {
            Directory.CreateDirectory(destinationFolder);
            var destPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));

            // Avoid collision: append timestamp if file already exists in destination
            if (File.Exists(destPath))
                destPath = Path.Combine(
                    destinationFolder,
                    $"{Path.GetFileNameWithoutExtension(sourcePath)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(sourcePath)}");

            File.Move(sourcePath, destPath);
            _logger.LogInformation("Moved {Source} → {Destination}", sourcePath, destPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not move {Source} to {Folder}", sourcePath, destinationFolder);
        }
    }
}
