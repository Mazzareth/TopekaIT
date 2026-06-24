# Avalonia UI Migration Inventory

This is the current Blazor UI surface area to account for before replacing the web UI with Avalonia. It is intentionally descriptive: page, route, visible elements, current display model, and stateful dialogs/panels.

Before implementation, verify the latest stable Avalonia package/template versions from NuGet and pin the migration to that current version.

## Current Shell And Shared Display Model

The app is a compact operations portal, not a marketing site. Most pages use a restrained `.page` container, `.page-head` header, cards, segmented filters, table-like rows, and modal/drawer workflows.

1. **Main app shell**
   - Source: `src/TopekaIT.Web/Components/Layout/MainLayout.razor`
   - Display: full-height `.app.refresh-shell` grid with topbar, horizontal app navigation, fleet pulse strip, scrollable main region, command palettes, toast container, and Blazor error UI.
   - Shared pieces: `TopBar`, `Sidebar`, `FleetPulse`, `CommandPalette`, `PrinterCommandPalette`, `ToastContainer`.

2. **Top bar**
   - Source: `src/TopekaIT.Web/Components/Layout/TopBar.razor`
   - Display: top rail with workspace/division switcher, global search button, light/dark theme segmented toggle, signed-in user chip, profile dropdown, and logout icon.
   - States: division dropdown with search and division options; profile dropdown with change-password link.

3. **Navigation row**
   - Source: `src/TopekaIT.Web/Components/Layout/Sidebar.razor`
   - Display: permission-built horizontal primary and secondary nav rows with icons, active underline, and count badges.
   - Possible destinations: Home/Control room, Printers, Assets, RMA Flow, Station, Tickets/My requests, Lockers, Loaners, Divisions, Lantronix, Users/Workers, Admin.

4. **Login shell**
   - Source: `src/TopekaIT.Web/Components/Layout/LoginLayout.razor`
   - Display: full-height centered auth surface using `.login-wrap`; no topbar/navigation.

5. **Shared primitives to recreate**
   - `Modal`: centered overlay, optional wide mode, header/body/footer slots.
   - `DetailDrawer`: side drawer used by ticket details.
   - `Pill`: compact status labels, often with dot variants.
   - `EmptyState`: icon, title, explanatory copy.
   - `PageHeader`: breadcrumb header with title/subtitle/actions.
   - `SegmentedControl` and `.seg`: compact filter/time-window controls.
   - `TicketRow`, `TicketDetail`, `TicketComposer`: repeated ticket list/detail/create workflow.
   - `PrinterCard`, `StatCard`, `Sparkline`, `Icon`.
   - Asset modals: `AssetDetail`, `AssetEditor`, `ManualAssetLookupModal`, `ScanModal`, `RfidScanModal`, `RfidTagModal`, `LoanIssuer`.

## Auth, Error, And Routing Pages

### Login
Route: `/login`

1. Centered login card titled "Local Warehouse Operations Portal".
2. Optional error message above the form.
3. Username input.
4. Password input.
5. Hidden return URL and antiforgery token.
6. Full-width "Sign in" submit button.
7. Full-width station link below the card: "Use Check-In / Check-Out Station", leading to `/station/equipment`.
8. Display model: `.login-stack` inside `LoginLayout`, one card plus one station button.

### Change Password
Route: `/change-password`

1. Page header with "Change Password".
2. Subtitle changes for forced password change vs normal update.
3. Card-limited form, max width about 420px.
4. Current password input.
5. New password input.
6. Confirm new password input.
7. Optional inline error message.
8. Optional Cancel button when password change is not forced.
9. "Update Password" submit button.

### Error
Route: `/Error`

1. Plain error heading and subheading.
2. Optional request ID.
3. Development-mode guidance text.
4. Display model: basic unstyled content compared with the rest of the app.

### Not Found
Route: `/not-found`

1. Delegates to `PageUnavailablePanel`.
2. Uses the main app shell.

### Enter Division Redirect
Route: `/admin/enter/{DivisionId}`

1. No normal visible UI.
2. Immediately force-navigates to `/auth/enter-division/{DivisionId}`.

## Station Page

### Equipment Station
Routes: `/station/equipment`, `/station/equipment/{DivisionCode}`

1. Header with "Equipment Station" and subtitle "PIN unlock and device scan for check-in / check-out."
2. Clear employee button when an employee is signed in.
3. Clear device button when a scanned device is selected.
4. Optional message banner for success/error.
5. Terminal card containing division strip, PIN panel, and device scan panel.
6. Division strip:
   - Current division display.
   - Saved/locked status text.
   - Division select, disabled when route-locked or employee is signed in.
7. PIN panel:
   - Six-digit numeric PIN input, large centered text.
   - Unlock button.
   - Signed-in pill when validated.
8. Device scan panel:
   - Scan/RFID/tag/serial/IMEI input.
   - Find button.
   - Disabled until employee sign-in.
9. Signed-in employee summary card:
   - Employee name, role, division.
   - Devices out count.
   - Open tickets count.
10. Summary grid:
   - Devices currently out list, clickable rows with state pills.
   - Open tickets list with priority pills.
11. Scanned device panel:
   - Device label, model/serial detail, state pill.
   - Status, holder, locker metadata cells.
   - Action button to check out to current employee when available.
   - Action button to check in when same holder.
   - Blocked message when not available for that employee.
12. Display model: full-width station page, card terminal, two-column input/summary grids, single-column responsive layout under 900px.

## Worker Pages

### Worker Home
Route: `/worker`

1. Greeting header with first name.
2. Optional "New request" button.
3. Three stat cards: my open requests, printers down, resolved this week.
4. Quick-actions card with tiles for new request, printer status, and my requests.
5. Recent requests card.
6. Empty state when no recent requests.
7. Ticket list uses compact `TicketRow`.
8. Modals: `TicketComposer`, `TicketDetail`.
9. Display model: standard page, stat grid, stacked cards, responsive action tile grid.

### My Requests / Division Requests
Routes: `/worker/tickets`, `/manager/tickets`

1. Header title changes between "My requests" and "Requests" depending on access.
2. Optional "New request" button.
3. Segmented filters: Active, Resolved, All, each with counts.
4. Ticket list using `TicketRow`.
5. Empty state when filter has no matches.
6. Modals: `TicketComposer`, `TicketDetail`.
7. Display model: simple page with segment row and vertical ticket list.

### Worker/Manager Printers
Routes: `/worker/printers`, `/manager/printers`

1. Header with "Printers" and exception-first subtitle.
2. Segmented filters: All, Down, Warn.
3. Sort select: Attention, Name, Latency.
4. Auto-refresh/status line with colored dot and last monitor sample.
5. Printer card grid.
6. Empty state when no printers match.
7. Clicking a printer opens `TicketComposer` prefilled for that printer when allowed.
8. Display model: card grid optimized for status scanning.

## Manager Pages

### Manager Home
Route: `/manager`

1. Greeting header.
2. Employee name lookup search with absolute-position dropdown.
3. Lookup result rows show employee, locker, reveal-combo button, device assignment, and status pill.
4. Action grid:
   - Locker console.
   - Loaner console.
   - Open asset console.
   - Check printers.
   - Review crew requests.
   - Optional new request.
5. Three lower cards:
   - Outstanding devices.
   - Recent crew requests.
   - Devices needing attention.
6. Device attention rows can show "Issue spare" button when allowed.
7. Modals: `TicketComposer`, `TicketDetail`, `LoanIssuer`.
8. Display model: standard page, lookup dropdown, action tile grid, then operational summary cards.

### Manager Assets
Route: `/manager/assets`

1. Header with optional Add Asset, RFID Scan, and Scan/focus actions.
2. Persistent scan dock for barcode/serial entry.
3. Scan states:
   - Found asset with Open and Clear buttons.
   - Not found with optional Register button.
   - Locker QR scan can switch to lockers tab and highlight a locker.
4. Tabs: Lockers, SAE Devices, TC77 PODs, Scanners, Batteries.
5. Lockers tab:
   - Filter toolbar: All, Attention, Empty.
   - Locker/person search.
   - Locker card grid with number, section/shared state, occupant, device rows, scanner pairing chips, audit timestamp, and edit button when allowed.
6. Asset tabs:
   - Filter toolbar: All, Available, In Use, Attention.
   - Search by tag, serial, holder.
   - Table columns: Tag/S/N, Type, Model, RFID, Holder, State, Action.
   - Row actions include view, pair scanner, delete when allowed.
7. Modals:
   - `AssetDetail`.
   - `AssetEditor`.
   - `ScanModal`.
   - `RfidScanModal`.
   - Pair Scanner modal.
   - Locker metadata modal.
8. Display model: scan dock, tabs, locker grid or table card, state-colored rows and pills.

### Locker Console
Route: `/manager/lockers`

1. Header with optional Add Locker.
2. Segmented filters: All, Empty, Attention.
3. Search by locker, person, or device.
4. Locker grid of cards:
   - Locker number.
   - Section.
   - Shared badge.
   - Occupant names or unassigned text.
   - Up to four device rows with state.
   - Audit timestamp.
   - Open button.
5. Split inspector appears when a locker is selected.
6. Inspector sections:
   - Locker Details: locker number, section, combo/reveal, lock serial, notes, shared checkbox, Save locker, Audited.
   - Users: assigned user rows, primary badge, remove button, search/add user autocomplete, create-and-assign user panel, temporary password display.
   - Devices: assigned devices, remove button, search/add device, create-and-assign device panel.
7. Add Locker modal with locker number input, Cancel, Create.
8. Display model: `.locker-workspace` split pane, grid-only until selection, then `1fr 380px` inspector; responsive CSS available.

### Loaner Console
Route: `/manager/loaners`

1. Header with "Loaners".
2. Active Loans table:
   - Borrower.
   - Device.
   - Reason.
   - Duration.
   - Date Loaned.
   - Return action.
3. Overdue state shows warning row/pill.
4. Empty state when no active loans.
5. Spare Pool card grid:
   - Asset tag/serial.
   - Model.
   - Status pill.
   - Issue button when available and allowed.
   - On-loan info panel when not available.
6. Modal: `LoanIssuer`.
7. Display model: table card plus responsive spare card grid.

## IT Pages

### Control Room
Route: `/it`

1. Breadcrumb page header: Operations / Control room.
2. Time-window segmented control: 1h, 24h, 7d, 30d.
3. Queue button.
4. Four KPI dash cards:
   - Printer attention.
   - Open tickets.
   - Assets out.
   - Average time to close.
5. Printer fleet tile panel with status-colored printer tiles.
6. Event histogram.
7. Top of queue list with priority stripe/tone.
8. Recent activity list.
9. Empty states for no printers, empty queue, or quiet activity.
10. Modal/drawer: `TicketDetail`.
11. Display model: `dash-grid` KPI row and `dash-two` two-column dashboard blocks.

### IT Ticket Queue
Route: `/it/tickets`

1. Header with "Ticket queue".
2. Segmented filters: Open, In progress, On hold, Resolved, All.
3. Ticket list using `TicketRow`.
4. Empty state when no matching tickets.
5. Modal/drawer: `TicketDetail`.
6. Display model: standard page with header filter row and vertical ticket list.

### IT Asset Console
Route: `/it/assets`

1. Header with optional Add Asset, RFID Scan, and Scan actions.
2. KPI strip:
   - Total SAE.
   - TC77 PODs.
   - With Holder plus loaned count.
   - Attention.
   - In RMA.
   - Missing.
3. Asset workspace grid:
   - Left filter rail.
   - Main asset table.
   - Optional right inspector.
4. Filter rail:
   - Search field.
   - Category checkboxes: SAE Devices, TC77 PODs, Scanners, Batteries.
   - State checkboxes: Available, In Use / Loaned, Attention.
   - Showing count.
5. Asset table rows with tag/SN, category, model, RFID, holder, state.
6. Inspector:
   - Asset identity and metadata.
   - Status flag toggles.
   - Open issue chips.
   - Holder.
   - Paired scanner.
   - Notes.
   - Actions: Detail, RFID, Assign, Check In, Delete.
7. Modals: `ScanModal`, `RfidScanModal`, `AssetDetail`, `AssetEditor`.
8. Display model: `.asset-workspace` grid is `200px 1fr`, expands to `200px 1fr 340px` with inspector.

### Printer Admin
Route: `/it/printers`

1. Header with optional Auto setup and Add printer actions.
2. Health strip:
   - Down.
   - Warnings.
   - Slow latency.
   - Logged errors.
3. Collapsible Auto Printer Setup card:
   - IP addresses textarea.
   - Test button.
   - Run all button.
   - Test/run status text.
   - Results table with IP, status, detected name/model, model select, and run result.
4. Printer toolbar:
   - Filters: All, Attention, Slow.
   - Sort select: Health, Printer, Latency.
5. Printer table:
   - Printer.
   - Department.
   - Model.
   - IP Address.
   - Status.
   - Latency.
   - Trend sparkline.
   - Actions.
6. Inline add/edit row:
   - Name, department, model select or add-model input, IP address, save/cancel.
7. Existing row:
   - Alert blips and clear-alert buttons.
   - Status/latency/trend.
   - Details, edit, delete actions according to permissions.
8. Export Printer Logs card:
   - From/to date and time inputs.
   - All printers checkbox.
   - Export CSV button.
   - Optional printer checkbox list.
9. Printer Error Log card:
   - From/to date filters.
   - Sort select.
   - Apply/Clear.
   - Grouped alert table with expandable raw occurrences and View action.
10. Display model: dense table/card admin console with inline editing.

### Printer Detail
Route: `/it/printers/{Id}`

1. Not-found empty state when printer is missing.
2. Header with back button, printer name, and status pill.
3. Metadata subtitle with model, IP, last ping, latency, consecutive failures.
4. Optional info card for serial, firmware, MAC, location, contact.
5. Uptime chart card for last 24h.
6. Latency chart card for last 24h.
7. Current Alerts card when alerts exist:
   - Severity pill.
   - Alert title/detail.
   - Training badge.
   - Friendly message.
   - Last seen.
   - Clear button when allowed.
8. Printer Log card when allowed:
   - Severity segmented filters: All, Errors, Warnings, Info.
   - Event type select.
   - Time select.
   - From/to date and time fields.
   - Search input.
   - Apply, Export CSV, Clear.
   - Log table with date/time, type pill, message, full raw-message details.
9. Display model: stacked cards with Apex charts and dense log filter toolbar.

### RMA Flow
Routes: `/it/rma`, `/manager/rma`

1. Header with "RMA Flow" and RMA Device action.
2. Optional RMA intake card:
   - Scan input for RFID, asset tag, serial, or IMEI.
   - Find Device button.
   - Success/error message.
   - Matched device summary and identifiers.
   - Send to RMA button.
   - Completion message with attached ticket/RMA record.
3. KPI strip:
   - Active RMAs.
   - Pending Handoff.
   - With DST/RMA.
   - Overdue Returns.
4. List pane:
   - Segmented filters: Active, Pending, With DST, Overdue, Completed.
   - Search field.
   - RMA table columns: Asset, DST/Section, Date Submitted, DST Handoff, Return Expected, Status.
5. Inspector pane:
   - Empty select-a-record state.
   - RMA details card with asset summary.
   - Timeline/stepper:
     - Submitted for RMA.
     - Handoff to Local DST with Confirm DST Handoff.
     - DST/RMA repair progress with reschedule return date inline form.
     - Resolved/complete with return status/comments inline form.
   - Comments log.
6. Display model: `.rma-workspace` grid with main list and sticky 400px inspector, single column under 1024px.

### User Admin / Worker Admin
Routes: `/it/users`, `/manager/users`, `/admin/users`

1. Header title changes between Users and Workers.
2. Selected-count button when rows are selected.
3. Optional Add user button.
4. Role stat strip.
5. Card toolbar:
   - Search name/username/position/locker.
   - Division filter for multi-division actors.
6. Selectable user table:
   - Select checkbox.
   - Name.
   - Username.
   - Division for super admin.
   - Position.
   - Locker.
   - Last active.
   - Role pill.
   - Actions.
7. Inline new/edit row:
   - Name, username, division, position, locker, audit checkbox, role select, password field for new row, save/cancel.
8. Row actions:
   - Reset/set password.
   - Station PIN.
   - Access catalog.
   - Edit.
   - Delete.
9. Footer integrity counts.
10. Modals:
   - Access Catalog wide modal with search, category tabs, tier-grouped permission rows, default/effective/grantable pills, Inherit/Allow/Deny controls.
   - Set Password modal with generated/custom password.
   - Temporary Password modal with copy button.
   - Station PIN modal with 6-digit PIN input and Clear PIN.
11. Display model: table-first admin page with role summary strip and modal permission editor.

## Admin Pages

### Admin Home
Route: `/admin`

1. Header with active tab subtitle.
2. Optional New Division button on Divisions tab.
3. Segmented tabs: Reports, Divisions.
4. Reports tab controls:
   - Report select.
   - Division select.
   - Optional From/To date inputs.
   - Optional sort select.
   - Run button.
   - Optional Clear button.
   - Report description caption.
5. Report output cards:
   - Active Printer Incidents.
   - SNMP Auth & Monitoring.
   - Printer Fault Trends.
   - All Printer Errors.
6. Report rows use table-like `asset-table-head` and `asset-row` layouts with severity pills, grouped expand buttons, detail rows, raw-message details, and View actions.
7. Divisions tab:
   - Table columns: Name, Code, ZIP, Actions.
   - Editable printer password code and ZIP inputs.
   - Save button when allowed.
   - Enter button when allowed.
   - Empty state when no divisions exist.
8. Display model: page with top segmented tab, card-based report controls, dense report tables, editable division table.

### New Division
Route: `/admin/divisions/new`

1. Header "New Division".
2. Card form with max width around 780px.
3. Division code input.
4. Division name input.
5. Printer password code input.
6. Printer password ZIP input.
7. Database preview text.
8. Error text.
9. Cancel and Save buttons.
10. Display model: single provisioning form card.

### Lantronix Devices
Route: `/admin/lantronix`

1. Header with reporting summary.
2. Refresh button.
3. Optional Poll now/Polling button.
4. Loading panel or empty state.
5. Device/detail workspace:
   - Left panel device list with selectable rows.
   - Device row shows name, division, endpoint, status pill.
   - Right detail panel for selected device.
6. Detail panel:
   - Device title, endpoint/hostname, status pill.
   - Four metric tiles: Volume, Height, Water, Temp.
   - Metadata strip: Last poll, Auto poll, Latency, Command, Serial.
   - Tank volume line chart or empty message.
   - Recent polls table with Time, Status, Latency, Volume, Temp, Message.
   - Raw response details per sample.
7. Display model: responsive two-column grid `260-340px` list plus flexible detail; single column below 1040px.

## Shared Asset And Ticket Workflows

### Asset Detail Modal

1. Wide modal opened when an asset is selected.
2. Two-column body: identity/side rail and action/detail area.
3. Identity content: tag, serial, ID, RFID payload, RFID NTAG213 button, scanner type pill.
4. Detail fields: type/category, status, serial, IMEI, quantity, holder warning, notes.
5. Recent loans section with day-loan pill.
6. RMA history section with open/total pills, history cards, RMA status pills, Track RMA link, sent/returned dates.
7. SAE status buttons: InUse, InRMA, Repair, Holding, MIA.
8. Inline RMA initialization form when starting RMA:
   - Section.
   - Tentative return date.
   - Issue comments.
   - Create RMA & Ticket.
   - Cancel.
9. Check-out form for non-SAE in-stock assets.
10. Check-in form for non-SAE out assets with condition buttons.
11. Assignment form with employee search dropdown, Save Assignment, Unassign.
12. Footer: Close and optional Delete device.
13. Nested modal: `RfidTagModal`.

### Asset Editor Modal

1. Add new asset modal.
2. Category options: SAE device, TC77 POD, Scanner, Battery.
3. Conditional form fields per category.
4. Serial prefill support.
5. SAE model select with inline add-new-model path.
6. Validation error message.
7. Save disabled when fields/permissions are not valid.

### Loan Issuer Modal

1. Issue Spare modal.
2. Optional spare dropdown.
3. Employee autocomplete.
4. Reason select.
5. Duration select.
6. Comments textarea.
7. Submit disabled until borrower and spare are selected and permission allows issue.

### Scan And RFID Modals

1. `ManualAssetLookupModal`: shared simple modal with title, lookup input, error, submit, cancel.
2. `ScanModal`: wraps manual lookup for tag/serial/IMEI searches.
3. `RfidScanModal`: wraps manual lookup for RFID/NTAG values.
4. `RfidTagModal`: wide modal showing current RFID link/payload byte count, Generate link, Link value, optional Clear.

### Ticket Workflows

1. `TicketComposer`: modal for creating a request, optionally prefilled for printer/asset context.
2. `TicketRow`: compact or full ticket row with priority/status pills and related user/printer/asset labels.
3. `TicketDetail`: side drawer for viewing/changing ticket status, assignment, resolution, and details.

## Page-To-Route Checklist

1. `/login` - Login.
2. `/change-password` - Change Password.
3. `/station/equipment` and `/station/equipment/{DivisionCode}` - Equipment Station.
4. `/worker` - Worker Home.
5. `/worker/tickets` - My Requests.
6. `/worker/printers` - Worker Printers.
7. `/manager` - Manager Home.
8. `/manager/tickets` - Division Requests via MyTickets.
9. `/manager/printers` - Manager Printers.
10. `/manager/assets` - Manager Assets.
11. `/manager/lockers` - Locker Console.
12. `/manager/loaners` - Loaner Console.
13. `/manager/rma` - RMA Flow.
14. `/manager/users` - Worker Admin.
15. `/it` - Control Room.
16. `/it/tickets` - Ticket Queue.
17. `/it/assets` - IT Asset Console.
18. `/it/printers` - Printer Admin.
19. `/it/printers/{Id}` - Printer Detail.
20. `/it/rma` - RMA Flow.
21. `/it/users` - User Admin.
22. `/admin` - Admin Home.
23. `/admin/divisions/new` - New Division.
24. `/admin/enter/{DivisionId}` - Redirect bridge.
25. `/admin/lantronix` - Lantronix Devices.
26. `/admin/users` - Super-admin User Admin.
27. `/Error` - Error page.
28. `/not-found` - Not Found.
