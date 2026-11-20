using System;
using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public enum CardHighlight { None, Playable, Ready, Selected, Target, Combo }

    public class CardView : MonoBehaviour
    {
        public enum Mode { Idle, Hand, Tween, Dropping, Board, Dying }

        [Header("Física")]
        public Rigidbody body;
        public BoxCollider hitBox;

        [Header("Visual")]
        public Transform visual;
        public Animator animator;
        public Transform aim;

        [Header("Face da carta")]
        public RawImage art;
        public Image glow;
        public Image regionBar;
        public Text nameText;
        public Text regionText;
        public Text rulesText;
        public Text costText;
        public Text atkText;
        public Text hpText;
        public Text statusText;
        public GameObject atkBadge;
        public GameObject hpBadge;

        public CardInstance Card { get; private set; }
        public Mode CurrentMode { get; private set; }

        Vector3 targetPos;
        Quaternion targetRot = Quaternion.identity;
        float targetScale = 1f;
        bool landed;
        float comboFlash;
        CardHighlight highlight;

        static readonly int AttackHash = Animator.StringToHash("Attack");
        static readonly int AttackDownHash = Animator.StringToHash("AttackDown");
        static readonly int HitHash = Animator.StringToHash("Hit");

        public void Bind(CardInstance card)
        {
            Card = card;
            var def = card.Def;
            nameText.text = def.Name;
            regionText.text = CardDatabase.RegionName(def.Region).ToUpperInvariant();
            var rc = UIStyle.RegionColor(def.Region);
            regionText.color = rc;
            regionBar.color = rc;
            rulesText.text = def.RulesText;
            costText.text = def.Cost.ToString();
            atkBadge.SetActive(def.IsUnit);
            hpBadge.SetActive(def.IsUnit);
            art.color = new Color(1f, 1f, 1f, 0f);
            ArtLoader.Request(def, OnArt);
            SetHighlight(CardHighlight.None);
            Refresh(false);
        }

        void OnArt(Texture2D tex)
        {
            if (this == null || art == null || tex == null) return;
            art.texture = tex;
            art.color = Color.white;
            var r = art.rectTransform.rect;
            float window = r.height > 0f ? r.width / r.height : 1.06f;
            float aspect = (float)tex.width / tex.height;
            if (aspect < window)
            {
                float h = aspect / window;
                float y = Mathf.Clamp(0.64f - h * 0.5f, 0f, 1f - h);
                art.uvRect = new Rect(0f, y, 1f, h);
            }
            else
            {
                float w = window / aspect;
                art.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
        }

        public void Refresh(bool ownersTurn)
        {
            if (Card == null) return;
            var c = Card;
            if (!c.IsUnit)
            {
                statusText.text = "";
                return;
            }
            atkText.text = c.Attack.ToString();
            hpText.text = c.Health.ToString();
            atkText.color = c.Attack > c.Def.Attack ? UIStyle.Heal : c.Attack < c.Def.Attack ? UIStyle.Crimson : Color.white;
            hpText.color = c.Damage > 0 ? new Color(1f, 0.55f, 0.5f) : c.MaxHealth > c.Def.Health ? UIStyle.Heal : Color.white;
            if (c.IsStunned)
            {
                statusText.text = "ATORDOADO";
                statusText.color = UIStyle.Stun;
            }
            else if (c.OnBoard && c.WaitingSummon)
            {
                statusText.text = "INVOCADO";
                statusText.color = UIStyle.Muted;
            }
            else if (c.OnBoard && ownersTurn && c.CanAttack)
            {
                statusText.text = "PRONTO";
                statusText.color = UIStyle.Teal;
            }
            else statusText.text = "";
        }

        public void SetHighlight(CardHighlight h)
        {
            highlight = h;
            ApplyGlow();
        }

        public void FlashCombo()
        {
            comboFlash = 1.4f;
            ApplyGlow();
        }

        void ApplyGlow()
        {
            if (glow == null) return;
            var h = comboFlash > 0f ? CardHighlight.Combo : highlight;
            glow.enabled = h != CardHighlight.None;
            switch (h)
            {
                case CardHighlight.Playable: glow.color = new Color(UIStyle.Teal.r, UIStyle.Teal.g, UIStyle.Teal.b, 0.75f); break;
                case CardHighlight.Ready: glow.color = new Color(UIStyle.Teal.r, UIStyle.Teal.g, UIStyle.Teal.b, 0.55f); break;
                case CardHighlight.Selected: glow.color = new Color(1f, 0.85f, 0.45f, 1f); break;
                case CardHighlight.Target: glow.color = new Color(1f, 0.3f, 0.25f, 0.95f); break;
                case CardHighlight.Combo: glow.color = new Color(1f, 0.9f, 0.6f, 1f); break;
            }
        }

        public void PlayAttack(bool towardEnemy)
        {
            if (animator != null) animator.SetTrigger(towardEnemy ? AttackHash : AttackDownHash);
        }

        public void PlayHit()
        {
            if (animator != null) animator.SetTrigger(HitHash);
        }

        public void SetHandTarget(Vector3 pos, Quaternion rot, float scale)
        {
            targetPos = pos;
            targetRot = rot;
            targetScale = scale;
        }

        public void EnterHand()
        {
            CurrentMode = Mode.Hand;
            MakeKinematic();
        }

        public void SnapToHandTarget()
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            transform.localScale = Vector3.one * targetScale;
        }

        void Update()
        {
            if (comboFlash > 0f)
            {
                comboFlash -= Time.deltaTime;
                if (comboFlash <= 0f) ApplyGlow();
            }
            if (CurrentMode != Mode.Hand) return;
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, k);
        }

        void MakeKinematic()
        {
            if (body == null) return;
            if (!body.isKinematic)
            {
                PhysicsCompat.SetVelocity(body, Vector3.zero);
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
        }

        public IEnumerator FlyTo(Vector3 pos, Quaternion rot, float scale, float duration, float arc = 0f)
        {
            CurrentMode = Mode.Tween;
            MakeKinematic();
            Vector3 p0 = transform.position;
            Quaternion r0 = transform.rotation;
            float s0 = transform.localScale.x;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                transform.position = Vector3.Lerp(p0, pos, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * arc);
                transform.rotation = Quaternion.Slerp(r0, rot, u);
                transform.localScale = Vector3.one * Mathf.Lerp(s0, scale, u);
                yield return null;
            }
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = Vector3.one * scale;
        }

        public IEnumerator DropToSlot(Vector3 slot, float height)
        {
            CurrentMode = Mode.Dropping;
            landed = false;
            transform.localScale = Vector3.one;
            transform.position = slot + Vector3.up * height;
            transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-9f, 9f), UnityEngine.Random.Range(-7f, 7f));
            body.isKinematic = false;
            body.useGravity = true;
            PhysicsCompat.SetVelocity(body, Vector3.down * 1.2f);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 1.2f;
            float t = 0f;
            while (!landed && t < 2.5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            AudioManager.Fire(Sfx.Play);
            yield return new WaitForSeconds(0.22f);
            yield return FlyTo(slot, Quaternion.identity, 1f, 0.16f);
            CurrentMode = Mode.Board;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (CurrentMode == Mode.Dropping && collision.collider.GetComponentInParent<TableSurface>() != null) landed = true;
        }

        public void Die(Action onGone = null)
        {
            if (CurrentMode == Mode.Dying) return;
            StartCoroutine(DieRoutine(onGone));
        }

        IEnumerator DieRoutine(Action onGone)
        {
            CurrentMode = Mode.Dying;
            MakeKinematic();
            if (hitBox != null) hitBox.enabled = false;
            SetHighlight(CardHighlight.None);
            Vector3 p0 = transform.position;
            Quaternion r0 = transform.rotation;
            Quaternion r1 = r0 * Quaternion.Euler(0f, UnityEngine.Random.Range(-40f, 40f), 0f);
            float t = 0f;
            const float dur = 0.45f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                transform.position = p0 + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.4f) - Vector3.up * (u * 0.2f);
                transform.rotation = Quaternion.Slerp(r0, r1, u);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, u * u);
                yield return null;
            }
            if (onGone != null) onGone();
            Destroy(gameObject);
        }
    }
}
