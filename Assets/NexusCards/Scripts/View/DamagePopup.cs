using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class DamagePopup : MonoBehaviour
    {
        public Text label;
        public CanvasGroup group;
        public float lifetime = 1.15f;

        Vector3 origin;
        float age;

        public void Show(string text, Color color, Vector3 position, float scale = 1f)
        {
            label.text = text;
            label.color = color;
            origin = position;
            transform.position = position;
            transform.localScale = Vector3.one * scale * 0.006f;
            age = 0f;
        }

        void LateUpdate()
        {
            age += Time.deltaTime;
            float u = age / lifetime;
            transform.position = origin + Vector3.up * (0.25f + Mathf.Sqrt(Mathf.Clamp01(u)) * 0.9f);
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            if (group != null) group.alpha = 1f - Mathf.SmoothStep(0.55f, 1f, u);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
