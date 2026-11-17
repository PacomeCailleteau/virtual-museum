using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualMuseum;

namespace CampusMuseum
{
    public static class WorldView
    {
        public static readonly Color Navy = new(.085f, .145f, .215f);
        public static readonly Color Teal = new(.10f, .74f, .68f);
        public static readonly Color Quiet = new(.38f, .57f, .66f);
        public static readonly Color Cream = new(.88f, .86f, .78f);
        public static readonly Color Sand = new(.49f, .34f, .23f);
        public static readonly Color Gold = new(.96f, .69f, .30f);
        public static readonly Color Red = new(.68f, .26f, .28f);
        static Font font;
        static Material worldText;
        static Material lit;
        static Material unlit;
        static readonly Dictionary<(Color, bool), Material> materials = new();

        public static Material MakeMaterial(Color color, bool flat = false)
        {
            if (materials.TryGetValue((color, flat), out var cached) && cached) return cached;
            if (!lit) lit = Resources.Load<Material>("CampusMuseum/Surface");
            if (!unlit) unlit = Resources.Load<Material>("CampusMuseum/Signal");
            var material = new Material(flat ? unlit : lit) { color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
            materials[(color, flat)] = material;
            return material;
        }

        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Color color, bool collision = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
            box.GetComponent<Collider>().enabled = collision;
            return box;
        }

        public static Transform Root(Transform parent, string name, Vector3 position)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = position;
            return g;
        }

        public static TextMesh Text(Transform parent, string label, Vector3 position, float height, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var g = Root(parent, label, position);
            var mesh = g.gameObject.AddComponent<TextMesh>();
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            UseWorldText(mesh);
            mesh.text = label;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            mesh.fontSize = 70;
            mesh.characterSize = height * .148f;
            mesh.color = color;
            return mesh;
        }

        public static void UseWorldText(TextMesh mesh)
        {
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mesh.font && mesh.font != font) return;
            if (!worldText)
            {
                worldText = new Material(Resources.Load<Shader>("CampusMuseum/WorldText"));
                worldText.mainTexture = font.material.mainTexture;
                Font.textureRebuilt -= UpdateFontTexture;
                Font.textureRebuilt += UpdateFontTexture;
            }
            mesh.font = font;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = worldText;
        }

        static void UpdateFontTexture(Font changed)
        {
            if (changed == font && worldText) worldText.mainTexture = font.material.mainTexture;
        }

        public static Transform LabelPlate(Transform parent, string title, Vector3 position,
            float width, float height, float textHeight)
        {
            var plate = Root(parent, "Signalétique", position);
            var backing = Box(plate, "Fond du cartel", Vector3.zero, new Vector3(width,height,.028f), Navy, false);
            backing.GetComponent<Renderer>().sharedMaterial = MakeMaterial(Navy, true);
            Text(plate,title,new Vector3(0,0,-.02f),textHeight,Color.white,TextAnchor.MiddleCenter);
            return plate;
        }

        public static WorldAction Action(GameObject box, Action<MuseumPlayer.Hand> press, Action<MuseumPlayer.Hand> release = null)
        {
            var action = box.AddComponent<WorldAction>();
            action.Press = press;
            action.Release = release;
            var renderer = box.GetComponent<Renderer>();
            if (renderer) action.SetSurface(renderer);
            return action;
        }

        public static void Key(Transform parent, string label, float x, float y, float width, float height,
            Action invoke, Color? color = null)
        {
            var key = Box(parent, label, new Vector3(x,y,-.05f), new Vector3(width,height,.045f), color ?? Quiet);
            Text(parent, label, new Vector3(x,y,-.077f),
                Mathf.Min(height * .35f, width / Mathf.Max(label.Length * .54f, 1f)), Color.white, TextAnchor.MiddleCenter);
            Action(key, _ => invoke());
        }

        public static Transform Shell(Transform parent, Room room, Vector3 center, Quaternion rotation,
            float scale, bool miniature, Color floor)
        {
            var root = Root(parent, room.name, center);
            root.localRotation = rotation;
            root.localScale = Vector3.one * scale;
            float height = miniature ? room.height * .24f : room.height;
            float back = miniature ? room.height * .52f : room.height;
            var tint = miniature ? Color.Lerp(Quiet,Navy,.25f) : Navy;
            Box(root, "Sol", new Vector3(0,-.045f,0), new Vector3(room.width,.09f,room.length), floor);
            Box(root, "Mur gauche", new Vector3(-room.width/2,height/2,0), new Vector3(.11f,height,room.length), tint);
            Box(root, "Mur droit", new Vector3(room.width/2,height/2,0), new Vector3(.11f,height,room.length), tint);
            Box(root, "Mur fond", new Vector3(0,back/2,room.length/2), new Vector3(room.width,back,.11f), tint);
            Box(root, "Mur avant", new Vector3(0,height/2,-room.length/2), new Vector3(room.width,height,.11f), tint);
            return root;
        }

        public static GameObject Artwork(Transform parent, ArtworkCatalog catalog, ArtworkInstance instance)
        {
            var definition = catalog.Find(instance.definitionId);
            var root = Root(parent, instance.title, instance.localPosition);
            root.localRotation = instance.localRotation;
            root.localScale = instance.localScale;
            if (definition != null && definition.prefab)
            {
                var visual = UnityEngine.Object.Instantiate(definition.prefab, root, false);
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var b = renderer.localBounds;
                    foreach (var x in new[] {b.min.x,b.max.x})
                        foreach (var y in new[] {b.min.y,b.max.y})
                            foreach (var z in new[] {b.min.z,b.max.z})
                                bounds.Encapsulate(root.InverseTransformPoint(renderer.transform.TransformPoint(new Vector3(x,y,z))));
                }
                foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var box = root.gameObject.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size, Vector3.one * .35f);
            }
            else
            {
                Box(root, "Œuvre", Vector3.zero, Vector3.one * .45f, Gold);
            }
            return root.gameObject;
        }

        public static void Line(Transform parent, Vector3 from, Vector3 to, float width, Color color)
        {
            var g = Root(parent, "Lien", Vector3.zero);
            var line = g.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = MakeMaterial(color, true);
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }

        public static string Wrap(string value, int width, int limit = 3)
        {
            if (string.IsNullOrWhiteSpace(value)) return " ";
            var rest = value.Replace('\n',' ').Trim();
            var lines = new System.Collections.Generic.List<string>();
            while (rest.Length > width && lines.Count < limit - 1)
            {
                var split = rest.LastIndexOf(' ', width);
                if (split < 1) split = width;
                lines.Add(rest[..split]);
                rest = rest[split..].TrimStart();
            }
            lines.Add(rest.Length > width ? rest[..Mathf.Max(width - 1, 1)] + "…" : rest);
            return string.Join("\n", lines);
        }
    }
}
