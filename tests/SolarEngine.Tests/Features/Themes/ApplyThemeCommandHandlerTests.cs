// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Themes;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.Themes;

/// <summary>
/// Verifies theme commands delegate exactly once to the active mutator.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class ApplyThemeCommandHandlerTests
{
    /// <summary>
    /// Verifies the handler forwards the requested theme mode to the mutator.
    /// </summary>
    [Fact]
    public void HandleDelegatesRequestedModeToThemeMutator()
    {
        RecordingThemeMutator themeMutator = new();
        ApplyThemeCommandHandler handler = new(themeMutator);

        Result<ThemeMode> result = handler.Handle(new ApplyThemeCommand(ThemeMode.Dark));

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemeMode.Dark, result.Value);
        Assert.Equal(ThemeMode.Dark, themeMutator.LastMode);
        Assert.Equal(1, themeMutator.ApplyCallCount);
    }

    private sealed class RecordingThemeMutator : IThemeMutator
    {
        public int ApplyCallCount
        {
            get; private set;
        }

        public ThemeMode? LastMode
        {
            get; private set;
        }

        public Result<ThemeMode> Apply(ThemeMode mode)
        {
            ApplyCallCount++;
            LastMode = mode;
            return Result<ThemeMode>.Success(mode);
        }

        public ThemeMode? TryGetCurrentMode()
        {
            return ThemeMode.Light;
        }
    }
}
