using System.Net.Http;
using System.Text.Json;

namespace ESeriesSwitch.Services
{
    public sealed record UpdateInfo(Version Version, string Url);

    /// <summary>
    /// Asks GitHub for the latest published release. Drafts and pre-releases are ignored by the API.
    /// Any failure (offline, rate limit, unexpected answer) simply means "no update".
    /// </summary>
    public static class UpdateChecker
    {
        public static async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"ESeriesSwitch/{AppInfo.VersionText}");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                using var stream = await http.GetStreamAsync(AppInfo.LatestReleaseApiUrl);
                using var json = await JsonDocument.ParseAsync(stream);

                var tag = json.RootElement.GetProperty("tag_name").GetString();
                var url = json.RootElement.GetProperty("html_url").GetString();
                if (tag == null || url == null || !Version.TryParse(tag.TrimStart('v', 'V'), out var latest))
                    return null;

                latest = AppInfo.Normalize(latest);
                return latest > AppInfo.Version ? new UpdateInfo(latest, url) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
