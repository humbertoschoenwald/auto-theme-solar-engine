using System.Net.Http.Headers;
using System.Text.Json;
using SolarEngine.Features.Updates.Domain;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed class GitHubReleaseFeedClient
{
    private const string DraftPropertyName = "draft";
    private const string TagNamePropertyName = "tag_name";
    private const string AssetsPropertyName = "assets";
    private const string AssetNamePropertyName = "name";
    private const string AssetUrlPropertyName = "browser_download_url";
    private const string ReleaseNamePropertyName = "name";
    private const string ReleaseBodyPropertyName = "body";
    private const string UserAgentProductName = "AutoThemeSolarEngine";
    private const string UserAgentProductVersion = "1.0";
    private const string GitHubJsonMediaType = "application/vnd.github+json";
    private const string SelfContainedAssetMarker = "self-contained";
    private const string YankedMarker = "YANKED";
    private const int NotFoundIndex = -1;
    private const int PreviousCharacterOffset = 1;
    private const int ComparisonEqual = 0;
    private const int SearchStartIndex = 0;
    private const int LowerBoundIndex = 0;

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
            if (releaseElement.TryGetProperty(DraftPropertyName, out JsonElement draftElement) && draftElement.GetBoolean())
            {
                continue;
            }

            if (IsYankedRelease(releaseElement))
            {
                continue;
            }

            if (!releaseElement.TryGetProperty(TagNamePropertyName, out JsonElement tagElement))
            {
                continue;
            }

            string? tag = tagElement.GetString();
            if (!CalVersion.TryParse(tag, out CalVersion releaseVersion) || releaseVersion.CompareTo(currentVersion) <= ComparisonEqual)
            {
                continue;
            }

            if (!releaseElement.TryGetProperty(AssetsPropertyName, out JsonElement assetsElement))
            {
                continue;
            }

            foreach (JsonElement assetElement in assetsElement.EnumerateArray())
            {
                string assetName = assetElement.GetProperty(AssetNamePropertyName).GetString() ?? string.Empty;
                if (!MatchesFlavor(assetName, releaseFlavor))
                {
                    continue;
                }

                string assetUrl = assetElement.GetProperty(AssetUrlPropertyName).GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(assetUrl))
                {
                    continue;
                }

                if (bestMatch is null || releaseVersion.CompareTo(bestMatch.Value.Version) > ComparisonEqual)
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
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgentProductName, UserAgentProductVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(GitHubJsonMediaType));
        return client;
    }

    private static bool ContainsWholeWordYanked(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan();
        int index = SearchStartIndex;

        while ((index = value.IndexOf(YankedMarker, index, StringComparison.OrdinalIgnoreCase)) >= NotFoundIndex + PreviousCharacterOffset)
        {
            int previousIndex = index - PreviousCharacterOffset;
            int nextIndex = index + YankedMarker.Length;
            bool startBoundary = previousIndex < LowerBoundIndex || !char.IsLetterOrDigit(span[previousIndex]);
            bool endBoundary = nextIndex >= span.Length || !char.IsLetterOrDigit(span[nextIndex]);

            if (startBoundary && endBoundary)
            {
                return true;
            }

            index += YankedMarker.Length;
        }

        return false;
    }

    private static bool IsYankedRelease(JsonElement releaseElement)
    {
        string? releaseName = releaseElement.TryGetProperty(ReleaseNamePropertyName, out JsonElement nameElement)
            ? nameElement.GetString()
            : null;
        string? releaseBody = releaseElement.TryGetProperty(ReleaseBodyPropertyName, out JsonElement bodyElement)
            ? bodyElement.GetString()
            : null;

        return ContainsWholeWordYanked(releaseName) || ContainsWholeWordYanked(releaseBody);
    }

    private static bool MatchesFlavor(string assetName, ReleaseFlavor releaseFlavor)
    {
        _ = releaseFlavor;
        return assetName.Contains(SelfContainedAssetMarker, StringComparison.OrdinalIgnoreCase);
    }
}
