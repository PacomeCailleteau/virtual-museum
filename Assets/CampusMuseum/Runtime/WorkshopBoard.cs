using System;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        enum TextField { None, Title, Description, TourName, TourDescription }
        TextField activeField;
        string textValue;
        int tourPage;
        int artPage;
        int stopPage;
        Vector3 draggingPosition;
        Quaternion draggingOrientation;
        Vector3 draggingDirection;
        float draggingTurn;
        string draggingId;
        string draggingDefinition;
        const int PageSize = 4;

        void DrawBoard()
        {
            var board = Panel(stage,"Tableau de l’atelier",new Vector3(-.85f,1.65f,1.77f),4.3f,1.9f);
            Text(board,"ATELIER   /   PARCOURS ET ŒUVRES",new Vector3(-2.03f,.83f,-.08f),.105f,Cream);
            Text(board,Wrap(status,69,2),new Vector3(-2.03f,.65f,-.08f),.061f,Teal);
            if(activeField != TextField.None) { DrawKeyboard(board); return; }

            var tour = Api.GetTour(selectedTour);
            var list = data.tours;
            tourPage = Mathf.Clamp(tourPage,0,Mathf.Max(0,(list.Count-1)/PageSize));
            Text(board,"PARCOURS",new Vector3(-2.03f,.46f,-.08f),.071f,Cream);
            for(int i=0;i<PageSize && tourPage*PageSize+i<list.Count;i++)
            {
                var item=list[tourPage*PageSize+i];
                Key(board,Short(item.name,21),-1.52f,.30f-i*.15f,1.02f,.126f,()=>{
                    selectedTour=item.id; stopPage=0; Tell(item.name);
                },item.id==selectedTour ? Teal : Quiet);
            }
            Key(board,"Préc.",-1.88f,-.33f,.29f,.115f,()=>{tourPage--;QueueDraw();});
            Key(board,"Suiv.",-1.55f,-.33f,.29f,.115f,()=>{tourPage++;QueueDraw();});
            Key(board,"Créer",-1.20f,-.33f,.33f,.115f,CreateTour,Teal);
            if(tour != null)
            {
                Text(board,Wrap(tour.description,28,2),new Vector3(-2.03f,-.48f,-.08f),.058f,Cream);
                Key(board,"Nom",-1.82f,-.65f,.39f,.118f,()=>BeginText(TextField.TourName,tour.name));
                Key(board,"Description",-1.35f,-.65f,.48f,.118f,()=>BeginText(TextField.TourDescription,tour.description));
                Key(board,"Supprimer",-1.50f,-.80f,.99f,.12f,()=>DeleteTour(tour.id),Red);
            }

            Text(board,"ÉTAPES",new Vector3(-.93f,.46f,-.08f),.071f,Cream);
            int stopCount=tour?.artworkIds.Count ?? 0;
            stopPage=Mathf.Clamp(stopPage,0,Mathf.Max(0,(stopCount-1)/PageSize));
            for(int i=0;i<PageSize && stopPage*PageSize+i<stopCount;i++)
            {
                var index=stopPage*PageSize+i;
                var id=tour.artworkIds[index];
                var title=Api.GetArtwork(id)?.title ?? "Œuvre";
                var y=.30f-i*.15f;
                Text(board,$"{index+1}. {Short(title,17)}",new Vector3(-.93f,y,-.08f),.055f,Cream);
                if(index>0) Key(board,"▲",-.01f,y,.15f,.115f,()=>Reorder(tour.id,index,index-1));
                if(index<stopCount-1) Key(board,"▼",.16f,y,.15f,.115f,()=>Reorder(tour.id,index,index+1));
                Key(board,"×",.33f,y,.15f,.115f,()=>RemoveStop(tour.id,index),Red);
            }
            Key(board,"Préc.",-.76f,-.33f,.31f,.115f,()=>{stopPage--;QueueDraw();});
            Key(board,"Suiv.",-.42f,-.33f,.31f,.115f,()=>{stopPage++;QueueDraw();});
            Key(board,"Ajouter l’œuvre",-.25f,-.49f,1.32f,.13f,AddStop,Teal);

            var artList=(draft?.Api.Data ?? data).artworks;
            artPage=Mathf.Clamp(artPage,0,Mathf.Max(0,(artList.Count-1)/PageSize));
            Text(board,"ŒUVRES PLACÉES",new Vector3(.55f,.46f,-.08f),.071f,Cream);
            for(int i=0;i<PageSize && artPage*PageSize+i<artList.Count;i++)
            {
                var item=artList[artPage*PageSize+i];
                Key(board,Short(item.title,24),1.28f,.30f-i*.15f,1.46f,.126f,()=>{
                    selectedArt=item.id;
                    if(draft==null) selectedRoom=item.roomId;
                    Tell(item.title);
                },item.id==selectedArt ? Teal : Quiet);
            }
            Key(board,"Préc.",.73f,-.33f,.34f,.115f,()=>{artPage--;QueueDraw();});
            Key(board,"Suiv.",1.10f,-.33f,.34f,.115f,()=>{artPage++;QueueDraw();});
            var art=(draft?.Api ?? Api).GetArtwork(selectedArt);
            if(art != null)
            {
                Text(board,Wrap(art.description,34,2),new Vector3(.55f,-.48f,-.08f),.057f,Cream);
                Key(board,"Titre",.75f,-.65f,.36f,.113f,()=>BeginText(TextField.Title,art.title));
                Key(board,"Description",1.21f,-.65f,.50f,.113f,()=>BeginText(TextField.Description,art.description));
                Key(board,"Retirer",1.76f,-.65f,.48f,.113f,RemoveArtwork,Red);
            }
            Text(board,"MUSÉE",new Vector3(.55f,-.80f,-.08f),.058f,Cream);
            Key(board,"Sauver",1.25f,-.80f,.51f,.12f,SaveMuseum,Teal);
            Key(board,"Charger",1.79f,-.80f,.49f,.12f,LoadMuseum);
            RoomControls();
        }

        static Transform Panel(Transform parent,string name,Vector3 world,float w,float h)
        {
            var panel=Root(parent,name,world);
            Box(panel,"Cadre",new Vector3(0,0,.04f),new Vector3(w+.04f,h+.04f,.10f),Sand,false);
            Box(panel,"Écran",Vector3.zero,new Vector3(w,h,.055f),Navy);
            return panel;
        }

        static string Short(string value,int size) => string.IsNullOrEmpty(value) ? " " :
            value.Length <= size ? value : value[..(size-1)] + "…";

        void RoomControls()
        {
            var controls=Panel(stage,"Commandes de la maquette",new Vector3(-.9f,.69f,-1.51f),4.02f,.62f);
            controls.localRotation=Quaternion.Euler(38,0,0);
            var room=(draft?.Api ?? Api).GetRoom(draft?.RoomId ?? selectedRoom);
            Text(controls,room==null ? "SALLE" :
                $"{Short(room.name,25)}    {room.width:0.#} × {room.length:0.#} × {room.height:0.#} m",
                new Vector3(-1.87f,.22f,-.08f),.074f,Cream);
            Key(controls,drawing?"Arrêter le tracé":"Tracer une salle",-1.56f,.02f,.76f,.115f,()=>{
                if(!WorkshopReady()) return;
                drawing=!drawing; linking=false; corner=null;
                Tell(drawing?"Pointe le premier coin sur la table.":"Tracé annulé.");
            },Teal);
            Key(controls,"Tourner −15°",-.81f,.02f,.65f,.115f,()=>TurnRoom(-15));
            Key(controls,"Tourner +15°",-.11f,.02f,.65f,.115f,()=>TurnRoom(15));
            Key(controls,linking?"Annuler le lien":"Relier / délier",.62f,.02f,.71f,.115f,()=>{
                if(!WorkshopReady() || selectedRoom==null) return;
                linking=!linking; drawing=false; corner=null;
                Tell(linking?"Pointe une deuxième salle.":"Création de lien annulée.");
            });
            Key(controls,confirmDelete?"Confirmer":"Supprimer",1.45f,.02f,.82f,.115f,DeleteRoom,Red);
            DimensionKeys(controls,-.19f);
        }

        void DimensionKeys(Transform parent,float y)
        {
            for(int i=0;i<3;i++)
            {
                int axis=i;
                string label=new[]{"Largeur","Longueur","Hauteur"}[i];
                var x=-1.60f+i*1.08f;
                Text(parent,label,new Vector3(x-.20f,y,-.08f),.061f,Cream,TextAnchor.MiddleCenter);
                Key(parent,"−",x+.22f,y,.22f,.11f,()=>Resize(axis,-.5f));
                Key(parent,"+",x+.48f,y,.22f,.11f,()=>Resize(axis,.5f));
            }
        }

        void CreateTour()
        {
            if(!WorkshopReady()) return;
            selectedTour=Api.CreateTour("Parcours "+(data.tours.Count+1)).id;
            tourPage=data.tours.Count/PageSize;
            stopPage=0;
            Tell("Parcours créé.");
        }

        void DeleteTour(string id)
        {
            if(!WorkshopReady()) return;
            Api.DeleteTour(id); selectedTour=null; stopPage=0; Tell("Parcours supprimé.");
        }

        void AddStop()
        {
            if(!WorkshopReady()) return;
            if (Api.GetTour(selectedTour)==null || Api.GetArtwork(selectedArt)==null)
            { Tell("Choisis un parcours et une œuvre placée."); return; }
            Api.AddTourArtwork(selectedTour,selectedArt);
            stopPage=(Api.GetTour(selectedTour).artworkIds.Count-1)/PageSize;
            Tell("Étape ajoutée à la fin.");
        }

        void RemoveStop(string tour,int index)
        {
            if(!WorkshopReady()) return;
            Api.RemoveTourArtwork(tour,index);
            Tell("Étape retirée. L’œuvre reste dans le musée.");
        }

        void Reorder(string tour,int from,int to)
        {
            if(!WorkshopReady()) return;
            Api.ReorderTourArtwork(tour,from,to);
            stopPage=to/PageSize;
            Tell("Ordre des étapes modifié.");
        }

        bool WorkshopReady()
        {
            if(draft == null && returningRoom == null && !carriedView && !heldArtwork) return true;
            Tell("Valide ou annule l’édition, puis rapporte la salle sur la maquette.");
            return false;
        }

        void SaveMuseum()
        {
            if(!WorkshopReady()) return;
            Tell(backend.Save(out var error) ? "Musée et visite sauvegardés." : "Échec de la sauvegarde : "+error);
        }

        void LoadMuseum()
        {
            if(!WorkshopReady()) return;
            if(!backend.Load(out var error)) { Tell("Chargement impossible : "+error); return; }
            selectedRoom=selectedArt=null;
            selectedTour=Api.Session.tourId;
            Tell("Musée et progression restaurés.");
        }

        void BeginText(TextField field,string value)
        {
            var art=(draft?.Api ?? Api).GetArtwork(selectedArt);
            if (draft!=null && (field==TextField.Title || field==TextField.Description) && art?.roomId!=draft.RoomId)
            { Tell("Choisis une œuvre de la salle en cours d’édition."); return; }
            if((field==TextField.TourName || field==TextField.TourDescription) && !WorkshopReady()) return;
            if(returningRoom!=null) { Tell("Rapporte d’abord la salle sur la maquette."); return; }
            activeField=field;
            textValue=value??"";
            Tell(field==TextField.Title || field==TextField.TourName ? "Écris le titre." : "Écris la description.");
        }

        void DrawKeyboard(Transform board)
        {
            Text(board,Wrap(textValue+"▏",68,3),new Vector3(-2.02f,.39f,-.08f),.064f,Cream);
            var rows=new[]{"abcdefghijklmn","opqrstuvwxyz.,","àâçéèêëîïôùûœ’","0123456789!?-/"};
            for(int i=0;i<rows.Length;i++)
                for(int j=0;j<rows[i].Length;j++)
                {
                    var value=rows[i][j].ToString();
                    Key(board,value,-1.91f+j*.29f,.13f-i*.195f,.26f,.155f,()=>Append(value));
                }
            Key(board,"Maj.",-1.78f,-.72f,.52f,.16f,()=>{upper=!upper;QueueDraw();},upper?Teal:Quiet);
            Key(board,"Espace",-.99f,-.72f,.96f,.16f,()=>Append(" "));
            Key(board,"Effacer",-.22f,-.72f,.53f,.16f,()=>{if(textValue.Length>0)textValue=textValue[..^1];QueueDraw();});
            Key(board,"Vider",.31f,-.72f,.45f,.16f,()=>{textValue="";QueueDraw();});
            Key(board,"Annuler",.90f,-.72f,.60f,.16f,()=>{activeField=TextField.None;Tell("Texte inchangé.");});
            Key(board,"Appliquer",1.71f,-.72f,.75f,.16f,ApplyText,Teal);
        }

        bool upper;
        void Append(string value)
        {
            if(textValue.Length<220) textValue += upper ? value.ToUpperInvariant() : value;
            QueueDraw();
        }

        void ApplyText()
        {
            var value=textValue.Trim();
            var field=activeField;
            if((field==TextField.Title || field==TextField.TourName) && value=="") { Tell("Le nom doit contenir du texte."); return; }
            activeField=TextField.None;
            var tour=Api.GetTour(selectedTour);
            if((field==TextField.TourName || field==TextField.TourDescription) && tour != null)
                Api.EditTour(tour.id,field==TextField.TourName?value:tour.name,field==TextField.TourDescription?value:tour.description);
            else
            {
                var art=(draft?.Api ?? Api).GetArtwork(selectedArt);
                if(art!=null) EditArtwork(a=>a.EditArtwork(art.id,field==TextField.Title?value:art.title,
                    field==TextField.Description?value:art.description));
            }
            Tell("Fiche mise à jour.");
        }

        void EditArtwork(Action<MuseumApi> change)
        {
            if(draft!=null) draft.Apply(change);
            else change(Api);
        }

        void RemoveArtwork()
        {
            var art=(draft?.Api ?? Api).GetArtwork(selectedArt);
            if(art==null || (draft!=null && art.roomId!=draft.RoomId)) return;
            EditArtwork(a=>a.RemoveArtwork(art.id));
            selectedArt=null;
            Tell("Œuvre retirée du musée et des parcours.");
        }
    }
}
