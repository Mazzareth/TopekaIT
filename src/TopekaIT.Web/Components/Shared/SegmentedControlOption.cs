namespace TopekaIT.Web.Components.Shared;

/// <summary>
/// One option in a segmented control. Count is optional because some filters have numbers and some are just modes.
/// </summary>
public sealed record SegmentedControlOption(string Id, string Label, string? Count = null, bool Disabled = false);
