using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public Vector3 introOffset = new Vector3(0f, 5f, -4f);
        public float introDuration = 1.8f;

        Vector3 finalPos;
        Quaternion finalRot;
        Vector3 focus;
        float shakeTime;
        float shakeDuration;
        float shakeMagnitude;

        void Awake()
        {
            finalPos = transform.position;
            finalRot = transform.rotation;
            focus = finalPos + finalRot * Vector3.forward * 14f;
        }

        public IEnumerator Intro()
        {
            Vector3 start = finalPos + introOffset;
            float t = 0f;
            while (t < introDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / introDuration);
                transform.position = Vector3.Lerp(start, finalPos, u);
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
                yield return null;
            }
            transform.SetPositionAndRotation(finalPos, finalRot);
        }

        public void Shake(float magnitude, float duration)
        {
            if (magnitude < shakeMagnitude && shakeTime > 0f) return;
            shakeMagnitude = magnitude;
            shakeDuration = duration;
            shakeTime = duration;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                cam.transform.localPosition = Random.insideUnitSphere * shakeMagnitude * k;
            }
            else cam.transform.localPosition = Vector3.zero;
        }
    }
}
