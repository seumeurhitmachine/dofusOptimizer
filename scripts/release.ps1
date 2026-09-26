<#
.SYNOPSIS
    Construit un paquet de release Velopack (installateur + delta updates) pour Dofus Optimizer.

.DESCRIPTION
    Enchaîne : publish self-contained (sans single-file, requis par Velopack) -> vpk download
    (récupère les releases existantes pour calculer les deltas) -> vpk pack.
    Produit dans artifacts/releases : l'installateur (Setup.exe), le portable .zip, les packages
    .nupkg (full + delta) et le fichier RELEASES consommé par l'auto-update.

    Avec -Upload, publie le résultat dans les GitHub Releases du dépôt (nécessite un token dans
    $env:GITHUB_TOKEN). En CI, la publication est gérée par .github/workflows/release.yml.

.PARAMETER Version
    Version SemVer de la release (ex. 1.4.0). DOIT être strictement supérieure à la dernière publiée.

.PARAMETER Upload
    Publie les artefacts sur GitHub Releases (sinon, produit uniquement les fichiers locaux).

.EXAMPLE
    ./scripts/release.ps1 -Version 1.4.0
.EXAMPLE
    $env:GITHUB_TOKEN = '...'; ./scripts/release.ps1 -Version 1.4.0 -Upload
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$Upload
)

$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'artifacts/publish'
$releaseDir = Join-Path $repoRoot 'artifacts/releases'
$repoUrl    = 'https://github.com/seumeurhitmachine/dofusOptimizer'
$packId     = 'DofusOptimizer'
$mainExe    = 'DofusOptimizer.exe'
$iconPath   = Join-Path $repoRoot 'src/App/Resources/app.ico'

Write-Host "== Restauration de l'outil vpk ==" -ForegroundColor Cyan
dotnet tool restore

Write-Host "== Publication ($Version, self-contained sans single-file) ==" -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish "$repoRoot/src/App" -c Release -r win-x64 -p:VelopackPack=true -o $publishDir

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

# Récupère les releases existantes pour produire des mises à jour différentielles. Toléré si aucune
# release n'existe encore (première release Velopack) : on continue avec un paquet complet.
Write-Host "== Récupération des releases existantes (deltas) ==" -ForegroundColor Cyan
$downloadArgs = @('download', 'github', '--repoUrl', $repoUrl, '-o', $releaseDir)
if ($env:GITHUB_TOKEN) { $downloadArgs += @('--token', $env:GITHUB_TOKEN) }
try { dotnet vpk @downloadArgs } catch { Write-Host "  (aucune release existante — paquet complet)" -ForegroundColor Yellow }

Write-Host "== Empaquetage Velopack ==" -ForegroundColor Cyan
dotnet vpk pack `
    --packId $packId `
    --packTitle 'Dofus Optimizer' `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe $mainExe `
    --icon $iconPath `
    -o $releaseDir

if ($Upload) {
    if (-not $env:GITHUB_TOKEN) { throw 'Upload demandé mais $env:GITHUB_TOKEN est vide.' }
    Write-Host "== Publication sur GitHub Releases ==" -ForegroundColor Cyan
    dotnet vpk upload github `
        --repoUrl $repoUrl `
        --token $env:GITHUB_TOKEN `
        -o $releaseDir `
        --publish `
        --releaseName "Dofus Optimizer $Version" `
        --tag $Version
}

Write-Host "== Terminé. Artefacts : $releaseDir ==" -ForegroundColor Green
