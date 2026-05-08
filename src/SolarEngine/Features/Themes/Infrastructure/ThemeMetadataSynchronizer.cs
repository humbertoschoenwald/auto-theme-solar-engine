// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Shared;

namespace SolarEngine.Features.Themes.Infrastructure;

internal static class ThemeMetadataSynchronizer
{
    private const string GeneratedThemesDirectoryName = "Themes";
    private const string ThemeFileExtension = ".theme";
    private const string ThemeSectionName = "Theme";
    private const string VisualStylesSectionName = "VisualStyles";
    private const string DisplayNameKey = "DisplayName";
    private const string SystemModeKey = "SystemMode";
    private const string AppModeKey = "AppMode";
    private const string LightThemeName = "Light";
    private const string DarkThemeName = "Dark";
    private const string ThemeSectionHeader = "[Theme]";
    private const string VisualStylesSectionHeader = "[VisualStyles]";
    private const string VisualStylesPathKeyValue = @"Path=%ResourceDir%\Themes\Aero\Aero.msstyles";
    private const string VisualStylesColorStyleKeyValue = "ColorStyle=NormalColor";
    private const string VisualStylesSizeKeyValue = "Size=NormalSize";
    private const string LightSystemThemeFileName = "aero.theme";
    private const string DarkSystemThemeFileName = "dark.theme";
    private const string WindowsResourcesThemesRelativePath = @"Resources\Themes";
    private const string WindowsDirectoryResolutionErrorMessage = "Resolve the Windows directory before reading built-in theme templates.";
    private const string LocalApplicationDataResolutionErrorMessage = "Resolve LocalAppData before writing generated theme metadata.";
    private const string CarriageReturnLineFeed = "\r\n";
    private const string LineFeed = "\n";
    private const string CarriageReturn = "\r";
    private const string CommentPrefix = ";";
    private const string KeyValueSeparatorText = "=";
    private const char ByteOrderMark = '\uFEFF';
    private const char SectionStartDelimiter = '[';
    private const char SectionEndDelimiter = ']';
    private const char KeyValueSeparator = '=';
    private const byte Utf8BomFirstByte = 0xEF;
    private const byte Utf8BomSecondByte = 0xBB;
    private const byte Utf8BomThirdByte = 0xBF;
    private const byte Utf16LittleEndianBomFirstByte = 0xFF;
    private const byte Utf16LittleEndianBomSecondByte = 0xFE;
    private const byte Utf16BigEndianBomFirstByte = 0xFE;
    private const byte Utf16BigEndianBomSecondByte = 0xFF;
    private const byte NullByte = 0x00;
    private const int FirstElementIndex = 0;
    private const int NextElementOffset = 1;
    private const int ThirdElementIndex = 2;
    private const int FourthElementIndex = 3;
    private const int SectionDelimiterCharacterCount = 2;
    private const int MinimumSectionLineLength = 2;
    private const int Utf8BomLength = 3;
    private const int Utf16BomLength = 2;
    private const int Utf16ProbeLength = 4;
    private const int MissingIndex = -1;
    private const int MinimumKeySeparatorIndex = 0;

    private static readonly Encoding s_latin1WithoutBom = Encoding.Latin1;
    private static readonly UTF8Encoding s_utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    internal static string Synchronize(ThemeMode mode, string? currentThemePath)
    {
        return Synchronize(mode, currentThemePath, ResolveGeneratedThemeDirectory());
    }

    internal static string Synchronize(
        ThemeMode mode,
        string? currentThemePath,
        string generatedThemeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generatedThemeDirectory);

        ThemeTextDocument document =
            TryReadThemeDocument(currentThemePath)
            ?? TryReadThemeDocument(ResolveBuiltInThemePath(mode))
            ?? ThemeTextDocument.Create(
                CreateFallbackThemeLines(mode),
                CarriageReturnLineFeed,
                s_utf8WithBom,
                endsWithNewLine: true);

        ApplyModeMetadata(document.Lines, mode);

        _ = Directory.CreateDirectory(generatedThemeDirectory);
        string generatedThemePath = Path.GetFullPath(Path.Combine(
            generatedThemeDirectory,
            ResolveThemeName(mode) + ThemeFileExtension));

        string content = string.Join(document.NewLine, document.Lines);
        if (document.EndsWithNewLine)
        {
            content += document.NewLine;
        }

        File.WriteAllText(generatedThemePath, content, document.Encoding);
        return generatedThemePath;
    }

    private static void ApplyModeMetadata(List<string> lines, ThemeMode mode)
    {
        string themeName = ResolveThemeName(mode);
        SetSectionValue(lines, ThemeSectionName, DisplayNameKey, themeName);
        SetSectionValue(lines, VisualStylesSectionName, SystemModeKey, themeName);
        SetSectionValue(lines, VisualStylesSectionName, AppModeKey, themeName);
    }

    private static void SetSectionValue(
        List<string> lines,
        string sectionName,
        string key,
        string value)
    {
        int sectionStartIndex = FindSectionStart(lines, sectionName);
        if (sectionStartIndex == MissingIndex)
        {
            AppendSectionWithValue(lines, sectionName, key, value);
            return;
        }

        int sectionEndIndex = FindSectionEnd(lines, sectionStartIndex);
        for (int index = sectionStartIndex + NextElementOffset; index < sectionEndIndex; index++)
        {
            if (!IsKeyLine(lines[index], key))
            {
                continue;
            }

            lines[index] = ComposeKeyValueLine(key, value);
            return;
        }

        lines.Insert(sectionEndIndex, ComposeKeyValueLine(key, value));
    }

    private static void AppendSectionWithValue(
        List<string> lines,
        string sectionName,
        string key,
        string value)
    {
        if (lines.Count > FirstElementIndex && !string.IsNullOrWhiteSpace(lines[^NextElementOffset]))
        {
            lines.Add(string.Empty);
        }

        lines.Add(ComposeSectionHeader(sectionName));
        lines.Add(ComposeKeyValueLine(key, value));
    }

    private static int FindSectionStart(List<string> lines, string sectionName)
    {
        for (int index = FirstElementIndex; index < lines.Count; index++)
        {
            if (IsSectionLine(lines[index], sectionName))
            {
                return index;
            }
        }

        return MissingIndex;
    }

    private static int FindSectionEnd(List<string> lines, int sectionStartIndex)
    {
        for (int index = sectionStartIndex + NextElementOffset; index < lines.Count; index++)
        {
            if (IsAnySectionLine(lines[index]))
            {
                return index;
            }
        }

        return lines.Count;
    }

    private static bool IsSectionLine(string line, string sectionName)
    {
        string trimmedLine = line.Trim();
        return trimmedLine.Length == sectionName.Length + SectionDelimiterCharacterCount
            && trimmedLine[FirstElementIndex] == SectionStartDelimiter
            && trimmedLine[^NextElementOffset] == SectionEndDelimiter
            && string.Equals(
                trimmedLine.Substring(NextElementOffset, sectionName.Length),
                sectionName,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnySectionLine(string line)
    {
        string trimmedLine = line.Trim();
        return trimmedLine.Length >= MinimumSectionLineLength
            && trimmedLine[FirstElementIndex] == SectionStartDelimiter
            && trimmedLine[^NextElementOffset] == SectionEndDelimiter;
    }

    private static bool IsKeyLine(string line, string key)
    {
        string trimmedLine = line.TrimStart();
        if (trimmedLine.StartsWith(CommentPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        int separatorIndex = trimmedLine.IndexOf(KeyValueSeparator, StringComparison.Ordinal);
        if (separatorIndex <= MinimumKeySeparatorIndex)
        {
            return false;
        }

        string currentKey = trimmedLine[..separatorIndex].Trim();
        return string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComposeSectionHeader(string sectionName)
    {
        return SectionStartDelimiter + sectionName + SectionEndDelimiter;
    }

    private static string ComposeKeyValueLine(string key, string value)
    {
        return key + KeyValueSeparatorText + value;
    }

    private static ThemeTextDocument? TryReadThemeDocument(string? themePath)
    {
        if (string.IsNullOrWhiteSpace(themePath) || !File.Exists(themePath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(themePath);
        Encoding encoding = ResolveEncoding(bytes);
        string content = StripByteOrderMark(encoding.GetString(bytes));
        string newLine = ResolveNewLine(content);
        bool endsWithNewLine = content.EndsWith(CarriageReturnLineFeed, StringComparison.Ordinal)
            || content.EndsWith(LineFeed, StringComparison.Ordinal)
            || content.EndsWith(CarriageReturn, StringComparison.Ordinal);

        string normalizedContent = content
            .Replace(CarriageReturnLineFeed, LineFeed, StringComparison.Ordinal)
            .Replace(CarriageReturn, LineFeed, StringComparison.Ordinal);

        List<string> lines = [.. normalizedContent.Split(LineFeed)];
        if (endsWithNewLine && lines.Count > FirstElementIndex)
        {
            lines.RemoveAt(lines.Count - NextElementOffset);
        }

        return ThemeTextDocument.Create(lines, newLine, encoding, endsWithNewLine);
    }

    private static Encoding ResolveEncoding(byte[] bytes)
    {
        return bytes switch
        {
            _ when bytes.Length >= Utf8BomLength
                && bytes[FirstElementIndex] == Utf8BomFirstByte
                && bytes[NextElementOffset] == Utf8BomSecondByte
                && bytes[ThirdElementIndex] == Utf8BomThirdByte => s_utf8WithBom,
            _ when bytes.Length >= Utf16BomLength
                && bytes[FirstElementIndex] == Utf16LittleEndianBomFirstByte
                && bytes[NextElementOffset] == Utf16LittleEndianBomSecondByte => Encoding.Unicode,
            _ when bytes.Length >= Utf16BomLength
                && bytes[FirstElementIndex] == Utf16BigEndianBomFirstByte
                && bytes[NextElementOffset] == Utf16BigEndianBomSecondByte => Encoding.BigEndianUnicode,
            _ when bytes.Length >= Utf16ProbeLength
                && bytes[NextElementOffset] == NullByte
                && bytes[FourthElementIndex] == NullByte => Encoding.Unicode,
            _ when bytes.Length >= Utf16ProbeLength
                && bytes[FirstElementIndex] == NullByte
                && bytes[ThirdElementIndex] == NullByte => Encoding.BigEndianUnicode,
            _ => s_latin1WithoutBom
        };
    }

    private static string StripByteOrderMark(string content)
    {
        return content.Length > FirstElementIndex && content[FirstElementIndex] == ByteOrderMark
            ? content[NextElementOffset..]
            : content;
    }

    private static string ResolveNewLine(string content)
    {
        return content.Contains(CarriageReturnLineFeed, StringComparison.Ordinal)
            ? CarriageReturnLineFeed
            : LineFeed;
    }

    private static string ResolveThemeName(ThemeMode mode)
    {
        return mode == ThemeMode.Light ? LightThemeName : DarkThemeName;
    }

    private static string ResolveBuiltInThemePath(ThemeMode mode)
    {
        string windowsDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.Windows,
            Environment.SpecialFolderOption.DoNotVerify);

        string resolvedWindowsDirectory = !string.IsNullOrWhiteSpace(windowsDirectory)
            ? windowsDirectory
            : throw new DirectoryNotFoundException(WindowsDirectoryResolutionErrorMessage);

        return Path.Combine(
            resolvedWindowsDirectory,
            WindowsResourcesThemesRelativePath,
            mode == ThemeMode.Light ? LightSystemThemeFileName : DarkSystemThemeFileName);
    }

    private static string ResolveGeneratedThemeDirectory()
    {
        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        string resolvedLocalApplicationData = !string.IsNullOrWhiteSpace(localApplicationData)
            ? localApplicationData
            : throw new DirectoryNotFoundException(LocalApplicationDataResolutionErrorMessage);

        return Path.GetFullPath(Path.Combine(
            resolvedLocalApplicationData,
            AppIdentity.DirectoryName,
            GeneratedThemesDirectoryName));
    }

    private static string[] CreateFallbackThemeLines(ThemeMode mode)
    {
        string themeName = ResolveThemeName(mode);

        return
        [
            ThemeSectionHeader,
            ComposeKeyValueLine(DisplayNameKey, themeName),
            string.Empty,
            VisualStylesSectionHeader,
            VisualStylesPathKeyValue,
            VisualStylesColorStyleKeyValue,
            VisualStylesSizeKeyValue,
            ComposeKeyValueLine(SystemModeKey, themeName),
            ComposeKeyValueLine(AppModeKey, themeName)
        ];
    }

    private sealed class ThemeTextDocument(
        List<string> lines,
        string newLine,
        Encoding encoding,
        bool endsWithNewLine)
    {
        internal List<string> Lines { get; } = lines;

        internal string NewLine { get; } = newLine;

        internal Encoding Encoding { get; } = encoding;

        internal bool EndsWithNewLine { get; } = endsWithNewLine;

        internal static ThemeTextDocument Create(
            IEnumerable<string> lines,
            string newLine,
            Encoding encoding,
            bool endsWithNewLine)
        {
            return new ThemeTextDocument([.. lines], newLine, encoding, endsWithNewLine);
        }
    }
}
