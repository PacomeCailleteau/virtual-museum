using System.Collections.Generic;
using UnityEngine;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed class StretchArrow : MonoBehaviour
    {
        Transform shaft;
        Transform head;
        Color tint;
        static Mesh cone;

        public static StretchArrow Create(Transform parent,string name,Vector3 origin,Vector3 direction,Color color)
        {
            var root=Root(parent,name,Vector3.zero);
            root.SetPositionAndRotation(origin,Quaternion.LookRotation(direction));
            var arrow=root.gameObject.AddComponent<StretchArrow>();arrow.tint=color;
            arrow.shaft=Box(root,"Tige de la flèche",Vector3.zero,Vector3.one,color,false).transform;
            var tip=new GameObject("Pointe de la flèche");arrow.head=tip.transform;arrow.head.SetParent(root,false);
            var filter=tip.AddComponent<MeshFilter>();filter.sharedMesh=Cone();
            tip.AddComponent<MeshRenderer>().sharedMaterial=MakeMaterial(color,true);
            var collider=tip.AddComponent<BoxCollider>();collider.center=new Vector3(0,0,.025f);collider.size=new Vector3(.075f,.075f,.07f);
            return arrow;
        }

        public WorldAction Bind(System.Action<MuseumPlayer.Hand> press,System.Action<MuseumPlayer.Hand> release)
        {
            var action=Action(head.gameObject,press,release);action.Direct=true;action.RequiresGrab=true;
            return action;
        }

        public void SetLength(float value,bool valid=true)
        {
            float length=Mathf.Max(.06f,value);
            shaft.localPosition=Vector3.forward*(length-.05f)*.5f;
            shaft.localScale=new Vector3(.012f,.012f,Mathf.Max(.01f,length-.05f));
            shaft.GetComponent<Renderer>().sharedMaterial=MakeMaterial(valid?tint:Red,true);
            head.localPosition=Vector3.forward*(length-.05f);
            head.GetComponent<Renderer>().sharedMaterial=MakeMaterial(valid?tint:Red,true);
        }

        static Mesh Cone()
        {
            if(cone)return cone;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int sides=16;
            vertices.Add(new Vector3(0,0,.065f));vertices.Add(Vector3.zero);
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(angle)*.036f,Mathf.Sin(angle)*.036f,0));
            }
            for(int i=0;i<sides;i++)
            {
                int a=i+2,b=(i+1)%sides+2;triangles.AddRange(new[]{0,a,b,1,b,a});
            }
            cone=new Mesh{name="Pointe de manipulation"};cone.SetVertices(vertices);cone.SetTriangles(triangles,0);cone.RecalculateNormals();
            return cone;
        }
    }
}
