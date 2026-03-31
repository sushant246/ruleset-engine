using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RulesetEngine.FileWatcher.Services;

namespace RulesetEngine.FileWatcher;

/// <summary>
/// Long-running background service that watches a configured folder for
/// incoming *.zip files and dispatches each to <see cref="IOrderFileProcessor"/>.
/// </summary>
public sealed class ZipOrderWatcherService : BackgroundService
{
    private readonly IOrderFileProcessor _processor;
    private readonly FileWatcherOptions _options;
    private readonly ILogger<ZipOrderWatcherService> _logger;
    private FileSystemWatcher? _watcher;

    public ZipOrderWatcherService(
        IOrderFileProcessor processor,
        IOptions<FileWatcherOptions> options,
        ILogger<ZipOrderWatcherService> logger)
    {
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_options.WatchFolder);
        Directory.CreateDirectory(_options.ArchiveFolder);
        Directory.CreateDirectory(_options.ErrorFolder);

        _logger.LogInformation("FileWatcher watching: {Folder}", Path.GetFullPath(_options.WatchFolder));

        // Process any ZIP files already present before the watcher started
        foreach (var existing in Directory.GetFiles(_options.WatchFolder, "*.zip"))
        {
            _ = SafeProcessAsync(existing, stoppingToken);
        }

        _watcher = new FileSystemWatcher(_options.WatchFolder, "*.zip")
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        _watcher.Created += (_, e) => _ = SafeProcessAsync(e.FullPath, stoppingToken);

        stoppingToken.Register(() =>
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation("FileWatcher stopped");
        });

        return Task.CompletedTask;
    }

    private async Task SafeProcessAsync(string path, CancellationToken cancellationToken)
    {
        // Brief delay to allow the file to be fully written before we open it
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);

        if (!File.Exists(path))
            return;

        try
        {
            await _processor.ProcessZipAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing {Path}", path);
        }
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
