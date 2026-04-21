using System.Reflection;
using System.Runtime.InteropServices;
using SolarEngine.Features.Themes.Infrastructure;
using SolarEngine.Infrastructure.Security;
using SolarEngine.UI;
using Xunit;

namespace SolarEngine.Tests.Infrastructure.Interop;

/// <summary>
/// Guards the repository interop policy so authored P/Invoke stays modern and ownership stays explicit.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class NativeInteropPolicyTests
{
    private static readonly string s_repositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    /// <summary>
    /// Lists repository interop types that declare authored native entry points.
    /// </summary>
    public static TheoryData<Type> PInvokeTypes =>
        [
            typeof(NativeInterop),
            typeof(WindowsDataProtection),
            typeof(WindowsRegistryThemeMutator)
        ];

    /// <summary>
    /// Verifies authored source files do not reintroduce DllImport declarations.
    /// </summary>
    [Fact]
    public void AuthoredSource_DoesNotUseDllImport()
    {
        string[] matches =
        [
            .. EnumerateAuthoredSourceFiles()
            .SelectMany(static filePath => File.ReadLines(filePath)
                .Select((line, index) => new { filePath, line, lineNumber = index + 1 }))
            .Where(static entry => entry.line.Contains(GetForbiddenInteropMarker(), StringComparison.Ordinal))
            .Select(static entry => $"{entry.filePath}:{entry.lineNumber}: {entry.line.Trim()}")
        ];

        Assert.True(matches.Length == 0, string.Join(Environment.NewLine, matches));
    }

    /// <summary>
    /// Verifies repository interop types expose authored LibraryImport declarations.
    /// </summary>
    [Theory]
    [MemberData(nameof(PInvokeTypes))]
    public void PInvokeTypes_UseLibraryImport(Type interopType)
    {
        ArgumentNullException.ThrowIfNull(interopType);

        MethodInfo[] methods = interopType.GetMethods(
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly);

        Assert.Contains(
            methods,
            static method => method.GetCustomAttribute<LibraryImportAttribute>() is not null);
    }

    /// <summary>
    /// Verifies ownership-bearing UI resources stay wrapped in SafeHandle types.
    /// </summary>
    [Fact]
    public void OwnedUiResources_UseSafeHandleFields()
    {
        AssertSafeHandleField<SettingsWindow>("_fontHandle");
        AssertSafeHandleField<SettingsWindow>("_windowIconHandle");
        AssertSafeHandleField<SettingsWindow>("_backgroundBrushHandle");
        AssertSafeHandleField<TrayIconHost>("_menuHandle");
        AssertSafeHandleField<TrayIconHost>("_iconHandle");
    }

    private static void AssertSafeHandleField<T>(string fieldName)
    {
        FieldInfo field = typeof(T).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException($"{typeof(T).Name}.{fieldName} was not found.");

        Assert.True(
            typeof(SafeHandle).IsAssignableFrom(field.FieldType),
            $"{typeof(T).Name}.{fieldName} must use a SafeHandle-derived type.");
    }

    private static IEnumerable<string> EnumerateAuthoredSourceFiles()
    {
        return EnumerateSourceFilesUnder("src").Concat(EnumerateSourceFilesUnder("tests"));
    }

    private static string GetForbiddenInteropMarker()
    {
        return string.Concat("[", nameof(DllImportAttribute).Replace("Attribute", string.Empty, StringComparison.Ordinal), "(");
    }

    private static IEnumerable<string> EnumerateSourceFilesUnder(string relativeRoot)
    {
        string absoluteRoot = Path.Combine(s_repositoryRoot, relativeRoot);
        if (!Directory.Exists(absoluteRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static filePath =>
                !filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }
}
