# Topeka IT Portal - Codebase Documentation

This document provides a comprehensive overview of the `6IA-IT-Portal` solution. The solution is built with .NET 10 and uses Blazor for the web front-end, following Clean Architecture principles.

## Project Structure

The solution (`6IA-IT-Portal.slnx`) is divided into three main projects:

### 1. TopekaIT.Core (`src/TopekaIT.Core`)
This is the heart of the application, containing all domain rules and business logic.
- **Domain**: Contains the core business models (`Entities`) and standard data types (`Enums`).
- **Ports**: Defines repository interfaces and other dependencies that the core layer needs but does not implement.
- **Services**: Contains the primary business logic and use cases, interacting with domain entities via the defined ports.

### 2. TopekaIT.Infrastructure (`src/TopekaIT.Infrastructure`)
This layer handles external concerns, primarily data persistence.
- **Data**: Contains the EF Core `DbContext`, entity `Configurations`, and `Migrations`.
- **Repositories**: Implements the repository interfaces defined in the Core project (`Ports`).
- **Seed**: Logic for populating the database with initial/default data.
- **Tenant**: Components for handling multi-tenancy or tenant-specific logic if applicable.

### 3. TopekaIT.Web (`src/TopekaIT.Web`)
This is the user interface and web host for the portal.
- **Components**: Shared Blazor components and `Layout` elements.
- **Pages**: Role-specific UI views organized by function: `Admin`, `IT`, `Manager`, and `Worker`.
- **Controllers**: API endpoints for client-side or external interactions.
- **Services**: Web-specific services (e.g., hosted background services, UI state management).
- **wwwroot**: Static assets including JavaScript (`js`), CSS, and images.

## Architecture & Principles
- **Clean Architecture**: Domain rules live purely in `Core`. Persistence details (EF Core) are strictly contained within `Infrastructure`. Web hosting and UI concerns are isolated to `Web`.
- **Dependency Injection**: Interfaces (e.g., `IAssetRepository`) are defined in `Core` and implemented in `Infrastructure`, then injected into services and Blazor components (`@inject`).
- **Nullable Types**: Nullable reference types and implicit usings are enabled globally.

## Development & Operations

### Useful Commands
- **Restore Dependencies**: `dotnet restore 6IA-IT-Portal.slnx`
- **Build**: `dotnet build 6IA-IT-Portal.slnx`
- **Run Application**: `dotnet run --project src/TopekaIT.Web/TopekaIT.Web.csproj`
- **Run Tests**: `dotnet test` (Tests should be placed in the `tests/` directory and follow the `SubjectUnder_ExpectedBehavior` naming convention).

### Database Migrations Workflow

Because this application runs as a single instance per device, `Program.cs` and `DataSeeder.cs` are configured to automatically apply pending database migrations using `await db.Database.MigrateAsync()` during application startup. Your developer workflow for updating the schema is:
1. Make changes to the domain models in `TopekaIT.Core`.
2. Generate the migration locally using the appropriate command below.
3. Commit the generated migration files. Upon deployment and restart, the server will automatically apply the changes to the existing production database.

**To create a migration for the Master Database (`MasterDbContext`):**
```bash
dotnet ef migrations add <Name> --context MasterDbContext --output-dir Data/MasterMigrations --project src/TopekaIT.Infrastructure --startup-project src/TopekaIT.Web
```

**To create a migration for the Tenant/Division Database (`TopekaDbContext`):**
```bash
dotnet ef migrations add <Name> --context TopekaDbContext --output-dir Data/Migrations --project src/TopekaIT.Infrastructure --startup-project src/TopekaIT.Web
```

### Code Style Guidelines
- 4-space indentation.
- File-scoped namespaces for new files.
- PascalCase for public types/members.
- camelCase for locals and parameters.
- Razor components use PascalCase filenames (e.g., `PrinterEditor.razor`) and keep their specific CSS beside them (e.g., `PrinterEditor.razor.css`).

### Security
- Do not commit production credentials.
- Use `appsettings.Development.json` for local environments.
- Use environment variables or user secrets for sensitive configurations.
