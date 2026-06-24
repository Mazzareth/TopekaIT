using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Access;

/// <summary>
/// A permission card in plain data form: what it is called, where it sits, who gets it by default, and who can hand it out.
/// </summary>
public sealed record AccessPermissionDefinition(
    string Key,
    string DisplayName,
    string Group,
    AccessTier DefaultTier,
    AccessTier GrantableTier);
