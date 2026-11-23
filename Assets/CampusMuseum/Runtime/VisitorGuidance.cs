using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        [Serializable] sealed class TourVisits { public string tourId;public List<string> artworks=new(); }
        [Serializable] sealed class VisitArchive { public List<TourVisits> tours=new(); }
        readonly Dictionary<string,HashSet<string>> visitHistory=new();
        readonly List<(Transform Door,float Width,Action Enter)> visitorDoors=new();
        float doorCooldown;
        bool suppressVisitSync;
        static readonly Color[] RouteColors={new(.10f,.78f,.72f),new(1f,.64f,.20f),new(.65f,.48f,.98f),new(.25f,.67f,1f),new(.96f,.42f,.62f)};
        static Mesh routeTriangle;
        static readonly string[] RouteShapes={"ronds","triangles","carrés","losanges"};
        string VisitHistoryPath=>Path.Combine(Application.persistentDataPath,"campus-visited-artworks.json");

        HashSet<string> Visited(string tourId)
        {
            if(!visitHistory.TryGetValue(tourId,out var set))visitHistory[tourId]=set=new HashSet<string>();
            return set;
        }

        bool HasVisited(string tourId,string artworkId)=>tourId!=null && Visited(tourId).Contains(artworkId);

        bool SaveVisitHistory()
        {
            try
            {
                var archive=new VisitArchive();
                foreach(var pair in visitHistory)archive.tours.Add(new TourVisits{tourId=pair.Key,artworks=pair.Value.ToList()});
                var temporary=VisitHistoryPath+".tmp";
                File.WriteAllText(temporary,JsonUtility.ToJson(archive));
                if(File.Exists(VisitHistoryPath))File.Replace(temporary,VisitHistoryPath,null);
                else File.Move(temporary,VisitHistoryPath);
                return true;
            }
            catch(Exception error){Reject("Historique de visite non enregistré : "+error.Message);return false;}
        }

        void LoadVisitHistory()
        {
            visitHistory.Clear();
            try
            {
                if(File.Exists(VisitHistoryPath))
                {
                    var archive=JsonUtility.FromJson<VisitArchive>(File.ReadAllText(VisitHistoryPath));
                    foreach(var entry in archive?.tours ?? new List<TourVisits>())
                    {
                        var tour=Api.GetTour(entry.tourId);
                        if(tour!=null)visitHistory[entry.tourId]=new HashSet<string>(entry.artworks.Where(tour.artworkIds.Contains));
                    }
                }
                if(Api.HasSelectedTour)
                {
                    var tour=Api.GetTour(Api.Session.tourId);
                    foreach(var id in tour.artworkIds.Take(Api.Session.index))Visited(tour.id).Add(id);
                    SyncVisitedSteps();
                }
            }
            catch(Exception error){Reject("Historique de visite illisible : "+error.Message);}
        }

        void SyncVisitedSteps()
        {
            if(suppressVisitSync || !Api.HasSelectedTour)return;
            suppressVisitSync=true;
            try {while(Api.CurrentArtwork is ArtworkInstance art && HasVisited(Api.Session.tourId,art.id))Api.Advance();}
            finally{suppressVisitSync=false;}
        }

        void UpdateVisitorInteraction()
        {
            if(Time.unscaledTime>=doorCooldown)
            {
                var head=player.Eye.position;
                foreach(var target in visitorDoors)
                {
                    if(!target.Door)continue;
                    var local=target.Door.InverseTransformPoint(head);
                    if(Mathf.Abs(local.x)<target.Width*.5f+.07f && Mathf.Abs(local.z)<.52f)
                    {doorCooldown=Time.unscaledTime+1.1f;target.Enter();return;}
                }
            }
            if(!guided || !Api.HasSelectedTour)return;
            var tour=Api.GetTour(Api.Session.tourId);
            var room=Api.GetRoom(visitorRoom);
            if(tour==null || room==null)return;
            var foot=room.rotation==Quaternion.identity?player.Eye.position-room.position
                :Quaternion.Inverse(room.rotation)*(player.Eye.position-room.position);
            bool changed=false;
            foreach(var id in tour.artworkIds.Distinct())
            {
                var art=Api.GetArtwork(id);
                if(art==null || art.roomId!=room.id || HasVisited(tour.id,id))continue;
                if(Vector2.Distance(new Vector2(foot.x,foot.z),new Vector2(art.localPosition.x,art.localPosition.z))>1.20f)continue;
                Visited(tour.id).Add(id);changed=true;
            }
            if(!changed)return;
            SyncVisitedSteps();
            Tell(Api.IsComplete?"Parcours terminé. Tu peux continuer à explorer.":"Œuvre visitée. La coche verte confirme ton passage.");
        }

        List<string> RoomPath(string from,string to)
        {
            var queue=new Queue<string>();var previous=new Dictionary<string,string>{{from,null}};
            queue.Enqueue(from);
            while(queue.Count>0)
            {
                var id=queue.Dequeue();if(id==to)break;
                foreach(var next in Api.GetRoom(id)?.connections ?? new List<string>())
                    if(!previous.ContainsKey(next)){previous[next]=id;queue.Enqueue(next);}
            }
            if(!previous.ContainsKey(to))return new List<string>();
            var path=new List<string>();for(var at=to;at!=null;at=previous[at])path.Add(at);path.Reverse();return path;
        }

        int DoorWall(Room room,string destination)
        {
            var other=Api.GetRoom(destination);
            if(other==null)return 0;
            var delta=Quaternion.Inverse(room.rotation)*(other.position-room.position);
            if(Mathf.Abs(delta.x)/room.width>Mathf.Abs(delta.z)/room.length)return delta.x>0?1:3;
            return delta.z>0?0:2;
        }

        float VisitorDoorYaw(Room room,string destination)=>DoorWall(room,destination) switch
        {1=>90f,2=>180f,3=>-90f,_=>0f};

        Vector3 VisitorDoorPosition(Room room,string destination)
        {
            int wall=DoorWall(room,destination);
            var same=room.connections.Where(id=>DoorWall(room,id)==wall).ToList();
            int index=same.IndexOf(destination);
            bool side=wall==1||wall==3;
            float length=side?room.length:room.width;
            float along=Mathf.Lerp(-length/2+.55f,length/2-.55f,(index+1f)/(same.Count+1));
            return side?new Vector3((wall==1?1:-1)*(room.width/2-.10f),0,along)
                :new Vector3(along,0,(wall==0?1:-1)*(room.length/2-.10f));
        }

        Vector3 VisitorDoorApproach(Room room,string destination)
        {
            var point=VisitorDoorPosition(room,destination);
            point+=Quaternion.Euler(0,VisitorDoorYaw(room,destination),0)*Vector3.back*.65f;
            return point;
        }

        void DrawTourTrails(Transform root,Room currentRoom)
        {
            for(int index=0;index<data.tours.Count;index++)
            {
                var tour=data.tours[index];if(tour.artworkIds.Count==0)continue;
                bool active=guided && Api.Session.tourId==tour.id;
                var color=active?RouteColors[index%RouteColors.Length]:Color.Lerp(RouteColors[index%RouteColors.Length],new Color(.7f,.73f,.74f),.72f);
                var firstRoom=data.rooms.FirstOrDefault();if(firstRoom==null)continue;
                var atRoom=firstRoom.id;var at=new Vector3(0,0,-firstRoom.length/2+.8f);
                foreach(var id in tour.artworkIds)
                {
                    var art=Api.GetArtwork(id);if(art==null)continue;
                    var path=RoomPath(atRoom,art.roomId);
                    if(path.Count==0)continue;
                    for(int p=0;p<path.Count-1;p++)
                    {
                        var room=Api.GetRoom(path[p]);
                        if(room.id==currentRoom.id)DrawTrailSegment(root,at,VisitorDoorApproach(room,path[p+1]),tour.id,index,color,active);
                        var next=Api.GetRoom(path[p+1]);at=VisitorDoorApproach(next,room.id);
                    }
                    if(art.roomId==currentRoom.id)DrawTrailSegment(root,at,new Vector3(art.localPosition.x,0,art.localPosition.z-.95f),tour.id,index,color,active);
                    atRoom=art.roomId;at=new Vector3(art.localPosition.x,0,art.localPosition.z-.95f);
                }
            }
        }

        void DrawTrailSegment(Transform root,Vector3 from,Vector3 to,string tourId,int style,Color color,bool active)
        {
            var delta=to-from;delta.y=0;float distance=delta.magnitude;if(distance<.08f)return;
            var side=Vector3.Cross(Vector3.up,delta.normalized)*(((style%5)-2)*.13f);
            int count=Mathf.Clamp(Mathf.CeilToInt(distance/.33f),1,100);
            for(int i=0;i<=count;i++)
            {
                var position=Vector3.Lerp(from,to,i/(float)count)+side;position.y=.018f+style*.0015f;
                var marker=RouteMarker(root,position,style,color,active?.12f:.085f);
                Action(marker,_=>StartTour(tourId));
                if(i%6==0)
                {
                    var label=Text(root,(style+1).ToString(),position+Vector3.up*.015f,.065f,color,TextAnchor.MiddleCenter);
                    label.transform.localRotation=Quaternion.Euler(90,0,0);
                }
            }
        }

        GameObject RouteMarker(Transform parent,Vector3 position,int style,Color color,float size)
        {
            GameObject marker;
            if(style%4==0)
            {
                marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Parcours "+(style+1);
                marker.transform.SetParent(parent,false);marker.transform.localPosition=position;
                marker.transform.localScale=new Vector3(size,.008f,size);
            }
            else
            {
                marker=Box(parent,"Parcours "+(style+1),position,new Vector3(size,.016f,size),color);
                if(style%4==1)
                {
                    if(!routeTriangle)
                    {
                        routeTriangle=new Mesh{name="Repère triangulaire"};
                        routeTriangle.vertices=new[]{new Vector3(-.5f,.5f,-.4f),new Vector3(0,.5f,.6f),new Vector3(.5f,.5f,-.4f)};
                        routeTriangle.triangles=new[]{0,1,2,2,1,0};routeTriangle.RecalculateNormals();
                    }
                    marker.GetComponent<MeshFilter>().sharedMesh=routeTriangle;
                }
                if(style%4==3)marker.transform.localRotation=Quaternion.Euler(0,45,0);
            }
            marker.GetComponent<Renderer>().sharedMaterial=MakeMaterial(color,true);
            return marker;
        }

        void DrawVisitZone(Transform root,ArtworkInstance art,Color color)
        {
            var center=new Vector3(art.localPosition.x,.026f,art.localPosition.z);
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                Line(root,root.TransformPoint(center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.20f),
                    root.TransformPoint(center+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*1.20f),.018f,color);
            }
        }
    }
}
