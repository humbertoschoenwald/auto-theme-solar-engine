using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Groups updater integration tests into a non-parallel collection because they coordinate real process and registry state.
/// </summary>
[CollectionDefinition("UpdaterScripts", DisableParallelization = true)]
public sealed class UpdaterScriptsCollectionDefinition
{
}
