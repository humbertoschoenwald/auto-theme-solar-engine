using SolarEngine.Shared;

namespace SolarEngine.Features.SystemHost.Domain;

internal sealed class AppPaths
{
    private const string DirectoryResolutionErrorMessage = "Resolve a stable per-user application data root before composing runtime file paths.";
    private const string InvalidDirectoryPathErrorMessage = "Provide a stable application data directory.";

    private static readonly string s_baseDirectory = ResolveBaseDirectory();

    public AppPaths()
        : this(s_baseDirectory)
    {
    }

    internal AppPaths(string directoryPath)
    {
        DirectoryPath = !string.IsNullOrWhiteSpace(directoryPath)
            ? Path.GetFullPath(directoryPath)
            : throw new ArgumentException(InvalidDirectoryPathErrorMessage, nameof(directoryPath));

        ConfigPath = Path.Combine(DirectoryPath, AppIdentity.ConfigFileName);
        LogPath = Path.Combine(DirectoryPath, AppIdentity.LogFileName);
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
            ? throw new DirectoryNotFoundException(DirectoryResolutionErrorMessage)
            : Path.GetFullPath(Path.Combine(localApplicationData, AppIdentity.DirectoryName));
    }
}
