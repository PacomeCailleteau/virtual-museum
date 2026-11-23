# Virtual Museum

Nous avons réalisé Virtual Museum dans le cadre du cours RX à l’IMT Atlantique. Dans l’atelier, nous construisons le musée sur une table initialement vide. Nous pouvons prendre et déplacer les salles miniatures. Une porte donne accès au même musée à taille humaine.

Nous avons placé les commandes dans le décor. Le tableau, sur le mur de gauche, regroupe trois rubriques : parcours, œuvres et sauvegarde. Nous affichons les commandes de la rubrique choisie pour garder un écran lisible. Les tiroirs servent à choisir les œuvres à exposer. Pendant la visite, les cartels donnent les informations des œuvres et les repères au sol permettent de choisir un parcours. Un journal mural séparé garde les dernières actions, avec une couleur et un symbole pour les résultats et les refus.

## Ouvrir le projet

1. Cloner ce dépôt et télécharger le ZIP de la branche `main` du socle Unity avec un compte IMT autorisé. [Socle Unity sur GitLab IMT](https://gitlab.imt-atlantique.fr/e20peill/projet-rx)
2. Placer le ZIP à la racine du dépôt sous le nom `projet-rx-main.zip`, puis lancer cette commande avec Python 3.

   ```powershell
   python scripts/install.py projet-rx-main.zip
   ```

3. Ajouter le dossier `Unity` dans Unity Hub. Le socle cible Unity `6000.3.26f1`, XR Interaction Toolkit `3.5.1` et OpenXR `1.17.1`. La version de Unity utilisée pour vérifier le projet est `6000.3.27f1`.
4. Ouvrir `Assets/Scenes/VirtualMuseum.unity` et cliquer sur Play. Pour compiler sur Quest, installer le module Android Build Support dans Unity Hub, choisir Android comme plateforme et vérifier que la scène est incluse et qu’OpenXR est activé.

Nous gardons les fichiers du groupe dans `Assets/CampusMuseum`. Après les avoir modifiés, fermer Unity et relancer le script pour les recopier dans le projet. Si le registre Unity bloque un téléchargement, l’option `--package-archives chemin` permet d’utiliser les archives `.tgz` correspondant aux versions du fichier `Packages/packages-lock.json` du socle.

## Modifier l’atelier

L’objet `Workshop` est visible dans la hiérarchie de `VirtualMuseum.unity` avant Play. Nous avons regroupé les tables, leurs enseignes, le tableau, le catalogue, la porte et le point d’arrivée dans le prefab `Workshop.prefab`. Les salles, les œuvres et le contenu des panneaux apparaissent au lancement avec les données du musée.

Pour modifier le décor du groupe, ouvrir `Assets/CampusMuseum/Resources/CampusMuseum/Workshop.prefab` dans Unity. Déplacer une table par son objet parent déplace aussi ses commandes et sa zone de dépôt. Garder les tables alignées sur les axes de la scène et conserver les références du composant `WorkshopLayout`. Pour partager le changement, recopier le prefab ou les matériaux modifiés du dossier `Unity` vers les mêmes emplacements dans `Assets/CampusMuseum` à la racine du dépôt.

## Commandes

| Action | Casque | Éditeur sans casque |
| --- | --- | --- |
| Prendre et poser | Viser et maintenir le bouton Grab sous le majeur, puis le relâcher pour poser | Viser à la souris et maintenir le clic gauche, puis le relâcher |
| Actionner un bouton ou désigner une œuvre | Viser et presser la gâchette sous l’index | Clic gauche |
| Régler les dimensions d’une salle | Saisir une flèche et la tirer sur son axe | Tirer la pointe avec le clic gauche |
| Tourner une salle | Saisir l’arc et tourner par crans de 90° | Tirer la pointe de l’arc autour de la salle |
| Valider une salle | Saisir le levier avec Grab, l’abaisser puis relâcher | Cliquer et tirer le levier vers soi, puis relâcher |
| Regarder et tourner | Tourner la tête ou utiliser le joystick droit | Maintenir le clic droit et bouger la souris |
| Se déplacer | Joystick gauche | ZQSD ou WASD |
| Se téléporter | Maintenir le joystick droit vers l’avant, viser le sol et relâcher le joystick | T pour activer le mode, maintenir le clic gauche et relâcher sur une destination valide |
| Tourner une œuvre | Orienter le rayon pendant la prise, ou utiliser le bouton Tourner | R pendant la prise, ou utiliser le bouton Tourner |

Nous avons réduit les tables pour faciliter l’accès aux miniatures. Dans l’atelier, le point de vue et les contrôleurs sont relevés de 15 cm pour mieux voir les plateaux. La visite conserve la hauteur réelle. Les salles portées et les nouvelles œuvres prises dans les tiroirs restent près de la main. Pour les poser, nous plaçons la main au-dessus du plateau et relâchons Grab. Nous conservons sa hauteur dans la salle, réglable avec les boutons Monter et Descendre. Pour se téléporter, nous utilisons le fournisseur de téléportation du XR Interaction Toolkit. Une trajectoire courbe mène au repère d’arrivée. Le repère turquoise indique une destination accessible et sa barre dorée montre la direction du regard à l’arrivée. Une trajectoire rouge indique que la destination est refusée.

## Essayer les deux modes

1. Depuis la table vide, activer « Tracer une salle » et pointer deux coins. Saisir l’arc pour tourner la salle par quarts de tour. Pour relier ou délier deux salles, activer « Relier / délier », puis pointer la salle A et la salle B avec la gâchette sous l’index. Aucune présélection n’est nécessaire. Si le lien existe déjà, cette opération le retire. Pour régler sa largeur, sa longueur ou sa hauteur, prendre une des trois flèches colorées et la tirer. Les dimensions suivent une grille de 0,5 m. La flèche s’étire avec la prévisualisation et sa base reste fixe. La longueur se règle depuis le devant de la salle. Le repère devient rouge si le changement dépasse la table, recouvre une salle ou ferait sortir une œuvre. Des salles déposées assez près bord à bord s’aimantent et se relient automatiquement. Le lien crée des passages dans les deux sens en visite. Éloigner ensuite une salle conserve son lien, qui reste modifiable avec « Relier / délier ».
2. Déposer une salle sur la table d’édition pour l’agrandir. Ouvrir un tiroir et placer une œuvre dans la salle. Pour modifier son titre ou sa description, pointer l’œuvre puis utiliser le tableau. Saisir et abaisser le levier jusqu’au repère doré, puis relâcher pour valider. Des scintillements confirment la validation. Appuyer ensuite sur « Replacer sur la maquette », sous la salle, pour la retrouver à son emplacement d’origine. Il reste possible de la rapporter à la main.
3. Créer un parcours sur le tableau. Sélectionner les œuvres à visiter, puis ordonner les étapes. Le tracé apparaît sur la maquette. Dans la rubrique Sauvegarde, le bouton « Sauvegarder » enregistre la configuration du musée et la progression de la visite sur l’appareil.
4. Passer par la porte « Visiter le musée ». Pointer les repères au sol pour choisir un parcours. Chaque parcours a une couleur, une forme et un numéro. Les parcours non choisis restent visibles avec une teinte plus discrète. Entrer dans la zone de 1,20 m autour d’une œuvre du parcours valide son passage et ajoute une coche verte. Les traces et toutes les portes restent disponibles pour revenir en arrière. Un clic sur une porte ou un déplacement jusqu’à elle fait changer de salle. La dalle « LIBRE » revient à l’exploration libre. Les cartels permettent de lire les titres et les descriptions.

## Organisation du code

Nous utilisons le même `MuseumApi` dans l’atelier et dans la visite. Quand nous déplaçons une œuvre dans l’atelier, c’est donc la même instance qui change de place dans la galerie. Le dossier `Assets/CampusMuseum/Runtime` contient la mise en scène, les interactions et l’édition. Le script assemble le projet dans `Unity` avec le socle et les œuvres mis à disposition sur GitLab IMT.

## Retrouver et reprendre le musée

Nous conservons les œuvres déjà visitées séparément pour chaque parcours. Changer de parcours puis revenir au précédent garde les coches acquises. L’étape suivante est la première œuvre non visitée dans l’ordre du parcours. La sauvegarde enregistre le musée, la session courante et cet historique. Charger remet à jour les deux représentations.

Au lancement, la maquette est vide. Le bouton « Charger » retrouve la sauvegarde de l’appareil. Pour recommencer une séance de test, « Nouvelle maquette vide » demande une confirmation et vide le musée en mémoire. Cette action ne supprime pas le fichier sauvegardé.

## Vérification des interactions

Nous avons regroupé les contrôles de non régression dans `tests/test-workshop.csx` et `tests/test-visitor.csx`. Le premier part d’un lancement vide. Le second poursuit avec un musée de test en trois salles. Ils vérifient 47 points dans Unity, dont les refus de transformation, le geste du levier, les parcours indépendants, le retour entre salles et les suppressions suivies d’une nouvelle visite. Les scénarios remplacent seulement les données de la session Play et conservent le fichier d’historique présent avant le test.

Ces contrôles se lancent dans l’éditeur avec Unity Pipeline et le CLI Unity. Les sensations de prise, le confort et la lisibilité restent à vérifier avec les personnes portant le casque.

```powershell
unity command editor.playmode start --caller plugin --skill unity-cli --project-path Unity
unity status --until-ready --project-path Unity
$verification = Get-Content tests/test-workshop.csx -Raw
unity command script eval --caller plugin --skill unity-cli --project-path Unity -- --code $verification
$verification = Get-Content tests/test-visitor.csx -Raw
unity command script eval --caller plugin --skill unity-cli --project-path Unity -- --code $verification
unity command editor.playmode stop --caller plugin --skill unity-cli --project-path Unity
```

## Tests utilisateurs

Nous partons d’une maquette vide pour observer la construction de trois salles, l’ajout de sept œuvres, la création d’un parcours, sa visite puis les suppressions et le rechargement. Le protocole reprend le scénario du groupe et précise les observations, les temps, les aides et le questionnaire SUS à dix questions. Il est à faire valider par l’enseignant avant les passations.

- [Protocole et feuilles vierges en PDF](docs/Protocole-tests-utilisateurs.pdf)
- [Version modifiable du protocole](docs/protocole-tests-utilisateurs.md)

Pour régénérer le PDF à partir du document Markdown :

```powershell
python -m pip install reportlab pypdf
python scripts/generate_protocol.py
```

Les réponses individuelles et les enregistrements restent hors du dépôt public, dans le dossier local `Resultats-tests`. Le dossier `Retours` reste également exclu.
