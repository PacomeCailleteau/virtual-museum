using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        int openDrawer = -1;
        Vector3 artworkDropPosition;
        bool artworkCanDrop;
        Vector3 artworkHandOffset;
        float artworkHeight;
        bool artworkDirectCarry;

        void DrawEditor()
        {
            var room = draft?.Api.GetRoom(draft.RoomId) ?? Api.GetRoom(returningRoom);
            var rail = WorkshopContent(workshop.editingControls);
            var label = room == null ? "Dépose une salle sur ce plateau." : Short(room.name,22);
            Text(rail,label,new Vector3(-1.11f,.103f,-.08f),.065f,Cream);
            if(room == null) return;

            if(draft == null)
            {
                var small=Shell(stage,room,EditCenter,room.rotation,Scale,true,Teal);
                foreach(var art in Api.GetArtworksInRoom(room.id)) Artwork(small,Api.Catalog,art);
                var grab=small.gameObject.AddComponent<WorldAction>();
                grab.Direct=true;
                grab.RequiresGrab=true;
                grab.Press=h=>
                {
                    if(carriedView) return;
                    BeginRoomDrag(room,h,small);
                };
                grab.Release=DropRoom;
                Key(rail,"Replacer sur la maquette",0,-.066f,2.14f,.136f,ReturnEditedRoom,Teal);
                return;
            }

            editScale = Mathf.Min(Mathf.Max(.1f,EditWidth-.35f)/room.width,Mathf.Max(.1f,EditLength-.32f)/room.length,.43f);
            artEditor=Shell(stage,room,EditCenter,Quaternion.identity,editScale,true,new Color(.69f,.78f,.72f));
            DrawRoomHandles(room,artEditor);
            foreach(var art in draft.Api.GetArtworksInRoom(room.id))
            {
                var visual=Artwork(artEditor,Api.Catalog,art);
                var grab=Action(visual,h=>BeginArtwork(h,visual,art.id,null,art.localPosition),EndArtwork);
                grab.Direct=true;
                grab.RequiresGrab=true;
            }
            Key(rail,"Annuler",-.88f,.01f,.44f,.136f,()=>{if(!draft.CanUndo){Reject("Aucune action à annuler.");return;}draft.Undo();Tell("Dernière action annulée.");});
            Key(rail,"Rétablir",-.39f,.01f,.44f,.136f,()=>{if(!draft.CanRedo){Reject("Aucune action à rétablir.");return;}draft.Redo();Tell("Dernière action rétablie.");});
            Key(rail,"Abandonner",.15f,.01f,.56f,.136f,()=>CloseEdit(false),Red);
            Key(rail,"Tourner",.72f,.01f,.46f,.136f,RotateExhibit);
            Key(rail,"Descendre l’œuvre",-.63f,-.15f,.90f,.115f,()=>LiftExhibit(-.15f));
            Key(rail,"Monter l’œuvre",.33f,-.15f,.90f,.115f,()=>LiftExhibit(.15f));

            DrawValidationLever();
            DrawTableFeedback(rail);

            DrawDrawers();
        }

        void DrawDrawers()
        {
            var cabinet = WorkshopContent(workshop.catalogue);
            var definitions=Api.Catalog.Definitions;
            for(int i=0;i<definitions.Count;i++)
            {
                var definition=definitions[i];
                var index=i;
                float y=.46f-i*.188f;
                var box=Box(cabinet,definition.title,new Vector3(0,y,openDrawer==i?-.26f:-.11f),
                    new Vector3(.98f,.171f,openDrawer==i?.32f:.06f),openDrawer==i?Teal:Quiet);
                Action(box,_=>{openDrawer=openDrawer==index ? -1 : index;Tell(definition.title);}).Direct=true;
                Text(cabinet,Short(definition.title,27),new Vector3(0,y,openDrawer==i?-.427f:-.146f),.057f,Cream,TextAnchor.MiddleCenter);
                if(openDrawer!=i) continue;
                var boxPosition=new Vector3(.22f,y+.158f,-.36f);
                var sample=new ArtworkInstance {definitionId=definition.id,title=definition.title,localPosition=boxPosition,localScale=Vector3.one*.2f};
                var visual=Artwork(cabinet,Api.Catalog,sample);
                var pick=Action(visual,h=>
                {
                    if(heldArtwork || carriedView) return;
                    var initial=ExhibitStart(draft.Api.GetRoom(draft.RoomId));
                    var token=Artwork(artEditor,Api.Catalog,new ArtworkInstance
                    {
                        definitionId=definition.id,title=definition.title,localPosition=initial
                    });
                    BeginArtwork(h,token,null,definition.id,initial);
                },EndArtwork);
                pick.Direct=true;
                pick.RequiresGrab=true;
                Text(cabinet,"Prendre",new Vector3(-.18f,y+.105f,-.39f),.05f,Cream,TextAnchor.MiddleCenter);
            }
        }

        static Vector3 ExhibitStart(Room room) => new(0, Mathf.Min(1f,room.height-.15f),0);

        void BeginArtwork(MuseumPlayer.Hand hand,GameObject visual,string instanceId,string definitionId,Vector3 local)
        {
            if(draft==null || heldArtwork || carriedView || resizingHand!=null || rotatingHand!=null) return;
            selectedArt=instanceId;
            boardSection=BoardSection.Artworks;
            draggingId=instanceId;
            draggingDefinition=definitionId;
            heldArtwork=visual;
            artHand=hand;
            artworkHeight=local.y;
            artworkDirectCarry=instanceId==null || hand.NearSelection;
            artworkHandOffset=instanceId != null && hand.NearSelection
                ? artEditor.TransformPoint(local)-hand.Ray.origin : Vector3.zero;
            draggingDirection=Vector3.ProjectOnPlane(hand.Ray.direction,Vector3.up);
            draggingOrientation=visual.transform.localRotation;
            draggingTurn=0f;
            foreach(var c in visual.GetComponentsInChildren<Collider>()) c.enabled=false;
            placement.Begin(hand.Ray, artEditor.TransformPoint(local), instanceId != null,
                artworkDirectCarry);
            placementMarker = Box(stage, "Repère de dépôt", placement.Position,
                new Vector3(.13f,.008f,.13f), Teal, false).GetComponent<Renderer>();
            UpdateArtworkDrag(0);
        }

        void UpdateArtworkDrag(float deltaTime)
        {
            var handPosition=artHand.Ray.origin;
            bool aboveTable=InsideBench(handPosition,EditCenter,EditWidth+.16f,EditLength+.16f) &&
                handPosition.y>EditCenter.y-.10f && handPosition.y<EditCenter.y+.80f;
            if(artworkDirectCarry || aboveTable)
            {
                var target=handPosition+artworkHandOffset;
                target.y=artEditor.TransformPoint(new Vector3(0,artworkHeight,0)).y;
                placement.MoveTo(target,deltaTime);
            }
            else placement.Update(artHand.Ray, deltaTime);
            var room = draft.Api.GetRoom(draft.RoomId);
            var rawPosition = artEditor.InverseTransformPoint(placement.Position);
            var inside = MuseumLayout.ClampInside(room, rawPosition);
            artworkCanDrop = placement.CanPlace && (inside - rawPosition).sqrMagnitude * editScale * editScale <= .035f * .035f;
            artworkDropPosition = inside;
            if(artworkDirectCarry) heldArtwork.transform.position=handPosition+artHand.Ray.direction*.035f;
            else heldArtwork.transform.localPosition = inside + Vector3.up * Mathf.Min(.12f, room.height - inside.y);

            if (UnityEngine.InputSystem.Keyboard.current?.rKey.wasPressedThisFrame == true)
                draggingTurn += 15f;
            var direction = Vector3.ProjectOnPlane(artHand.Ray.direction, Vector3.up);
            var yaw = direction.sqrMagnitude > .005f && draggingDirection.sqrMagnitude > .005f
                ? Vector3.SignedAngle(draggingDirection, direction, Vector3.up) : 0;
            var intentionalYaw = Mathf.Sign(yaw) * Mathf.Max(0, Mathf.Abs(yaw) - 12f) * .6f;
            var rotation = Quaternion.AngleAxis(intentionalYaw + draggingTurn, Vector3.up) * draggingOrientation;
            heldArtwork.transform.localRotation = Quaternion.Slerp(heldArtwork.transform.localRotation, rotation,
                1f - Mathf.Exp(-deltaTime / .10f));

            placementMarker.transform.position = artEditor.TransformPoint(new Vector3(inside.x,.045f,inside.z));
            placementMarker.sharedMaterial = MakeMaterial(artworkCanDrop ? Teal : Red, true);
        }

        void EndArtwork(MuseumPlayer.Hand hand)
        {
            if(artHand != hand || !heldArtwork || draft==null) return;
            var room=draft.Api.GetRoom(draft.RoomId);
            var placed=artworkCanDrop && !hand.ReleaseCancelled;
            var rotation=heldArtwork.transform.localRotation;
            var position=artworkDropPosition;
            var id=draggingId;
            var definition=draggingDefinition;
            heldArtwork=null;
            artHand=null;
            placementMarker.enabled = false;
            if(!placed)
            {
                Tell("Dépose l’œuvre à l’intérieur de la salle agrandie.");
                return;
            }
            if(id!=null)
            {
                var art=draft.Api.GetArtwork(id);
                if (Vector3.Distance(position,art.localPosition) * editScale > .009f ||
                    Quaternion.Angle(rotation,art.localRotation) > 3f)
                    draft.Apply(api=>api.SetArtworkTransform(id,position,rotation,art.localScale));
            }
            else
            {
                draft.Apply(api=>
                {
                    id=api.AddArtwork(definition,room.id,position).id;
                    api.SetArtworkTransform(id,position,rotation,Vector3.one);
                });
            }
            selectedArt=id;
            Tell("Œuvre placée. Les changements sont en attente de validation.");
        }

        void RotateExhibit()
        {
            var art=draft?.Api.GetArtwork(selectedArt);
            if(art==null || art.roomId!=draft.RoomId) {Tell("Choisis une œuvre dans la salle agrandie.");return;}
            draft.Apply(api=>api.SetArtworkTransform(art.id,art.localPosition,
                Quaternion.Euler(0,15,0)*art.localRotation,art.localScale));
            Tell("Orientation de l’œuvre modifiée.");
        }

        void LiftExhibit(float amount)
        {
            var art=draft?.Api.GetArtwork(selectedArt);
            if(art==null || art.roomId!=draft.RoomId) {Tell("Choisis une œuvre dans la salle agrandie.");return;}
            var room=draft.Api.GetRoom(draft.RoomId);
            var position=MuseumLayout.ClampInside(room,art.localPosition+amount*Vector3.up);
            if(Mathf.Approximately(position.y,art.localPosition.y)) {Reject("Limite de hauteur atteinte dans cette salle.");return;}
            draft.Apply(api=>api.SetArtworkTransform(art.id,position,art.localRotation,art.localScale));
            Tell("Hauteur de l’œuvre modifiée.");
        }

        void CloseEdit(bool apply)
        {
            if(draft==null || heldArtwork || carriedView || resizingHand!=null || rotatingHand!=null) return;
            if(apply)
            {
                draft.Commit(Api);
                ConnectTouchingRooms(draft.RoomId);
                Sparkle(EditCenter+Vector3.up*.15f);
            }
            returningRoom=draft.RoomId;
            draft=null;
            selectedArt=null;
            openDrawer=-1;
            activeField=TextField.None;
            Tell(apply ? "Salle validée. Utilise Replacer sur la maquette." : "Édition abandonnée. Utilise Replacer sur la maquette.");
        }

        void ReturnEditedRoom()
        {
            if(returningRoom==null || draft!=null || carriedView || heldArtwork || resizingHand!=null) return;
            selectedRoom=returningRoom;
            returningRoom=null;
            Tell("Salle replacée à son emplacement sur la maquette.");
        }
    }
}
