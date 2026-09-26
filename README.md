# Dofus Optimizer

Bascule rapide entre plusieurs clients DOFUS, pilotée au clavier et à la souris.
L'application n'interagit jamais avec le processus du jeu : elle s'appuie
exclusivement sur les API fenêtres Windows (`user32`).

**Version courante : 1.2.0** — 2026-09-26

## Fonctionnalités

- Détection temps réel des clients DOFUS ouverts.
- Distinction **Compte** / **personnage** : liaison manuelle personnage → compte
  depuis l'onglet Comptes, gestion des comptes dans les Réglages.
- Onglet Comptes en **une liste unique des connectés** (liés ou non), l'ordre de la
  liste étant l'ordre de rotation.
- Rotation cyclique *suivant*/*précédent* entre les personnages connectés, avec saut
  des personnages absents ou exclus.
- Activation directe par un raccourci dédié, aussi bien pour un compte que pour un
  personnage sans compte.
- Raccourcis personnalisables (touche clavier ou bouton souris, boutons
  auxiliaires inclus).
- Interception conditionnée au focus d'un client DOFUS (aucun effet ailleurs).
- Réordonnancement et exclusion des comptes.
- **Ouverture/fermeture des clients** : lancer l'Ankama Launcher, fermer un client
  ou terminer la session, sans jamais interagir avec le jeu lui-même.
- **Comportements de fenêtre** configurables (fermer minimise, minimiser dans la
  barre d'état) et **instance unique** (relancer réveille la fenêtre existante).
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

# Publier l'exécutable autonome (single-file) → DofusOptimizer.exe
dotnet publish src/App -c Release -r win-x64
```

La configuration est stockée dans
`%APPDATA%\DofusSwitcher\config.json`.

## Licence

Usage privé.
