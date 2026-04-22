# AGENTS.md

This file applies to the entire repository.

## Project Summary

EquipmentRental is an ASP.NET Core MVC 10 application for construction equipment rental lifecycle management.

Core flow:

`设备入库 -> 资质审核 -> 线上调度 -> 进场核验 -> 安全交底 -> 使用监管 -> 退场评价`

Start with `docs/progress.md` for current status, then read the relevant doc in `docs/` before making non-trivial changes.

## Tech Stack

- C# 13 / .NET 10 / ASP.NET Core MVC
- EF Core 10 with SQL Server (`AppDbContext`)
- ASP.NET Core Identity + BCrypt password hasher
- Bootstrap 5 + jQuery 3 + Chart.js 4 + Summernote
- QuestPDF for PDF export
- EPPlus for Excel export

Local database is typically SQL Server 2022 in Docker via `equiprental-db`.

## Working Rules

- Keep controllers thin: accept requests, call services, return views/JSON.
- Put business rules, state transitions, and transaction boundaries in `Services/`.
- Keep `Models/Entities` free of business logic.
- Use `ViewModels` for UI-facing data and validation attributes.
- Reuse constants from `Constants/`, especially `Constants/Roles.cs` and `Constants/InspectionChecklist.cs`.
- Do not hardcode role names, status labels, or checklist items when constants already exist.

## Architecture Notes

Registered business services live in `Program.cs`:

- `EquipmentService`
- `QualificationService`
- `AuditService`
- `DispatchService`
- `VerificationService`
- `SafetyService`
- `InspectionService`
- `FaultService`
- `ReturnService`
- `NotificationService`
- `DashboardService`
- `FileService`
- `UserService`

Contract behavior is implemented in `DispatchService`; `ContractController` is only the web layer.

## Security Requirements

- Authorization must be explicit on protected pages and actions.
- CSRF protection is globally enabled in `Program.cs` through `AutoValidateAntiforgeryTokenAttribute`.
- Standard Razor forms with tag helpers are preferred; include antiforgery tokens for manual forms and AJAX POSTs.
- Use EF Core LINQ; do not introduce raw SQL unless there is a strong, documented reason.
- Rich text must be sanitized with `HtmlSanitizer` before persistence.
- Uploaded files must remain under the configured allowlist and be stored in `Uploads/`, not `wwwroot/`.
- Do not change password hashing away from `Infrastructure/BCryptPasswordHasher.cs` without an explicit migration plan.

## Common Commands

```bash
# Start database if needed
docker start equiprental-db

# Apply migrations
dotnet ef database update

# Run app
dotnet run

# Development reload
dotnet watch run

# Build validation
dotnet build --no-restore -v minimal
```

Default local app URL from launch settings is:

- `http://localhost:5085`

## Validation Expectations

- After code changes, run at least the relevant build/test checks for the touched area.
- At minimum, for normal code changes, run `dotnet build --no-restore -v minimal`.
- For UI or workflow changes, verify the affected path in a real browser.
- When you execute or change a user-facing business flow, update or add a note under `docs/qa/` if the change materially affects regression coverage or documented behavior.

## Key Docs

- `docs/PRD.md`: product requirements and role matrix
- `docs/database.md`: schema, fields, enums, indexes
- `docs/architecture.md`: structure and module boundaries
- `docs/deployment.md`: deployment steps
- `docs/user-guide.md`: role-based user operations
- `docs/demo-guide.md`: demo script and happy-path flow
- `docs/progress.md`: current project status
- `docs/qa/`: E2E regression records

## Guidance For Agentic Changes

- Prefer targeted changes over broad refactors.
- Preserve documented business states and transitions unless the task explicitly changes product behavior.
- If code and docs disagree, confirm the intended source of truth and update both when appropriate.
- If you touch a workflow spanning multiple roles, validate the full state transition, not only the page you edited.
