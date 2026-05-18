namespace TopekaIT.Core.Domain.Enums;

public enum AccessTier
{
    Worker = 0,
    Supervisor = 1,
    Admin = 2,
    SuperAdmin = 3,
}

public static class AccessTierExtensions
{
    public static AccessTier Normalize(this AccessTier tier) => tier;

    public static bool TryParseTier(string? value, out AccessTier tier)
    {
        tier = AccessTier.Worker;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var normalized = value.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (normalized.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            tier = AccessTier.Supervisor;
            return true;
        }

        if (normalized.Equals("IT", StringComparison.OrdinalIgnoreCase))
        {
            tier = AccessTier.Admin;
            return true;
        }

        if (normalized.All(char.IsDigit)) return false;

        return Enum.TryParse(normalized, ignoreCase: true, out tier)
            && Enum.IsDefined(tier);
    }

    public static AccessTier ParseTierOrWorker(string? value)
        => TryParseTier(value, out var tier) ? tier : AccessTier.Worker;

    public static string DisplayName(this AccessTier tier) => tier switch
    {
        AccessTier.SuperAdmin => "Super Admin",
        _ => tier.ToString(),
    };

    public static bool IsAbove(this AccessTier actorTier, AccessTier targetTier)
        => actorTier > targetTier;
}
