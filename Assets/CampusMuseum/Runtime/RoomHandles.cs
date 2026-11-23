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
                var local = axis==0 ? new Vector3(room.width/2+.55f,.45f,0)
                    : axis==1 ? new Vector3(0,.45f,room.length/2+.55f)
                    : new Vector3(0,room.height*.52f+.55f,0);
                var size = .065f / shell.lossyScale.x;
                var color = new[]{new Color(.94f,.38f,.30f),Teal,Gold}[axis];
                var handle=Box(shell,"Poignée "+new[]{"Largeur","Longueur","Hauteur"}[axis],local,Vector3.one*size,color);
                var action=Action(handle,h=>BeginRoomResize(room,shell,index,h),EndRoomResize);
                action.Direct=true;
                action.RequiresGrab=true;
                var label=Text(shell,new[]{"Largeur","Longueur","Hauteur"}[axis],local+Vector3.up*size,
                    .045f/shell.lossyScale.x,Color.white,TextAnchor.MiddleCenter);
                label.gameObject.SetActive(false);
                action.Focus=active=> { if(label) label.gameObject.SetActive(active); };
            }
        }

        void BeginRoomResize(Room room, Transform shell, int axis, MuseumPlayer.Hand hand)
        {
            if (resizingHand!=null || carriedView || heldArtwork || returningRoom!=null) return;
            resizingOriginal=room;
            resizingCandidate=(draft?.Api ?? Api).GetRoom(room.id);
            resizingAxis=axis;
            resizeDirection=axis==0?shell.right:axis==1?shell.forward:Vector3.up;
            resizeOrigin=shell.position;
            resizeScale=shell.lossyScale.x*(axis==2?.52f:.5f);
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
            if(!hand.ReleaseCancelled && resizeError==null)
            {
                var room=resizingCandidate;
                if(room.width==resizingOriginal.width && room.length==resizingOriginal.length && room.height==resizingOriginal.height)
                {
                    Tell("Dimensions conservées.");
                    return;
                }
                if(draft!=null) draft.Apply(api=>api.ResizeRoom(room.id,room.width,room.length,room.height));
                else Api.ResizeRoom(room.id,room.width,room.length,room.height);
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
