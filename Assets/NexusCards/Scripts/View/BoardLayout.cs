using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class BoardLayout : MonoBehaviour
    {
        public Camera cam;
        public SlotMarker[] playerSlots;
        public SlotMarker[] enemySlots;
        public DeckPileView playerDeck;
        public DeckPileView enemyDeck;
        public float handDistance = 7f;
        public float handViewportY = 0.115f;
        public float handScale = 0.8f;

        public Vector3 SlotPosition(Side side, int index)
        {
            var slots = side == Side.Player ? playerSlots : enemySlots;
            return slots[index].transform.position;
        }

        public void HandPose(int index, int count, bool lifted, out Vector3 pos, out Quaternion rot, out float scale)
        {
            float spread = Mathf.Min(0.085f, 0.52f / Mathf.Max(1, count));
            float offset = index - (count - 1) * 0.5f;
            float vx = 0.5f + offset * spread;
            float vy = lifted ? handViewportY + 0.075f : handViewportY;
            float dist = lifted ? handDistance - 0.6f : handDistance;
            pos = cam.ViewportToWorldPoint(new Vector3(vx, vy, dist));
            Vector3 toCam = (cam.transform.position - pos).normalized;
            Vector3 up = Vector3.Cross(cam.transform.right, toCam);
            rot = Quaternion.AngleAxis(lifted ? 0f : -offset * 2.5f, toCam) * Quaternion.LookRotation(up, toCam);
            pos += toCam * (index * 0.02f + (lifted ? 0.3f : 0f));
            scale = lifted ? handScale * 1.18f : handScale;
        }

        public Vector3 ShowcasePoint(out Quaternion rot)
        {
            Vector3 pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.55f, handDistance));
            Vector3 toCam = (cam.transform.position - pos).normalized;
            rot = Quaternion.LookRotation(Vector3.Cross(cam.transform.right, toCam), toCam);
            return pos;
        }
    }
}
