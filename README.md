# DPD Operations Management PoC (.NET 8)

Implementation conforme aux documents [Docs/PRD.docx](Docs/PRD.docx), [Docs/SAD.docx](Docs/SAD.docx), [Docs/TDD.docx](Docs/TDD.docx) et [Docs/ADR.docx](Docs/ADR.docx).

## Decisions appliquees strictement

- Monolithe modulaire Clean Architecture (ADR-001)
- Front-end Blazor Web App (ADR-002)
- EF Core avec Soft Delete et AuditInterceptor (ADR-003, ADR-006)
- Authentification abstraite via IUserService (Mock pour PoC, Entra pour prod) (ADR-004)
- RBAC via Authorization Policies (ADR-005)
- Worker de fond pour alertes/calculs asynchrones (ADR-007)
- Referentiel fournisseurs (ADR-010)
- Allocation previsionnelle distincte des TimeEntries (ADR-011)
- Entite documentaire centralisee (ADR-009)

## Structure de la solution

- [DPD.sln](DPD.sln)
- [src/DPD.Domain](src/DPD.Domain)
- [src/DPD.Application](src/DPD.Application)
- [src/DPD.Infrastructure](src/DPD.Infrastructure)
- [src/DPD.Web](src/DPD.Web)
- [tests/DPD.Domain.Tests](tests/DPD.Domain.Tests)
- [tests/DPD.Application.Tests](tests/DPD.Application.Tests)
- [tests/DPD.Integration.Tests](tests/DPD.Integration.Tests)

## Modules metier presents

- Resource Management (FTE, departements AD/FPD/MSI, hierarchie)
- Project and Study Management (statuts projet, portfolio)
- Time Tracking (submit/approve, verrouillage par statut)
- Capacity Planning (projections 3/6/12/24 mois)
- Equipment and Maintenance (reservations, conflits, statut maintenance)
- Reporting and KPI (dashboard + projections)
- Administration et RBAC policies
- Audit and Compliance (trace automatique immutable)

## Base de donnees PoC

- Provider PoC: SQLite (TDD section deployment phase 1)
- Fichier local: `dpd-poc.db`
- Initialisation automatique au demarrage via [src/DPD.Infrastructure/Persistence/AppDbInitializer.cs](src/DPD.Infrastructure/Persistence/AppDbInitializer.cs)

## Demarrage

Prerequis:

- .NET SDK 8+

Commandes:

```powershell
dotnet restore .\DPD.sln
dotnet build .\DPD.sln
dotnet run --project .\src\DPD.Web\DPD.Web.csproj
```

URL:

- App + API: `https://localhost:5001` ou `http://localhost:5000` (selon launch profile)

## Authentification PoC (hors domaine)

Le mode par defaut est `Mock` dans [src/DPD.Web/appsettings.json](src/DPD.Web/appsettings.json).

Vous pouvez simuler les roles via en-tetes HTTP:

- `X-Mock-Role: Scientist`
- `X-Mock-Role: Manager`
- `X-Mock-Role: SD`
- `X-Mock-Role: EquipmentOwner`
- `X-Mock-Role: Admin`

## API interne exposee

- `GET /health`
- `GET /api/dashboard`
- `GET /api/dashboard/capacity`
- `GET /api/projects`
- `POST /api/projects`
- `PATCH /api/projects/{id}/status`
- `GET /api/timesheets`
- `POST /api/timesheets`
- `PATCH /api/timesheets/{id}/approve`
- `GET /api/equipment/reservations`
- `POST /api/equipment/reservations`
- `GET /api/reference/resources`
- `GET /api/reference/studies`
- `GET /api/reference/equipment`

## Securite et conformite

- Validation des commandes par FluentValidation
- Gestion centralisee des exceptions HTTP (400/404/422/409/500)
- Soft delete global via Query Filters
- Audit trail automatique EF Core dans table `AuditLogs`
- RowVersion active pour detection de concurrence
- Serilog configure (console + fichier)

## Bascule production

Le code est prepare pour:

- remplacement `UseSqlite` vers `UseSqlServer`
- swap `MockUserService` vers `EntraIdUserService` via `Authentication:Mode=Entra`
- hebergement IIS / SQL Server / Entra ID selon TDD