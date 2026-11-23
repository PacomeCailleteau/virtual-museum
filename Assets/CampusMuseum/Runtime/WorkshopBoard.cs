using System;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        static readonly Color Ink=new(.025f,.04f,.055f);
        string boardNotice;
        float boardNoticeUntil;
        string lastAddedArtwork;
        enum TextField { None, Title, Description, TourName, TourDescription }
        TextField activeField;
        string textValue;
        int tourPage;
        int artPage;
        int stopPage;
        Quaternion draggingOrientation;
        Vector3 draggingDirection;
        float draggingTurn;
        string draggingId;
        string draggingDefinition;
        const int PageSize = 4;

        enum BoardSection { Tours, Artworks, Storage }
        BoardSection boardSection;
        bool editTour;
        int selectedStep;

        WorldAction BoardKey(Transform board,string label,float x,float y,float width,Action action,Color? color=null)
        {
            var key=Box(board,label,new Vector3(x,y,-.05f),new Vector3(width,.16f,.045f),color ?? Color.white);
            key.GetComponent<Renderer>().sharedMaterial=MakeMaterial(color.HasValue?Color.Lerp(color.Value,Color.white,.67f):new Color(.94f,.94f,.90f),true);
            Text(board,label,new Vector3(x,y,-.078f),Mathf.Min(.083f,width/Mathf.Max(label.Length*.54f,1f)),Ink,TextAnchor.MiddleCenter);
            return Action(key,_=>action());
        }

        void DrawBoard()
        {
            var board=WorkshopContent(workshop.board);
            RoomControls();
            if(activeField!=TextField.None) { DrawKeyboard(board); return; }
            var sections=new[]{"Parcours","Œuvres","Sauvegarde"};
            for(int i=0;i<sections.Length;i++)
            {
                var section=(BoardSection)i;
                BoardKey(board,sections[i],-1.36f+i*1.36f,.61f,1.26f,()=>{boardSection=section;QueueDraw();},boardSection==section?Teal:null);
            }
            Text(board,boardNotice??(boardSection==BoardSection.Tours?"Prends une étiquette et dépose-la dans le parcours."
                : boardSection==BoardSection.Artworks?"Pointe un nom pour repérer l’œuvre sur la table."
                : "Enregistre ou retrouve ton musée."),new Vector3(-1.96f,.37f,-.033f),.066f,boardNotice!=null?new Color(.015f,.20f,.06f):Ink);
            if(boardSection==BoardSection.Storage)
            {
                Text(board,"Retrouver le musée et la progression de la visite",new Vector3(-1.85f,.09f,-.033f),.09f,Ink);
                BoardKey(board,"Sauvegarder",-.95f,-.20f,1.65f,SaveMuseum,Teal);
                BoardKey(board,"Charger",.95f,-.20f,1.65f,LoadMuseum);
                Text(board,"Valide et replace la salle avant de sauvegarder.",new Vector3(-1.85f,-.49f,-.033f),.085f,Ink);
                Text(board,"Les deux buzzers permettent de recommencer.",new Vector3(-1.85f,-.75f,-.033f),.078f,Ink);
                return;
            }
            if(boardSection==BoardSection.Artworks) { DrawArtworkBoard(board); return; }
            DrawTourComposition(board);
        }

        void DrawArtworkBoard(Transform board)
        {
            var api=draft?.Api ?? Api;
            var list=api.Data.artworks;
            artPage=Mathf.Clamp(artPage,0,Mathf.Max(0,(list.Count-1)/PageSize));
            for(int i=0;i<PageSize && artPage*PageSize+i<list.Count;i++)
            {
                var item=list[artPage*PageSize+i];
                var key=BoardKey(board,Short(item.title,23),-1.12f,.13f-i*.22f,1.72f,()=>{
                    selectedArt=item.id;if(draft==null)selectedRoom=item.roomId;Tell(item.title);
                },item.id==selectedArt?Teal:null);
                key.Focus=focused=>HighlightArtworkById(item.id,focused);
            }
            BoardKey(board,"‹",-1.81f,-.79f,.32f,()=>{artPage--;QueueDraw();});
            BoardKey(board,"›",-1.43f,-.79f,.32f,()=>{artPage++;QueueDraw();});
            var art=api.GetArtwork(selectedArt);
            if(art==null)
            {
                Text(board,"Choisis une œuvre\npour consulter sa fiche.",new Vector3(.03f,-.05f,-.033f),.095f,Ink);
                return;
            }
            Text(board,Short(art.title,27),new Vector3(.03f,.16f,-.033f),.10f,Ink);
            Text(board,Wrap(art.description,37,4),new Vector3(.03f,-.10f,-.033f),.083f,Ink);
            BoardKey(board,"Titre",.35f,-.56f,.60f,()=>BeginText(TextField.Title,art.title));
            BoardKey(board,"Description",1.09f,-.56f,.80f,()=>BeginText(TextField.Description,art.description));
            BoardKey(board,"Retirer",1.78f,-.56f,.53f,RemoveArtwork,Red);
            BoardKey(board,"Ajouter au parcours",1f,-.80f,1.87f,AddStop,Teal);
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
            var controls=WorkshopContent(workshop.mapControls);
            var room=(draft?.Api??Api).GetRoom(draft?.RoomId??selectedRoom);
            Text(controls,room==null?"MAQUETTE":$"{Short(room.name,25)} · {room.width:0.#} × {room.length:0.#} × {room.height:0.#} m",
                new Vector3(-1.87f,.22f,-.031f),.07f,Cream);
            Key(controls,drawing?"Annuler le tracé":"Tracer une salle",-1.45f,.015f,.95f,.115f,()=>{
                if(!WorkshopReady())return;
                drawing=!drawing;linking=false;linkFirstRoom=null;corner=null;
                Tell(drawing?"Maintiens la gâchette sur la table, trace et relâche.":"Tracé annulé.");
            },Teal);
            Key(controls,linking?"Annuler le lien":"Relier / délier",-.10f,.015f,1.1f,.115f,()=>{
                if(!WorkshopReady())return;
                linking=!linking;linkFirstRoom=null;drawing=false;corner=null;
                Tell(linking?"Pointe la première salle, puis la deuxième avec la gâchette index.":"Création de lien annulée.");
            });
            Key(controls,confirmDelete?"Confirmer":"Supprimer",1.36f,.015f,1f,.115f,DeleteRoom,Red);
            rotationHelp=Text(controls,"Flèches : dimensions · Arc : rotation par quarts de tour",new Vector3(-1.87f,-.13f,-.031f),.045f,Cream);
            DrawTableFeedback(controls);
        }

        void CreateTour()
        {
            if(!WorkshopReady()) return;
            selectedTour=Api.CreateTour("Parcours "+(data.tours.Count+1)).id;
            tourPage=(Api.Data.tours.Count-1)/PageSize;
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
            selectedStep=Api.GetTour(selectedTour).artworkIds.Count-1;
            stopPage=selectedStep/3;
            ConfirmArtworkAdded(selectedArt,selectedStep);
            boardSection=BoardSection.Tours;
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
            stopPage=to/3;
            Tell("Ordre des étapes modifié.");
        }

        bool WorkshopReady()
        {
            if(draft == null && returningRoom == null && !carriedView && !heldArtwork && resizingHand==null && rotatingHand==null && leverHand==null && traceHand==null && tagHand==null) return true;
            Tell("Valide ou annule l’édition, puis replace la salle sur la maquette.");
            return false;
        }

        void SaveMuseum()
        {
            if(!WorkshopReady()) return;
            if(!backend.Save(out var error)) {Reject("Échec de la sauvegarde : "+error);return;}
            if(SaveVisitHistory())Tell("Musée et visites sauvegardés.");
        }

        void LoadMuseum()
        {
            if(!WorkshopReady()) return;
            if(!backend.Load(out var error)) { Tell("Chargement impossible : "+error); return; }
            LoadVisitHistory();
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
            Text(board,Wrap(textValue+"▏",68,3),new Vector3(-2.02f,.39f,-.033f),.064f,Ink);
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
            if(art==null || (draft!=null && art.roomId!=draft.RoomId)) {Reject("Choisis une œuvre dans la salle en cours d’édition.");return;}
            EditArtwork(a=>a.RemoveArtwork(art.id));
            selectedArt=null;
            Tell("Œuvre retirée du musée et des parcours.");
        }
    }
}
