// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Groups updater integration tests into a non-parallel collection because they coordinate real process and registry state.
/// </summary>
[CollectionDefinition("UpdaterScripts", DisableParallelization = true)]
public sealed class UpdaterScriptsCollectionDefinition
{
}
