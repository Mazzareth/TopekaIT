# Fix: ApexCharts `SeriesName` Property Error on Printer Detail Page

## Problem

Clicking the printer detail page (`/it/printers/{Id}`) throws an `InvalidOperationException`:

```
Object of type 'ApexCharts.ApexPointSeries`1[...UptimeDataPoint...]' does not have a property matching the name 'SeriesName'.
```

## Root Cause

The project uses **Blazor-ApexCharts v6.1.0**. In this version, the `ApexPointSeries<T>` component inherits from `ApexBaseSeries<T>`, which exposes the series name via the **`Name`** property — not `SeriesName`.

The code in [`PrinterDetail.razor`](src/TopekaIT.Web/Components/Pages/IT/PrinterDetail.razor) uses `SeriesName` in two places (lines 99 and 122), which is the old/incorrect property name for this library version.

## Fix

A single two-line change in [`src/TopekaIT.Web/Components/Pages/IT/PrinterDetail.razor`](src/TopekaIT.Web/Components/Pages/IT/PrinterDetail.razor):

| Line | Current | Correct |
|------|---------|---------|
| 99 | `SeriesName="Status"` | `Name="Status"` |
| 122 | `SeriesName="Latency"` | `Name="Latency"` |

## Verification

After applying the fix, navigate to any printer detail page (`/it/printers/{id}`) and confirm:
1. The page loads without the `InvalidOperationException`
2. The "Online / Offline — last 24h" uptime chart renders
3. The "Latency (ms) — last 24h" chart renders
