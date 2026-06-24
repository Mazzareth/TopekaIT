using System.Net;

namespace TopekaIT.Web.Services;

/// <summary>
/// Normalizes printer IP strings before matching logs to printer records.
/// </summary>
public static class PrinterIpNormalizer
{
    public static string Normalize(IPAddress? address)
    {
        if (address == null)
        {
            return "unknown";
        }

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }

    public static string Normalize(string ip)
    {
        return IPAddress.TryParse(ip, out var address)
            ? Normalize(address)
            : ip.Trim();
    }
}
