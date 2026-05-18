using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class PrinterSetupServiceTests
{
    [Fact]
    public void BuildDivisionPassword_UsesCodeAndZip()
    {
        var division = new Division { PrinterPasswordCode = "6I", PrinterPasswordZipCode = "66618" };

        Assert.Equal("6I@66618", PrinterSetupService.BuildDivisionPassword(division));
    }

    [Fact]
    public async Task TestAsync_TriesBlankPasswordBeforeDivisionPassword()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddFailure("");
        telnet.AddSuccess("6I@66618", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["list sysinfo"] = "Printer Name: Dock Printer",
            ["list ptrcfg"] = "Model: T8000-93993",
        }));
        var service = CreateService(telnet);

        var results = await service.TestAsync(TestDivision(), "10.36.155.20");

        Assert.True(results.Single().Connected);
        Assert.Equal(new[] { "", "6I@66618" }, telnet.PasswordAttempts);
        Assert.Equal("Dock Printer", results.Single().DetectedName);
        Assert.Equal("T8000-93993", results.Single().DetectedModel);
        Assert.Equal(PrinterModels.T8000, results.Single().SelectedModel);
    }

    [Fact]
    public async Task TestAsync_ReturnsPasswordFormatMessageWhenBothCredentialsFail()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddFailure("");
        telnet.AddFailure("6I@66618");
        var service = CreateService(telnet);

        var results = await service.TestAsync(TestDivision(), "10.36.155.20");

        var result = results.Single();
        Assert.False(result.Connected);
        Assert.Equal("Password for Printer not valid, Password for printer needs to be 6I@66618 or Blank.", result.ErrorMessage);
    }

    [Fact]
    public void ParseDetectedInfo_ReadsSysInfoAndPtrCfg()
    {
        var detected = PrinterSetupService.ParseDetectedInfo(
            "System Name: Shipping Printer\r\nLocation: Dock",
            "Printer Model: T8000-93993\r\nPort: LPT1");

        Assert.Equal("Shipping Printer", detected.Name);
        Assert.Equal("T8000-93993", detected.Model);
    }

    [Fact]
    public void ParseDetectedInfo_ReadsPrintNetDescriptionAsName()
    {
        var detected = PrinterSetupService.ParseDetectedInfo(
            """
            list sysinfo
            -- System Information ---------------------------------------------------------
                               description: P062365
                                  location: 6A Grand Island, NE
                                   contact: Benjamin Miller
                                model name: Integrated PrintNet Enterprise
            """,
            "");

        Assert.Equal("P062365", detected.Name);
        Assert.Equal("Integrated PrintNet Enterprise", detected.Model);
    }

    [Fact]
    public async Task TestAsync_PreservesPerIpSuccessAndFailure()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["list sysinfo"] = "Name: One",
            ["list ptrcfg"] = "Model: T8000",
        }));
        var service = CreateService(telnet);

        var results = await service.TestAsync(TestDivision(), "10.36.155.20\nnot-an-ip");

        Assert.Collection(results,
            first => Assert.True(first.Connected),
            second =>
            {
                Assert.False(second.Connected);
                Assert.Equal("Invalid IP address.", second.ErrorMessage);
            });
    }

    [Fact]
    public async Task RunAllAsync_UsesSysinfoNameAndDoesNotDuplicateExistingIp()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["list sysinfo"] = "Printer Name: Dock-01",
        }));
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>()));
        var printers = new FakePrinterRepository(new[]
        {
            new Printer { Id = "p-01", Name = "10.36.155.21", Model = PrinterModels.T8000, IpAddress = "10.36.155.21" },
        });
        var service = CreateService(telnet, printers);

        var results = await service.RunAllAsync(TestDivision(), new[]
        {
            new PrinterSetupRunRequest("10.36.155.20", "", "T8000", PrinterModels.T8000),
            new PrinterSetupRunRequest("10.36.155.21", "", "T8000", PrinterModels.T8000),
        });

        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(2, printers.Items.Count);
        Assert.Contains(printers.Items, p => p.IpAddress == "10.36.155.20" && p.Name == "Dock-01");
        Assert.Single(printers.Items, p => p.IpAddress == "10.36.155.21");
    }

    [Fact]
    public async Task RunAllAsync_EnablesSnmpAndConfiguresTrapManager()
    {
        var session = new FakeTelnetSession(new Dictionary<string, string>());
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", session);
        var service = CreateService(telnet);

        var results = await service.RunAllAsync(TestDivision(), new[]
        {
            new PrinterSetupRunRequest("10.36.155.20", "New Printer", "T8000", PrinterModels.T8000),
        });

        Assert.True(results.Single().Success);
        Assert.Collection(session.Commands,
            command => Assert.Equal("set snmp on", command),
            command => Assert.Equal("set snmp manager 1 10.36.155.64 Public", command),
            command => Assert.Equal("set snmp trapport 1 162", command),
            command => Assert.Equal("set snmp trap 1 active", command),
            command => Assert.Equal("save", command),
            command => Assert.Equal("list snmp", command),
            command => Assert.Equal("list sysinfo", command));
    }

    [Fact]
    public async Task RunAllAsync_TreatsUsageResponseAsCommandFailure()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["set snmp on"] = "Usage: set snmp on|off",
        }));
        var service = CreateService(telnet);

        var results = await service.RunAllAsync(TestDivision(), new[]
        {
            new PrinterSetupRunRequest("10.36.155.20", "New Printer", "T8000", PrinterModels.T8000),
        });

        var result = results.Single();
        Assert.False(result.Success);
        Assert.Equal("Command failed: set snmp on", result.ErrorMessage);
    }

    [Fact]
    public async Task RunAllAsync_TreatsUsageResponseAsCommandFailure_MidScript()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["set snmp manager 1 10.36.155.64 Public"] = "Usage: set snmp manager [-]v1",
        }));
        var service = CreateService(telnet);

        var results = await service.RunAllAsync(TestDivision(), new[]
        {
            new PrinterSetupRunRequest("10.36.155.20", "New Printer", "T8000", PrinterModels.T8000),
        });

        var result = results.Single();
        Assert.False(result.Success);
        Assert.Equal("Command failed: set snmp manager 1 10.36.155.64 Public", result.ErrorMessage);
    }

    [Fact]
    public async Task RunAllAsync_FailsIfRaceConditionDelaysUsageResponse()
    {
        var telnet = new FakeTelnetClient();
        telnet.AddSuccess("", new FakeTelnetSession(new Dictionary<string, string>
        {
            ["set snmp on"] = "set snmp on\r\n>\r\nUsage: set snmp on|off\r\n>",
        }));
        var service = CreateService(telnet);

        var results = await service.RunAllAsync(TestDivision(), new[]
        {
            new PrinterSetupRunRequest("10.36.155.20", "New Printer", "T8000", PrinterModels.T8000),
        });

        var result = results.Single();
        Assert.False(result.Success);
        Assert.Equal("Command failed: set snmp on", result.ErrorMessage);
    }

    private static PrinterSetupService CreateService(FakeTelnetClient telnet, FakePrinterRepository? printers = null) =>
        new(
            telnet,
            printers ?? new FakePrinterRepository(Array.Empty<Printer>()),
            new FakePrinterModelRepository(new[] { new PrinterModel { Name = PrinterModels.T8000, SupportsLogging = true } }),
            new PrinterSetupSettings { TimeoutMs = 1000 },
            new PrintNetCommandCatalog());

    private static Division TestDivision() =>
        new()
        {
            Id = "6I-A",
            Name = "Topeka",
            PrinterPasswordCode = "6I",
            PrinterPasswordZipCode = "66618",
        };

    private sealed class FakeTelnetClient : IPrinterSetupTelnetClient
    {
        private readonly Queue<PrinterSetupTelnetLogin> _logins = new();

        public List<string> PasswordAttempts { get; } = new();

        public void AddSuccess(string password, IPrinterSetupTelnetSession session) =>
            _logins.Enqueue(new PrinterSetupTelnetLogin(true, session));

        public void AddFailure(string password) =>
            _logins.Enqueue(new PrinterSetupTelnetLogin(false, null, "Login failed."));

        public Task<PrinterSetupTelnetLogin> TryLoginAsync(
            string ipAddress,
            int port,
            string username,
            string password,
            TimeSpan timeout,
            CancellationToken ct = default)
        {
            PasswordAttempts.Add(password);
            return Task.FromResult(_logins.Count > 0
                ? _logins.Dequeue()
                : new PrinterSetupTelnetLogin(false, null, "Login failed."));
        }
    }

    private sealed class FakeTelnetSession : IPrinterSetupTelnetSession
    {
        private readonly IReadOnlyDictionary<string, string> _responses;

        public FakeTelnetSession(IReadOnlyDictionary<string, string> responses)
        {
            _responses = responses;
        }

        public List<string> Commands { get; } = new();

        public Task<string> SendCommandAsync(string command, CancellationToken ct = default)
        {
            Commands.Add(command);
            
            if (_responses.TryGetValue(command, out var response))
            {
                return Task.FromResult(response.EndsWith(">") ? response : $"{command}\r\n{response}\r\n>");
            }

            var defaultResponse = command switch
            {
                "list snmp" => "SNMP: on\r\n1 10.36.155.64 Public 162 active\r\n2 unknown 0 inactive",
                _ => ""
            };
            
            return Task.FromResult($"{command}\r\n{defaultResponse}\r\n>");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakePrinterRepository : IPrinterRepository
    {
        public List<Printer> Items { get; }

        public FakePrinterRepository(IEnumerable<Printer> items)
        {
            Items = items.ToList();
        }

        public Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Printer>>(Items.ToList());

        public Task<Printer?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

        public Task AddAsync(Printer printer, CancellationToken ct = default)
        {
            Items.Add(printer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Printer printer, CancellationToken ct = default)
        {
            var index = Items.FindIndex(p => p.Id == printer.Id);
            if (index >= 0)
            {
                Items[index] = printer;
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            Items.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrinterModelRepository : IPrinterModelRepository
    {
        private readonly IReadOnlyList<PrinterModel> _models;

        public FakePrinterModelRepository(IReadOnlyList<PrinterModel> models)
        {
            _models = models;
        }

        public Task<IReadOnlyList<PrinterModel>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult(_models);

        public Task<PrinterModel> AddAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(new PrinterModel { Name = name });

        public Task EnsureDefaultAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
