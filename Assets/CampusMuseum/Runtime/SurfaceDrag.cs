using UnityEngine;

namespace CampusMuseum
{
    sealed class SurfaceDrag
    {
        const float DeadZone = .008f;
        const float SmoothingTime = .10f;
        const float AimGrace = .12f;

        Vector3 offset;
        float height;
        float timeWithoutAim;
        bool near;
        bool followFirstAim;

        public Vector3 Position { get; private set; }
        public bool CanPlace { get; private set; }

        public void Begin(Ray ray, Vector3 origin, bool keepGrabOffset, bool nearSelection)
        {
            Position = origin;
            height = origin.y;
            near = nearSelection;
            offset = Vector3.zero;
            timeWithoutAim = 0;
            CanPlace = keepGrabOffset;
            followFirstAim = !keepGrabOffset;
            if (keepGrabOffset && TryProject(ray, out var contact))
                offset = origin - contact;
        }

        public void Update(Ray ray, float deltaTime)
        {
            if (!TryProject(ray, out var point))
            {
                timeWithoutAim += deltaTime;
                if (timeWithoutAim > AimGrace) CanPlace = false;
                return;
            }

            timeWithoutAim = 0;
            CanPlace = true;
            var target = point + offset;
            if (followFirstAim)
            {
                Position = target;
                followFirstAim = false;
            }
            else if ((target - Position).sqrMagnitude > DeadZone * DeadZone)
                Position = Vector3.Lerp(Position, target, 1f - Mathf.Exp(-deltaTime / SmoothingTime));
        }

        bool TryProject(Ray ray, out Vector3 point)
        {
            point = ray.origin;
            if (!near)
            {
                if (Mathf.Abs(ray.direction.y) < .075f) return false;
                var distance = (height - ray.origin.y) / ray.direction.y;
                if (distance < 0 || distance > 10) return false;
                point = ray.GetPoint(distance);
            }
            point.y = height;
            return true;
        }
    }
}
