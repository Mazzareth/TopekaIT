using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class PrinterEventService
{
    private readonly IPrinterEventRepository _repo;

    public PrinterEventService(IPrinterEventRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<PrinterEvent>> GetByPrinterAsync(string printerId, int count = 100, CancellationToken ct = default)
        => _repo.GetByPrinterAsync(printerId, count, ct);

    public Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(int count = 0, CancellationToken ct = default)
        => _repo.GetErrorsAsync(count, ct);

    public Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
        => _repo.GetErrorsAsync(count, from, to, ct);

    public Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(int count = 0, CancellationToken ct = default)
        => _repo.GetAllDivisionErrorsAsync(count, ct);

    public Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
        => _repo.GetAllDivisionErrorsAsync(count, from, to, ct);

    public Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(int count = 0, CancellationToken ct = default)
        => _repo.GetGroupedErrorsAsync(count, ct);

    public Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
        => _repo.GetGroupedErrorsAsync(count, from, to, ct);

    public Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(int count = 0, CancellationToken ct = default)
        => _repo.GetAllDivisionGroupedErrorsAsync(count, ct);

    public Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
        => _repo.GetAllDivisionGroupedErrorsAsync(count, from, to, ct);

    public Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsAsync(CancellationToken ct = default)
        => _repo.GetActiveAlertsAsync(ct);

    public Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsByPrinterAsync(string printerId, CancellationToken ct = default)
        => _repo.GetActiveAlertsByPrinterAsync(printerId, ct);

    public Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetActiveIncidentsAsync(CancellationToken ct = default)
        => _repo.GetActiveIncidentsAsync(ct);

    public Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetAllDivisionActiveIncidentsAsync(CancellationToken ct = default)
        => _repo.GetAllDivisionActiveIncidentsAsync(ct);

    public Task SetAlertBlipSuppressedAsync(string printerId, string alertKey, bool suppressed, CancellationToken ct = default)
        => _repo.SetAlertBlipSuppressedAsync(printerId, alertKey, suppressed, ct);

    public Task ClearAlertAsync(string printerId, string alertKey, CancellationToken ct = default)
        => _repo.ClearAlertAsync(printerId, alertKey, ct);

    public Task AddAsync(PrinterEvent ev, CancellationToken ct = default)
        => _repo.AddAsync(ev, ct);
}
