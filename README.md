# Dofus Optimizer

Bascule rapide entre plusieurs clients DOFUS, pilotée au clavier et à la souris.
L'application n'interagit jamais avec le processus du jeu : elle s'appuie
exclusivement sur les API fenêtres Windows (`user32`).

**Version courante : 1.0.0** — 2026-09-24

## Fonctionnalités

- Détection temps réel des clients DOFUS ouverts.
- Rotation cyclique *suivant*/*précédent* entre les comptes, avec saut des comptes
  absents ou exclus.
- Activation directe d'un compte par un raccourci dédié.
- Raccourcis personnalisables (touche clavier ou bouton souris, boutons
  auxiliaires inclus).
- Interception conditionnée au focus d'un client DOFUS (aucun effet ailleurs).
- Réordonnancement et exclusion des comptes.
- Icône de zone de notification (tray) : suspendre/réactiver, ouvrir, quitter.
- Suspension globale, démarrage avec Windows (optionnel), export/import de la
  configuration.

## Prérequis

- Windows 10/11 (x64).
- [.NET SDK 10](https://dotnet.microsoft.com/) pour construire depuis les sources.

## Construire et lancer

```powershell
# Lancer en développement
dotnet run --project src/App/App.csproj

# Itérer sur l'UI avec rechargement à chaud du XAML
dotnet watch --project src/App/App.csproj

# Lancer les tests
dotnet test tests/App.Tests/App.Tests.csproj
```

La configuration est stockée dans
`%APPDATA%\DofusSwitcher\config.json`.

## Licence

Usage privé.
