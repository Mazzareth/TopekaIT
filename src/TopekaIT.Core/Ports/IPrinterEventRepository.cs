using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IPrinterEventRepository
{
    Task AddAsync(PrinterEvent ev, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterEvent>> GetByPrinterAsync(string printerId, int count, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(int count, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsByPrinterAsync(string printerId, CancellationToken ct = default);
    Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetActiveIncidentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetAllDivisionActiveIncidentsAsync(CancellationToken ct = default);
    Task SetAlertBlipSuppressedAsync(string printerId, string alertKey, bool suppressed, CancellationToken ct = default);
    Task ClearAlertAsync(string printerId, string alertKey, CancellationToken ct = default);
}
