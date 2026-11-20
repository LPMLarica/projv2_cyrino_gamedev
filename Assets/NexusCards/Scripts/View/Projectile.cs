using System;
using UnityEngine;

namespace Nexus
{
    public enum ProjectileOutcome { Hit, Blocked, Lost }

    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public Rigidbody body;
        public SphereCollider sphere;
        public Renderer core;
        public TrailRenderer trail;
        public Light glow;
        public float maxLifetime = 3.2f;

        Action<ProjectileOutcome> onDone;
        bool resolved;
        float age;

        public void Launch(Vector3 from, Vector3 to, Color color, GameObject ignoreRoot, Action<ProjectileOutcome> done)
        {
            onDone = done;
            transform.position = from;
            if (ignoreRoot != null && sphere != null)
                foreach (var c in ignoreRoot.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(sphere, c);
            Tint(color);
            float dist = Vector3.Distance(from, to);
            float time = Mathf.Clamp(dist / 7.5f, 0.5f, 1f);
            Vector3 v0 = (to - from - 0.5f * Physics.gravity * time * time) / time;
            body.isKinematic = false;
            body.useGravity = true;
            PhysicsCompat.SetVelocity(body, v0);
        }

        void Tint(Color color)
        {
            if (core != null)
            {
                var block = new MaterialPropertyBlock();
                core.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                block.SetColor("_EmissionColor", color * 4f);
                core.SetPropertyBlock(block);
            }
            if (trail != null)
            {
                trail.startColor = new Color(color.r, color.g, color.b, 0.9f);
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
            }
            if (glow != null) glow.color = color;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age > maxLifetime)
            {
                Resolve(ProjectileOutcome.Lost);
                Destroy(gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (resolved) return;
            if (collision.collider.GetComponentInParent<GateBarrier>() != null)
            {
                Resolve(ProjectileOutcome.Blocked);
                AudioManager.Fire(Sfx.Block);
                if (glow != null) glow.intensity *= 2f;
                Destroy(gameObject, 1.1f);
                return;
            }
            Resolve(ProjectileOutcome.Hit);
            AudioManager.Fire(Sfx.Hit);
            if (core != null) core.enabled = false;
            if (sphere != null) sphere.enabled = false;
            body.useGravity = false;
            PhysicsCompat.SetVelocity(body, Vector3.zero);
            if (glow != null) glow.intensity *= 3f;
            Destroy(gameObject, trail != null ? trail.time : 0.2f);
        }

        void Resolve(ProjectileOutcome outcome)
        {
            if (resolved) return;
            resolved = true;
            var cb = onDone;
            onDone = null;
            if (cb != null) cb(outcome);
        }
    }
}
