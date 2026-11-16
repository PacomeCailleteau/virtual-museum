# Virtual Museum

Nous avons réalisé Virtual Museum dans le cadre du cours RX à l’IMT Atlantique. Dans l’atelier, nous avons posé les salles miniatures sur une table pour pouvoir les prendre et les déplacer. Une porte donne accès au même musée à taille humaine.

Nous avons placé les commandes dans le décor. Le tableau sert à préparer les parcours et à renseigner les fiches des œuvres. Les tiroirs servent à choisir les œuvres à exposer. Les panneaux des galeries donnent les informations utiles pendant la visite.

## Ouvrir le projet

1. Cloner ce dépôt et télécharger le ZIP de la branche `main` du socle Unity avec un compte IMT autorisé. [Socle Unity sur GitLab IMT](https://gitlab.imt-atlantique.fr/e20peill/projet-rx)
2. Placer le ZIP à la racine du dépôt sous le nom `projet-rx-main.zip`, puis lancer cette commande avec Python 3.

   ```powershell
   python scripts/install.py projet-rx-main.zip
   ```

3. Ajouter le dossier `Unity` dans Unity Hub. Le socle cible Unity `6000.3.26f1`, XR Interaction Toolkit `3.5.1` et OpenXR `1.17.1`. La version de Unity utilisée pour vérifier le projet est `6000.3.27f1`.
4. Ouvrir `Assets/Scenes/VirtualMuseum.unity` et cliquer sur Play. Pour compiler sur Quest, installer le module Android Build Support dans Unity Hub, choisir Android comme plateforme et vérifier que la scène est incluse et qu’OpenXR est activé.

Nous gardons les fichiers du groupe dans `Assets/CampusMuseum`. Après les avoir modifiés, fermer Unity et relancer le script pour les recopier dans le projet. Si le registre Unity bloque un téléchargement, l’option `--package-archives chemin` permet d’utiliser les archives `.tgz` correspondant aux versions du fichier `Packages/packages-lock.json` du socle.

## Commandes

| Action | Casque | Éditeur sans casque |
| --- | --- | --- |
| Pointer, prendre et poser | Viser et maintenir la gâchette d’index, puis la relâcher pour poser | Viser à la souris et maintenir le clic gauche, puis le relâcher |
| Regarder et tourner | Tourner la tête ou utiliser le joystick droit | Maintenir le clic droit et bouger la souris |
| Se déplacer | Joystick gauche | ZQSD ou WASD |
| Basculer vers la téléportation | X à gauche, viser le sol puis presser la gâchette | T, puis clic gauche au sol |
| Tourner une œuvre | Orienter le rayon pendant la prise, ou utiliser le bouton Tourner | R pendant la prise, ou utiliser le bouton Tourner |

## Essayer les deux modes

1. Sélectionner une salle sur la maquette. Les commandes de la table permettent de la tourner, de la relier à une autre et de régler sa largeur, sa longueur et sa hauteur. Pour en créer une, appuyer sur « Tracer une salle » puis pointer deux coins libres sur la table.
2. Déposer une salle sur la table d’édition pour l’agrandir. Ouvrir un tiroir et placer une œuvre dans la salle. Pour modifier son titre ou sa description, pointer l’œuvre puis utiliser le tableau. Actionner le levier pour valider. Reprendre ensuite la salle à la taille de la maquette et la déposer sur le repère turquoise.
3. Créer un parcours sur le tableau. Sélectionner les œuvres à visiter, puis ordonner les étapes. Le tracé apparaît sur la maquette. Le bouton « Sauver » enregistre la configuration du musée et la progression de la visite sur l’appareil.
4. Passer par la porte « Visiter le musée ». Le panneau de la galerie permet de choisir une visite libre, de démarrer un parcours ou de le reprendre. Suivre les repères au sol et les portes jusqu’à l’œuvre attendue, puis valider l’étape à proximité.

## Organisation du code

Nous utilisons le même `MuseumApi` dans l’atelier et dans la visite. Quand nous déplaçons une œuvre dans l’atelier, c’est donc la même instance qui change de place dans la galerie. Le dossier `Assets/CampusMuseum/Runtime` contient la mise en scène, les interactions et l’édition. Le script assemble le projet dans `Unity` avec le socle et les œuvres mis à disposition sur GitLab IMT.
