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
        public GameObject exit;
        public Transform arrival;
    }
}
