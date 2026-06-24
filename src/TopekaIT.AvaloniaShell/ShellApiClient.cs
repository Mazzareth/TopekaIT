using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TopekaIT.AvaloniaShell;

public sealed class ShellApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PortalSettings _settings;
    private readonly HttpClient _http;

    public ShellApiClient(PortalSettings settings)
        : this(settings, new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
    {
    }

    public ShellApiClient(PortalSettings settings, HttpClient http)
    {
        _settings = settings;
        _http = http;
    }

    public async Task<ShellLoginResult> LoginAsync(
        string username,
        string password,
        CancellationToken ct = default)
    {
        var endpoint = _settings.BuildUri("/api/shell/login");
        if (!IsCredentialTransportAllowed(endpoint))
        {
            return ShellLoginResult.Failed("Credential sign-in requires HTTPS unless the portal URL is localhost.");
        }

        try
        {
            var response = await _http.PostAsJsonAsync(
                endpoint,
                new ShellLoginRequest(username, password),
                JsonOptions,
                ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var error = await response.Content.ReadFromJsonAsync<ShellLoginError>(JsonOptions, ct);
                return ShellLoginResult.Failed(error?.Message ?? "Invalid username or password.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ShellLoginResult.Failed($"Sign-in failed with HTTP {(int)response.StatusCode}.");
            }

            var login = await response.Content.ReadFromJsonAsync<ShellLoginResponse>(JsonOptions, ct);
            return login == null
                ? ShellLoginResult.Failed("The portal returned an empty sign-in response.")
                : ShellLoginResult.Success(login);
        }
        catch (HttpRequestException ex)
        {
            return ShellLoginResult.Failed($"Could not reach the portal. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ShellLoginResult.Failed("The portal did not respond before the sign-in timeout.");
        }
    }

    public static bool IsCredentialTransportAllowed(Uri endpoint)
    {
        if (endpoint.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return endpoint.Scheme == Uri.UriSchemeHttp && IsLoopbackHost(endpoint);
    }

    private static bool IsLoopbackHost(Uri endpoint)
    {
        if (endpoint.IsLoopback)
        {
            return true;
        }

        return IPAddress.TryParse(endpoint.Host, out var address) && IPAddress.IsLoopback(address);
    }
}

public sealed record ShellLoginResult(
    bool Succeeded,
    ShellLoginResponse? Response,
    string? ErrorMessage)
{
    public static ShellLoginResult Success(ShellLoginResponse response) => new(true, response, null);

    public static ShellLoginResult Failed(string message) => new(false, null, message);
}

public sealed record ShellLoginRequest(string Username, string Password);

public sealed record ShellLoginError(string Message);
