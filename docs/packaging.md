# Packaging & mises à jour automatiques (Velopack)

Dofus Optimizer est distribué via **Velopack** : un installateur `Setup.exe` + des mises à jour
automatiques depuis les **GitHub Releases** du dépôt.

## Pourquoi une exception « zéro NuGet »

L'app respecte la contrainte « zéro `PackageReference` fonctionnel ». Velopack est la **seule**
dépendance runtime autorisée ([DECISION] 2026-09-26) : porter un installateur, les hooks de cycle
de vie et le téléchargement des MAJ est impossible sans lib dédiée. Voir le commentaire de tête de
`src/App/App.csproj`.

## Comportement dans l'app

- `App.OnStartup` appelle **`VelopackApp.Build().Run()` en première instruction** : pendant les hooks
  install/update/désinstallation, l'exe est relancé avec des arguments spéciaux, traités puis le
  process se termine (la fenêtre ne s'affiche pas).
- `Services/UpdateService.cs` (`IUpdateService`) vérifie les MAJ **en arrière-plan au démarrage**,
  télécharge la nouvelle version et l'**applique à la prochaine fermeture** (`WaitExitThenApplyUpdates`)
  — aucune interruption ni redémarrage forcé. Sans-op si l'app n'est pas installée (build de dev) ou
  hors ligne (erreurs avalées).
- Feed configuré : `AppConstants.UpdateFeedRepoUrl`.

## Publication (self-contained, pas single-file)

`Directory.Build.props` : la cible Velopack est activée par `-p:VelopackPack=true` et publie
**self-contained sans `PublishSingleFile`** — Velopack empaquette lui-même et a besoin des fichiers
en vrac pour les *delta updates*. La cible single-file historique reste active hors Velopack.
Jamais `PublishTrimmed`/`PublishAot` (WPF casse).

## Produire une release

Prérequis : bumper la version via `/livraison vX.Y.Z` (met à jour `App.csproj` + CHANGELOG).
La version DOIT être strictement supérieure à la dernière publiée.

### Local (artefacts sans publication)

```powershell
./scripts/release.ps1 -Version 1.4.0
```

Produit dans `artifacts/releases/` : `Setup.exe`, le portable `.zip`, les `.nupkg` (full + delta)
et le fichier `RELEASES`.

### Local avec publication GitHub

```powershell
$env:GITHUB_TOKEN = '<token repo>'
./scripts/release.ps1 -Version 1.4.0 -Upload
```

### CI (recommandé)

Pousser un tag SemVer déclenche `.github/workflows/release.yml`, qui build et publie la release :

```bash
git tag 1.4.0 && git push origin 1.4.0
```

## Outil `vpk`

Épinglé en tant que dev-tool local dans `.config/dotnet-tools.json` (comme xUnit : hors app).
`dotnet tool restore` avant toute commande `dotnet vpk …`.

## Signature de code (décision : non signé)

[DECISION] 2026-09-26 : **pas de signature de code payante**. Conséquence assumée : SmartScreen affiche
« Windows a protégé votre ordinateur » au **premier** lancement de `Setup.exe`. Contournement documenté
pour l'utilisateur (README §Installation) : « Informations complémentaires » → « Exécuter quand même ».

- La friction est **ponctuelle** (première install). Les mises à jour Velopack sont téléchargées et
  appliquées par l'app elle-même, sans passer par le navigateur → pas de nouvel avertissement en pratique.
- La réputation SmartScreen d'un binaire **non signé** se calcule par hash de fichier : elle ne s'accumule
  pas d'une version à l'autre. C'est acceptable pour une diffusion communautaire, à revoir si l'audience grandit.
- Si un jour on veut supprimer l'avertissement sans cert cher : **Azure Trusted Signing** (~10 $/mois) signe
  pendant `vpk pack` (`--signParams` / dlib Trusted Signing) et fait accumuler la réputation sur l'éditeur.
  Un certificat **auto-signé** ne sert que pour un parc de machines contrôlé (import dans les Éditeurs de confiance).

## Limites connues

- **Démarrer avec Windows** (`StartupRegistryService`) écrit le chemin de l'exe courant. Après une MAJ
  Velopack, l'app vit dans un dossier versionné (`current`) ; à valider en recette que l'entrée Run
  reste correcte, sinon pointer vers le shim stable Velopack. Suivi packaging.
