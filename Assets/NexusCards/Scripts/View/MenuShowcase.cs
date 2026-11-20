using System.Collections.Generic;
using Nexus.Core;
using UnityEngine;

namespace Nexus
{
    public class MenuShowcase : MonoBehaviour
    {
        public CardView[] cards;
        public float radius = 3.4f;
        public float degreesPerSecond = 9f;
        public float height = 1.5f;

        float angle;

        void Start()
        {
            var pool = new List<CardDef>(CardDatabase.Champions);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var t = pool[i];
                pool[i] = pool[j];
                pool[j] = t;
            }
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                if (cards[i].body != null) cards[i].body.isKinematic = true;
                cards[i].Bind(new CardInstance(-(i + 1), pool[i % pool.Count], Side.Player));
            }
        }

        void Update()
        {
            angle += degreesPerSecond * Time.deltaTime;
            int n = cards.Length;
            for (int i = 0; i < n; i++)
            {
                if (cards[i] == null) continue;
                float a = (angle + i * 360f / n) * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));
                Vector3 p = transform.position + outward * radius + Vector3.up * (height + Mathf.Sin(Time.time * 0.9f + i) * 0.12f);
                cards[i].transform.SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.up, outward));
            }
        }
    }
}
