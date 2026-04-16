using System.Net.Http.Headers;
using System.Text.Json;
using SolarEngine.Features.Updates.Domain;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed class GitHubReleaseFeedClient
{
    private static readonly Uri ReleasesUri =
        new("https://api.github.com/repos/humbertoschoenwald/auto-theme-solar-engine/releases");
    private static readonly HttpClient Client = CreateHttpClient();

    public async ValueTask<(CalVersion Version, string Tag, string AssetName, string AssetUrl)?> FindLatestMatchingReleaseAsync(
        ReleaseFlavor releaseFlavor,
        CalVersion currentVersion,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, ReleasesUri);
        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? bestMatch = null;
        foreach (JsonElement releaseElement in document.RootElement.EnumerateArray())
        {
            if (releaseElement.TryGetProperty("draft", out JsonElement draftElement) && draftElement.GetBoolean())
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
