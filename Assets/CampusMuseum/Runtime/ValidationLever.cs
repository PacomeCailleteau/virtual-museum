using UnityEngine;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        const float LeverRestAngle=-10f;
        const float LeverLoweredAngle=-75f;
        const float LeverValidationAngle=-60f;
        MuseumPlayer.Hand leverHand;
        Transform leverPivot;
        float leverAngle;
        float leverLastCoordinate;
        bool leverReady;

        void DrawValidationLever()
        {
            var root=Root(stage,"Levier de validation",EditCenter+new Vector3(EditWidth/2-.10f,.10f,-EditLength/2+.12f));
            Box(root,"Socle",Vector3.zero,new Vector3(.22f,.12f,.24f),Navy);
            leverPivot=Root(root,"Pivot",new Vector3(0,.10f,0));
            leverPivot.localRotation=Quaternion.Euler(LeverRestAngle,0,0);
            Box(leverPivot,"Tige",new Vector3(0,.12f,0),new Vector3(.028f,.24f,.028f),Quiet,false);
            var knob=Box(leverPivot,"Poignée du levier",new Vector3(0,.25f,0),new Vector3(.13f,.065f,.065f),Teal);
            var mark=Quaternion.Euler(LeverValidationAngle,0,0)*Vector3.up*.25f;
            Box(root,"Repère de validation",mark+Vector3.up*.10f,new Vector3(.16f,.012f,.025f),Gold,false);
            var action=Action(knob,BeginValidationLever,EndValidationLever);
            action.Direct=true;action.RequiresGrab=true;
            Text(root,"SAISIR ET ABAISSER",new Vector3(0,-.03f,-.13f),.025f,Color.white,TextAnchor.MiddleCenter);
        }

        void BeginValidationLever(MuseumPlayer.Hand hand)
        {
            if(draft==null || heldArtwork || carriedView || resizingHand!=null || rotatingHand!=null)return;
            leverHand=hand;leverReady=false;leverAngle=LeverRestAngle;
            leverLastCoordinate=LeverCoordinate(hand);
        }

        float LeverCoordinate(MuseumPlayer.Hand hand)
        {
            var parent=leverPivot.parent;
            Vector3 point;
            if(hand.NearSelection)point=hand.Ray.origin;
            else
            {
                var plane=new Plane(parent.right,leverPivot.position);
                point=plane.Raycast(hand.Ray,out var distance)
                    ?hand.Ray.GetPoint(Mathf.Clamp(distance,0,6)):hand.Ray.origin;
            }
            var local=parent.InverseTransformPoint(point)-leverPivot.localPosition;
            if(local.y*local.y+local.z*local.z<.0004f)return leverLastCoordinate;
            return Mathf.Atan2(local.z,local.y)*Mathf.Rad2Deg;
        }

        void UpdateValidationLever()
        {
            float coordinate=LeverCoordinate(leverHand);
            leverAngle=Mathf.Clamp(leverAngle+Mathf.DeltaAngle(leverLastCoordinate,coordinate),LeverLoweredAngle,LeverRestAngle);
            leverLastCoordinate=coordinate;
            leverPivot.localRotation=Quaternion.Euler(leverAngle,0,0);
            leverReady=leverAngle<=LeverValidationAngle;
            var knob=leverPivot.Find("Poignée du levier").GetComponent<Renderer>();
            knob.sharedMaterial=MakeMaterial(leverReady?Gold:Teal,true);
        }

        void EndValidationLever(MuseumPlayer.Hand hand)
        {
            if(leverHand!=hand)return;
            bool apply=leverReady && !hand.ReleaseCancelled;
            leverHand=null;
            if(apply)CloseEdit(true);
            else
            {
                leverPivot.localRotation=Quaternion.Euler(LeverRestAngle,0,0);
                leverPivot.Find("Poignée du levier").GetComponent<Renderer>().sharedMaterial=MakeMaterial(Teal);
                Tell("Saisis et abaisse le levier jusqu’au repère doré pour valider.");
            }
        }

        void Sparkle(Vector3 center)
        {
            var effect=new GameObject("Validation scintillante");effect.transform.position=center;
            var system=effect.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=false;main.duration=.35f;main.startLifetime=.85f;main.startSpeed=.23f;
            main.startSize=.022f;main.startColor=Gold;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=system.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,55)});
            var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(EditWidth*.8f,.25f,EditLength*.75f);
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial=MakeMaterial(Gold,true);
            system.Play();Destroy(effect,1.4f);
        }
    }
}
