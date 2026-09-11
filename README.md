# DPD Operations Management PoC (.NET 8)

[![SonarCloud Analysis](https://github.com/Gauthier-Damien/PoC-LabManagement/actions/workflows/sonarcloud.yml/badge.svg)](https://github.com/Gauthier-Damien/PoC-LabManagement/actions/workflows/sonarcloud.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Gauthier-Damien_PoC-LabManagement&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Gauthier-Damien_PoC-LabManagement)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Gauthier-Damien_PoC-LabManagement&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Gauthier-Damien_PoC-LabManagement)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Gauthier-Damien_PoC-LabManagement&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Gauthier-Damien_PoC-LabManagement)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Gauthier-Damien_PoC-LabManagement&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Gauthier-Damien_PoC-LabManagement)

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

## Qualite de code - Tableau de bord SonarCloud

Le pipeline [`.github/workflows/sonarcloud.yml`](.github/workflows/sonarcloud.yml) est decoupe en
**4 paliers** distincts, visibles individuellement dans l'onglet *Actions* de GitHub :

1. **Build** - restauration + compilation rapide de la solution (feedback immediat en cas d'erreur).
2. **Test (matrice)** - 3 jobs paralleles, un par projet de tests (`DPD.Domain.Tests`,
   `DPD.Application.Tests`, `DPD.Integration.Tests`), avec publication des resultats `.trx` en
   artefacts telechargeables.
3. **SonarCloud Quality Analysis** - build instrumente + tests avec couverture (Coverlet, formats
   OpenCover + Cobertura via [`coverlet.runsettings`](coverlet.runsettings)), puis publication sur
   le tableau de bord [SonarCloud](https://sonarcloud.io).
4. **Summary** - recapitulatif final (tableau des statuts + lien direct vers le dashboard) affiche
   dans le *Job Summary* du run GitHub Actions.

Le pipeline se declenche sur chaque push (`main`/`master`/`Backend`) et sur chaque Pull Request
(Quality Gate visible directement dans les checks GitHub de la PR).

### Configuration requise (une seule fois)

1. Creer/relier le depot sur [sonarcloud.io](https://sonarcloud.io) (import direct depuis GitHub).
2. Noter la **cle de projet** (`Project Key`) et l'**organisation** SonarCloud generees.
3. Generer un token d'analyse (My Account > Security > Generate Token).
4. Dans GitHub : `Settings > Secrets and variables > Actions` :
   - Secret **`SONAR_TOKEN`** = le token genere a l'etape precedente.
   - (Optionnel, sinon valeurs par defaut du workflow utilisees) Variables **`SONAR_PROJECT_KEY`**
     et **`SONAR_ORGANIZATION`** si elles different de `Gauthier-Damien_PoC-LabManagement` / `gauthier-damien`.
5. Mettre a jour les URLs des badges ci-dessus si la cle de projet differe.

### Executer l'analyse en local (optionnel)

```powershell
dotnet tool restore
dotnet tool run dotnet-sonarscanner begin /k:"<PROJECT_KEY>" /o:"<ORGANIZATION>" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.token="<SONAR_TOKEN>" /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
dotnet build DPD.sln --configuration Release
dotnet test DPD.sln --configuration Release --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool run dotnet-sonarscanner end /d:sonar.token="<SONAR_TOKEN>"
```

> Pour une instance **SonarQube Server** auto-hebergee plutot que SonarCloud, remplacer uniquement
> `sonar.host.url` par l'URL de l'instance et retirer le parametre `/o:` (organisation), propre a SonarCloud.

