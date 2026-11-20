using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class DeckPileView : MonoBehaviour
    {
        public Transform stack;
        public Transform top;
        public float perCard = 0.012f;

        public void SetCount(int n)
        {
            float h = Mathf.Max(0.01f, n * perCard);
            if (stack != null)
            {
                var s = stack.localScale;
                stack.localScale = new Vector3(s.x, h, s.z);
                stack.localPosition = new Vector3(0f, h * 0.5f, 0f);
            }
            if (top != null)
            {
                top.localPosition = new Vector3(0f, h + 0.002f, 0f);
                top.gameObject.SetActive(n > 0);
            }
        }

        public Vector3 TopPosition
        {
            get { return top != null ? top.position + Vector3.up * 0.05f : transform.position + Vector3.up * 0.3f; }
        }
    }
}
