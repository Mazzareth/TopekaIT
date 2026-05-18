# Agent TODO

Global rule: Your only responsibility is task #X. Only complete this task. If you hit a blocker, report it to the Coordinator. Do not broaden scope.

1. P0 Security cleanup: remove/rehome root `index_script.js` and `package.json` Google helper; add ignore coverage if local scripts remain; report key rotation required.
2. P0 Locker fix: replace detached locker graph updates with tracked/narrow occupant add-unassign methods; add regression tests for assign/unassign.
3. P0 Role model: make four user-facing tiers: Super Admin, Admin, Supervisor, Worker; migrate/normalize IT to Admin; update permissions/tests/routing labels.
4. P0 Password model: add `PasswordIterations` and `MustChangePassword`; preserve legacy login compatibility.
5. P0 PrintNet command catalog: use the provided manual to create safe/protected/excluded telnet commands.
6. P1 Requests: supervisors see crew/all division requests; workers still see their own; admins keep queue access.
7. P1 Ticket composer: workers may select all printers but only non-printer assets assigned to them.
8. P1 Users UI: add search and division filter where actor can see multiple divisions.
9. P1 Access Catalog UI: organize by category/tier with search or tabs; keep underlying permission model intact.
10. P1 Ctrl-K: add ArrowUp/ArrowDown selection, Enter on selected item, and natural scoring for `6I printers` / `6I workers`.
11. P1 Ctrl-` printer palette: open telnet session for Admin/Super Admin only; run allowlisted PrintNet commands; return success/failure; gate writes and `save`.
12. P1 Asset UI: reduce Model column width, move serial under Tag/S/N, remove misleading QR visual, and hide or compute HealthScore.
13. P2 Security hardening: rehash PBKDF2 to 600k after successful legacy verification; force first-login/reset password change.
14. P2 LockCombo encryption: DataProtection EF converters for locker and legacy user combo fields; widen columns and migrate safely.
15. P2 Operations: add retention for PrinterEvents and LantronixPollSamples; keep active alert states intact.
16. P2 Health/docs/dead code: add health endpoints, remove confirmed dead editors/unused registration if still unused, refresh docs and ManagerHome typo.
17. P2 RMA/audit/tenant scope: add active/longest RMA query path; defer AuditEntry/AuditSession removal unless a workflow is implemented; do not fake tenant global query filters while tenant data is separate databases.
