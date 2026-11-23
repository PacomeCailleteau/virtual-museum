using System;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        void EnterMuseum()
        {
            if(!WorkshopReady())return;
            var room=data.rooms.FirstOrDefault();
            if(room==null){Reject("Crée une salle avant de visiter.");return;}
            visiting=true;guided=false;player.ViewElevation=0f;
            Tell("Choisis un parcours en pointant ses repères au sol, ou explore librement.");
            MoveToRoom(room);
        }

        void BackToWorkshop()
        {
            visiting=false;player.ViewElevation=workshop.viewElevation;visitorArt=null;
            player.Place(workshop.arrival.position,workshop.arrival.eulerAngles.y);
            doorCooldown=Time.unscaledTime+1.1f;
            Tell("De retour à l’atelier.");
        }

        void MoveToRoom(Room room,string fromRoom=null)
        {
            visitorRoom=room.id;visitorArt=null;
            var local=fromRoom!=null && room.connections.Contains(fromRoom)
                ?VisitorDoorApproach(room,fromRoom):new Vector3(0,0,-room.length/2+.8f);
            var p=room.position+room.rotation*local;
            player.Place(p,room.rotation.eulerAngles.y+(fromRoom!=null?VisitorDoorYaw(room,fromRoom)+180:0));
            doorCooldown=Time.unscaledTime+1.1f;QueueDraw();
        }

        bool CanStand(Vector3 world)
        {
            if(!visiting)
            {
                bool inside=world.x>-4.20f && world.x<6.3f && world.z>-4.25f && world.z<3.15f;
                return inside && !InsideBench(world,MapCenter,MapWidth+.22f,MapLength+.15f) &&
                    !InsideBench(world,EditCenter,EditWidth+.22f,EditLength+.18f) &&
                    !InsideBench(world,workshop.catalogue.position+Vector3.forward*.19f,1.30f,.8f);
            }
            var room=Api.GetRoom(visitorRoom);if(room==null)return false;
            var p=Quaternion.Inverse(room.rotation)*(world-room.position);
            if(Mathf.Abs(p.x)>room.width/2-.24f || Mathf.Abs(p.z)>room.length/2-.24f)return false;
            return !data.artworks.Any(a=>a.roomId==room.id && Vector2.Distance(new Vector2(a.localPosition.x,a.localPosition.z),new Vector2(p.x,p.z))<.44f);
        }

        void DrawVisitor()
        {
            var room=Api.GetRoom(visitorRoom);
            if(room==null){BackToWorkshop();return;}
            visitorDoors.Clear();
            var root=Shell(stage,room,room.position,room.rotation,1,false,new Color(.62f,.69f,.68f));
            var tour=guided?Api.GetTour(Api.Session.tourId):null;
            var current=guided?Api.CurrentArtwork:null;
            string nextRoom=current==null?null:MuseumLayout.NextRoom(data,room.id,current.roomId);
            foreach(var id in room.connections)
            {
                var connected=Api.GetRoom(id);if(connected==null)continue;
                Action enter=()=>MoveToRoom(connected,room.id);
                int wall=DoorWall(room,id);
                int same=room.connections.Count(next=>DoorWall(room,next)==wall);
                float width=Mathf.Clamp((wall==1||wall==3?room.length:room.width)/(same+1),.62f,1.03f);
                var door=Door(root,Short(connected.name,18),VisitorDoorPosition(room,id),enter,id==nextRoom,width);
                door.localRotation=Quaternion.Euler(0,VisitorDoorYaw(room,id),0);
                visitorDoors.Add((door,width,enter));
            }
            var exit=Door(root,"Atelier",new Vector3(room.width/2-.10f,0,-room.length/2+.83f),BackToWorkshop,false,.65f);
            exit.localRotation=Quaternion.Euler(0,90,0);visitorDoors.Add((exit,.65f,BackToWorkshop));
            int routeIndex=tour==null?-1:data.tours.FindIndex(t=>t.id==tour.id);
            foreach(var art in data.artworks.Where(a=>a.roomId==room.id))
            {
                bool inTour=tour?.artworkIds.Contains(art.id)??false;
                bool visited=inTour && HasVisited(tour.id,art.id);
                var exhibit=Artwork(root,Api.Catalog,art);
                Action(exhibit,_=>{visitorArt=visitorArt==art.id?null:art.id;Tell(art.title);}).Direct=true;
                var bottom=exhibit.GetComponentsInChildren<Renderer>().Select(r=>r.bounds.min.y-root.position.y).DefaultIfEmpty(.28f).Min();
                var height=Mathf.Clamp(bottom,.05f,room.height-.10f);
                var pad=Box(root,"Socle de présentation",new Vector3(art.localPosition.x,height/2,art.localPosition.z),
                    new Vector3(.86f,height,.86f),inTour&&!visited?Teal:Cream);
                Action(pad,_=>{visitorArt=visitorArt==art.id?null:art.id;Tell(art.title);});
                var label=Wrap(art.title,22,2);var lineLength=label.Split('\n').Max(l=>l.Length);
                float width=Mathf.Clamp(lineLength*.077f+.22f,1.05f,Mathf.Min(1.95f,room.width-.18f));
                float labelHeight=label.Contains('\n')?.36f:.24f;
                var pos=new Vector3(Mathf.Clamp(art.localPosition.x,-room.width/2+width/2+.08f,room.width/2-width/2-.08f),
                    Mathf.Clamp(height+.25f,.8f,room.height-.26f),Mathf.Max(art.localPosition.z-.62f,-room.length/2+.12f));
                var plate=LabelPlate(root,label,pos,width,labelHeight,Mathf.Min(.125f,(width-.16f)/Mathf.Max(1,lineLength)*1.35f));
                var plateAction=Action(plate.GetChild(0).gameObject,_=>{visitorArt=visitorArt==art.id?null:art.id;Tell(art.title);});
                plate.GetChild(0).GetComponent<Collider>().enabled=true;
                if(visited)DrawVisitedCheck(plate,width,labelHeight);
                if(inTour)
                {
                    string step="Étape "+(tour.artworkIds.IndexOf(art.id)+1);
                    if(current?.id==art.id)step+=" · Prochaine œuvre";
                    Text(plate,step,new Vector3(-width/2,-labelHeight/2-.075f,-.03f),.065f,current?.id==art.id?Gold:Color.white);
                }
                float stand=pos.y-labelHeight/2;
                Box(root,"Support du cartel",new Vector3(pos.x,stand/2,pos.z+.025f),new Vector3(.035f,stand,.045f),Navy,false);
                if(visitorArt==art.id)
                {
                    float detailWidth=Mathf.Min(1.6f,width+.2f);
                    var detail=Panel(root,"Description de l’œuvre",pos-Vector3.up*(labelHeight/2+.29f),detailWidth,.48f);
                    Text(detail,Wrap(art.description,36,5),new Vector3(-detailWidth/2+.06f,0,-.08f),.075f,Color.white);
                }
                if(inTour && !visited)DrawVisitZone(root,art,RouteColors[Mathf.Max(0,routeIndex)%RouteColors.Length]);
            }
            DrawTourTrails(root,room);
            DrawVisitorWelcome(root,room);
            Physics.SyncTransforms();
        }

        void DrawVisitedCheck(Transform plate,float width,float height)
        {
            var at=new Vector3(width/2-.025f,height/2+.025f,-.045f);
            var check=Root(plate,"Œuvre visitée",at);
            var color=new Color(.18f,.96f,.40f);
            Line(check,check.TransformPoint(new Vector3(-.07f,0,0)),check.TransformPoint(new Vector3(-.015f,-.065f,0)),.025f,color);
            Line(check,check.TransformPoint(new Vector3(-.015f,-.065f,0)),check.TransformPoint(new Vector3(.10f,.10f,0)),.025f,color);
        }

        void DrawVisitorWelcome(Transform root,Room room)
        {
            float width=Mathf.Min(2.7f,room.width-.3f);
            var header=Root(root,"Repères de la salle",new Vector3(0,1.76f,-room.length/2+.10f));
            header.localRotation=Quaternion.Euler(0,180,0);
            LabelPlate(header,Short(room.name,36),Vector3.zero,width,.24f,.095f);
            var active=guided?Api.GetTour(Api.Session.tourId):null;
            if(active!=null)
            {
                int seen=Visited(active.id).Count(id=>active.artworkIds.Contains(id));
                string progress=Api.IsComplete?"PARCOURS TERMINÉ":$"{seen}/{active.artworkIds.Distinct().Count()} œuvres visitées";
                string next=Api.CurrentArtwork==null?"Toutes les œuvres ont été visitées.":"À suivre : "+Api.CurrentArtwork.title;
                LabelPlate(header,Short(active.name,28)+" · "+progress+"\n"+Short(next,50),new Vector3(0,-.35f,0),width,.36f,.075f);
            }
            for(int i=0;i<data.tours.Count;i++)
            {
                var tour=data.tours[i];var color=RouteColors[i%RouteColors.Length];
                var sign=Root(root,"Cartel du parcours "+(i+1),new Vector3(-room.width/2+.11f,1.45f,room.length/2-.65f-i*.70f));
                sign.localRotation=Quaternion.Euler(0,-90,0);
                LabelPlate(sign,(i+1)+" · "+Short(tour.name,27),Vector3.zero,Mathf.Min(1.6f,room.length-.2f),.20f,.075f);
                Text(sign,Wrap(tour.description,38,2),new Vector3(-.74f,-.19f,-.025f),.065f,Color.white);
                Text(sign,RouteShapes[i%4]+" · Pointer les repères au sol",new Vector3(-.74f,-.35f,-.025f),.060f,color);
            }
            var pos=new Vector3(0,.02f,-room.length/2+.42f);
            var free=RouteMarker(root,pos,0,Quiet,.32f);
            free.name="Exploration libre";Action(free,_=>{guided=false;Tell("Exploration libre. Les parcours restent visibles au sol.");});
            var label=Text(root,"LIBRE",pos+Vector3.up*.02f,.065f,Color.white,TextAnchor.MiddleCenter);label.transform.localRotation=Quaternion.Euler(90,0,0);
        }

        void StartTour(string id)
        {
            var tour=Api.GetTour(id);
            if(tour==null || tour.artworkIds.Count==0){Reject("Ce parcours ne contient aucune œuvre.");return;}
            Api.StartTour(id);selectedTour=id;guided=true;SyncVisitedSteps();
            Tell("Parcours choisi : "+tour.name+". Les visites déjà effectuées sont conservées.");
        }

        static Transform Door(Transform parent,string label,Vector3 local,Action enter,bool highlighted,float width=.94f)
        {
            var door=Root(parent,label,local);var color=highlighted?Teal:Quiet;
            Box(door,"Montant gauche",new Vector3(-width*.5f,.95f,0),new Vector3(.085f,1.90f,.15f),color);
            Box(door,"Montant droit",new Vector3(width*.5f,.95f,0),new Vector3(.085f,1.90f,.15f),color);
            Box(door,"Linteau",new Vector3(0,1.9f,0),new Vector3(width+.07f,.13f,.15f),color);
            var target=Box(door,"Passage",new Vector3(0,.93f,-.026f),new Vector3(width-.07f,1.77f,.07f),highlighted?new Color(.13f,.48f,.51f):new Color(.20f,.30f,.43f));
            Action(target,_=>enter());
            Text(door,label,new Vector3(0,1.52f,-.072f),Mathf.Min(.08f,width*.11f),Cream,TextAnchor.MiddleCenter);
            Text(door,"ENTRER",new Vector3(0,.85f,-.072f),Mathf.Min(.075f,width*.105f),Cream,TextAnchor.MiddleCenter);
            return door;
        }
    }
}
