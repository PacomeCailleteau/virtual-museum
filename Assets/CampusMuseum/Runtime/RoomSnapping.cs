using System;
using System.Linq;
using UnityEngine;
using VirtualMuseum;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        static Vector2 RoomHalfExtents(Room room)
        {
            var x=room.rotation*Vector3.right;
            var z=room.rotation*Vector3.forward;
            return new Vector2(Mathf.Abs(x.x)*room.width+Mathf.Abs(z.x)*room.length,
                Mathf.Abs(x.z)*room.width+Mathf.Abs(z.z)*room.length)*.5f;
        }

        static bool IsAligned(Room room) => Mathf.Abs(Mathf.DeltaAngle(room.rotation.eulerAngles.y,
            Mathf.Round(room.rotation.eulerAngles.y/90f)*90f))<1f;

        void SnapRoom(Room room)
        {
            if(!IsAligned(room)) return;
            var original=room.position;
            var half=RoomHalfExtents(room);
            float best=.045f/Scale;
            Vector3 chosen=original;
            foreach(var other in data.rooms.Where(r=>r.id!=room.id && IsAligned(r)))
            {
                var size=RoomHalfExtents(other);
                for(int axis=0;axis<2;axis++)
                {
                    int coordinate=axis==0?0:2;
                    int cross=axis==0?2:0;
                    float overlap=half[1-axis]+size[1-axis]-Mathf.Abs(original[cross]-other.position[cross]);
                    if(overlap<.6f)continue;
                    foreach(var sign in new[]{-1f,1f})
                    {
                        var candidate=original;
                        candidate[coordinate]=other.position[coordinate]+sign*(half[axis]+size[axis]);
                        float distance=Vector3.Distance(candidate,original);
                        if(distance>=best)continue;
                        room.position=candidate;
                        if(FitsTable(room) && !MuseumLayout.Overlaps(room,data.rooms))
                        {best=distance;chosen=candidate;}
                    }
                }
            }
            room.position=chosen;
        }

        void ConnectTouchingRooms(string id)
        {
            var room=Api.GetRoom(id);
            if(room==null || !IsAligned(room))return;
            var half=RoomHalfExtents(room);
            foreach(var other in Api.Data.rooms.Where(r=>r.id!=id && IsAligned(r)))
            {
                var size=RoomHalfExtents(other);
                var delta=room.position-other.position;
                bool touchingX=Mathf.Abs(Mathf.Abs(delta.x)-half.x-size.x)<.04f &&
                    half.y+size.y-Mathf.Abs(delta.z)>.6f;
                bool touchingZ=Mathf.Abs(Mathf.Abs(delta.z)-half.y-size.y)<.04f &&
                    half.x+size.x-Mathf.Abs(delta.x)>.6f;
                if((touchingX||touchingZ) && !room.connections.Contains(other.id))
                {
                    Api.ConnectRooms(id,other.id);
                    Tell("Salles aimantées et reliées : "+room.name+" et "+other.name+".");
                }
            }
        }
    }
}
