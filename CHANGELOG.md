# Changelog

Toutes les évolutions notables de Dofus Optimizer sont consignées dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
et le projet suit le [versionnage sémantique](https://semver.org/lang/fr/).

## [1.4.1] - 2026-10-06

Correctif de stabilité.

### Fixed

- **Plus de blocage « Ne répond pas » lors d'une bascule** : lorsqu'un client DOFUS
  figé se trouvait au premier plan, le changement de fenêtre pouvait geler
  l'application de façon permanente jusqu'à ce que Windows la ferme de force.
  L'activation détecte désormais une fenêtre qui ne répond plus et évite de s'y
  attacher.

## [1.4.0] - 2026-09-26

Installation simplifiée et **mises à jour automatiques** : Dofus Optimizer se
distribue désormais via un installateur et se tient à jour tout seul.

### Added

- **Installateur** : l'application s'installe via un `Setup.exe` téléchargeable
  depuis les *releases* GitHub, avec raccourcis et désinstallation propre.
- **Mises à jour automatiques** : au démarrage, l'application vérifie en arrière-plan
  s'il existe une version plus récente, la télécharge et l'**installe silencieusement
  à la prochaine fermeture** — sans interruption ni manipulation.

### Note

- **Avertissement Windows au premier lancement** : l'application n'étant pas signée
  par un certificat payant, SmartScreen peut afficher « Windows a protégé votre
  ordinateur ». Cliquer sur « Informations complémentaires » → « Exécuter quand
  même ». Cette étape n'apparaît qu'à la première installation ; les mises à jour
  suivantes sont silencieuses.
- Aucune migration : les configurations existantes sont reprises telles quelles.

## [1.3.0] - 2026-09-26

Prise en charge des clients DOFUS ouverts sans personnage connecté, ouverture de
session plus souple, et quelques finitions d'interface.

### Added

- **Clients sans personnage dans la rotation** : un client DOFUS ouvert mais **sans
  personnage connecté** (écran de sélection) apparaît désormais dans la liste des
  connectés sous le nom « Dofus 1 », « Dofus 2 »… Il est **fermable** et **inclus
  par défaut dans la rotation**, mais ne peut pas recevoir de raccourci ni être
  rattaché à un compte.
- **Réglage « Réduire à l'ouverture d'une session »** : lorsqu'il est activé,
  Dofus Optimizer se **minimise** automatiquement après un clic sur « Ouvrir une
  session ». Affiché uniquement si un chemin d'Ankama Launcher est renseigné.

### Changed

- **« Ouvrir une session »** est proposé tant qu'**aucune fenêtre DOFUS n'est
  ouverte** (même à l'écran de sélection). Si le launcher est déjà ouvert sans
  client, le bouton **ramène sa fenêtre au premier plan** au lieu d'en relancer un.
- **Coins de la fenêtre légèrement arrondis** pour un rendu plus moderne
  (Windows 11).

### Note

- Aucune migration : les configurations existantes sont reprises telles quelles.

## [1.2.0] - 2026-09-26

Les personnages sans compte deviennent des participants de plein droit de la
rotation, et l'onglet Comptes est simplifié en une liste unique des connectés.

### Added

- **Personnages sans compte dans la rotation** : un personnage connecté non lié à
  un compte peut désormais recevoir un **raccourci d'activation directe** (comme
  les comptes), et il participe pleinement à la rotation *suivant*/*précédent*.
- **Lier un personnage à un compte** depuis l'onglet Comptes : chaque ligne non
  liée propose un menu « + Compte » puis un bouton **« Lier »**. À la liaison, un
  raccourci direct éventuel du personnage est **transféré au compte** (ou effacé
  si le compte en possède déjà un).

### Changed

- **Onglet Comptes simplifié** : les personnages connectés — liés à un compte ou
  non — sont réunis dans une **seule liste « Connectés »** ordonnée (l'ordre = la
  rotation). Chaque ligne porte la poignée de déplacement, l'œil d'exclusion et la
  croix de fermeture ; les lignes non liées ajoutent le menu de liaison.
- Le menu « + Compte » est **masqué s'il n'existe aucun compte disponible**, et le
  bouton « Lier » n'apparaît **qu'une fois un compte sélectionné**.
- Glisser-déposer de réordonnancement **plus fluide** (déplacement en place, sans
  clignotement).
- En activation directe, le libellé d'un compte affiche le **nom du compte**
  (raccourci unique partagé par tous ses personnages).

### Note

- Aucune migration : les configurations existantes sont reprises telles quelles.

## [1.1.0] - 2026-09-25

Distinction Compte/Personnage, ouverture et fermeture des clients depuis
l'application, et comportements de fenêtre configurables.

### Added

- **Comptes ↔ Personnages** : notion de **Compte** (nom libre) distincte du
  **personnage** (fenêtre détectée), avec liaison manuelle personnage → compte.
  Onglet Comptes réorganisé en 3 zones (comptes connectés · personnages connectés
  sans compte · comptes déconnectés). Gestion des comptes dans les Réglages :
  créer, supprimer (cascade sur les personnages liés), liste dépliable des
  personnages liés. Activation directe désormais **portée par le compte**.
- **Ouvrir une session** : bouton lançant l'Ankama Launcher lorsqu'aucun client
  n'est ouvert ; chemin de l'exécutable **configurable** dans les Réglages
  (auto-détection par défaut, bouton masqué si aucun chemin utilisable).
- **Fermer les clients** : croix rouge par client connecté (fermeture forcée) et
  bouton « Terminer session » (ferme tous les clients puis quitte l'application).
- **Comportements de fenêtre** (Réglages) : « Fermer [X] l'application la
  minimise » et « Minimiser dans la barre d'état » (complémentaires), bouton
  « Fermer l'application », bouton « + » pour déplier la création de compte.
- **Instance unique** : relancer l'application réveille la fenêtre existante au
  lieu d'ouvrir un second processus.

### Changed

- Menu de la zone de notification : « Ouvrir la configuration » → « Ouvrir ».
- Exécutable renommé **`DofusOptimizer.exe`**.
- Fermer la fenêtre suit le réglage « Fermer minimise » (activé par défaut : le
  comportement tray historique est préservé pour les configurations existantes).
- Barre de défilement sombre appliquée globalement.

### Note

- Les interactions avec les **processus** DOFUS (lancer le launcher, fermer un
  client) sont volontaires et bornées : l'application n'injecte rien, ne lit pas
  la mémoire et n'interagit pas avec le jeu — la reconnaissance reste fondée sur
  les seules API fenêtres `user32`.
- Les configurations existantes sont migrées automatiquement (aucune action requise).

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
