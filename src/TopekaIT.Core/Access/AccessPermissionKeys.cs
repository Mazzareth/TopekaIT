namespace TopekaIT.Core.Access;

/// <summary>
/// Stable permission IDs. These strings are the shared language between the database, policies, and sidebar links.
/// </summary>
public static class AccessPermissionKeys
{
    public const string WorkerHome = "workspace.worker.home";
    public const string SupervisorHome = "workspace.supervisor.home";
    public const string ItDashboard = "workspace.it.dashboard";

    public const string TicketsViewOwn = "tickets.view-own";
    public const string TicketsCreate = "tickets.create";
    public const string TicketsViewQueue = "tickets.view-queue";
    public const string TicketsEditStatus = "tickets.edit-status";
    public const string TicketsAssign = "tickets.assign";
    public const string TicketsEditResolution = "tickets.edit-resolution";

    public const string PrintersViewStatus = "printers.view-status";
    public const string PrintersViewAdmin = "printers.view-admin";
    public const string PrintersViewDetail = "printers.view-detail";
    public const string PrintersViewErrorLogs = "printers.view-error-logs";
    public const string PrintersAddEdit = "printers.add-edit";
    public const string PrintersDelete = "printers.delete";
    public const string PrintersManageModels = "printers.manage-models";
    public const string PrintersClearAlerts = "printers.clear-alerts";
    public const string PrintersAutoSetup = "printers.auto-setup";

    public const string AssetsViewSupervisorConsole = "assets.view-supervisor-console";
    public const string AssetsViewItConsole = "assets.view-it-console";
    public const string AssetsCreate = "assets.create";
    public const string AssetsDelete = "assets.delete";
    public const string AssetsManageModels = "assets.manage-models";
    public const string AssetsScanSearch = "assets.scan-search";
    public const string AssetsAssign = "assets.assign-holder";
    public const string AssetsCheckInOut = "assets.check-in-out";
    public const string AssetsUpdateStatus = "assets.update-status";
    public const string AssetsUpdateFlags = "assets.update-flags";
    public const string AssetsPairScanners = "assets.pair-scanners";
    public const string AssetsIssueSpareLoans = "assets.issue-spare-loans";
    public const string AssetsReturnSpareLoans = "assets.return-spare-loans";

    public const string LockersView = "lockers.view";
    public const string LockersRevealCombos = "lockers.reveal-combos";
    public const string LockersEditMetadata = "lockers.edit-metadata";
    public const string LockersMarkAudits = "lockers.mark-audits";

    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersResetPasswords = "users.reset-passwords";
    public const string UsersChangeTier = "users.change-tier";
    public const string UsersEditAccess = "users.edit-access";

    public const string AdminView = "admin.view";
    public const string AdminRunReports = "admin.run-reports";
    public const string AdminEnterDivisions = "admin.enter-divisions";
    public const string AdminUpdateDivisionSettings = "admin.update-division-settings";
    public const string AdminCreateDivisions = "admin.create-divisions";
    public const string AdminViewLantronix = "admin.view-lantronix";
    public const string AdminPollLantronix = "admin.poll-lantronix";
}
