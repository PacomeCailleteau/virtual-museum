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
        Transform rotationRig;
        TextMesh rotationHelp;

        void DrawRoomRotation(Room room,Transform shell)
        {
            if(draft!=null)return;
            float radius=Mathf.Max(room.width,room.length)*shell.lossyScale.x*.5f+.075f;
            var center=shell.position+Vector3.up*.035f;
            rotationRig=Root(stage,"Commande de rotation",center);
            rotationRig.rotation=room.rotation;
            for(int i=0;i<24;i++)
            {
                float a=Mathf.Lerp(195,345,i/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(195,345,(i+1)/24f)*Mathf.Deg2Rad;
                var from=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius;
                var to=new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*radius;
                Line(rotationRig,Vector3.zero,Vector3.zero,.008f,Teal);
                var line=rotationRig.GetChild(rotationRig.childCount-1).GetComponent<LineRenderer>();
                line.useWorldSpace=false;line.SetPosition(0,from);line.SetPosition(1,to);
            }
            float angle=225*Mathf.Deg2Rad;
            var pos=rotationRig.TransformPoint(new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius);
            var tangent=rotationRig.TransformDirection(new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle)));
            var arrow=StretchArrow.Create(rotationRig,"Arc de rotation",pos-tangent*.04f,tangent,Teal);arrow.SetLength(.09f);
            var action=arrow.Bind(hand=>BeginRoomRotation(room,shell,hand),EndRoomRotation);
            action.Focus=focused=>
            {
                if(rotationHelp){rotationHelp.text=focused?"Rotation : maintenir Grab et tourner par quarts de tour":"Flèches : dimensions · Arc : rotation par quarts de tour";rotationHelp.color=focused?Teal:Cream;}
            };
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
            if(rotationRig)rotationRig.rotation=rotatingCandidate.rotation;
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
