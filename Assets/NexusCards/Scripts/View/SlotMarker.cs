using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public enum SlotState { Hidden, Idle, Available, Hover }

    public class SlotMarker : MonoBehaviour
    {
        public Side side;
        public int index;
        public Renderer quad;

        Color idle;
        Material mat;

        void Awake()
        {
            if (quad == null) return;
            mat = quad.material;
            idle = mat.color;
        }

        public void SetState(SlotState s)
        {
            if (mat == null) return;
            quad.enabled = s != SlotState.Hidden;
            switch (s)
            {
                case SlotState.Idle: mat.color = idle; break;
                case SlotState.Available: mat.color = new Color(UIStyle.Teal.r, UIStyle.Teal.g, UIStyle.Teal.b, 0.85f); break;
                case SlotState.Hover: mat.color = new Color(1f, 0.88f, 0.55f, 1f); break;
            }
        }
    }
}
