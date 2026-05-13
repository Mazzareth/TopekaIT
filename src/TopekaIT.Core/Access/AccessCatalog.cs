using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Access;

public static class AccessCatalog
{
    private static readonly AccessPermissionDefinition[] _permissions =
    [
        new(AccessPermissionKeys.WorkerHome, "Worker home", "Workspaces", AccessTier.Worker, AccessTier.Worker),
        new(AccessPermissionKeys.SupervisorHome, "Supervisor home", "Workspaces", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.ItDashboard, "IT dashboard", "Workspaces", AccessTier.Admin, AccessTier.Admin),

        new(AccessPermissionKeys.TicketsViewOwn, "View own tickets", "Tickets", AccessTier.Worker, AccessTier.Worker),
        new(AccessPermissionKeys.TicketsCreate, "Create tickets", "Tickets", AccessTier.Worker, AccessTier.Worker),
        new(AccessPermissionKeys.TicketsViewQueue, "View ticket queue", "Tickets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.TicketsEditStatus, "Edit ticket status", "Tickets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.TicketsAssign, "Assign tickets", "Tickets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.TicketsEditResolution, "Edit ticket resolution", "Tickets", AccessTier.Admin, AccessTier.Admin),

        new(AccessPermissionKeys.PrintersViewStatus, "View printer status", "Printers", AccessTier.Worker, AccessTier.Worker),
        new(AccessPermissionKeys.PrintersViewAdmin, "View printer admin", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersViewDetail, "View printer detail and history", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersViewErrorLogs, "View printer error logs", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersAddEdit, "Add or edit printers", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersDelete, "Delete printers", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersManageModels, "Manage printer models", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersClearAlerts, "Clear printer alerts", "Printers", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.PrintersAutoSetup, "Run auto printer setup", "Printers", AccessTier.IT, AccessTier.IT),

        new(AccessPermissionKeys.AssetsViewSupervisorConsole, "View supervisor asset console", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsViewItConsole, "View IT asset console", "Assets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.AssetsCreate, "Create assets", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsDelete, "Delete assets", "Assets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.AssetsManageModels, "Manage asset models", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsScanSearch, "Scan and search assets", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsAssign, "Assign or unassign asset holders", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsCheckInOut, "Check assets in or out", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsUpdateStatus, "Update asset status", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsUpdateFlags, "Update raw asset flags", "Assets", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.AssetsPairScanners, "Pair scanners", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsIssueSpareLoans, "Issue spare loans", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.AssetsReturnSpareLoans, "Return spare loans", "Assets", AccessTier.Supervisor, AccessTier.Supervisor),

        new(AccessPermissionKeys.LockersView, "View lockers", "Lockers", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.LockersRevealCombos, "Reveal locker combos", "Lockers", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.LockersEditMetadata, "Edit locker metadata", "Lockers", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.LockersMarkAudits, "Mark locker audits complete", "Lockers", AccessTier.Supervisor, AccessTier.Supervisor),

        new(AccessPermissionKeys.UsersView, "View users", "Users", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.UsersCreate, "Create users", "Users", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.UsersEdit, "Edit users", "Users", AccessTier.Supervisor, AccessTier.Supervisor),
        new(AccessPermissionKeys.UsersDelete, "Delete users", "Users", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.UsersResetPasswords, "Reset passwords", "Users", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.UsersChangeTier, "Change user tier", "Users", AccessTier.Admin, AccessTier.Admin),
        new(AccessPermissionKeys.UsersEditAccess, "Edit user access", "Users", AccessTier.Supervisor, AccessTier.Supervisor),

        new(AccessPermissionKeys.AdminView, "View admin dashboard", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
        new(AccessPermissionKeys.AdminRunReports, "Run all-division reports", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
        new(AccessPermissionKeys.AdminEnterDivisions, "Enter divisions", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
        new(AccessPermissionKeys.AdminUpdateDivisionSettings, "Update division settings", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
        new(AccessPermissionKeys.AdminCreateDivisions, "Create divisions", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
        new(AccessPermissionKeys.AdminViewLantronix, "View Lantronix devices", "Admin / System", AccessTier.SuperAdmin, AccessTier.Admin),
        new(AccessPermissionKeys.AdminPollLantronix, "Poll Lantronix devices", "Admin / System", AccessTier.SuperAdmin, AccessTier.SuperAdmin),
    ];

    private static readonly IReadOnlyDictionary<string, AccessPermissionDefinition> _byKey =
        _permissions.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<AccessPermissionDefinition> Permissions => _permissions;

    public static bool TryGet(string key, out AccessPermissionDefinition definition)
        => _byKey.TryGetValue(key, out definition!);

    public static IReadOnlySet<string> DefaultPermissionsFor(AccessTier tier)
    {
        if (tier == AccessTier.SuperAdmin)
        {
            return _permissions.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return _permissions
            .Where(p => tier >= p.DefaultTier)
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsPermissionGrantableBy(AccessTier actorTier, AccessPermissionDefinition permission)
        => actorTier > permission.GrantableTier;

    public static bool CanManageTier(AccessTier actorTier, AccessTier targetTier)
        => actorTier == AccessTier.SuperAdmin
            ? targetTier < AccessTier.SuperAdmin
            : actorTier > targetTier;

    public static IEnumerable<AccessTier> AssignableTiersFor(AccessTier actorTier)
        => Enum.GetValues<AccessTier>()
            .Where(t => actorTier == AccessTier.SuperAdmin || actorTier > t)
            .OrderBy(t => t);
}
