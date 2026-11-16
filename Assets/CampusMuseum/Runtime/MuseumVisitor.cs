using System;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        int visitorPage;

        void EnterMuseum()
        {
            if(!WorkshopReady()) return;
            var room=data.rooms.FirstOrDefault();
            if(room==null) {Tell("Crée une salle avant de visiter.");return;}
            visiting=true;
            guided=false;
            status="Choisis un parcours sur le panneau d’accueil ou visite librement.";
            MoveToRoom(room);
        }

        void BackToWorkshop()
        {
            visiting=false;
            visitorArt=null;
            player.Place(Arrival,0);
            Tell("De retour à l’atelier.");
        }

        void MoveToRoom(Room room)
        {
            visitorRoom=room.id;
            visitorArt=null;
            var local = new Vector3(0,0,-room.length/2+.8f);
            var p = room.position + room.rotation*local;
            player.Place(p,room.rotation.eulerAngles.y);
            QueueDraw();
        }

        bool CanStand(Vector3 world)
        {
            if(!visiting)
            {
                bool inside = world.x>-4.3f && world.x<6.3f && world.z>-4.25f && world.z<3.15f;
                return inside && !InsideBench(world,MapCenter,MapWidth+.22f,MapLength+.15f) &&
                    !InsideBench(world,EditCenter,2.67f,2.08f) &&
                    !InsideBench(world,new Vector3(4.85f,0,.84f),1.30f,.8f);
            }
            var room=Api.GetRoom(visitorRoom);
            if(room==null) return false;
            var p=Quaternion.Inverse(room.rotation)*(world-room.position);
            if(Mathf.Abs(p.x)>room.width/2-.24f || Mathf.Abs(p.z)>room.length/2-.24f) return false;
            return !data.artworks.Any(a=> a.roomId==room.id && Vector2.Distance(new Vector2(a.localPosition.x,a.localPosition.z),new Vector2(p.x,p.z))<.44f);
        }

        void DrawVisitor()
        {
            var room=Api.GetRoom(visitorRoom);
            if(room==null) { BackToWorkshop();return; }
            var root=Shell(stage,room,room.position,room.rotation,1,false,new Color(.62f,.69f,.68f));
            var current=guided?Api.CurrentArtwork:null;
            var tour=guided?Api.GetTour(Api.Session.tourId):null;
            var nextRoom=current==null ? null : MuseumLayout.NextRoom(data,room.id,current.roomId);
            Vector3? guidePoint=null;
            for(int i=0;i<room.connections.Count;i++)
            {
                var connected=Api.GetRoom(room.connections[i]);
                if(connected==null) continue;
                int n=room.connections.Count;
                bool stack=n>4;
                float z=stack ? Mathf.Lerp(-room.length/2+.45f,room.length/2-.43f,(i/2f)/(n/2f)) : room.length/2-.10f;
                float x=stack?(i%2==0?-room.width/2+.1f:room.width/2-.1f): Mathf.Lerp(-room.width/2+.28f,room.width/2-.28f,(i+1f)/(n+1));
                var door=Door(root,Short(connected.name,18),new Vector3(x,0,z),()=>MoveToRoom(connected),connected.id==nextRoom,Mathf.Clamp(room.width/(n+1),.38f,1.03f));
                if(stack) door.localRotation=Quaternion.Euler(0,i%2==0?-90:90,0);
                if(connected.id==nextRoom) guidePoint=new Vector3(x,0,z-.3f);
            }
            Door(root,"Atelier",new Vector3(room.width/2-.1f,0,Mathf.Min(-.3f, -room.length/2+.83f)),BackToWorkshop,false,.65f)
                .localRotation=Quaternion.Euler(0,90,0);

            foreach(var art in data.artworks.Where(a=>a.roomId==room.id))
            {
                bool target = art.id==current?.id;
                bool inTour=tour?.artworkIds.Contains(art.id) ?? false;
                var exhibit=Artwork(root,Api.Catalog,art);
                Action(exhibit,_=>{ visitorArt=art.id; Tell(art.title); }).Direct=true;
                var bottom=exhibit.GetComponentsInChildren<Renderer>()
                    .Select(renderer=>renderer.bounds.min.y-root.position.y).DefaultIfEmpty(.28f).Min();
                var height=Mathf.Clamp(bottom,.05f,room.height-.10f);
                var pad=Box(root,"Socle de présentation",new Vector3(art.localPosition.x,height/2,art.localPosition.z),
                    new Vector3(.86f,height,.86f),target?Teal:Cream);
                Action(pad,_=>{visitorArt=art.id;Tell(art.title);});
                var label=Wrap(art.title+(inTour?"  •":""),22,2);
                var lineLength=label.Split('\n').Max(line=>line.Length);
                var labelWidth=Mathf.Clamp(lineLength*.077f+.22f,1.05f,Mathf.Min(1.95f,room.width-.18f));
                var labelHeight=label.Contains('\n') ? .36f : .24f;
                var labelX=Mathf.Clamp(art.localPosition.x,-room.width/2+labelWidth/2+.08f,room.width/2-labelWidth/2-.08f);
                var labelZ=Mathf.Max(art.localPosition.z-.62f,-room.length/2+.12f);
                var pos=new Vector3(labelX,Mathf.Clamp(height+.25f,.80f,room.height-.26f),labelZ);
                var size=Mathf.Min(.125f,(labelWidth-.16f)/Mathf.Max(1,lineLength)*1.35f);
                LabelPlate(root,label,pos,labelWidth,labelHeight,size);
                var standHeight = pos.y - labelHeight/2;
                Box(root,"Support du cartel",new Vector3(pos.x,standHeight/2,pos.z+.025f),
                    new Vector3(.035f,standHeight,.045f),Navy,false);
                if(target && visitorArt==art.id)
                {
                    var plaque=Panel(root,"Cartel de l’œuvre",pos-Vector3.up*(labelHeight/2+.09f),.90f,.15f);
                    Key(plaque,"Valider l’étape",0,0,.75f,.112f,()=>ValidateStep(art),Teal);
                }
                if(target) guidePoint=new Vector3(art.localPosition.x,0,art.localPosition.z-.65f);
            }
            if(guidePoint.HasValue) DrawGuide(root,guidePoint.Value);

            DrawVisitorBoard(root,room,tour,current,nextRoom);
            Physics.SyncTransforms();
        }

        void DrawVisitorBoard(Transform root,Room room,VisitorTour tour,ArtworkInstance current,string nextRoom)
        {
            float w=Mathf.Min(3.1f,room.length-.28f);
            var board=Panel(root,"Panneau de la galerie",new Vector3(-room.width/2+.14f,1.22f,0),3.1f,1.64f);
            board.localRotation=Quaternion.Euler(0,-90,0);
            var scale=w/3.1f;
            board.localScale=new Vector3(scale,1,scale);
            Text(board,Short(room.name,31),new Vector3(-1.43f,.68f,-.08f),.09f,Cream);
            string guidance;
            if(!guided) guidance="EXPLORATION LIBRE";
            else if(Api.IsComplete) guidance="PARCOURS TERMINÉ";
            else if(current==null) guidance="Aucune œuvre à visiter";
            else
            {
                var next=Api.GetRoom(nextRoom);
                var destination=current.roomId==room.id ? "dans cette salle" :
                    next==null ? "destination inaccessible" : "via "+next.name;
                guidance=$"Étape {Api.Session.index+1}/{tour?.artworkIds.Count ?? 0}  ·  {Short(current.title,22)}  ·  {destination}";
            }
            Text(board,Wrap(guidance,49,2),new Vector3(-1.43f,.49f,-.08f),.057f,Teal);
            var inspected=Api.GetArtwork(visitorArt);
            if(inspected != null)
            {
                Text(board,Short(inspected.title,40),new Vector3(-1.43f,.29f,-.08f),.068f,Cream);
                Text(board,Wrap(inspected.description,54,4),new Vector3(-1.43f,.05f,-.08f),.055f,Cream);
                bool part=tour?.artworkIds.Contains(inspected.id) ?? false;
                Text(board,part?"Œuvre du parcours":"Œuvre de la collection",new Vector3(-1.43f,-.25f,-.08f),.053f,Quiet);
                if(current?.id==inspected.id)
                    Key(board,"Valider cette étape",-.60f,-.40f,1.62f,.122f,()=>ValidateStep(inspected),Teal);
                Key(board,"Les parcours",.91f,-.40f,1.03f,.122f,()=>{visitorArt=null;QueueDraw();});
            }
            else
            {
                var all=data.tours;
                const int size=3;
                visitorPage=Mathf.Clamp(visitorPage,0,Mathf.Max(0,(all.Count-1)/size));
                for(int i=0;i<size && i+visitorPage*size<all.Count;i++)
                {
                    var item=all[i+visitorPage*size];
                    var y=.29f-i*.187f;
                    Key(board,Short(item.name,20),-.91f,y,1.02f,.129f,()=>StartTour(item.id),item.id==tour?.id?Teal:Quiet);
                    Text(board,Wrap(item.description,34,2),new Vector3(-.33f,y,-.08f),.049f,Cream);
                }
                Key(board,"Préc.",-1.23f,-.34f,.39f,.112f,()=>{visitorPage--;QueueDraw();});
                Key(board,"Suiv.",-.80f,-.34f,.39f,.112f,()=>{visitorPage++;QueueDraw();});
                Key(board,"Visite libre",-.08f,-.34f,.89f,.112f,()=>{guided=false;Tell("Exploration libre.");},Teal);
                Key(board,"Reprendre",.95f,-.34f,1.01f,.112f,()=>{
                    if(Api.HasSelectedTour) {guided=true;Tell("Parcours repris.");}
                });
            }
            Text(board,Wrap(status,58,2),new Vector3(-1.43f,-.53f,-.08f),.049f,Teal);
            Key(board,"Sauvegarder",-.94f,-.69f,.99f,.106f,SaveMuseum);
            Key(board,"Atelier",.96f,-.69f,.97f,.106f,BackToWorkshop);
        }

        void StartTour(string id)
        {
            Api.StartTour(id);
            selectedTour=id;
            guided=true;
            Tell("Parcours démarré.");
        }

        void ValidateStep(ArtworkInstance art)
        {
            var room=Api.GetRoom(visitorRoom);
            var world=room.position+room.rotation*art.localPosition;
            var head=player.Eye.position;
            var distance=Vector2.Distance(new Vector2(head.x,head.z),new Vector2(world.x,world.z));
            if(!guided || Api.CurrentArtwork?.id!=art.id || art.roomId!=room.id || distance>2.2f)
            {Tell("Approche-toi de l’œuvre indiquée pour valider.");return;}
            Api.Advance();
            visitorArt=null;
            Tell(Api.IsComplete?"Toutes les étapes ont été validées.":"Étape validée. Suis les repères vers la suivante.");
        }

        static Transform Door(Transform parent,string label,Vector3 local,Action enter,bool highlighted,float width=.94f)
        {
            var door=Root(parent,label,local);
            var color=highlighted?Teal:Quiet;
            Box(door,"Montant gauche",new Vector3(-width*.5f,.95f,0),new Vector3(.085f,1.90f,.15f),color);
            Box(door,"Montant droit",new Vector3(width*.5f,.95f,0),new Vector3(.085f,1.90f,.15f),color);
            Box(door,"Linteau",new Vector3(0,1.9f,0),new Vector3(width+.07f,.13f,.15f),color);
            var target=Box(door,"Passage",new Vector3(0,.93f,-.026f),new Vector3(width-.07f,1.77f,.07f),
                highlighted?new Color(.13f,.48f,.51f):new Color(.20f,.30f,.43f));
            Action(target,_=>enter());
            Text(door,label,new Vector3(0,1.52f,-.072f),Mathf.Min(.08f,width*.11f),Cream,TextAnchor.MiddleCenter);
            Text(door,"ENTRER",new Vector3(0,.85f,-.072f),Mathf.Min(.075f,width*.105f),Cream,TextAnchor.MiddleCenter);
            return door;
        }

        void DrawGuide(Transform parent,Vector3 end)
        {
            var local = parent.InverseTransformPoint(player.Eye.position);
            var at = new Vector3(local.x,.016f,local.z);
            end.y=at.y;
            var pointCount=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(at,end)*3f),3,36);
            for(int i=1;i<=pointCount;i++)
            {
                var p=Vector3.Lerp(at,end,i/(float)pointCount);
                Box(parent,"Repère de parcours",p,new Vector3(.095f,.018f,.095f),Teal,false);
            }
        }
    }
}
