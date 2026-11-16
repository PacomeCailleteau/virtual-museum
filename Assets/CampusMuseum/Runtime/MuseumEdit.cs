using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualMuseum;

namespace CampusMuseum
{
    public sealed class MuseumEdit
    {
        readonly Stack<(Museum Data, VisitorSession Session)> undo = new();
        readonly Stack<(Museum Data, VisitorSession Session)> redo = new();
        public string RoomId { get; }
        public MuseumApi Api { get; }
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        public MuseumEdit(MuseumApi source, string roomId)
        {
            RoomId = roomId;
            Api = new MuseumApi(source.Catalog);
            Api.Replace(source.Data, source.Session);
        }

        public void Apply(System.Action<MuseumApi> edit)
        {
            var previous = (Api.Data, Api.Session);
            edit(Api);
            undo.Push(previous);
            redo.Clear();
        }

        public void Undo() => Restore(undo, redo);
        public void Redo() => Restore(redo, undo);
        public void Commit(MuseumApi target) => target.Replace(Api.Data, Api.Session);

        void Restore(Stack<(Museum Data, VisitorSession Session)> from, Stack<(Museum Data, VisitorSession Session)> to)
        {
            if (from.Count == 0) return;
            to.Push((Api.Data, Api.Session));
            var snapshot = from.Pop();
            Api.Replace(snapshot.Data, snapshot.Session);
        }
    }

    public static class MuseumLayout
    {
        public static bool Overlaps(Room room, IEnumerable<Room> others)
        {
            foreach (var other in others)
            {
                if (room.id == other.id) continue;
                if (Intersects(room, other)) return true;
            }
            return false;
        }

        static bool Intersects(Room a, Room b)
        {
            var axes = new[] { a.rotation * Vector3.right, a.rotation * Vector3.forward,
                b.rotation * Vector3.right, b.rotation * Vector3.forward };
            var delta = b.position - a.position;
            foreach (var axis in axes)
            {
                if (Mathf.Abs(Vector3.Dot(delta, axis)) >= Radius(a, axis) + Radius(b, axis) - 0.015f)
                    return false;
            }
            return true;
        }

        static float Radius(Room room, Vector3 axis) =>
            (Mathf.Abs(Vector3.Dot(room.rotation * Vector3.right, axis)) * room.width +
             Mathf.Abs(Vector3.Dot(room.rotation * Vector3.forward, axis)) * room.length) * 0.5f;

        public static Vector3 ClampInside(Room room, Vector3 point) => new(
            Mathf.Clamp(point.x, -room.width / 2 + .15f, room.width / 2 - .15f),
            Mathf.Clamp(point.y, .1f, room.height - .1f),
            Mathf.Clamp(point.z, -room.length / 2 + .15f, room.length / 2 - .15f));

        public static bool Contains(Room room, Vector3 point) =>
            (ClampInside(room, point) - point).sqrMagnitude < .000001f;

        public static string NextRoom(Museum museum, string start, string goal)
        {
            var rooms = museum.rooms.ToDictionary(room => room.id);
            if (start == goal || !rooms.ContainsKey(start) || !rooms.ContainsKey(goal)) return null;

            var queue = new Queue<string>();
            var first = new Dictionary<string, string> { [start] = null };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var neighbor in rooms[id].connections)
                {
                    if (first.ContainsKey(neighbor) || !rooms.ContainsKey(neighbor)) continue;
                    first[neighbor] = first[id] ?? neighbor;
                    if (neighbor == goal) return first[neighbor];
                    queue.Enqueue(neighbor);
                }
            }
            return null;
        }
    }
}
