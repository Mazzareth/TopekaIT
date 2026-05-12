using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Access;

public sealed record AccessPermissionDefinition(
    string Key,
    string DisplayName,
    string Group,
    AccessTier DefaultTier,
    AccessTier GrantableTier);
