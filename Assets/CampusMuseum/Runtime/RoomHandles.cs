using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        MuseumPlayer.Hand resizingHand;
        Room resizingOriginal;
        Room resizingCandidate;
        int resizingAxis;
        Vector3 resizeDirection;
        Vector3 resizeOrigin;
        float resizeStart;
        float resizeScale;
        Transform resizePreview;
        TextMesh resizeCaption;
        string resizeError;
        float resizeDimension;
        StretchArrow activeResizeArrow;
        float resizeWorldScale;
        Renderer[] resizeOriginalSurfaces;

        void DrawRoomHandles(Room room, Transform shell)
        {
            var grid=Root(stage,"Grille de redimensionnement",Vector3.zero);
            var gridColor=Color.Lerp(Quiet,Cream,.45f);
            for(float x=-room.width/2+.5f;x<room.width/2;x+=.5f)
                Line(grid,shell.TransformPoint(new Vector3(x,.012f,-room.length/2)),
                    shell.TransformPoint(new Vector3(x,.012f,room.length/2)),.0015f,gridColor);
            for(float z=-room.length/2+.5f;z<room.length/2;z+=.5f)
                Line(grid,shell.TransformPoint(new Vector3(-room.width/2,.012f,z)),
                    shell.TransformPoint(new Vector3(room.width/2,.012f,z)),.0015f,gridColor);
            for (int axis=0;axis<3;axis++)
            {
                var index=axis;
                var baseLocal=axis==2?new Vector3(0,.15f,0):new Vector3(0,.45f,0);
                var direction=axis==0?shell.right:axis==1?-shell.forward:Vector3.up;
                var color=new[]{new Color(.94f,.38f,.30f),Teal,Gold}[axis];
                var arrow=StretchArrow.Create(stage,"Flèche "+new[]{"Largeur","Longueur","Hauteur"}[axis],shell.TransformPoint(baseLocal),direction,color);
                float distance=(axis==0?room.width*.5f:axis==1?room.length*.5f:room.height*.24f)+.55f;
                arrow.SetLength(distance*shell.lossyScale.x);
                var action=arrow.Bind(h=>{activeResizeArrow=arrow;BeginRoomResize(room,shell,index,h);},EndRoomResize);
                var label=Text(stage,new[]{"Largeur","Longueur","Hauteur"}[axis],
                    arrow.transform.position+direction*(distance*shell.lossyScale.x)+Vector3.up*.05f,
                    .045f,Color.white,TextAnchor.MiddleCenter);
                label.gameObject.SetActive(false);
                action.Focus=active=> { if(label) label.gameObject.SetActive(active); };
            }
            DrawRoomRotation(room,shell);
        }

        void BeginRoomResize(Room room, Transform shell, int axis, MuseumPlayer.Hand hand)
        {
            if (resizingHand!=null || rotatingHand!=null || leverHand!=null || carriedView || heldArtwork || returningRoom!=null) return;
            resizeOriginalSurfaces=shell.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Sol"||r.name.StartsWith("Mur ")).ToArray();
            foreach(var renderer in resizeOriginalSurfaces)renderer.enabled=false;
            for(int axisIndex=0;axisIndex<3;axisIndex++)
                foreach(var a in new[]{-1f,1f})foreach(var b in new[]{-1f,1f})
                {
                    var half=new Vector3(room.width/2,room.height*.12f,room.length/2);
                    int next=(axisIndex+1)%3,last=(axisIndex+2)%3;
                    var from=new Vector3(0,half.y,0);from[axisIndex]-=half[axisIndex];from[next]+=half[next]*a;from[last]+=half[last]*b;
                    var to=from;to[axisIndex]+=half[axisIndex]*2;
                    Line(stage,shell.TransformPoint(from),shell.TransformPoint(to),.002f,Quiet);
                }
            resizingOriginal=room;
            resizingCandidate=(draft?.Api ?? Api).GetRoom(room.id);
            resizingAxis=axis;
            resizeDirection=axis==0?shell.right:axis==1?-shell.forward:Vector3.up;
            resizeWorldScale=shell.lossyScale.x;
            resizeOrigin=shell.position;
            resizeScale=shell.lossyScale.x*(axis==2?.24f:.5f);
            resizeStart=ResizeCoordinate(hand);
            resizeDimension=float.NaN;
            resizingHand=hand;
            resizeCaption=Text(stage,"",shell.position+Vector3.up*.5f,.075f,Navy,TextAnchor.MiddleCenter);
            UpdateRoomResize();
        }

        float ResizeCoordinate(MuseumPlayer.Hand hand)
        {
            if(hand.NearSelection) return Vector3.Dot(hand.Ray.origin-resizeOrigin,resizeDirection);
            var ray=hand.Ray;
            var dot=Vector3.Dot(ray.direction,resizeDirection);
            var from=ray.origin-resizeOrigin;
            var denominator=1-dot*dot;
            if(denominator<.015f) return Vector3.Dot(from,resizeDirection);
            return (Vector3.Dot(from,resizeDirection)-Vector3.Dot(from,ray.direction)*dot)/denominator;
        }

        void UpdateRoomResize()
        {
            var initial = resizingAxis==0?resizingOriginal.width:resizingAxis==1?resizingOriginal.length:resizingOriginal.height;
            var dimension=Mathf.Round((initial+(ResizeCoordinate(resizingHand)-resizeStart)/resizeScale)*2f)/2f;
            dimension=Mathf.Clamp(dimension,resizingAxis==2?2.5f:2f,resizingAxis==2?7f:18f);
            if(Mathf.Approximately(resizeDimension,dimension)) return;
            resizeDimension=dimension;
            if(resizingAxis==0) resizingCandidate.width=dimension;
            if(resizingAxis==1) resizingCandidate.length=dimension;
            if(resizingAxis==2) resizingCandidate.height=dimension;
            resizeError=RoomResizeError(resizingCandidate);
            if(activeResizeArrow)activeResizeArrow.SetLength((dimension*(resizingAxis==2?.24f:.5f)+.55f)*resizeWorldScale,resizeError==null);
            if(resizePreview) { resizePreview.gameObject.SetActive(false); Destroy(resizePreview.gameObject); }
            var onBench=draft!=null;
            resizePreview=Shell(stage,resizingCandidate,resizeOrigin,onBench?Quaternion.identity:resizingCandidate.rotation,
                onBench?editScale:Scale,true,resizeError==null?Teal:Red);
            foreach(var collider in resizePreview.GetComponentsInChildren<Collider>()) collider.enabled=false;
            resizeCaption.text=$"{resizingCandidate.width:0.#} × {resizingCandidate.length:0.#} × {resizingCandidate.height:0.#} m\n"+
                (resizeError??"Grille de 0,5 m · Relâcher pour appliquer");
        }

        void EndRoomResize(MuseumPlayer.Hand hand)
        {
            if(resizingHand!=hand) return;
            resizingHand=null;
            foreach(var renderer in resizeOriginalSurfaces)if(renderer)renderer.enabled=true;
            if(!hand.ReleaseCancelled && resizeError==null)
            {
                var room=resizingCandidate;
                if(room.width==resizingOriginal.width && room.length==resizingOriginal.length && room.height==resizingOriginal.height)
                {
                    Tell("Dimensions conservées.");
                    return;
                }
                if(draft!=null) draft.Apply(api=>api.ResizeRoom(room.id,room.width,room.length,room.height));
                else {Api.ResizeRoom(room.id,room.width,room.length,room.height);ConnectTouchingRooms(room.id);}
                Tell("Dimensions appliquées sur la grille de 0,5 m.");
            }
            else Tell(resizeError??"Redimensionnement annulé.");
        }

        string RoomResizeError(Room room)
        {
            if(!FitsTable(room) || MuseumLayout.Overlaps(room,data.rooms))
                return "Emplacement occupé ou dimensions trop grandes pour la maquette.";
            var outside=(draft?.Api??Api).GetArtworksInRoom(room.id).FirstOrDefault(a=>!MuseumLayout.Contains(room,a.localPosition));
            return outside==null?null:"Déplace d’abord « "+outside.title+" » vers l’intérieur.";
        }
    }
}
