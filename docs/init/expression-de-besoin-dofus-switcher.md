# Expression de besoin — Dofus Window Switcher

| Version | Date | Auteur | Statut |
|---|---|---|---|
| 0.1 | 2026-09-24 | Mathias | Brouillon |
| 0.2 | 2026-09-24 | Mathias | Questions ouvertes tranchées |

## 1. Contexte

Le joueur fait tourner plusieurs clients DOFUS en parallèle sous Windows. Pour basculer entre eux, il utilise Alt+Tab ou Alt+Échap. C'est lent, et l'ordre des fenêtres est mélangé avec celui des autres applications.

## 2. Objectif

Fournir une application de bureau légère. Elle détecte les fenêtres DOFUS ouvertes et permet de passer de l'une à l'autre selon un ordre défini par l'utilisateur. La bascule se fait avec des boutons physiques (souris ou clavier) configurables.

## 3. Périmètre

Dans le périmètre : la détection des fenêtres, la bascule cyclique et directe, une interface de configuration et la persistance de cette configuration.

Hors périmètre : toute interaction avec le contenu du jeu. Cela exclut l'envoi de touches ou de clics au client, la lecture mémoire, la lecture réseau et l'automatisation d'actions.

## 4. Exigences fonctionnelles

Priorités MoSCoW : M = Must, S = Should, C = Could.

Définition : une « entrée » est une touche clavier ou un bouton souris unique. Les combinaisons (modificateur + touche, accords) sont exclues.

| ID | Exigence | Prio |
|---|---|---|
| EF-01 | Détecter automatiquement les fenêtres des clients DOFUS ouvertes et identifier chacune par le personnage associé. | M |
| EF-02 | Afficher la liste des comptes détectés avec leur état (connecté ou absent) et la mettre à jour en temps réel. | M |
| EF-03 | Permettre de définir et réordonner l'ordre de rotation des comptes. | M |
| EF-04 | Associer une entrée « suivant » et une entrée « précédent » (par défaut XButton2 / XButton1) qui activent le compte adjacent dans l'ordre de rotation. Les comptes absents sont ignorés. | M |
| EF-05 | Associer une entrée dédiée (clavier ou souris) à un compte précis. Cette entrée active directement la fenêtre du compte. | S |
| EF-06 | Afficher et modifier toutes les associations d'entrées depuis l'interface. L'assignation se fait par capture de l'entrée (« appuyez sur un bouton »). | M |
| EF-07 | N'intercepter une entrée que si une fenêtre DOFUS a le focus. Dans les autres cas, l'entrée est transmise normalement aux autres applications. | M |
| EF-08 | Signaler un conflit quand une même entrée est assignée à deux actions. | S |
| EF-09 | Exclure temporairement un compte de la rotation sans le supprimer. | C |
| EF-10 | Suspendre ou réactiver globalement l'interception via l'icône de la zone de notification. | S |

## 5. Persistance

| ID | Exigence | Prio |
|---|---|---|
| EP-01 | Sauvegarder la configuration (ordre, associations, exclusions) dans un fichier lisible, dans le profil utilisateur. | M |
| EP-02 | Sauvegarder automatiquement à chaque modification et recharger au démarrage. | M |
| EP-03 | Conserver un compte configuré même lorsqu'il n'est pas connecté. | M |
| EP-04 | Si le fichier est corrompu, démarrer avec une configuration vide et conserver une copie du fichier fautif. | S |
| EP-05 | Permettre l'export et l'import de la configuration. | C |

## 6. Exigences non fonctionnelles

| ID | Exigence | Prio |
|---|---|---|
| ENF-01 | Plateforme : Windows 10 et 11, x64. | M |
| ENF-02 | Latence entre l'appui et l'activation de la fenêtre : moins de 100 ms. | M |
| ENF-03 | Empreinte au repos : processeur quasi nul, moins de 100 Mo de RAM. | S |
| ENF-04 | L'application fonctionne réduite dans la zone de notification. Le démarrage avec Windows est optionnel. | S |
| ENF-05 | L'application fonctionne sans élévation de privilèges, sauf si les clients DOFUS tournent eux-mêmes en administrateur. | M |
| ENF-06 | Le code est découpé en couches séparées : détection, capture d'entrées, activation de fenêtre, persistance et interface. | M |

## 7. Contraintes

C-01 : une entrée utilisateur produit exactement un changement de focus, et rien d'autre.

C-02 : l'outil n'agit que sur le système d'exploitation, jamais sur le processus DOFUS. Il n'ouvre pas de handle sur le processus, ne lit pas sa mémoire, ne s'y injecte pas, ne modifie pas ses fichiers et n'intercepte pas son trafic réseau. Seules les API de gestion des fenêtres et les hooks d'entrée globaux de user32 sont utilisés.

C-03 : l'outil n'envoie aucune entrée synthétique. Il n'utilise ni SendInput, ni PostMessage, ni SendMessage de clavier ou de souris, y compris pour contourner les restrictions de SetForegroundWindow. Une telle entrée atterrirait dans la fenêtre DOFUS active.

## 8. Critères d'acceptation

CA-01 : avec 4 clients ouverts, 4 appuis successifs sur « suivant » parcourent les 4 comptes dans l'ordre configuré et reviennent au premier.

CA-02 : quand un navigateur a le focus, le bouton « précédent » de la souris y garde son comportement natif.

CA-03 : si un client est fermé, il disparaît de la rotation sans provoquer d'erreur. À sa réouverture, il reprend sa place.

CA-04 : après un redémarrage de l'application, l'ordre et les associations sont identiques.

## 9. Hypothèses, décisions et risques

H-01 (confirmée) : le titre de fenêtre du client DOFUS 3 contient le nom du personnage. C'est la clé d'identification des comptes.

D-01 (ex-Q-02) : les associations acceptent le clavier et la souris, avec une seule touche ou un seul bouton par association.

R-01 (ex-Q-01) : Ankama est opposé à tout logiciel tiers. L'utilisation de l'outil est contraire à cette position, et le risque de sanction sur les comptes est assumé par l'utilisateur. Les contraintes C-02 et C-03 réduisent la surface d'interaction avec le client, sans garantir quoi que ce soit.
