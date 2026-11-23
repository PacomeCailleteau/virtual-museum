using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        MuseumPlayer.Hand rotatingHand;
        Room rotatingOriginal;
        Room rotatingCandidate;
        Vector3 rotationCenter;
        Vector3 rotationStart;
        Transform rotationPreview;
        TextMesh rotationCaption;
        string rotationError;
        float rotationAngle;
        float rotationScale;

        void DrawRoomRotation(Room room,Transform shell)
        {
            if(draft!=null)return;
            float radius=Mathf.Max(room.width,room.length)*shell.lossyScale.x*.5f+.075f;
            var center=shell.position+Vector3.up*.035f;
            for(int i=0;i<24;i++)
            {
                float a=Mathf.Lerp(195,345,i/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(195,345,(i+1)/24f)*Mathf.Deg2Rad;
                Line(stage,center+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius,
                    center+new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*radius,.008f,Teal);
            }
            float angle=225*Mathf.Deg2Rad;
            var pos=center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius;
            var tangent=new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle));
            var arrow=StretchArrow.Create(stage,"Arc de rotation",pos-tangent*.04f,tangent,Teal);arrow.SetLength(.09f);
            arrow.Bind(hand=>BeginRoomRotation(room,shell,hand),EndRoomRotation);
            Text(stage,"90°",pos+Vector3.up*.06f,.043f,Color.white,TextAnchor.MiddleCenter);
        }

        Vector3 RotationPoint(MuseumPlayer.Hand hand)
        {
            if(hand.NearSelection)return hand.Ray.origin-rotationCenter;
            return HitPlane(hand.Ray,rotationCenter.y,out var point)?point-rotationCenter:hand.Ray.origin-rotationCenter;
        }

        void BeginRoomRotation(Room room,Transform shell,MuseumPlayer.Hand hand)
        {
            if(!WorkshopReady())return;
            rotatingOriginal=room;rotatingCandidate=Api.GetRoom(room.id);rotatingHand=hand;
            rotationCenter=shell.position;rotationScale=shell.lossyScale.x;
            rotationStart=Vector3.ProjectOnPlane(RotationPoint(hand),Vector3.up);
            rotationCaption=Text(stage,"Rotation par crans de 90°",shell.position+Vector3.up*.45f,.07f,Navy,TextAnchor.MiddleCenter);
            rotationAngle=float.NaN;UpdateRoomRotation();
        }

        void UpdateRoomRotation()
        {
            var point=Vector3.ProjectOnPlane(RotationPoint(rotatingHand),Vector3.up);
            float delta=Vector3.SignedAngle(rotationStart,point,Vector3.up);
            float angle=Mathf.Round(delta/90f)*90f;
            if(Mathf.Approximately(angle,rotationAngle))return;
            rotationAngle=angle;
            rotatingCandidate.rotation=Quaternion.AngleAxis(angle,Vector3.up)*rotatingOriginal.rotation;
            rotationError=(!FitsTable(rotatingCandidate)||MuseumLayout.Overlaps(rotatingCandidate,data.rooms))
                ?"Rotation refusée : espace insuffisant.":null;
            if(rotationPreview){rotationPreview.gameObject.SetActive(false);Destroy(rotationPreview.gameObject);}
            rotationPreview=Shell(stage,rotatingCandidate,rotationCenter,rotatingCandidate.rotation,rotationScale,true,rotationError==null?Teal:Red);
            foreach(var collider in rotationPreview.GetComponentsInChildren<Collider>())collider.enabled=false;
            rotationCaption.text=rotationError??$"{angle:0}° · Relâcher pour appliquer";
        }

        void EndRoomRotation(MuseumPlayer.Hand hand)
        {
            if(rotatingHand!=hand)return;
            rotatingHand=null;
            if(hand.ReleaseCancelled){Tell("Rotation annulée.");return;}
            if(rotationError!=null){Reject(rotationError);return;}
            Api.SetRoomTransform(rotatingOriginal.id,rotatingOriginal.position,rotatingCandidate.rotation);
            ConnectTouchingRooms(rotatingOriginal.id);
            Tell("Rotation appliquée par crans de 90°.");
        }
    }
}
