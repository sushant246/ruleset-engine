namespace RulesetEngine.FileWatcher;

public class FileWatcherOptions
{
    public const string SectionName = "FileWatcher";

    public string WatchFolder { get; set; } = "orders/incoming";
    public string ArchiveFolder { get; set; } = "orders/archive";
    public string ErrorFolder { get; set; } = "orders/error";
}
