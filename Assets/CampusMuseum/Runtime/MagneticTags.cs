using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        MuseumPlayer.Hand tagHand;
        string tagArtwork;
        int tagSourceIndex;
        Transform magneticTag;
        Renderer tourDropSurface;
        bool tagCanDrop;
        int tagInsertIndex;

        void DrawTourComposition(Transform board)
        {
            Text(board,"PARCOURS",new Vector3(-1.98f,.22f,-.033f),.075f,Ink);
            Text(board,"ŒUVRES · STOCK ∞",new Vector3(-.99f,.22f,-.033f),.070f,Ink);
            var tours=data.tours;
            tourPage=Mathf.Clamp(tourPage,0,Mathf.Max(0,(tours.Count-1)/PageSize));
            for(int i=0;i<PageSize && tourPage*PageSize+i<tours.Count;i++)
            {
                var item=tours[tourPage*PageSize+i];
                BoardKey(board,Short(item.name,14),-1.53f,.04f-i*.20f,.90f,()=>{
                    selectedTour=item.id;stopPage=selectedStep=0;editTour=false;Tell(item.name);
                },item.id==selectedTour?Teal:null);
            }
            BoardKey(board,"‹",-1.88f,-.82f,.23f,()=>{tourPage--;QueueDraw();});
            BoardKey(board,"›",-1.60f,-.82f,.23f,()=>{tourPage++;QueueDraw();});
            BoardKey(board,"Créer",-1.20f,-.82f,.50f,CreateTour,Teal);
            var arts=Api.Data.artworks;
            artPage=Mathf.Clamp(artPage,0,Mathf.Max(0,(arts.Count-1)/PageSize));
            for(int i=0;i<PageSize && artPage*PageSize+i<arts.Count;i++)
            {
                var art=arts[artPage*PageSize+i];
                var room=Api.GetRoom(art.roomId);
                var label=Short(art.title,18)+"\n"+Short(room?.name,18);
                CreateArtworkTag(board,art.id,label,new Vector3(-.35f,.04f-i*.20f,-.055f),1.18f,-1);
            }
            BoardKey(board,"‹",-.67f,-.77f,.32f,()=>{artPage--;QueueDraw();});
            BoardKey(board,"›",-.20f,-.77f,.32f,()=>{artPage++;QueueDraw();});
            var tour=Api.GetTour(selectedTour);
            if(tour==null)
            {Text(board,"Crée ou choisis un parcours.",new Vector3(.35f,-.10f,-.033f),.075f,Ink);return;}
            Text(board,Short(tour.name,25),new Vector3(.35f,.22f,-.033f),.08f,Ink);
            if(editTour)
            {
                Text(board,Wrap(tour.description,32,2),new Vector3(.35f,-.02f,-.033f),.065f,Ink);
                BoardKey(board,"Renommer",1.18f,-.24f,1.63f,()=>BeginText(TextField.TourName,tour.name));
                BoardKey(board,"Description",1.18f,-.46f,1.63f,()=>BeginText(TextField.TourDescription,tour.description));
                BoardKey(board,"Supprimer",.76f,-.78f,.79f,()=>DeleteTour(tour.id),Red);
                BoardKey(board,"Étapes",1.61f,-.78f,.79f,()=>{editTour=false;QueueDraw();});return;
            }
            var area=Box(board,"Zone des étiquettes du parcours",new Vector3(1.17f,-.27f,-.032f),new Vector3(1.79f,.84f,.004f),new Color(.89f,.94f,.95f),false);
            tourDropSurface=area.GetComponent<Renderer>();tourDropSurface.sharedMaterial=MakeMaterial(new Color(.89f,.94f,.95f),true);
            int count=tour.artworkIds.Count;stopPage=Mathf.Clamp(stopPage,0,Mathf.Max(0,(count-1)/3));
            for(int i=0;i<3 && stopPage*3+i<count;i++)
            {
                int index=stopPage*3+i;var id=tour.artworkIds[index];
                var art=Api.GetArtwork(id);
                var label=(id==lastAddedArtwork?"✓ ":"")+(index+1)+". "+Short(art?.title,22);
                CreateArtworkTag(board,id,label,new Vector3(1.17f,.04f-i*.22f,-.06f),1.65f,index);
            }
            if(count==0)Text(board,"Déposer une étiquette ici",new Vector3(.42f,-.16f,-.04f),.075f,Ink);
            selectedStep=Mathf.Clamp(selectedStep,0,Mathf.Max(0,count-1));
            if(count>0)
            {
                BoardKey(board,"↑",.55f,-.65f,.38f,()=>{if(selectedStep>0){Reorder(tour.id,selectedStep,selectedStep-1);selectedStep--;stopPage=selectedStep/3;}});
                BoardKey(board,"↓",.99f,-.65f,.38f,()=>{if(selectedStep<count-1){Reorder(tour.id,selectedStep,selectedStep+1);selectedStep++;stopPage=selectedStep/3;}});
                BoardKey(board,"Retirer",1.63f,-.65f,.79f,()=>RemoveStop(tour.id,selectedStep),Red);
            }
            BoardKey(board,"‹",.50f,-.84f,.32f,()=>{stopPage--;QueueDraw();});
            BoardKey(board,"›",.88f,-.84f,.32f,()=>{stopPage++;QueueDraw();});
            BoardKey(board,"Modifier",1.57f,-.84f,.93f,()=>{editTour=true;QueueDraw();});
        }

        void CreateArtworkTag(Transform board,string id,string label,Vector3 position,float width,int sourceIndex)
        {
            var paper=Box(board,"Étiquette œuvre "+id+" "+(sourceIndex<0?"stock":sourceIndex.ToString()),position,new Vector3(width,.17f,.035f),sourceIndex<0?new Color(1f,.97f,.75f):new Color(.82f,.95f,.87f));
            paper.GetComponent<Renderer>().sharedMaterial=MakeMaterial(sourceIndex<0?new Color(1f,.97f,.75f):new Color(.82f,.95f,.87f),true);
            Text(board,label,position+Vector3.back*.026f,label.Contains('\n')?.055f:.065f,Ink,TextAnchor.MiddleCenter);
            var action=Action(paper,h=>BeginMagneticTag(h,id,sourceIndex,paper.transform),EndMagneticTag);
            action.Direct=true;action.RequiresGrab=true;
            action.Focus=hover=>HighlightArtworkById(id,hover);
            var magnet=Box(board,"Aimant",position+new Vector3(-width/2+.045f,.052f,-.026f),new Vector3(.045f,.045f,.015f),sourceIndex<0?Gold:Teal,false);
            magnet.transform.localRotation=Quaternion.Euler(0,0,15);
        }

        void BeginMagneticTag(MuseumPlayer.Hand hand,string id,int sourceIndex,Transform source)
        {
            if(!WorkshopReady())return;
            if(Api.GetTour(selectedTour)==null){Reject("Crée ou choisis un parcours avant de prendre une étiquette.");return;}
            tagHand=hand;tagArtwork=id;tagSourceIndex=sourceIndex;selectedArt=id;
            if(sourceIndex>=0)selectedStep=sourceIndex;
            magneticTag=Root(stage,"Étiquette tenue",source.position);magneticTag.rotation=workshop.board.rotation;magneticTag.localScale=workshop.board.lossyScale;
            Box(magneticTag,"Papier",Vector3.zero,new Vector3(1.1f,.17f,.035f),new Color(1,.94f,.55f),false);
            Text(magneticTag,Short(Api.GetArtwork(id)?.title,24),new Vector3(0,0,-.025f),.065f,Ink,TextAnchor.MiddleCenter);
            HighlightArtworkById(id,true);UpdateMagneticTag();
        }

        Vector3 TagPosition(MuseumPlayer.Hand hand)
        {
            if(hand.NearSelection)return hand.Ray.origin;
            var plane=new Plane(workshop.board.forward,workshop.board.position);
            return plane.Raycast(hand.Ray,out var distance)?hand.Ray.GetPoint(Mathf.Clamp(distance,0,12)):hand.Ray.GetPoint(.5f);
        }

        void UpdateMagneticTag()
        {
            var world=TagPosition(tagHand);var local=workshop.board.InverseTransformPoint(world);
            magneticTag.position=world-workshop.board.forward*.04f;
            tagCanDrop=local.x>.28f && local.x<2.08f && local.y<.16f && local.y>-.58f && Mathf.Abs(local.z)<.45f;
            tagInsertIndex=Mathf.Clamp(stopPage*3+Mathf.RoundToInt((.04f-local.y)/.22f),0,Api.GetTour(selectedTour).artworkIds.Count);
            if(tourDropSurface)tourDropSurface.sharedMaterial=MakeMaterial(tagCanDrop?new Color(.67f,.91f,.75f):new Color(.89f,.94f,.95f),true);
        }

        void EndMagneticTag(MuseumPlayer.Hand hand)
        {
            if(tagHand!=hand)return;
            UpdateMagneticTag();
            bool accept=tagCanDrop&&!hand.ReleaseCancelled;
            tagHand=null;Destroy(magneticTag.gameObject);magneticTag=null;
            if(!accept){Tell("Étiquette remise dans son stock. Dépose-la dans le parcours à droite.");return;}
            var tour=Api.GetTour(selectedTour);
            int index=Mathf.Clamp(tagInsertIndex,0,tour.artworkIds.Count);
            if(tagSourceIndex>=0)
            {
                index=Mathf.Min(index,tour.artworkIds.Count-1);
                if(index!=tagSourceIndex)Api.ReorderTourArtwork(tour.id,tagSourceIndex,index);
                selectedStep=index;stopPage=index/3;Tell(index==tagSourceIndex?"Étape sélectionnée.":"Étiquette déplacée dans le parcours.");
            }
            else
            {
                Api.AddTourArtwork(tour.id,tagArtwork,index);selectedStep=index;stopPage=index/3;
                ConfirmArtworkAdded(tagArtwork,index);
            }
        }

        void ConfirmArtworkAdded(string id,int index)
        {
            lastAddedArtwork=id;
            boardNotice="✓ "+Short(Api.GetArtwork(id)?.title,23)+" ajouté à "+Short(Api.GetTour(selectedTour)?.name,20)+" · étape "+(index+1);
            boardNoticeUntil=Time.unscaledTime+5f;
            Tell(boardNotice);
        }
    }
}
