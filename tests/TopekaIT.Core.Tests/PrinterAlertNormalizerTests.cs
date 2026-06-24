using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Printer alert text is messy; these tests keep the friendly alert shape stable.
/// </summary>
public class PrinterAlertNormalizerTests
{
    [Theory]
    [InlineData("SNMP V1 trap from 10.32.144.167 | enterprise=1.3.6.1.4.1.10504.0.0.0.43690 | generic=authenticationFailure | specific=0")]
    [InlineData("SNMP V1 trap from 10.35.60.24 | Enterprise=1.3.6.1.4.1.10504.43690.43690 | Generic=authent | Specific=0")]
    [InlineData("SNMP V2 trap from 10.36.156.24 | 1.3.6.1.6.3.1.1.4.1.0=1.3.6.1.6.3.1.1.5.5")]
    public void Normalize_GroupsAuthenticationFailureTrapVariants(string rawMessage)
    {
        var alert = PrinterAlertNormalizer.Normalize(rawMessage, "Warning", "Warning");

        Assert.Equal(PrinterAlertNormalizer.SnmpAuthenticationFailureAlertKey, alert.AlertKey);
        Assert.Equal("SNMP Authentication Failure", alert.AlertTitle);
        Assert.Equal("SNMP Authentication", alert.AlertCategory);
        Assert.Equal("Authentication Failure", alert.AlertDetail);
        Assert.Equal("Warning", alert.Severity);
        Assert.Contains("not a printer hardware fault", alert.FriendlyMessage);
    }

    [Fact]
    public void Normalize_PreservesPrinterFaultGrouping()
    {
        var alert = PrinterAlertNormalizer.Normalize(
            "Message=Media path recoverable fault. (reported \"gap Not Detected See Manual\", Location 2404, Severity Critical)",
            "Error",
            "Error");

        Assert.Equal("GAP_NOT_DETECTED_SEE_MANUAL", alert.AlertKey);
        Assert.Equal("Gap Not Detected See Manual", alert.AlertDetail);
        Assert.Equal("Gap Not Detected", alert.AlertCategory);
        Assert.Equal("Critical", alert.Severity);
    }
}
