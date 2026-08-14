param(
    [string]$Project = ".\src\DPD.Web\DPD.Web.csproj",
    [string]$Urls = "https://localhost:7218;http://localhost:5182",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Project)) {
    throw "Project file not found: $Project"
}

$projectPath = (Resolve-Path $Project).Path
$projectDir = Split-Path -Parent $projectPath

Write-Host "Starting PoC from $projectPath" -ForegroundColor Cyan
Write-Host "Press Ctrl+C or close this terminal to stop the app." -ForegroundColor Yellow

Push-Location $projectDir
try {
    if ($NoBuild) {
        & dotnet run --project "$projectPath" --urls "$Urls" --no-build
    }
    else {
        & dotnet run --project "$projectPath" --urls "$Urls"
    }
}
finally {
    Pop-Location
}
