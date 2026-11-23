using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CampusMuseum
{
    public sealed class MuseumTeleport : MonoBehaviour
    {
        MuseumPlayer player;
        TeleportationProvider provider;
        LineRenderer arc;
        Transform destination;
        TextMesh caption;
        readonly Vector3[] points = new Vector3[41];
        bool aiming;
        bool valid;
        Vector3 landing;
        Quaternion heading;

        public void Initialize(MuseumPlayer owner, Camera camera)
        {
            player = owner;
            var origin = gameObject.AddComponent<XROrigin>();
            origin.Camera = camera;
            origin.Origin = gameObject;
            origin.enabled = false;
            var body = gameObject.AddComponent<XRBodyTransformer>();
            body.xrOrigin = origin;
            var mediator = gameObject.AddComponent<LocomotionMediator>();
            provider = gameObject.AddComponent<TeleportationProvider>();
            provider.mediator = mediator;
            var visual = new GameObject("Trajectoire de téléportation");
            visual.transform.SetParent(transform,false);
            arc = visual.AddComponent<LineRenderer>();
            arc.startWidth = .012f;
            arc.endWidth = .006f;
            destination = WorldView.Root(transform,"Destination de téléportation",Vector3.zero);
            WorldView.Box(destination,"Destination",new Vector3(0,.012f,0),new Vector3(.38f,.016f,.38f),WorldView.Teal,false);
            WorldView.Box(destination,"Direction d’arrivée",new Vector3(0,.025f,.30f),new Vector3(.10f,.018f,.32f),WorldView.Gold,false);
            caption = WorldView.Text(destination,"Relâcher pour se téléporter",new Vector3(0,.16f,0),.065f,Color.white,TextAnchor.MiddleCenter);
            Hide();
        }

        public void Aim(Ray ray, bool active)
        {
            if (!active)
            {
                if (aiming && valid && player.CanMove())
                    provider.QueueTeleportRequest(new TeleportRequest
                    {
                        destinationPosition = landing,
                        destinationRotation = heading,
                        matchOrientation = MatchOrientation.TargetUpAndForward
                    });
                aiming = false;
                Hide();
                return;
            }
            aiming = true;
            valid = false;
            var count = 1;
            points[0] = ray.origin;
            for (int i=1;i<points.Length;i++)
            {
                float t = i * .055f;
                var next = ray.origin + ray.direction * (6f*t) + Physics.gravity*(t*t*.5f);
                var segment = next-points[count-1];
                if (Physics.Raycast(points[count-1],segment,out var hit,segment.magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    points[count++] = hit.point;
                    landing = hit.point;
                    valid = hit.normal.y>.85f && Mathf.Abs(landing.y)<.05f && player.CanStand(landing);
                    break;
                }
                points[count++] = next;
            }
            arc.enabled = true;
            arc.positionCount = count;
            for(int i=0;i<count;i++) arc.SetPosition(i,points[i]);
            arc.sharedMaterial = WorldView.MakeMaterial(valid?WorldView.Teal:WorldView.Red,true);
            destination.gameObject.SetActive(valid);
            if (!valid) return;
            var forward = Vector3.ProjectOnPlane(player.Eye.forward,Vector3.up).normalized;
            heading = Quaternion.LookRotation(forward.sqrMagnitude>.001f?forward:Vector3.forward);
            destination.SetPositionAndRotation(landing,heading);
            caption.transform.rotation=Quaternion.LookRotation(caption.transform.position-player.Eye.position);
        }

        public void Cancel()
        {
            aiming=false;
            valid=false;
            Hide();
        }

        void Hide()
        {
            arc.enabled=false;
            destination.gameObject.SetActive(false);
        }
    }
}
