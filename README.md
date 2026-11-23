# Virtual Museum

Nous avons réalisé Virtual Museum dans le cadre du cours RX à l’IMT Atlantique. Nous aménageons le musée sur une maquette, puis nous visitons les mêmes salles à taille humaine. Au démarrage, nous retrouvons un exemple complet avec trois salles, six œuvres et deux parcours.

Nous avons placé les commandes dans le décor. Les tables servent à construire et modifier les salles. Un tableau blanc regroupe les fiches et les parcours, avec des étiquettes d’œuvres à déplacer. Le terminal voisin garde l’historique des actions. Deux buzzers permettent de restaurer l’exemple ou de partir d’une maquette vide.

## Ouvrir le projet

1. Cloner ce dépôt et télécharger le ZIP de la branche `main` du socle Unity avec un compte IMT autorisé. [Socle Unity sur GitLab IMT](https://gitlab.imt-atlantique.fr/e20peill/projet-rx)
2. Placer le ZIP à la racine du dépôt sous le nom `projet-rx-main.zip`, puis lancer cette commande avec Python 3.

   ```powershell
   python scripts/install.py projet-rx-main.zip
   ```

3. Ajouter le dossier `Unity` dans Unity Hub. Le socle cible Unity `6000.3.26f1`, XR Interaction Toolkit `3.5.1` et OpenXR `1.17.1`. Nous vérifions le projet avec Unity `6000.3.27f1`.
4. Ouvrir `Assets/Scenes/VirtualMuseum.unity` et lancer Play. Pour compiler sur Quest, installer le module Android Build Support, choisir Android et vérifier que la scène est incluse et qu’OpenXR est activé.

Nous gardons les fichiers du groupe dans `Assets/CampusMuseum`. Après une modification de ces fichiers, fermer Unity et relancer le script pour les recopier dans le projet. L’option `--package-archives chemin` permet d’utiliser les archives `.tgz` correspondant au fichier `Packages/packages-lock.json` du socle si le registre Unity bloque leur téléchargement.

## Modifier l’atelier dans Unity

L’objet `Workshop` est visible dans la hiérarchie avant Play. Le prefab `Workshop.prefab` contient les tables, leurs enseignes, le tableau blanc, le terminal, le catalogue, les deux buzzers, la porte et le point d’arrivée. Les salles, les œuvres et le contenu du tableau sont construits au lancement depuis les données du musée.

Pour modifier notre décor, ouvrir `Assets/CampusMuseum/Resources/CampusMuseum/Workshop.prefab`. Déplacer une table par son parent déplace aussi ses commandes et sa zone de dépôt. Garder les tables alignées sur les axes de la scène et conserver les références de `WorkshopLayout`. Pour partager un changement, recopier le prefab ou les matériaux du dossier `Unity` vers les mêmes emplacements dans `Assets/CampusMuseum` à la racine du dépôt.

## Commandes

| Action | Casque | Éditeur sans casque |
| --- | --- | --- |
| Prendre et poser une salle ou une œuvre | Maintenir Grab sous le majeur, puis relâcher | Maintenir puis relâcher le clic gauche |
| Actionner une commande | Gâchette sous l’index | Clic gauche |
| Créer une salle | Activer le tracé, maintenir l’index au premier coin, tracer et relâcher au coin opposé | Même geste au clic gauche |
| Régler les dimensions | Saisir une pointe de flèche et la tirer sur son axe | Tirer la pointe avec le clic gauche |
| Tourner une salle | Saisir l’arc et tourner par quarts de tour | Tirer la pointe de l’arc autour de la salle |
| Rapprocher ou éloigner un objet tenu | Joystick vertical de la main qui tient l’objet | Molette pendant la prise |
| Valider une salle | Saisir et abaisser le levier jusqu’au repère doré, puis relâcher | Tirer le levier vers soi et vers le bas |
| Ajouter une étape au parcours | Prendre une étiquette du stock et la déposer à droite | Glisser l’étiquette avec le clic gauche |
| Regarder | Tourner la tête | Maintenir le clic droit et bouger la souris |
| Se déplacer | Joystick gauche quand cette main ne règle pas la portée d’un objet | ZQSD ou WASD |
| Tourner le point de vue | Joystick droit horizontal quand cette main ne règle pas un objet | Regarder avec la souris |
| Se téléporter | Sans objet tenu, maintenir le joystick droit vers l’avant, viser et relâcher | T pour activer le mode, maintenir le clic gauche et relâcher sur une destination valide |
| Restaurer l’exemple | Frapper le buzzer vert vers le bas | F5 |
| Vider la maquette pour un test | Frapper le buzzer orange vers le bas | F6 |

Le joystick d’une main qui tient une salle ou une œuvre règle sa portée et ne déplace pas le joueur. La portée va de la proximité de la main jusqu’à 1,80 m. La table elle-même reste sans réaction au clic ou au survol, sauf pour recevoir le tracé d’une salle lorsque cet outil est actif.

## Aménager le musée

Nous pouvons déplacer les salles déjà présentes ou en tracer de nouvelles. La création se termine au relâchement. Un tracé trop petit, hors du plateau ou qui recouvre une autre salle est refusé.

Les dimensions suivent une grille de 0,5 m. Les flèches changent de longueur avec la prévisualisation et gardent leur base fixe. La flèche de longueur est devant la salle. Les quatre murs miniatures ont la même hauteur d’affichage. Pendant une réduction, le volume initial reste indiqué par un contour et le nouveau mesh est visible. L’arc tourne avec la salle. Sa consigne apparaît sur la console de la table, en dehors de la zone de manipulation.

Des salles déposées bord à bord s’aimantent et se relient automatiquement. « Relier / délier » permet aussi de désigner deux salles avec l’index pour ajouter ou retirer une connexion. Éloigner une salle conserve ses liens.

Pour modifier les œuvres, prendre une salle et la déposer sur la table d’édition. Ouvrir un tiroir, prendre un exemplaire et le déposer dans la salle agrandie. Les œuvres peuvent être déplacées, orientées ou réglées en hauteur. « Annuler » et « Rétablir » concernent les modifications de cette salle en cours d’édition.

Le levier a une course descendante plus longue, avec un repère doré et des étincelles à la validation. Après validation ou abandon, ramener la salle réduite au-dessus de son socle sur la maquette. Le socle devient turquoise et la salle s’aimante à sa place avant le relâchement. La commande « Replacer sur la maquette » reste disponible.

Les œuvres miniatures ne sont pas cliquables. Leur sélection passe par le tableau blanc. Survoler un nom ou une étiquette permet de repérer la bonne instance sur la maquette ou dans la salle agrandie. La rubrique « Œuvres » permet de modifier son titre et sa description ou de la retirer.

## Composer un parcours

La rubrique « Parcours » rassemble les parcours à gauche, un stock d’étiquettes d’œuvres au centre et les étapes du parcours choisi à droite. Nous prenons une étiquette avec Grab et la déposons dans la zone de droite. Une confirmation indique l’œuvre ajoutée et le numéro de l’étape. Le stock reste disponible pour ajouter la même œuvre à plusieurs parcours.

Les étiquettes d’étapes peuvent être déplacées pour modifier leur ordre. Les flèches haut et bas restent disponibles. Retirer une étape conserve l’œuvre dans le musée. Les commandes « Modifier », « Renommer », « Description » et « Supprimer » concernent le parcours choisi. Le trajet et les numéros se mettent à jour sur la maquette.

## Visiter

La porte « Visiter le musée » ouvre les salles à taille humaine. Le parcours choisi dans l’atelier est actif dès l’entrée, sans clic supplémentaire sur les pointillés. S’il n’existe aucun parcours rempli, la visite démarre en exploration libre.

Nous pouvons choisir un autre parcours en pointant ses repères au sol. Chaque parcours a une couleur, une forme et un numéro. Les autres restent visibles avec une teinte plus discrète. Entrer dans la zone de 1,20 m autour d’une œuvre du parcours valide le passage et ajoute une coche verte. Les traces et toutes les portes restent visibles pour revenir en arrière. Les cartels indiquent les titres et permettent d’ouvrir les descriptions.

Un clic sur une porte ou un déplacement à son contact fait changer de salle. La dalle « LIBRE » permet de suspendre le guidage. La porte « Atelier » ramène à la maquette. Nous conservons les œuvres déjà vues séparément pour chaque parcours, y compris lors d’un changement de parcours. L’étape suivante est la première œuvre non visitée dans l’ordre du parcours.

Le point de vue et les contrôleurs sont relevés de 15 cm dans l’atelier. La visite garde la hauteur réelle. La téléportation utilise le fournisseur du XR Interaction Toolkit. Le turquoise indique une arrivée accessible et le rouge une destination refusée.

## Sauvegarder ou recommencer

« Sauvegarder » enregistre le musée, la session courante et l’historique de visite. « Charger » retrouve cette configuration et actualise les deux modes.

Le buzzer vert « MUSÉE INITIAL » restaure trois salles, six œuvres et deux parcours. Le buzzer orange « TEST UTILISATEUR » vide le musée et les progressions en mémoire. Nous les actionnons par un coup descendant de la manette sur le capuchon. Une animation et des étincelles confirment la frappe. Un simple survol ou un poing immobile ne les déclenche pas. Il faut poser les objets et terminer l’édition avant une réinitialisation. Les buzzers ne suppriment pas le fichier sauvegardé sur l’appareil.

## Vérifications et tests utilisateurs

Les scénarios `tests/test-workshop.csx`, `tests/test-composition.csx` et `tests/test-visitor.csx` vérifient les interactions et la cohérence des données dans Unity. Le premier contrôle notamment les buzzers, le tracé au relâchement, les murs, la rotation, la portée, le levier et les étiquettes. Le scénario de composition vérifie le déplacement des étiquettes, les raccourcis et les portées indépendantes. Le scénario de visite poursuit avec les parcours, les portes, les suppressions et le rechargement. Ils remplacent les données de la session Play et conservent le fichier d’historique présent avant le test.

```powershell
unity command editor.playmode start --caller plugin --skill unity-cli --project-path Unity
unity status --until-ready --project-path Unity
$verification = Get-Content tests/test-workshop.csx -Raw
unity command script eval --caller plugin --skill unity-cli --project-path Unity -- --code $verification
$verification = Get-Content tests/test-composition.csx -Raw
unity command script eval --caller plugin --skill unity-cli --project-path Unity -- --code $verification
$verification = Get-Content tests/test-visitor.csx -Raw
unity command script eval --caller plugin --skill unity-cli --project-path Unity -- --code $verification
unity command editor.playmode stop --caller plugin --skill unity-cli --project-path Unity
```

Le protocole reprend la création de trois salles et de sept œuvres, la préparation et la visite d’un parcours, les suppressions puis le chargement. Avant chaque passation, nous utilisons le buzzer orange pour partir d’une maquette vide. Le confort, la lisibilité et la compréhension des gestes restent à évaluer au casque. Le protocole est à faire valider par l’enseignant avant les passations.

- [Protocole et feuilles vierges en PDF](docs/Protocole-tests-utilisateurs.pdf)
- [Version modifiable du protocole](docs/protocole-tests-utilisateurs.md)

Pour régénérer le PDF :

```powershell
python -m pip install reportlab pypdf
python scripts/generate_protocol.py
```

Les réponses individuelles restent dans le dossier local `Resultats-tests`. Ce dossier et `Retours` sont exclus du dépôt public.

## Organisation du code

Nous utilisons le même `MuseumApi` pour l’atelier et la visite. Les changements passent par l’API et les vues sont actualisées avec ses événements. Notre code de mise en scène et d’interaction est dans `Assets/CampusMuseum/Runtime`. L’installateur assemble ces fichiers avec le socle et les ressources fournis sur GitLab IMT.
