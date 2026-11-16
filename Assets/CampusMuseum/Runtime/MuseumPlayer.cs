using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

namespace CampusMuseum
{
    public sealed class WorldAction : MonoBehaviour
    {
        public Action<MuseumPlayer.Hand> Press;
        public Action<MuseumPlayer.Hand> Release;
        public bool Direct;
        Material initial;
        Material focused;
        Renderer surface;

        public void SetSurface(Renderer renderer)
        {
            surface = renderer;
            initial = renderer.sharedMaterial;
            focused = WorldView.MakeMaterial(Color.Lerp(initial.color,Color.white,.35f));
        }

        public void Hover(bool active)
        {
            if (surface) surface.sharedMaterial = active ? focused : initial;
        }
    }

    public sealed class MuseumPlayer : MonoBehaviour
    {
        public sealed class Hand
        {
            public Ray Ray;
            public WorldAction Held;
            internal WorldAction Hovered;
            internal bool pressed;
            internal LineRenderer line;
            internal Transform controller;
        }

        public readonly Hand Left = new();
        public readonly Hand Right = new();
        public Transform Eye => head;
        public Transform Space => transform;
        public bool TeleportMode { get; set; }
        public Func<Vector3, bool> CanStand;
        public Func<bool> CanMove;
        Transform head;
        Camera playerCamera;
        float pitch;
        float yaw;
        float turnCooldown;
        bool xr;
        bool floorTracked;
        bool leftToggle;
        readonly List<XRInputSubsystem> subsystems = new();

        public void Initialize(Camera camera)
        {
            playerCamera = camera;
            head = playerCamera.transform;
            var origin = playerCamera.GetComponentInParent<XROrigin>();
            head.SetParent(transform, false);
            head.localPosition = Vector3.up * 1.65f;
            head.localRotation = Quaternion.identity;
            foreach (var behaviour in head.GetComponents<Behaviour>())
                if (behaviour.GetType().Name == "TrackedPoseDriver") behaviour.enabled = false;
            if (origin) origin.gameObject.SetActive(false);
            playerCamera.nearClipPlane = .035f;
            playerCamera.farClipPlane = 85f;
            MakeHand(Left, new Color(.25f, .8f, .92f));
            MakeHand(Right, new Color(.25f, .8f, .92f));
        }

        void MakeHand(Hand hand, Color color)
        {
            var g = new GameObject("Pointeur");
            g.transform.SetParent(transform, false);
            hand.line = g.AddComponent<LineRenderer>();
            hand.line.positionCount = 2;
            hand.line.startWidth = .007f;
            hand.line.endWidth = .003f;
            hand.line.sharedMaterial = WorldView.MakeMaterial(color, true);
            var grip = WorldView.Box(g.transform, "Poignée", Vector3.zero, new Vector3(.045f,.048f,.11f), WorldView.Navy, false);
            hand.controller = grip.transform;
        }

        public void Place(Vector3 floor, float heading)
        {
            var offset = head.position - transform.position;
            offset.y = 0;
            transform.position = floor - offset;
            if (xr)
            {
                transform.RotateAround(head.position, Vector3.up, Mathf.DeltaAngle(head.eulerAngles.y, heading));
            }
            else
            {
                yaw = heading;
                pitch = 8;
                head.rotation = Quaternion.Euler(pitch, yaw, 0);
            }
        }

        public void Tick()
        {
            var hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool wasXr = xr;
            xr = hmd.isValid && hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked) && tracked;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            Vector2 move = Vector2.zero;

            if (xr)
            {
                if (!wasXr)
                {
                    SubsystemManager.GetSubsystems(subsystems);
                    floorTracked = false;
                    foreach (var subsystem in subsystems)
                        if (subsystem.running)
                        {
                            subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                            floorTracked |= subsystem.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor;
                        }
                }
                if (hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var p))
                    head.localPosition = p + (floorTracked ? Vector3.zero : Vector3.up * 1.65f);
                if (hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var q))
                    head.localRotation = q;

                leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out move);
                rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var turn);
                turnCooldown -= Time.unscaledDeltaTime;
                if (Mathf.Abs(turn.x) > .7f && turnCooldown <= 0 && CanMove())
                {
                    transform.RotateAround(head.position, Vector3.up, Mathf.Sign(turn.x) * 30);
                    turnCooldown = .28f;
                }
                leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var toggle);
                if (toggle && !leftToggle) TeleportMode = !TeleportMode;
                leftToggle = toggle;
                AimXR(Left, leftDevice);
                AimXR(Right, rightDevice);
            }
            else if (keyboard != null && mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var d = mouse.delta.ReadValue();
                    yaw += d.x * .17f;
                    pitch = Mathf.Clamp(pitch - d.y * .17f, -84, 84);
                    head.rotation = Quaternion.Euler(pitch, yaw, 0);
                }
                move.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.qKey.isPressed ? 1 : 0);
                move.y = (keyboard.wKey.isPressed || keyboard.zKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                if (keyboard.tKey.wasPressedThisFrame) TeleportMode = !TeleportMode;
                Right.Ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
                SetPointer(Left, false, false);
                SetPointer(Right, true, mouse.leftButton.isPressed);
            }

            if (CanMove() && !TeleportMode)
            {
                var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                var delta = (Vector3.Cross(Vector3.up, forward) * move.x + forward * move.y)
                    .normalized * Mathf.Min(move.magnitude, 1) * (1.55f * Time.deltaTime);
                var candidate = head.position + delta;
                if (delta.sqrMagnitude > 0 && CanStand(candidate)) transform.position += delta;
            }
        }

        void AimXR(Hand hand, InputDevice device)
        {
            var valid = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position);
            valid &= device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
            if (valid)
            {
                var world = transform.TransformPoint(position + (floorTracked ? Vector3.zero : Vector3.up * 1.65f));
                var orientation = transform.rotation * rotation;
                hand.controller.SetPositionAndRotation(world, orientation);
                hand.Ray = new Ray(world + orientation * Vector3.forward * .055f, orientation * Vector3.forward);
            }
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var trigger);
            SetPointer(hand, valid, trigger);
        }

        void SetPointer(Hand hand, bool valid, bool trigger)
        {
            hand.controller.gameObject.SetActive(valid && xr);
            hand.line.enabled = valid && (xr || hand.Held != null);
            WorldAction action = null;
            var end = hand.Ray.GetPoint(10);
            if (valid && Physics.Raycast(hand.Ray, out var hit, 12, ~0, QueryTriggerInteraction.Ignore))
            {
                action = hit.collider.GetComponentInParent<WorldAction>();
                end = hit.point;
            }
            if (valid && xr && hand.Held == null)
            {
                float closest=.085f;
                foreach (var nearby in Physics.OverlapSphere(hand.Ray.origin,closest,~0,QueryTriggerInteraction.Ignore))
                {
                    var candidate=nearby.GetComponentInParent<WorldAction>();
                    if (!candidate || !candidate.Direct) continue;
                    var point=nearby.ClosestPoint(hand.Ray.origin);
                    var contactDistance=Vector3.Distance(point,hand.Ray.origin);
                    if (contactDistance>closest) continue;
                    closest=contactDistance;
                    action=candidate;
                    end=point;
                }
            }
            if (hand.Hovered != action)
            {
                if (hand.Hovered) hand.Hovered.Hover(false);
                hand.Hovered = action;
                if (action) action.Hover(true);
            }
            bool target = action != null;
            bool teleport = valid && TeleportMode && hand.Held == null && !target &&
                new Plane(Vector3.up, Vector3.zero).Raycast(hand.Ray, out var distance) &&
                distance < 12 && CanStand(hand.Ray.GetPoint(distance));
            if (teleport && new Plane(Vector3.up, Vector3.zero).Raycast(hand.Ray, out var floorDistance))
                end = hand.Ray.GetPoint(floorDistance);

            hand.line.SetPosition(0, hand.Ray.origin);
            hand.line.SetPosition(1, end);
            hand.line.sharedMaterial = WorldView.MakeMaterial(target || teleport ? WorldView.Teal : WorldView.Quiet, true);

            if (valid && trigger && !hand.pressed)
            {
                if (action)
                {
                    hand.Held = action;
                    action.Press?.Invoke(hand);
                }
                else if (teleport && CanMove())
                {
                    Place(new Vector3(end.x, 0, end.z), head.eulerAngles.y);
                }
            }
            if ((!valid || !trigger) && hand.pressed)
            {
                var held = hand.Held;
                hand.Held = null;
                if (held) held.Release?.Invoke(hand);
            }
            hand.pressed = valid && trigger;
        }
    }
}
