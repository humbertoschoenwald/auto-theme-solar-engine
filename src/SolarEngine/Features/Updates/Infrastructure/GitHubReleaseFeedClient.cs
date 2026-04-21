using System.Net.Http.Headers;
using System.Text.Json;
using SolarEngine.Features.Updates.Domain;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed class GitHubReleaseFeedClient
{
    private static readonly Uri s_releasesUri =
        new("https://api.github.com/repos/humbertoschoenwald/auto-theme-solar-engine/releases");
    private static readonly HttpClient s_client = CreateHttpClient();

    public async ValueTask<(CalVersion Version, string Tag, string AssetName, string AssetUrl)?> FindLatestMatchingReleaseAsync(
        ReleaseFlavor releaseFlavor,
        CalVersion currentVersion,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, s_releasesUri);
        using HttpResponseMessage response = await s_client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return SelectLatestMatchingRelease(document.RootElement, releaseFlavor, currentVersion);
    }

    internal static (CalVersion Version, string Tag, string AssetName, string AssetUrl)? SelectLatestMatchingRelease(
        JsonElement releasesElement,
        ReleaseFlavor releaseFlavor,
        CalVersion currentVersion)
    {
        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? bestMatch = null;
        foreach (JsonElement releaseElement in releasesElement.EnumerateArray())
        {
            if (releaseElement.TryGetProperty("draft", out JsonElement draftElement) && draftElement.GetBoolean())
            {
                continue;
            }

            if (IsYankedRelease(releaseElement))
            {
                continue;
            }

            if (!releaseElement.TryGetProperty("tag_name", out JsonElement tagElement))
            {
                continue;
            }

            string? tag = tagElement.GetString();
            if (!CalVersion.TryParse(tag, out CalVersion releaseVersion) || releaseVersion.CompareTo(currentVersion) <= 0)
            {
                continue;
            }

            if (!releaseElement.TryGetProperty("assets", out JsonElement assetsElement))
            {
                continue;
            }

            foreach (JsonElement assetElement in assetsElement.EnumerateArray())
            {
                string assetName = assetElement.GetProperty("name").GetString() ?? string.Empty;
                if (!MatchesFlavor(assetName, releaseFlavor))
                {
                    continue;
                }

                string assetUrl = assetElement.GetProperty("browser_download_url").GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(assetUrl))
                {
                    continue;
                }

                if (bestMatch is null || releaseVersion.CompareTo(bestMatch.Value.Version) > 0)
                {
                    bestMatch = (releaseVersion, tag!, assetName, assetUrl);
                }
            }
        }

        return bestMatch;
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AutoThemeSolarEngine", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool ContainsWholeWordYanked(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan();
        const string Marker = "YANKED";
        int index = 0;

        while ((index = value.IndexOf(Marker, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int previousIndex = index - 1;
            int nextIndex = index + Marker.Length;
            bool startBoundary = previousIndex < 0 || !char.IsLetterOrDigit(span[previousIndex]);
            bool endBoundary = nextIndex >= span.Length || !char.IsLetterOrDigit(span[nextIndex]);

            if (startBoundary && endBoundary)
            {
                return true;
            }

            index += Marker.Length;
        }

        return false;
    }

    private static bool IsYankedRelease(JsonElement releaseElement)
    {
        string? releaseName = releaseElement.TryGetProperty("name", out JsonElement nameElement)
            ? nameElement.GetString()
            : null;
        string? releaseBody = releaseElement.TryGetProperty("body", out JsonElement bodyElement)
            ? bodyElement.GetString()
            : null;

        return ContainsWholeWordYanked(releaseName) || ContainsWholeWordYanked(releaseBody);
    }

    private static bool MatchesFlavor(string assetName, ReleaseFlavor releaseFlavor)
    {
        return releaseFlavor switch
        {
            ReleaseFlavor.SelfContained => assetName.Contains("self-contained", StringComparison.OrdinalIgnoreCase),
            ReleaseFlavor.FrameworkDependent => assetName.Contains("framework-dependent", StringComparison.OrdinalIgnoreCase),
            ReleaseFlavor.Unknown => false,
            _ => false
        };
    }
}
