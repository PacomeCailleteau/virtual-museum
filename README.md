# Virtual Museum

Projet RX de l’IMT Atlantique. Dans l’atelier, on aménage un musée miniature sur une table. La porte donne accès au même musée à taille humaine. Les commandes sont intégrées au décor, sur le tableau de l’atelier, les tables, les tiroirs et les panneaux des galeries.

## Ouvrir le projet

1. Cloner ce dépôt et télécharger le ZIP de la branche `main` du [socle Unity sur GitLab IMT](https://gitlab.imt-atlantique.fr/e20peill/projet-rx). L’accès au ZIP demande un compte IMT autorisé.
2. Depuis la racine du dépôt, lancer la commande suivante avec Python 3. Renommer le ZIP si nécessaire pour utiliser le même nom que dans la commande.

   ```powershell
   python scripts/install.py projet-rx-main.zip
   ```

3. Ajouter le dossier `Unity` dans Unity Hub. Le socle cible Unity `6000.3.26f1`, XR Interaction Toolkit `3.5.1` et OpenXR `1.17.1`.
4. Ouvrir `Assets/Scenes/VirtualMuseum.unity`, puis passer en mode Play. Pour un Quest, ajouter le module Android Build Support dans Unity Hub et utiliser le profil Android avec cette scène et OpenXR activé.

Pour modifier le projet, éditer les fichiers suivis dans `Assets/CampusMuseum` et relancer le script avec Unity fermé. Si le registre Unity pose un problème de téléchargement, `--package-archives chemin` permet d’installer localement les versions `.tgz` indiquées dans le verrou du socle.

## Commandes

| Action | Casque | Éditeur sans casque |
| --- | --- | --- |
| Pointer, prendre et poser | Viser et tenir la gâchette d’index. Relâcher pour poser | Viser à la souris, tenir puis relâcher clic gauche |
| Regarder et tourner | Casque et joystick droit | Maintenir clic droit et bouger la souris |
| Se déplacer | Joystick gauche | ZQSD ou WASD |
| Basculer vers la téléportation | X à gauche, viser le sol puis presser la gâchette | T, puis clic gauche au sol |
| Tourner une œuvre | Orienter le rayon pendant la prise, ou utiliser le bouton Tourner | R pendant la prise, ou utiliser le bouton Tourner |

## Essayer les deux modes

1. Sélectionner une salle sur la maquette. Les commandes du plateau permettent de l’orienter, de la relier à une autre et de changer largeur, longueur et hauteur. Pour créer une salle, presser « Tracer une salle » puis pointer deux coins libres sur la table.
2. Prendre une salle et la déposer sur la table d’édition. Elle s’agrandit à cet endroit. Ouvrir un tiroir du catalogue et déposer un exemplaire dans la salle. Le tableau sert à modifier le titre ou la description d’une œuvre sélectionnée. Actionner le levier de validation, puis reprendre la salle réduite et la déposer sur le repère turquoise.
3. Créer un parcours sur le tableau. Choisir une œuvre placée, l’ajouter au parcours et régler l’ordre des étapes. Le tracé correspondant apparaît sur la maquette. Sauver permet de retrouver le musée et la session en cours sur le même appareil.
4. Prendre la porte « Visiter le musée ». Le panneau de la galerie permet de démarrer un parcours, de reprendre une visite ou d’explorer librement. Les repères au sol et les portes indiquent le chemin. Désigner l’œuvre attendue et valider l’étape à proximité.

## Organisation du code

Le dossier `Assets/CampusMuseum/Runtime` contient la mise en scène, les interactions et l’édition. Les deux modes partagent le `MuseumApi` et les œuvres fournis par le socle. Le projet Unity assemblé est dans `Unity`, avec les dépendances et les contenus accessibles via GitLab IMT.
