using UnityEngine;

namespace CampusMuseum
{
    [DisallowMultipleComponent]
    public sealed class WorkshopLayout : MonoBehaviour
    {
        public Transform mapOrigin;
        public BoxCollider mapSurface;
        public Transform mapControls;
        public Transform editingOrigin;
        public BoxCollider editingSurface;
        public Transform editingControls;
        public Transform board;
        public Transform catalogue;
        public Transform terminal;
        public Transform resetBuzzer;
        public Transform emptyBuzzer;
        public bool startWithEmptyMuseum = false;
        public GameObject exit;
        public Transform arrival;
        [Range(0f,.35f)] public float viewElevation = .15f;
    }
}
