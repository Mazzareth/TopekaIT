# Repository Guidelines

## Project Structure & Module Organization

This is a .NET 10 solution for the Topeka IT Portal. The solution file is `6IA-IT-Portal.slnx`.

- `src/TopekaIT.Core`: domain entities, enums, service logic, and repository ports.
- `src/TopekaIT.Infrastructure`: EF Core `DbContext`, migrations, configurations, repositories, dependency injection, and seed data.
- `src/TopekaIT.Web`: Blazor app, pages, shared components, layouts, controllers, hosted services, static assets, and app settings.
- `src/TopekaIT.DeploymentChecker`: local WPF deployment/status helper included in solution builds, but not part of the Blazor web runtime.
- `References`: source notes and business reference material.
- `plans`: implementation notes for planned or completed fixes.

Keep domain rules in Core, persistence details in Infrastructure, and UI or web-hosting concerns in Web.

## Build, Test, and Development Commands

- `dotnet restore 6IA-IT-Portal.slnx`: restore NuGet packages.
- `dotnet build 6IA-IT-Portal.slnx`: compile all projects.
- `dotnet run --project src/TopekaIT.Web/TopekaIT.Web.csproj`: run the Blazor app locally. (Note: Database migrations are automatically applied on app startup via `DataSeeder.cs`).
- `dotnet ef migrations add <Name> --context MasterDbContext --output-dir Data/MasterMigrations --project src/TopekaIT.Infrastructure --startup-project src/TopekaIT.Web`: create a migration for the master database.
- `dotnet ef migrations add <Name> --context TopekaDbContext --output-dir Data/Migrations --project src/TopekaIT.Infrastructure --startup-project src/TopekaIT.Web`: create a migration for the tenant/division database.

Run commands from the repository root unless noted otherwise.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Prefer 4-space indentation, file-scoped namespaces for new files, PascalCase for public types and members, camelCase for locals and parameters, and `I...` prefixes for interfaces such as `IAssetRepository`.

Razor components use PascalCase filenames, for example `PrinterEditor.razor`. Keep component-specific CSS beside the component as `.razor.css`. Favor dependency injection through constructors or standard Blazor `@inject` usage, matching nearby code.

## Testing Guidelines

Focused test projects live under `tests/`, including Core, Infrastructure, and Web source/behavior tests. For new behavior, add focused tests under `tests/` using names like `TopekaIT.Core.Tests` or `TopekaIT.Infrastructure.Tests`. Name test classes after the subject under test, for example `TicketServiceTests`, and use method names that describe the expected behavior.

Before submitting changes, at minimum run `dotnet build 6IA-IT-Portal.slnx`. If tests are added, run `dotnet test`.

## Commit & Pull Request Guidelines

This checkout does not include Git history, so use concise, imperative commit subjects such as `Add printer event retention cleanup` or `Fix ticket queue filtering`. Keep commits scoped to one logical change.

Pull requests should include a short summary, verification steps, related issue or plan references, and screenshots for UI changes. Call out database migrations, appsettings changes, or operational impacts such as printer monitoring or SNMP behavior.

## Security & Configuration Tips

Do not commit real credentials or production connection strings. Use `appsettings.Development.json` only for local-safe values and prefer user secrets or environment variables for sensitive settings.
