using System.Collections;
using UnityEngine;

namespace Nexus
{
    public class GateController : MonoBehaviour
    {
        public Animator animator;
        public Collider barrierCollider;
        public Light gateLight;
        public float openDuration = 1.1f;

        public bool IsOpen { get; private set; }

        static readonly int OpenHash = Animator.StringToHash("Open");

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (animator != null) animator.SetTrigger(OpenHash);
            AudioManager.Fire(Sfx.Gate);
            StartCoroutine(Finish());
        }

        IEnumerator Finish()
        {
            float start = gateLight != null ? gateLight.intensity : 0f;
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                if (gateLight != null) gateLight.intensity = Mathf.Lerp(start, 0f, t / openDuration);
                yield return null;
            }
            if (barrierCollider != null) barrierCollider.enabled = false;
            if (gateLight != null) gateLight.enabled = false;
        }
    }
}
