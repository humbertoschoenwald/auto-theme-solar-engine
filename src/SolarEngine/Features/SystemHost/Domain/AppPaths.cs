namespace SolarEngine.Features.SystemHost.Domain;

internal sealed class AppPaths
{
    private static readonly string BaseDirectory = ResolveBaseDirectory();

    public AppPaths()
        : this(BaseDirectory)
    {
    }

    internal AppPaths(string directoryPath)
    {
        DirectoryPath = !string.IsNullOrWhiteSpace(directoryPath)
            ? Path.GetFullPath(directoryPath)
            : throw new ArgumentException("Provide a stable application data directory.", nameof(directoryPath));

        ConfigPath = Path.Combine(DirectoryPath, "config.json");
        LogPath = Path.Combine(DirectoryPath, "AutoThemeSolarEngine.log");
    }

    public string DirectoryPath { get; }

    public string ConfigPath { get; }

    public string LogPath { get; }

    private static string ResolveBaseDirectory()
    {
        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        return string.IsNullOrWhiteSpace(localApplicationData)
            ? throw new DirectoryNotFoundException("Resolve a stable per-user application data root before composing runtime file paths.")
            : Path.GetFullPath(Path.Combine(localApplicationData, "AutoThemeSolarEngine"));
    }
}
