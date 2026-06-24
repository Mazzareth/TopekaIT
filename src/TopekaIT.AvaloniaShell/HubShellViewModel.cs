using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopekaIT.AvaloniaShell;

public sealed class HubShellViewModel : INotifyPropertyChanged
{
    private readonly ShellSession _session;
    private HubTileViewModel _selectedTile;

    public HubShellViewModel(ShellSession session, Action signOut)
    {
        _session = session;
        SignOutCommand = new RelayCommand(signOut);
        Sections = HubTileCatalog.Build(session, SelectTile);
        _selectedTile = Sections.SelectMany(section => section.Tiles).FirstOrDefault()
            ?? HubTileViewModel.Empty(SelectTile);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Subtitle => "Native hub built from your current permission data.";

    public string UserDisplayName => _session.User.Name;

    public string AccessSummary
    {
        get
        {
            var division = string.IsNullOrWhiteSpace(_session.User.DivisionId)
                ? "No active division"
                : $"Division {_session.User.DivisionId}";
            return $"{_session.User.TierLabel} - {division} - {_session.Permissions.Count} permissions";
        }
    }

    public IReadOnlyList<HubSectionViewModel> Sections { get; }

    public HubTileViewModel SelectedTile
    {
        get => _selectedTile;
        private set
        {
            if (!ReferenceEquals(_selectedTile, value))
            {
                _selectedTile = value;
                OnPropertyChanged();
            }
        }
    }

    public string TileCountLabel
    {
        get
        {
            var tileCount = Sections.Sum(section => section.Tiles.Count);
            var sectionCount = Sections.Count;
            return $"{tileCount} native placeholders across {sectionCount} permission groups.";
        }
    }

    public ICommand SignOutCommand { get; }

    private void SelectTile(HubTileViewModel tile)
    {
        SelectedTile = tile;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record HubSectionViewModel(string Name, IReadOnlyList<HubTileViewModel> Tiles);

public sealed class HubTileViewModel
{
    private readonly Action<HubTileViewModel> _select;

    public HubTileViewModel(
        string group,
        string title,
        string description,
        string permissionKey,
        string permissionLabel,
        Action<HubTileViewModel> select)
    {
        Group = group;
        Title = title;
        Description = description;
        PermissionKey = permissionKey;
        PermissionLabel = $"Visible because you have: {permissionLabel} ({permissionKey})";
        PlaceholderText = $"{title} is visible because the current database resolved this permission for your account. The native module is not rebuilt yet.";
        _select = select;
        SelectCommand = new RelayCommand(() => _select(this));
    }

    public string Group { get; }

    public string Title { get; }

    public string Description { get; }

    public string PermissionKey { get; }

    public string PermissionLabel { get; }

    public string PlaceholderText { get; }

    public ICommand SelectCommand { get; }

    public static HubTileViewModel Empty(Action<HubTileViewModel> select)
        => new(
            "Hub",
            "No native modules available",
            "No permission-backed hub tiles were returned for this user.",
            "none",
            "No permission",
            select);
}

internal static class HubTileCatalog
{
    private static readonly HubTileDefinition[] Tiles =
    [
        new("Workspaces", "Worker Home", "Personal requests and printer status.", "workspace.worker.home"),
        new("Workspaces", "Supervisor Home", "Shift pulse for gear, crew requests, and devices.", "workspace.supervisor.home"),
        new("Workspaces", "Control Room", "IT pulse across printers, tickets, assets, and activity.", "workspace.it.dashboard"),
        new("Admin / System", "Admin Reports", "Cross-division reports and system attention.", "admin.run-reports"),

        new("Tickets", "New Request", "Create a new IT request.", "tickets.create"),
        new("Tickets", "My Requests", "Track your submitted requests.", "tickets.view-own"),
        new("Tickets", "Ticket Queue", "Review and triage the active queue.", "tickets.view-queue"),

        new("Printers", "Printer Status", "See printer health and availability.", "printers.view-status"),
        new("Printers", "Printer Admin", "Administer printers, models, setup, and alerts.", "printers.view-admin"),
        new("Printers", "Auto Setup", "Run printer setup and telnet-backed actions.", "printers.auto-setup"),

        new("Assets", "Asset Console", "Search, assign, and update warehouse equipment.", "assets.view-supervisor-console"),
        new("Assets", "IT Asset Console", "IT inventory and scanner lookup.", "assets.view-it-console"),
        new("Assets", "Create Assets", "Add new devices or inventory records.", "assets.create"),
        new("Station", "Device Check-In", "Station-facing assignment confirmation and RMA help.", "assets.check-in-out"),
        new("Station", "Station", "Pinned station for check-in and broken-device handoff.", "assets.view-supervisor-console"),
        new("RMA", "RMA Flow", "Send devices through repair/RMA handling.", "assets.view-supervisor-console"),
        new("Loaners", "Loaner Console", "Issue and return spare-loan devices.", "assets.view-supervisor-console"),
        new("Loaners", "Issue Spare", "Issue a spare device to an employee.", "assets.issue-spare-loans"),

        new("Lockers", "Locker Console", "Locker assignments, combos, and audits.", "lockers.view"),
        new("Lockers", "Reveal Combos", "Reveal locker combinations when authorized.", "lockers.reveal-combos"),

        new("Users", "Users / Workers", "Manage visible users for your access tier.", "users.view"),
        new("Users", "Create Users", "Create users in your allowed scope.", "users.create"),
        new("Users", "Edit Access", "Review and edit permission overrides.", "users.edit-access"),

        new("Admin / System", "Divisions", "Create or update division setup.", "admin.create-divisions"),
        new("Admin / System", "Enter Division", "Enter division-scoped IT workspaces.", "admin.enter-divisions"),
        new("Admin / System", "Lantronix", "View Lantronix devices.", "admin.view-lantronix"),
        new("Admin / System", "Poll Lantronix", "Trigger Lantronix polling.", "admin.poll-lantronix"),
    ];

    public static IReadOnlyList<HubSectionViewModel> Build(
        ShellSession session,
        Action<HubTileViewModel> select)
    {
        var catalogLabels = session.PermissionGroups
            .SelectMany(group => group.Permissions)
            .ToDictionary(permission => permission.Key, permission => permission.Label, StringComparer.OrdinalIgnoreCase);

        return Tiles
            .Where(tile => session.Has(tile.PermissionKey))
            .GroupBy(tile => tile.Group)
            .OrderBy(group => group.Key)
            .Select(group => new HubSectionViewModel(
                group.Key,
                group
                    .Select(tile => new HubTileViewModel(
                        tile.Group,
                        tile.Title,
                        tile.Description,
                        tile.PermissionKey,
                        catalogLabels.GetValueOrDefault(tile.PermissionKey, tile.PermissionKey),
                        select))
                    .ToList()))
            .ToList();
    }

    private sealed record HubTileDefinition(
        string Group,
        string Title,
        string Description,
        string PermissionKey);
}
