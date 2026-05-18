using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class PrintNetCommandCatalogTests
{
    private readonly PrintNetCommandCatalog _catalog = new();

    [Theory]
    [InlineData("list all")]
    [InlineData("list arp")]
    [InlineData("list dest")]
    [InlineData("list dest d1prn")]
    [InlineData("list diff")]
    [InlineData("list ifc")]
    [InlineData("list logins")]
    [InlineData("list logpath l1")]
    [InlineData("list model m1")]
    [InlineData("list net")]
    [InlineData("list pping")]
    [InlineData("list pserver")]
    [InlineData("list ptrcfg")]
    [InlineData("list ptrmgmt")]
    [InlineData("list snmp")]
    [InlineData("list sysinfo")]
    [InlineData("list tcpip")]
    [InlineData("list test")]
    [InlineData("list tn")]
    [InlineData("list uptime")]
    [InlineData("list user")]
    [InlineData("list var")]
    [InlineData("list dhcp")]
    [InlineData("list lpd")]
    [InlineData("list stored snmp")]
    [InlineData("list default tcpip")]
    [InlineData("?")]
    [InlineData("lpstat")]
    [InlineData("lpstat d1prn-10")]
    [InlineData("ping 192.75.11.30")]
    [InlineData("ping -s 192.75.11.30 64 2")]
    public void Classify_ReturnsSafe_ForManualReadOnlyCommands(string command)
    {
        var result = _catalog.Classify(command);

        Assert.Equal(PrintNetCommandAccess.Safe, result.Access);
        Assert.True(result.CanRunWithoutAdditionalGate);
    }

    [Theory]
    [InlineData("set sysinfo name dock-printer")]
    [InlineData("set sysinfo contact IT")]
    [InlineData("set sysinfo location freezer")]
    [InlineData("set sysinfo prnserial 000123456789")]
    [InlineData("set snmp on")]
    [InlineData("set snmp manager 1 10.36.155.64 Public")]
    [InlineData("set snmp trapport 1 162")]
    [InlineData("set snmp trapport 1 49152")]
    [InlineData("set snmp trap 1 active")]
    [InlineData("set snmp alerts 1 warning -cutter")]
    [InlineData("save")]
    public void Classify_ReturnsProtected_ForIdentitySnmpAndSaveCommands(string command)
    {
        var result = _catalog.Classify(command);

        Assert.Equal(PrintNetCommandAccess.Protected, result.Access);
        Assert.True(result.RequiresProtectedGate);
    }

    [Theory]
    [InlineData("save default")]
    [InlineData("reset")]
    [InlineData("reboot")]
    [InlineData("load default")]
    [InlineData("store tcpip 1 addr 192.168.1.10")]
    [InlineData("store tcpip from default")]
    [InlineData("set user passwd root secret")]
    [InlineData("set user passwd snmp private")]
    [InlineData("set user from default")]
    [InlineData("disable snmp")]
    [InlineData("enable telnet")]
    [InlineData("set snmp off")]
    [InlineData("set snmp trap 1 -active")]
    [InlineData("set sysinfo smtpserver 192.168.1.1")]
    [InlineData("set model from default")]
    public void Classify_ReturnsExcluded_ForDestructiveDefaultResetAndSecurityCommands(string command)
    {
        var result = _catalog.Classify(command);

        Assert.Equal(PrintNetCommandAccess.Excluded, result.Access);
        Assert.True(result.IsExcluded);
    }

    [Fact]
    public void Classify_NormalizesSpacingAndCase()
    {
        var result = _catalog.Classify("  LIST   SNMP  ");

        Assert.Equal("list snmp", result.NormalizedCommand);
        Assert.Equal(PrintNetCommandAccess.Safe, result.Access);
    }

    [Fact]
    public void Classify_ExcludesMultipleCommands()
    {
        var result = _catalog.Classify("list snmp; save");

        Assert.Equal(PrintNetCommandAccess.Excluded, result.Access);
        Assert.Contains("one telnet command", result.Reason);
    }

    [Fact]
    public void Classify_ExcludesUnknownCommandsByDefault()
    {
        var result = _catalog.Classify("debug tcp");

        Assert.Equal(PrintNetCommandAccess.Excluded, result.Access);
        Assert.Contains("approved PrintNet catalog", result.Reason);
    }
}
