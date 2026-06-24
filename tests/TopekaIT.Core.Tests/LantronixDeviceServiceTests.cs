using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Protects Lantronix inventory parsing so strange controller text still becomes usable samples.
/// </summary>
public class LantronixDeviceServiceTests
{
    [Fact]
    public void ParseInventory_ReadsVeederRootCurrentInventoryRow()
    {
        const string response = """
            IN-TANK INVENTORY

            TANK PRODUCT             VOLUME TC VOLUME   ULLAGE   HEIGHT    WATER     TEMP
              1  DIESEL                7337      7332     4705    52.39     0.00    61.47
            """;

        var snapshot = LantronixDeviceService.ParseInventory(response);

        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot.TankNumber);
        Assert.Equal("DIESEL", snapshot.Product);
        Assert.Equal(7337m, snapshot.Volume);
        Assert.Equal(7332m, snapshot.TcVolume);
        Assert.Equal(4705m, snapshot.Ullage);
        Assert.Equal(52.39m, snapshot.Height);
        Assert.Equal(0.00m, snapshot.Water);
        Assert.Equal(61.47m, snapshot.Temperature);
    }

    [Fact]
    public void ParseInventory_ReturnsNullWhenInventoryRowIsMissing()
    {
        var snapshot = LantronixDeviceService.ParseInventory("NO DATA AVAILABLE");

        Assert.Null(snapshot);
    }
}
