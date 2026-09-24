# Changelog

Toutes les évolutions notables de Dofus Optimizer sont consignées dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
et le projet suit le [versionnage sémantique](https://semver.org/lang/fr/).

## [1.0.0] - 2026-09-24

Première version. Bascule rapide entre clients DOFUS, pilotée au clavier/souris,
sans jamais interagir avec le processus du jeu (API fenêtres `user32` uniquement).

### Added

- **Socle applicatif** : fenêtre à onglets (Comptes · Raccourcis · Réglages),
  thème sombre, exécutable Windows autonome (single-file, self-contained).
- **Persistance de la configuration** : stockage JSON sous
  `%APPDATA%`, sauvegarde automatique débouncée, écriture atomique et
  sauvegarde de secours en cas de fichier corrompu.
- **Détection temps réel** des clients DOFUS via les API fenêtres `user32`
  (titre/classe), sans aucune inspection du processus ; liste des comptes avec
  état connecté/absent tenu à jour à l'apparition/disparition des fenêtres.
- **Ordre & exclusion** : réordonnancement des comptes (glisser-déposer),
  exclusion/réintégration de la rotation, ordre de la liste = ordre de rotation.
- **Capture d'associations** : capture d'une touche clavier ou d'un bouton souris
  (y compris auxiliaires X1/X2) pour les raccourcis *suivant*/*précédent* et
  l'activation directe par compte ; détection de conflit inline.
- **Interception & bascule de focus** : interception clavier/souris bas niveau
  conditionnée au focus d'un client DOFUS, rotation cyclique avec saut des comptes
  absents/exclus, activation directe d'un compte, bascule de focus non synthétique.
- **Zone de notification (tray)** : icône avec menu Ouvrir · Suspendre/Réactiver ·
  Quitter, double-clic pour rouvrir la fenêtre, icône reflétant l'état suspendu.
- **Réglages** : suspension globale de l'interception, démarrage avec Windows
  (`HKCU\...\Run`, sans élévation, désactivé par défaut), export/import de la
  configuration, à-propos.
- **Onglet Comptes remanié** : numéro de rotation, pastille d'état, bouton œil
  d'exclusion/réintégration par ligne, comptes connectés en haut et déconnectés
  regroupés en bas.

### Changed

- Cycle de vie : fermer la fenêtre de configuration masque l'application (elle
  reste accessible via le tray) ; seule l'action « Quitter » ferme l'application.
- Onglets répartis sur toute la largeur de la fenêtre, contour léger sur les
  onglets inactifs.
- Renommage de l'application en « Dofus Optimizer ».
- Limitation des boutons souris exploitables pour le mode vendeur (post-recette Axe 6).

### Fixed

- Crash au démarrage lié au point d'entrée du hook bas niveau
  (`SetWindowsHookExW`).
- Activation désormais fiable en un seul appui (verrou de fenêtre de premier plan).
