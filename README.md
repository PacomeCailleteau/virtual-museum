# Virtual Museum

Nous avons réalisé Virtual Museum dans le cadre du cours RX à l’IMT Atlantique. Dans l’atelier, nous avons posé les salles miniatures sur une table pour pouvoir les prendre et les déplacer. Une porte donne accès au même musée à taille humaine.

Nous avons placé les commandes dans le décor. Le tableau, sur le mur de gauche, regroupe trois rubriques : parcours, œuvres et sauvegarde. Nous affichons les commandes de la rubrique choisie pour garder un écran lisible. Les tiroirs servent à choisir les œuvres à exposer. Les panneaux des galeries donnent les informations utiles pendant la visite.

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
| Régler les dimensions d’une salle | Prendre une poignée colorée avec Grab et la tirer sur son axe | Tirer une poignée avec le clic gauche |
| Regarder et tourner | Tourner la tête ou utiliser le joystick droit | Maintenir le clic droit et bouger la souris |
| Se déplacer | Joystick gauche | ZQSD ou WASD |
| Se téléporter | Maintenir le joystick droit vers l’avant, viser le sol et relâcher le joystick | T pour activer le mode, maintenir le clic gauche et relâcher sur une destination valide |
| Tourner une œuvre | Orienter le rayon pendant la prise, ou utiliser le bouton Tourner | R pendant la prise, ou utiliser le bouton Tourner |

Nous avons réduit les tables pour faciliter l’accès aux miniatures. Dans l’atelier, le point de vue et les contrôleurs sont relevés de 15 cm pour mieux voir les plateaux. La visite conserve la hauteur réelle. Une œuvre saisie au contact suit la main horizontalement au-dessus de la table d’édition. Nous conservons sa hauteur dans la salle, réglable avec les boutons Monter et Descendre. Pour se téléporter, nous utilisons le fournisseur de téléportation du XR Interaction Toolkit. Une trajectoire courbe mène au repère d’arrivée. Le repère turquoise indique une destination accessible et sa barre dorée montre la direction du regard à l’arrivée. Une trajectoire rouge indique que la destination est refusée.

## Essayer les deux modes

1. Sélectionner une salle sur la maquette. Les commandes de la table permettent de la tourner. Pour relier ou délier deux salles, activer « Relier / délier », puis pointer la salle A et la salle B avec la gâchette sous l’index. Aucune présélection n’est nécessaire. Si le lien existe déjà, cette opération le retire. Pour régler sa largeur, sa longueur ou sa hauteur, prendre une des trois poignées colorées et la tirer. Les dimensions suivent une grille de 0,5 m. Le repère devient rouge si le changement dépasse la table, recouvre une salle ou ferait sortir une œuvre. Pour en créer une, appuyer sur « Tracer une salle » puis pointer deux coins libres sur la table.
2. Déposer une salle sur la table d’édition pour l’agrandir. Ouvrir un tiroir et placer une œuvre dans la salle. Pour modifier son titre ou sa description, pointer l’œuvre puis utiliser le tableau. Actionner le levier pour valider. Appuyer ensuite sur « Replacer sur la maquette », sous la salle, pour la retrouver à son emplacement d’origine. Il reste possible de la rapporter à la main.
3. Créer un parcours sur le tableau. Sélectionner les œuvres à visiter, puis ordonner les étapes. Le tracé apparaît sur la maquette. Dans la rubrique Sauvegarde, le bouton « Sauvegarder » enregistre la configuration du musée et la progression de la visite sur l’appareil.
4. Passer par la porte « Visiter le musée ». Le panneau de la galerie permet de choisir une visite libre, de démarrer un parcours ou de le reprendre. Suivre les repères au sol et les portes jusqu’à l’œuvre attendue, puis valider l’étape à proximité.

## Organisation du code

Nous utilisons le même `MuseumApi` dans l’atelier et dans la visite. Quand nous déplaçons une œuvre dans l’atelier, c’est donc la même instance qui change de place dans la galerie. Le dossier `Assets/CampusMuseum/Runtime` contient la mise en scène, les interactions et l’édition. Le script assemble le projet dans `Unity` avec le socle et les œuvres mis à disposition sur GitLab IMT.
