# IT Portal — Implementation Plan

## Current Remediation Track — May 14, 2026

The active rollout is the multi-division remediation pass, not a new A-H feature batch. It standardizes user-facing roles to `Super Admin`, `Admin`, `Supervisor`, and `Worker`; fixes supervisor request visibility; narrows worker ticket asset selection; improves users/access search; retires the fake QR visual; hardens password and locker-combo handling; adds retention and health endpoints; and adds an Admin/Super Admin PrintNet telnet command palette based on the provided PrintNet manual.

For this pass, use `todo.md` as the worker assignment ledger. Each agent is scoped to one numbered task and reports blockers to the Coordinator.

Eight features total. A–D are device/ticket workflow improvements. E–H are the manager layer (Aubrey/Alex priority).

Features build loosely in order: A and B are independent. C depends on B being stable. D is independent. E–H are all independent of A–D but share the autocomplete pattern introduced in B.

---

## [x] Feature A — SAE Device: Add Flow

**What it does:** Cleans up the "Add Asset" modal for SAE devices. S/N becomes the sole identifier (no separate asset tag field). Model becomes a dropdown backed by a new database-driven catalog — seeded with WT6000, with an inline "add new model" escape hatch. Scanner Type field removed entirely.

---

`src\TopekaIT.Web\Components\Shared\AssetEditor.razor`
- Inject `AssetModelService`
- Load available models on init via `AssetModelService.GetAllAsync()`
- For the SAE branch: replace the free-text Model `<input>` with a `<select>` bound to those models, plus a final `<option>` that reads "Add new model…" which reveals an inline text input + confirm button
- Collapse the Tag and Serial fields into a single "S/N / Asset ID" field — write the value to `_draft.Tag` and `_draft.Serial` simultaneously (they're the same number on these devices)
- Remove the Scanner Type `<select>` block from the SAE branch entirely

`src\TopekaIT.Core\Domain\Entities\Asset.cs`
- `ScannerType` and `IsSAE` fields are now dead — leave them in place for this iteration (removing them requires a migration and they won't hurt anything), but stop populating them from the editor

**NEW** `src\TopekaIT.Core\Domain\Entities\AssetModel.cs`
- Properties: `Id` (string), `Name` (string)
- Mirror the shape of `PrinterModel.cs`

**NEW** `src\TopekaIT.Core\Ports\IAssetModelRepository.cs`
- `Task<IEnumerable<AssetModel>> GetAllAsync()`
- `Task AddAsync(AssetModel model)`

**NEW** `src\TopekaIT.Core\Services\AssetModelService.cs`
- `GetAllAsync()` — delegates to repository
- `AddAsync(string name)` — creates and persists a new model entry

**NEW** `src\TopekaIT.Infrastructure\Repositories\AssetModelRepository.cs`
- EF Core implementation of `IAssetModelRepository`
- Use the division `TopekaDbContext`

**NEW** `src\TopekaIT.Infrastructure\Data\Configurations\AssetModelConfig.cs`
- Table name `AssetModels`, `Id` as primary key, `Name` required

`src\TopekaIT.Infrastructure\Seed\DataSeeder.cs`
- After the existing PrinterModel seed, add WT6000 as the first AssetModel entry if the table is empty

`src\TopekaIT.Web\Program.cs` (or wherever DI is registered)
- Register `AssetModelRepository` as `IAssetModelRepository` (scoped)
- Register `AssetModelService` (scoped)

**NEW Migration**
- Add `AssetModels` table (Id, Name)
- No changes needed to the Assets table for this feature

---

## [x] Feature B — SAE Device: Edit / Assign Flow

**What it does:** Replaces the static employee dropdown in `AssetDetail` with a type-to-filter autocomplete. Adds an explicit Status panel so a manager can transition a device to InRMA, Repair, Holding, MIA, etc. without going through the check-in/check-out flow. Separates "assign a holder" from the loaner model — SAE devices are permanently assigned, not borrowed.

---

`src\TopekaIT.Web\Components\Shared\AssetDetail.razor`
- Replace the `<select @bind="_holder">` with:
  - A text `<input>` bound to a `_holderSearch` string
  - An `@oninput` handler that filters the `Workers` list to names containing the typed string
  - A small dropdown list rendered below the input showing matches — clicking one sets `_holder`
  - A "Can't find them? Add employee →" link at the bottom of the dropdown that navigates to `/it/users`
- Add a **Status** section (visible for all non-Battery assets, not just when checking out):
  - Button group: In Use · In RMA · Repair · Holding · MIA — clicking one calls `AssetService.SetStatusAsync`
  - Current status button should appear selected/active
  - Only show statuses that make sense for SAE (exclude `In`, `Out`, `Spare`, `Loaned`, `InLocker`, `InCC` from this quick panel — those are internal states)
- The existing check-out / check-in block should only appear for non-SAE assets (TC77, Battery); for SAE the new "Assign to" + "Status" panels replace it

`src\TopekaIT.Core\Services\AssetService.cs`
- Add `SetStatusAsync(string assetId, AssetStatus newStatus, string actorName)`
  - Loads asset, sets `Status`, saves
  - Calls `ActivityService.PushAsync` with a descriptive message
- Add `AssignHolderAsync(string assetId, string userId, string actorName)`
  - Sets `HolderId` and `CheckedOutAt = DateTimeOffset.UtcNow`, saves
  - Calls `ActivityService.PushAsync`
  - If `userId` is empty string, clears `HolderId` (unassign)

`src\TopekaIT.Web\Components\Pages\Shared\AssetConsole.razor`
- In the action button column: for SAE category assets, change the button label from "Check out" → "Assign" and pass `"assign"` as the action string to `AssetDetail`
- `AssetDetail` should recognize the `"assign"` action and show the assign panel rather than the checkout panel

---

## [x] Feature C — Auto-Ticket on RMA / Repair

**What it does:** When a device's status is set to `InRMA` or `Repair` (via the new Status panel in Feature B), the system automatically creates a ticket in the IT queue, pre-linked to that asset. IT sees it immediately without the manager filing a separate request.

**Note on architecture:** To avoid a circular service dependency (AssetService → TicketService → AssetService), trigger the ticket creation from the UI layer (`AssetDetail`), not inside `AssetService.SetStatusAsync`.

---

`src\TopekaIT.Web\Components\Shared\AssetDetail.razor`
- After calling `AssetService.SetStatusAsync` and the new status is `InRMA` or `Repair`:
  - Call `TicketService.CreateForRepairAsync(asset.Id, asset.Tag ?? asset.Serial, currentUserId, newStatus)`
  - Push a toast: `"Device marked [status] — IT ticket created"`
- Inject `TicketService`

`src\TopekaIT.Core\Services\TicketService.cs`
- Add `CreateForRepairAsync(string assetId, string assetLabel, string reportedByUserId, AssetStatus status)`
  - Builds a `Ticket` with:
    - `Title` = `$"Device {assetLabel} — {status}"` (e.g. "Device 380021 — InRMA")
    - `Description` = auto-populated context string
    - `AssetType` = `AssetKind.Asset`, `AssetId` = assetId
    - `Priority` = `TicketPriority.High`
    - `Status` = `TicketStatus.Open`
  - Calls the existing create path / repository

---

## [x] Feature D — Ticket UX Refinements

**What it does:** Priority is now visible in the ticket list. Assignee is editable inline in the detail view. Resolution has an editable text area. Tickets feel like a workspace, not a read-only report.

---

`src\TopekaIT.Web\Components\Shared\TicketRow.razor`
- Add a `Priority` pill next to the `Status` pill
- Use accent colors to distinguish urgency: Urgent → red border-left on the row, High → orange, Med/Low → default
- Tighten the layout so asset label + reporter name fit on one line, timestamp on the line below
- The `Compact` mode doesn't need changes

`src\TopekaIT.Web\Components\Shared\TicketDetail.razor`
- **Assignee**: replace the read-only name display with an inline picker for IT/SuperAdmin users — same type-to-filter autocomplete pattern as Feature B, bound to `_assigneeId`, with a Save button that calls `TicketService.UpdateAssigneeAsync`
- **Resolution**: replace the read-only resolution block with an editable `<textarea>` (when `_canEdit`), with a Save button calling `TicketService.UpdateResolutionAsync`
- **Status buttons**: give the currently-active status a stronger visual treatment (filled background, not just outline)
- Add `UpdatedAt` display at the bottom of the sidebar column

`src\TopekaIT.Web\Components\Shared\TicketComposer.razor`
- Add optional `PresetTitle` and `PresetDescription` parameters so Feature C can open the composer pre-filled (if you want the manager to review before submitting rather than auto-submitting)
- No structural changes needed if auto-submit is preferred (Feature C calls `TicketService` directly)

`src\TopekaIT.Core\Services\TicketService.cs`
- Add `UpdateAssigneeAsync(string ticketId, string assigneeId)` — sets `AssigneeId`, updates `UpdatedAt`, saves
- Add `UpdateResolutionAsync(string ticketId, string resolution)` — sets `Resolution`, updates `UpdatedAt`, saves

`src\TopekaIT.Web\Components\Pages\IT\TicketQueue.razor`
- No structural changes needed; benefits automatically from row/detail improvements

---

## [x] Feature E — Employee Quick Lookup

**What it does:** A prominent search panel on the Manager home — the VLOOKUP replacement. Aubrey types a name (or partial name) and instantly sees that person's locker number, combo, lock serial, device S/N, and device status. No more scrolling a spreadsheet.

---

`src\TopekaIT.Web\Components\Pages\Manager\ManagerHome.razor`
- Add `_lookupQuery` string and a search `<input>` at the top of the page, above the action grid
- On input change, filter `_users` list by name (case-insensitive contains)
- Render results as compact cards below the input: Name · Position · Locker # · Combo (hidden behind a "reveal" button) · Device S/N · Device Status chip
- To get the device: build a `Dictionary<string, Asset>` keyed by `HolderId` from the existing assets load — no new service call needed
- Clear results when the input is emptied or Escape is pressed
- Locker combo should be click-to-reveal (not shown in plaintext by default)

`src\TopekaIT.Core\Services\AssetService.cs`
- No new methods required — `GetAllAsync()` is already called on this page; index by `HolderId` in component code

---

## [x] Feature F — Locker Console

**What it does:** A new Manager page where every physical locker is a visual tile. TAKEN / AVAILABLE / ATTENTION status at a glance. Click any tile → full detail panel with combo, lock serial, audit toggle, device info, active loan, and notes. Divided into Floor Lockers (1–90) and Control Center (110–115). Replaces the row-by-row spreadsheet.

---

**NEW** `src\TopekaIT.Web\Components\Pages\Manager\LockerConsole.razor`
- Route: `/manager/lockers`
- On init: load all users and all assets
- Build a lookup: for each user with a `LockerNumber`, map `LockerNumber → (User, Asset)` where the Asset is the one whose `HolderId == user.Id`
- Also account for open lockers — you'll need a known list of locker numbers (1–90, 110–115) to show empty slots. Consider a static range or a future DB table. For now, generate the range and mark any locker not in the user map as AVAILABLE.
- Render as a CSS grid of tiles; each tile shows: locker number, employee name (or "Open"), device S/N, status chip
- Color coding: AVAILABLE = muted/dim, TAKEN = normal, ATTENTION (device in InRMA / MIA / Repair / Investigating) = warning highlight
- Click a tile → open a `<Modal>` showing:
  - Employee: Name, Position, User ID
  - Locker: Combo (revealed on click), Lock Serial, Audit checkbox
  - Device: Tag/S/N, Model, Status, Notes
  - Active Loan: if any LoanRecord with `DateReturned == null` for that device, show borrower name + duration
- Audit checkbox in the modal calls `UserService.SetAuditAsync(userId, value)` and updates local state
- Two `<section>` blocks: "Floor Lockers" (numbers 1–90) then "Control Center" (110–115)

`src\TopekaIT.Core\Services\UserService.cs`
- Add `SetAuditAsync(string userId, bool value)` — loads user, sets `Audit`, saves

`src\TopekaIT.Web\Components\Pages\Manager\ManagerHome.razor`
- Add a "Locker console" button to the `manager-action-grid`, linking to `/manager/lockers`
- Subtitle: show count of AVAILABLE lockers

Nav / layout (check `src\TopekaIT.Web\Components\Layout\` for where manager nav links are defined)
- Add a link to `/manager/lockers` in the manager sidebar/nav

---

## [x] Feature G — Spare Pool & Loaner Sign-Out

**What it does:** A dedicated Loaner Console page. Two sections: the spare pool (which spare units exist and which are free), and active loans (who has a spare right now, since when, overdue flag). "Issue spare" modal mirrors the workbook's sign-out section with reason codes and duration categories. "Mark Returned" closes the loan.

---

**NEW** `src\TopekaIT.Web\Components\Pages\Manager\LoanerConsole.razor`
- Route: `/manager/loaners`
- On init: call `AssetService.GetSparePoolAsync()` and `AssetService.GetActiveLoansAsync()`; load all users
- **Section 1 — Active Loans:**
  - Table: Borrower name, Spare S/N/Tag, Reason, Duration, Date Loaned, Overdue flag, "Mark Returned" button
  - Overdue logic: if `IsDayLoan == true` and `DateLoaned.Date < today` → flag red. For non-day loans, flag if loaned > 7 days and duration was `LessThanWeek`, etc.
  - "Mark Returned" calls `AssetService.ReturnSpareAsync(loanRecord.Id, actorName)`
- **Section 2 — Spare Pool:**
  - Cards for each spare asset: S/N, Asset Tag, Model, current Status chip
  - Available spares (Status = Spare, no active loan) show an "Issue →" button
  - On-loan spares show who has them (read from active loans cross-reference)
  - "Issue →" opens `LoanIssuer` modal with that spare pre-selected

**NEW** `src\TopekaIT.Web\Components\Shared\LoanIssuer.razor`
- Parameters: `[Parameter] public Asset? Spare { get; set; }`, `EventCallback OnIssued`, `EventCallback OnClose`
- Fields:
  - Employee (autocomplete search, same pattern as Feature B — filters all users)
  - Reason dropdown: Unit In RMA / Assigned Unit MIA / Unassigned / Assessing for RMA
  - Duration dropdown: Day Loan / Less Than 1 Week / More Than 1 Week (RMA) / TBD
  - Comments text area
- On submit: calls `AssetService.IssueSpareAsync(spare.Id, selectedUserId, reason, duration, comments, actorName)`
- Day Loan selection auto-sets `IsDayLoan = true` on the record

**NEW** `src\TopekaIT.Core\Domain\Enums\LoanDuration.cs`
```
public enum LoanDuration { DayLoan, LessThanWeek, MoreThanWeek, TbdRma, TbdMia }
```

`src\TopekaIT.Core\Domain\Entities\LoanRecord.cs`
- Add `public LoanDuration Duration { get; set; }`
- `IsDayLoan` stays — it's the fast overdue check. `Duration` is the display/reporting label.

`src\TopekaIT.Core\Services\AssetService.cs`
- Add `IssueSpareAsync(string spareAssetId, string borrowerId, string reason, LoanDuration duration, string comments, string actorName)`:
  - Creates a `LoanRecord` with all fields populated, `DateLoaned = now`, `IsDayLoan = (duration == LoanDuration.DayLoan)`
  - Sets spare asset `Status = AssetStatus.Loaned`
  - Logs activity
- Add `ReturnSpareAsync(string loanRecordId, string actorName)`:
  - Sets `LoanRecord.DateReturned = now`
  - Sets asset `Status = AssetStatus.Spare`
  - Logs activity

`src\TopekaIT.Core\Ports\IAssetRepository.cs`
- Add `Task<IEnumerable<Asset>> GetSparePoolAsync()`
- Add `Task<IEnumerable<LoanRecord>> GetActiveLoansAsync()`

`src\TopekaIT.Infrastructure\Repositories\AssetRepository.cs`
- Implement `GetSparePoolAsync()` — assets where `Status == Spare`, include `LoanRecords`
- Implement `GetActiveLoansAsync()` — `LoanRecords` where `DateReturned == null`, include `Asset`

**NEW Migration**
- Add `Duration` (int, for enum) column to `LoanRecords` table

`src\TopekaIT.Web\Components\Pages\Manager\ManagerHome.razor`
- Add "Loaner console" to the action grid, subtitle: active loan count

---

## [x] Feature H — RMA & Attention Board

**What it does:** A dashboard card on the Manager home showing all devices currently in a bad state (InRMA, MIA, Repair, Investigating). Each row shows the assigned employee and how long the device has been in that state. Quick "Issue spare →" action inline so the manager can act without navigating away.

---

`src\TopekaIT.Web\Components\Pages\Manager\ManagerHome.razor`
- Add a third card below the existing two: "Devices needing attention"
- Filter `assets` (already loaded) to those where `Status` is in `{ InRMA, MIA, Repair, Investigating, Holding }`
- Each row: Device tag · Model · Employee name (from `_users`) · Status chip · Duration in this state (`Format.RelTime(asset.CheckedOutAt ?? asset.UpdatedAt)` — or you may need a `StatusChangedAt` field for precision)
- Inline "Issue spare →" button that opens `LoanIssuer` with `ReportedForUserId` pre-filled to that device's holder
- "View all →" link navigates to `/manager/assets` with the Attention filter pre-applied
- If no devices in attention states, show a clean empty state ("All devices healthy")

`src\TopekaIT.Core\Domain\Entities\Asset.cs`
- Consider adding `StatusChangedAt` (DateTimeOffset?) so you can show how long a device has been in its current state — currently there's no reliable timestamp for this other than `CheckedOutAt`
- This is optional but significantly improves the attention board's usefulness

**NEW Migration** (if StatusChangedAt is added)
- Add `StatusChangedAt` nullable column to `Assets` table
- Populate via `AssetService.SetStatusAsync` whenever status changes

`src\TopekaIT.Web\Components\Pages\Shared\AssetConsole.razor`
- No structural changes needed; existing "Attention" filter tab already serves the IT view of the same data

---

## New Files Summary

```
src\TopekaIT.Core\Domain\Entities\AssetModel.cs              [Feature A]
src\TopekaIT.Core\Ports\IAssetModelRepository.cs             [Feature A]
src\TopekaIT.Core\Services\AssetModelService.cs              [Feature A]
src\TopekaIT.Infrastructure\Repositories\AssetModelRepository.cs   [Feature A]
src\TopekaIT.Infrastructure\Data\Configurations\AssetModelConfig.cs [Feature A]

src\TopekaIT.Core\Domain\Enums\LoanDuration.cs               [Feature G]

src\TopekaIT.Web\Components\Pages\Manager\LockerConsole.razor [Feature F]
src\TopekaIT.Web\Components\Pages\Manager\LoanerConsole.razor [Feature G]
src\TopekaIT.Web\Components\Shared\LoanIssuer.razor           [Feature G]
```

## Migration Summary

| Migration | Feature | Change |
|-----------|---------|--------|
| AddAssetModels | A | New `AssetModels` table |
| AddLoanDuration | G | `Duration` column on `LoanRecords` |
| AddAssetStatusChangedAt | H | `StatusChangedAt` column on `Assets` (optional) |
