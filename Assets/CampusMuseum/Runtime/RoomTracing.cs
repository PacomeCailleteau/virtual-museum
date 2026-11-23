using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        MuseumPlayer.Hand traceHand;
        Renderer returnMarker;

        void BeginRoomTrace(MuseumPlayer.Hand hand)
        {
            if(!drawing || traceHand!=null || !HitPlane(hand.Ray,MapCenter.y,out var point) ||
                !InsideBench(point,MapCenter,MapWidth,MapLength))return;
            traceHand=hand;corner=point;
            preview=Box(stage,"Emprise de la salle",point+Vector3.up*.016f,new Vector3(.02f,.02f,.02f),Teal,false).transform;
            previewDimensions=Text(stage,"",MapCenter+new Vector3(0,.12f,-MapLength/2),.07f,Navy,TextAnchor.MiddleCenter);
            Tell("Maintiens la gâchette, trace la salle et relâche pour valider.");
            UpdateRoomTrace();
        }

        Room TracedRoom(Vector3 end)
        {
            var first=corner.Value;var size=(end-first)/Scale;
            return new Room{position=InMuseum((end+first)*.5f),width=Mathf.Abs(size.x),length=Mathf.Abs(size.z),height=3};
        }

        bool ValidTrace(Room room)=>room.width>=2 && room.length>=2 && FitsTable(room) && !MuseumLayout.Overlaps(room,Api.Data.rooms);

        void UpdateRoomTrace()
        {
            if(!corner.HasValue || !preview)return;
            if(!HitPlane(traceHand.Ray,MapCenter.y,out var end))return;
            var room=TracedRoom(end);
            preview.position=(corner.Value+end)*.5f+Vector3.up*.016f;
            preview.localScale=new Vector3(room.width*Scale,.02f,room.length*Scale);
            bool valid=InsideBench(end,MapCenter,MapWidth,MapLength)&&ValidTrace(room);
            preview.GetComponent<Renderer>().sharedMaterial=MakeMaterial(valid?Teal:Red,true);
            previewDimensions.text=$"{room.width:0.#} × {room.length:0.#} m · Relâcher pour créer";
        }

        void EndRoomTrace(MuseumPlayer.Hand hand)
        {
            if(traceHand!=hand)return;
            traceHand=null;
            if(hand.ReleaseCancelled || !HitPlane(hand.Ray,MapCenter.y,out var end) || !corner.HasValue)
            {corner=null;Tell("Tracé annulé. Maintiens la gâchette pour recommencer.");return;}
            var room=TracedRoom(end);corner=null;
            if(!InsideBench(end,MapCenter,MapWidth,MapLength)||!ValidTrace(room))
            {Reject("Prévois au moins 2 m de côté et un emplacement libre.");return;}
            var created=Api.CreateRoom("Galerie "+(Api.Data.rooms.Count+1),room.position,room.width,room.length,room.height);
            selectedRoom=created.id;drawing=false;
            Tell(created.name+" créée au relâchement.");
        }
    }
}
