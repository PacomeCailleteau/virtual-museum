using System;
using UnityEngine;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed class PunchBuzzer : MonoBehaviour
    {
        public MuseumPlayer Player;
        public Func<bool> Activate;
        Vector3 rest;
        Vector3 previousLeft;
        Vector3 previousRight;
        bool sampled;
        bool armed=true;
        float cooldown;
        float animationUntil;
        Color tint;
        Renderer surface;

        public void Initialize(MuseumPlayer player,Func<bool> activate)
        {
            Player=player;Activate=activate;rest=transform.localPosition;
            surface=GetComponent<Renderer>();tint=surface.sharedMaterial.color;
        }

        void Update()
        {
            float remaining=animationUntil-Time.unscaledTime;
            transform.localPosition=rest+Vector3.down*(remaining>0?Mathf.Sin((1-remaining/.4f)*Mathf.PI)*.045f:0);
            if(remaining<=0 && surface)surface.sharedMaterial=MakeMaterial(tint);
            if(Player==null || !Player.IsTracked){sampled=false;return;}
            var left=Player.Left.Ray.origin;var right=Player.Right.Ray.origin;
            if(!sampled){previousLeft=left;previousRight=right;sampled=true;return;}
            ObserveStroke(previousLeft,left,Time.unscaledDeltaTime);
            ObserveStroke(previousRight,right,Time.unscaledDeltaTime);
            previousLeft=left;previousRight=right;
        }

        public bool ObserveStroke(Vector3 previous,Vector3 current,float deltaTime)
        {
            var bounds=GetComponent<Collider>().bounds;
            float top=bounds.max.y+.045f;
            if(current.y>top+.10f && Time.unscaledTime>=cooldown)armed=true;
            if(!armed || Time.unscaledTime<cooldown || deltaTime<=0 || deltaTime>.15f)return false;
            if(previous.y<top || current.y>top || (previous.y-current.y)/deltaTime<.25f)return false;
            float t=(previous.y-top)/Mathf.Max(.0001f,previous.y-current.y);
            var contact=Vector3.Lerp(previous,current,t);
            var center=bounds.center;
            if(Vector2.Distance(new Vector2(contact.x,contact.z),new Vector2(center.x,center.z))>bounds.extents.x+.025f)return false;
            armed=false;cooldown=Time.unscaledTime+1.2f;animationUntil=Time.unscaledTime+.4f;
            bool accepted=Activate?.Invoke()??false;
            if(surface)surface.sharedMaterial=MakeMaterial(accepted?Color.white:Red,true);
            if(accepted)EmitSparkles(new Vector3(center.x,bounds.max.y+.04f,center.z),new Vector3(.26f,.07f,.26f),Gold);
            return accepted;
        }
    }
}
