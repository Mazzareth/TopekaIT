using System.Text.Json;

namespace TopekaIT.AvaloniaShell;

public sealed record PortalSettings(string BaseUrl)
{
    private const string DefaultBaseUrl = "http://localhost:5117";
    private const string EnvironmentVariableName = "TOPEKAIT_PORTAL_BASE_URL";

    public static PortalSettings Load()
    {
        var environmentUrl = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentUrl))
        {
            return new PortalSettings(NormalizeBaseUrl(environmentUrl));
        }

        var configUrl = ReadConfigBaseUrl();
        return new PortalSettings(NormalizeBaseUrl(configUrl));
    }

    public Uri BuildUri(string relativePath)
    {
        var baseUri = new Uri(BaseUrl, UriKind.Absolute);
        return new Uri(baseUri, relativePath.TrimStart('/'));
    }

    private static string? ReadConfigBaseUrl()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("Portal", out var portal)
                && portal.TryGetProperty("BaseUrl", out var baseUrl)
                && baseUrl.ValueKind == JsonValueKind.String)
            {
                return baseUrl.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        return null;
    }

    private static string NormalizeBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultBaseUrl;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return DefaultBaseUrl;
        }

        return trimmed.EndsWith("/", StringComparison.Ordinal)
            ? trimmed
            : trimmed + "/";
    }
}
