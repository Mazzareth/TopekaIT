using System.Net;
using TopekaIT.Web.Services;
using Xunit;

namespace TopekaIT.Web.Tests;

public class PrinterIpNormalizerTests
{
    [Fact]
    public void Normalize_ReturnsUnknownForMissingAddress()
    {
        Assert.Equal("unknown", PrinterIpNormalizer.Normalize((IPAddress?)null));
    }

    [Fact]
    public void Normalize_MapsIpv4MappedIpv6AddressToIpv4()
    {
        var address = IPAddress.Parse("::ffff:10.36.155.20");

        Assert.Equal("10.36.155.20", PrinterIpNormalizer.Normalize(address));
    }

    [Fact]
    public void Normalize_TrimsUnparseableIpText()
    {
        Assert.Equal("not-an-ip", PrinterIpNormalizer.Normalize("  not-an-ip  "));
    }
}
