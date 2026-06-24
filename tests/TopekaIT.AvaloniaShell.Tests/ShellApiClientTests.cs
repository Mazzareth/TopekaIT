using TopekaIT.AvaloniaShell;
using Xunit;

namespace TopekaIT.AvaloniaShell.Tests;

public class ShellApiClientTests
{
    [Theory]
    [InlineData("https://portal.example.local/api/shell/login")]
    [InlineData("http://localhost:5117/api/shell/login")]
    [InlineData("http://127.0.0.1:5117/api/shell/login")]
    [InlineData("http://[::1]:5117/api/shell/login")]
    public void IsCredentialTransportAllowed_AllowsHttpsAndLoopbackHttp(string url)
    {
        Assert.True(ShellApiClient.IsCredentialTransportAllowed(new Uri(url)));
    }

    [Theory]
    [InlineData("http://10.36.155.64:5117/api/shell/login")]
    [InlineData("http://portal.local:5117/api/shell/login")]
    [InlineData("ftp://localhost/api/shell/login")]
    public void IsCredentialTransportAllowed_BlocksNonLoopbackHttp(string url)
    {
        Assert.False(ShellApiClient.IsCredentialTransportAllowed(new Uri(url)));
    }
}
