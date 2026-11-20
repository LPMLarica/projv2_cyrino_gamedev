using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class StructureView : MonoBehaviour
    {
        public Side side;
        public TargetKind kind = TargetKind.Nexus;
        public Transform aim;
        public Transform muzzle;
        public Animator animator;
        public Collider hitCollider;
        public Light glowLight;
        public Renderer targetRing;
        public Transform shakeRoot;
        public Image hpFill;
        public Text hpText;
        public GameObject hpBar;
        public Transform[] breakables;

        public bool Destroyed { get; private set; }

        float baseLight;
        Vector3 shakeOrigin;
        Coroutine hitRoutine;

        void Awake()
        {
            if (glowLight != null) baseLight = glowLight.intensity;
            if (shakeRoot != null) shakeOrigin = shakeRoot.localPosition;
            SetTargetable(false, Color.white);
        }

        void LateUpdate()
        {
            if (hpBar != null && Camera.main != null)
                hpBar.transform.rotation = Camera.main.transform.rotation;
        }

        public void SetHp(int hp, int max)
        {
            if (hpFill != null) hpFill.fillAmount = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            if (hpText != null) hpText.text = hp + " / " + max;
        }

        public void SetTargetable(bool on, Color color)
        {
            if (targetRing == null) return;
            targetRing.enabled = on && !Destroyed;
            if (on) targetRing.material.color = color;
        }

        public void PlayHit()
        {
            if (Destroyed) return;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(HitRoutine());
        }

        IEnumerator HitRoutine()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = 1f - t / 0.35f;
                if (shakeRoot != null) shakeRoot.localPosition = shakeOrigin + Random.insideUnitSphere * 0.07f * k;
                if (glowLight != null) glowLight.intensity = baseLight * (1f + 2.5f * k);
                yield return null;
            }
            if (shakeRoot != null) shakeRoot.localPosition = shakeOrigin;
            if (glowLight != null) glowLight.intensity = baseLight;
        }

        public void Collapse()
        {
            if (Destroyed) return;
            Destroyed = true;
            SetTargetable(false, Color.white);
            if (animator != null) animator.enabled = false;
            if (hitCollider != null) hitCollider.enabled = false;
            if (hpBar != null) hpBar.SetActive(false);
            Vector3 center = transform.position + Vector3.up * 0.6f;
            foreach (var piece in breakables)
            {
                if (piece == null) continue;
                piece.SetParent(null, true);
                var spin = piece.GetComponent<Spinner>();
                if (spin != null) spin.enabled = false;
                var rb = piece.GetComponent<Rigidbody>();
                if (rb == null) rb = piece.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.mass = 0.6f;
                rb.AddExplosionForce(7f, center, 4f, 0.8f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            if (glowLight != null) StartCoroutine(FadeLight());
        }

        IEnumerator FadeLight()
        {
            float start = glowLight.intensity;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                glowLight.intensity = Mathf.Lerp(start * 3f, 0f, t / 1.2f);
                yield return null;
            }
            glowLight.enabled = false;
        }
    }
}
