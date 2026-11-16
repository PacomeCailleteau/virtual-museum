using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]

namespace CampusMuseum
{
    public sealed partial class MuseumApp : MonoBehaviour
    {
        static readonly Vector3 MapCenter = new(-.85f,.94f,.05f);
        static readonly Vector3 EditCenter = new(3.15f,.94f,.1f);
        static readonly Vector3 Arrival = new(-.8f,0,-2.15f);
        const float Scale = .092f;
        const float MapWidth = 4.3f;
        const float MapLength = 2.8f;

        MuseumBackend backend;
        MuseumApi Api => backend.Api;
        MuseumPlayer player;
        Transform stage;
        Transform artEditor;
        MuseumEdit draft;
        Room carriedRoom;
        Room roomCandidate;
        Transform carriedView;
        MuseumPlayer.Hand carrier;
        enum RoomDrop { None, Map, Edit, Return, Occupied }
        RoomDrop roomDrop;
        readonly SurfaceDrag placement = new();
        Renderer placementMarker;
        GameObject heldArtwork;
        MuseumPlayer.Hand artHand;
        float editScale;
        string returningRoom;
        string selectedRoom;
        string selectedArt;
        string selectedTour;
        string visitorRoom;
        string visitorArt;
        bool guided;
        bool visiting;
        bool drawing;
        bool linking;
        bool confirmDelete;
        Vector3? corner;
        Transform preview;
        TextMesh previewDimensions;
        string status = "Choisis une salle ou trace une nouvelle galerie.";
        bool dirty;
        Museum data;
        Camera view;

        public MuseumApi Model => Api;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var backend = FindFirstObjectByType<MuseumBackend>();
            if (!backend || FindFirstObjectByType<MuseumApp>()) return;
            backend.gameObject.AddComponent<MuseumApp>();
        }

        void Start()
        {
            backend = GetComponent<MuseumBackend>();
            view = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();
            if (!view)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                view = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            player = new GameObject("Commandes Virtual Museum").AddComponent<MuseumPlayer>();
            var originalFloor = GameObject.Find("Plane");
            if (originalFloor) originalFloor.SetActive(false);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f,.51f,.56f);
            player.Initialize(view);
            player.CanMove = () => true;
            player.CanStand = CanStand;
            player.Place(Arrival, 0);
            Api.Changed += OnModelChanged;
            Api.SessionChanged += QueueDraw;
            data = Api.Data;
            selectedRoom = data.rooms.FirstOrDefault()?.id;
            selectedTour = data.tours.FirstOrDefault()?.id;
            Draw();
        }

        void OnDestroy()
        {
            if (!backend) return;
            Api.Changed -= OnModelChanged;
            Api.SessionChanged -= QueueDraw;
        }

        void OnModelChanged(MuseumChange change) => QueueDraw();
        void QueueDraw() => dirty = true;
        void Tell(string message) { status = message; dirty = true; }

        void Update()
        {
            if (!player) return;
            if (dirty && !carriedView && !heldArtwork) Draw();
            player.Tick();
            if (carriedView) UpdateRoomDrag(Time.unscaledDeltaTime);
            if (heldArtwork) UpdateArtworkDrag(Time.unscaledDeltaTime);
            if (drawing && corner.HasValue && preview)
            {
                var hand = player.Right;
                if (HitPlane(hand.Ray,MapCenter.y,out var end) && InsideBench(end,MapCenter,MapWidth,MapLength))
                {
                    var a = corner.Value;
                    preview.position = (a + end) / 2 + Vector3.up * .015f;
                    preview.localScale = new Vector3(Mathf.Abs(end.x-a.x), .025f, Mathf.Abs(end.z-a.z));
                    var width = preview.localScale.x / Scale;
                    var length = preview.localScale.z / Scale;
                    previewDimensions.text = $"{width * length:0.#} m²   ·   {3f * width * length:0.#} m³";
                }
            }
        }

        void Draw()
        {
            dirty = false;
            data = Api.Data;
            if (stage)
            {
                stage.gameObject.SetActive(false);
                Destroy(stage.gameObject);
            }
            stage = Root(transform, visiting ? "Musée à taille humaine" : "Atelier", Vector3.zero);
            if (visiting)
            {
                DrawVisitor();
                return;
            }
            if (data.rooms.All(r => r.id != selectedRoom)) selectedRoom = data.rooms.FirstOrDefault()?.id;
            if (data.tours.All(t => t.id != selectedTour)) selectedTour = data.tours.FirstOrDefault()?.id;
            DrawWorkshop();
        }

        static bool HitPlane(Ray ray, float y, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.up * y);
            bool hit = plane.Raycast(ray,out var d) && d < 12;
            point = ray.GetPoint(d);
            return hit;
        }

        static bool InsideBench(Vector3 point, Vector3 center, float width, float length) =>
            Mathf.Abs(point.x-center.x) < width/2 && Mathf.Abs(point.z-center.z) < length/2;

        static Vector3 OnMap(Vector3 point) => MapCenter + point * Scale;
        static Vector3 InMuseum(Vector3 point)
        {
            var local = (point - MapCenter) / Scale;
            return new Vector3(local.x,0,local.z);
        }

        void DrawWorkshop()
        {
            Box(stage,"Parquet",new Vector3(1,-.072f,0),new Vector3(11,.12f,9), new Color(.60f,.57f,.50f));
            Box(stage,"Fond de l’atelier",new Vector3(1,1.8f,3.55f),new Vector3(11,3.6f,.15f),Navy,false);
            Box(stage,"Mur latéral",new Vector3(-4.5f,1.5f,.3f),new Vector3(.14f,3.1f,6.5f),new Color(.20f,.31f,.38f),false);
            Bench(MapCenter, MapWidth, MapLength);
            var map = Box(stage,"Surface du musée", MapCenter - Vector3.up*.033f,
                new Vector3(MapWidth,.06f,MapLength), new Color(.74f,.65f,.50f));
            Action(map,h => Plot(h.Ray));
            Bench(EditCenter, 2.45f, 1.9f);
            Box(stage,"Surface d’édition",EditCenter - Vector3.up*.034f,new Vector3(2.45f,.06f,1.9f),new Color(.31f,.44f,.45f));
            var plaqueTilt = Quaternion.Euler(35,0,0);
            LabelPlate(stage,"MAQUETTE",new Vector3(-.85f,1.015f,-1.16f),1.05f,.17f,.11f).localRotation = plaqueTilt;
            LabelPlate(stage,"TABLE D’ÉDITION",new Vector3(3.15f,1.015f,-.59f),1.48f,.17f,.11f).localRotation = plaqueTilt;
            DrawMap();
            DrawEditor();
            DrawBoard();
            Door(stage,"Visiter le musée",new Vector3(4.63f,0,2.73f),EnterMuseum,false);
            if (drawing && corner.HasValue)
            {
                preview = Box(stage,"Emprise de la salle",corner.Value + Vector3.up*.016f,new Vector3(.05f,.025f,.05f),Teal,false).transform;
                previewDimensions = Text(stage,"",new Vector3(-.85f,1.1f,-1.37f),.09f,Navy,TextAnchor.MiddleCenter);
            }
            Physics.SyncTransforms();
        }

        void Bench(Vector3 center, float width,float length)
        {
            Box(stage,"Plateau",center-Vector3.up*.115f,new Vector3(width,.12f,length),Sand);
            foreach (var x in new[] {-width/2+.12f,width/2-.12f})
                foreach (var z in new[] {-length/2+.12f,length/2-.12f})
                    Box(stage,"Pied",new Vector3(center.x+x,.40f,center.z+z),new Vector3(.095f,.80f,.095f),Navy);
        }

        void DrawMap()
        {
            var tour = data.tours.FirstOrDefault(t => t.id == selectedTour);
            var roomViews = new Dictionary<string, Transform>();
            foreach (var room in data.rooms)
            {
                if (room.id == draft?.RoomId || room.id == returningRoom)
                {
                    var marker = Box(stage,"Place de retour",OnMap(room.position)+Vector3.up*.015f,
                        new Vector3(room.width*Scale,.02f, room.length*Scale),Teal,false);
                    marker.transform.rotation = room.rotation;
                    continue;
                }
                var root = Shell(stage,room,OnMap(room.position),room.rotation,Scale,true,room.id == selectedRoom ? Teal : Cream);
                roomViews.Add(room.id,root);
                var pick = root.gameObject.AddComponent<WorldAction>();
                pick.Direct=true;
                pick.Press = h => SelectRoom(room,h,root);
                pick.Release = DropRoom;
                Text(root,Wrap(room.name,19,1),new Vector3(0,room.height*.12f,-room.length/2-.065f),
                    Mathf.Min(.37f,room.width/18f),Color.white,TextAnchor.MiddleCenter);
                foreach (var art in data.artworks.Where(a => a.roomId == room.id))
                {
                    var visual = Artwork(root,Api.Catalog,art);
                    Action(visual,h =>
                    {
                        if (drawing) Plot(h.Ray);
                        else if (linking) SelectRoom(room,h,root);
                        else { selectedRoom=room.id; selectedArt=art.id; activeField=TextField.None; Tell(art.title); }
                    });
                    if (tour != null)
                    {
                        var steps=Enumerable.Range(0,tour.artworkIds.Count).Where(i=>tour.artworkIds[i]==art.id).Select(i=>(i+1).ToString()).ToArray();
                        if (steps.Length > 0)
                            Text(root,string.Join(" / ",steps),art.localPosition+new Vector3(0,1.45f,-.32f),.52f,Gold,TextAnchor.MiddleCenter);
                    }
                }
            }
            foreach (var room in data.rooms)
                foreach (var id in room.connections)
                    if (string.CompareOrdinal(room.id,id)<0 && roomViews.TryGetValue(id,out var other) && roomViews.TryGetValue(room.id,out var own))
                        Line(stage,own.position+Vector3.up*.27f,other.position+Vector3.up*.27f,.011f,Quiet);
            if (tour != null)
            {
                Vector3? last=null;
                foreach (var id in tour.artworkIds)
                {
                    var art = data.artworks.FirstOrDefault(a => a.id == id);
                    if (art == null || !roomViews.TryGetValue(art.roomId,out var room)) { last=null; continue; }
                    var point=room.TransformPoint(art.localPosition)+Vector3.up*.19f;
                    if (last.HasValue) Line(stage,last.Value,point,.008f,Gold);
                    last=point;
                }
            }
        }

        void SelectRoom(Room room, MuseumPlayer.Hand hand, Transform visual)
        {
            if (drawing) { Plot(hand.Ray); return; }
            if (linking)
            {
                if (selectedRoom == null || selectedRoom == room.id)
                {
                    Tell("Choisis une autre salle sur la maquette.");
                    return;
                }
                var from = Api.GetRoom(selectedRoom);
                bool connected = from.connections.Contains(room.id);
                if (connected) Api.DisconnectRooms(from.id,room.id);
                else Api.ConnectRooms(from.id,room.id);
                linking=false;
                Tell(connected ? "Connexion retirée." : "Salles reliées dans les deux sens.");
                return;
            }
            if (draft != null || returningRoom != null || carriedView || heldArtwork)
            {
                Tell("Termine le travail à la table d’édition.");
                return;
            }
            selectedRoom=room.id;
            selectedArt=null;
            activeField=TextField.None;
            confirmDelete=false;
            BeginRoomDrag(room, hand, visual);
        }

        void BeginRoomDrag(Room room, MuseumPlayer.Hand hand, Transform visual)
        {
            carriedRoom = room;
            roomCandidate = Api.GetRoom(room.id);
            carrier = hand;
            carriedView = visual;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
            placement.Begin(hand.Ray, visual.position, true, hand.NearSelection);
            placementMarker = Box(stage, "Repère de dépôt", visual.position,
                new Vector3(room.width * Scale + .035f, .012f, room.length * Scale + .035f), Teal, false).GetComponent<Renderer>();
            UpdateRoomDrag(0);
        }

        void UpdateRoomDrag(float deltaTime)
        {
            placement.Update(carrier.Ray, deltaTime);
            var point = placement.Position;
            carriedView.position = point + Vector3.up * .075f;
            roomDrop = RoomDrop.None;
            if (placement.CanPlace && InsideBench(point, MapCenter, MapWidth, MapLength))
            {
                if (returningRoom == carriedRoom.id)
                {
                    if ((point - OnMap(carriedRoom.position)).sqrMagnitude <= .22f * .22f)
                        roomDrop = RoomDrop.Return;
                }
                else
                {
                    roomCandidate.position = InMuseum(point);
                    roomDrop = FitsTable(roomCandidate) && !MuseumLayout.Overlaps(roomCandidate, data.rooms)
                        ? RoomDrop.Map : RoomDrop.Occupied;
                }
            }
            else if (placement.CanPlace && returningRoom == null && InsideBench(point, EditCenter, 2.45f, 1.9f))
            {
                roomDrop = RoomDrop.Edit;
            }

            var markerPoint = roomDrop == RoomDrop.Return ? OnMap(carriedRoom.position) : point;
            placementMarker.transform.SetPositionAndRotation(markerPoint + Vector3.up * .012f, carriedRoom.rotation);
            placementMarker.enabled = roomDrop != RoomDrop.None;
            placementMarker.sharedMaterial = MakeMaterial(roomDrop == RoomDrop.Occupied ? Red : Teal, true);
        }

        void DropRoom(MuseumPlayer.Hand hand)
        {
            if (carrier != hand || !carriedView) return;
            var room = carriedRoom;
            var position = roomCandidate.position;
            var target = hand.ReleaseCancelled ? RoomDrop.None : roomDrop;
            carriedRoom = null;
            roomCandidate = null;
            carriedView = null;
            carrier = null;
            placementMarker.enabled = false;
            if (target == RoomDrop.Edit)
            {
                draft = new MuseumEdit(Api,room.id);
                Tell("Salle agrandie. Place les œuvres puis actionne l’interrupteur.");
            }
            else if (target == RoomDrop.Return)
            {
                returningRoom = null;
                Tell("Salle remise sur la maquette.");
            }
            else if (target == RoomDrop.Map && Vector3.Distance(position, room.position) * Scale > .012f)
            {
                Api.SetRoomTransform(room.id, position, room.rotation);
                Tell(room.name + " déplacée.");
            }
            else if (target == RoomDrop.Occupied) Tell("Emplacement occupé ou en dehors du plateau.");
            else Tell(returningRoom != null ? "Repose la salle sur le repère turquoise." : room.name + " sélectionnée.");
        }

        bool FitsTable(Room room)
        {
            foreach (var x in new[]{-room.width/2,room.width/2})
                foreach (var z in new[]{-room.length/2,room.length/2})
                    if (!InsideBench(OnMap(room.position+room.rotation*new Vector3(x,0,z)),MapCenter,MapWidth-.12f,MapLength-.12f))
                        return false;
            return true;
        }

        void Plot(Ray ray)
        {
            if (!drawing || !HitPlane(ray, MapCenter.y, out var point)) return;
            if (!InsideBench(point,MapCenter,MapWidth,MapLength)) return;
            if (!corner.HasValue)
            {
                corner=point;
                Tell("Pointe le coin opposé de la salle.");
                return;
            }
            var first=corner.Value;
            var size=(point-first)/Scale;
            var room=new Room { position=InMuseum((point+first)/2f), width=Mathf.Abs(size.x), length=Mathf.Abs(size.z), height=3 };
            if (room.width < 2 || room.length < 2 || !FitsTable(room) || MuseumLayout.Overlaps(room,data.rooms))
            {
                Tell("Prévois au moins 2 m de côté et un emplacement libre.");
                return;
            }
            var created=Api.CreateRoom("Galerie "+(data.rooms.Count+1),room.position,room.width,room.length,room.height);
            selectedRoom=created.id;
            drawing=false;
            corner=null;
            Tell(created.name + " créée.");
        }

        void TurnRoom(float angle)
        {
            var room=Api.GetRoom(selectedRoom);
            if (room == null || draft != null || returningRoom != null) return;
            room.rotation=Quaternion.AngleAxis(angle,Vector3.up)*room.rotation;
            if (!FitsTable(room) || MuseumLayout.Overlaps(room,data.rooms))
            {
                Tell("La rotation ferait chevaucher une autre salle.");
                return;
            }
            Api.SetRoomTransform(room.id,room.position,room.rotation);
            Tell("Orientation modifiée.");
        }

        void Resize(int axis,float delta)
        {
            var target = draft?.Api ?? Api;
            var room = target.GetRoom(draft?.RoomId ?? selectedRoom);
            if(room == null || returningRoom != null) return;
            if(axis==0) room.width=Mathf.Clamp(room.width+delta,2f,18f);
            if(axis==1) room.length=Mathf.Clamp(room.length+delta,2f,18f);
            if(axis==2) room.height=Mathf.Clamp(room.height+delta,2.2f,7f);
            if(!FitsTable(room) || MuseumLayout.Overlaps(room,data.rooms))
            {
                Tell("Ces dimensions recouvriraient une autre salle ou dépasseraient du plateau.");
                return;
            }
            var outside=target.GetArtworksInRoom(room.id).FirstOrDefault(a=>!MuseumLayout.Contains(room,a.localPosition));
            if(outside!=null)
            {
                Tell("Déplace d’abord « "+outside.title+" » vers l’intérieur.");
                return;
            }
            if (draft != null) draft.Apply(a=>a.ResizeRoom(room.id,room.width,room.length,room.height));
            else Api.ResizeRoom(room.id,room.width,room.length,room.height);
            Tell("Dimensions mises à jour.");
        }

        void DeleteRoom()
        {
            var room=Api.GetRoom(selectedRoom);
            if(room==null || draft!=null || returningRoom!=null) return;
            if(!confirmDelete)
            {
                var arts=Api.GetArtworksInRoom(room.id);
                var stops=data.tours.Sum(t=>t.artworkIds.Count(id=>arts.Any(a=>a.id==id)));
                Tell($"Retirer {room.name}, {arts.Count} œuvre(s), {room.connections.Count} lien(s) et {stops} étape(s) ?");
                confirmDelete=true;
                return;
            }
            Api.DeleteRoom(room.id);
            selectedRoom=selectedArt=null;
            confirmDelete=false;
            Tell("Salle supprimée.");
        }
    }
}
