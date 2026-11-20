using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Nexus.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Nexus
{
    public class ArtLoader : MonoBehaviour
    {
        public const string LoadingArtUrl = "https://ddragon.leagueoflegends.com/cdn/img/champion/loading/{0}_0.jpg";

        static ArtLoader runner;
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, List<Action<Texture2D>>> waiting = new Dictionary<string, List<Action<Texture2D>>>();

        static string CacheDir
        {
            get { return Path.Combine(Application.persistentDataPath, "champion-art"); }
        }

        public static void Request(CardDef def, Action<Texture2D> onReady)
        {
            string key = def.Id;
            Texture2D tex;
            if (cache.TryGetValue(key, out tex) && tex != null)
            {
                onReady(tex);
                return;
            }

            tex = Resources.Load<Texture2D>("ChampionArt/" + key);
            if (tex == null && !def.RemoteArt) tex = Resources.Load<Texture2D>("GeneratedArt/" + key);
            if (tex == null) tex = LoadFromDisk(key);
            if (tex != null)
            {
                cache[key] = tex;
                onReady(tex);
                return;
            }

            onReady(Fallback(def));
            if (!def.RemoteArt) return;

            List<Action<Texture2D>> list;
            if (waiting.TryGetValue(key, out list))
            {
                list.Add(onReady);
                return;
            }
            waiting[key] = new List<Action<Texture2D>> { onReady };
            Runner.StartCoroutine(Download(key));
        }

        static ArtLoader Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("ArtLoader");
                    DontDestroyOnLoad(go);
                    runner = go.AddComponent<ArtLoader>();
                }
                return runner;
            }
        }

        static Texture2D LoadFromDisk(string key)
        {
            try
            {
                string path = Path.Combine(CacheDir, key + ".jpg");
                if (!File.Exists(path)) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!t.LoadImage(File.ReadAllBytes(path))) return null;
                t.wrapMode = TextureWrapMode.Clamp;
                return t;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static IEnumerator Download(string key)
        {
            using (var req = UnityWebRequestTexture.GetTexture(string.Format(LoadingArtUrl, key)))
            {
                req.timeout = 12;
                yield return req.SendWebRequest();
                List<Action<Texture2D>> list;
                waiting.TryGetValue(key, out list);
                waiting.Remove(key);
                if (req.result != UnityWebRequest.Result.Success) yield break;
                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null) yield break;
                tex.wrapMode = TextureWrapMode.Clamp;
                cache[key] = tex;
                try
                {
                    Directory.CreateDirectory(CacheDir);
                    File.WriteAllBytes(Path.Combine(CacheDir, key + ".jpg"), req.downloadHandler.data);
                }
                catch (Exception) { }
                if (list == null) yield break;
                foreach (var cb in list) cb(tex);
            }
        }

        static Texture2D Fallback(CardDef def)
        {
            string key = "fallback:" + def.Id;
            Texture2D t;
            if (cache.TryGetValue(key, out t) && t != null) return t;
            const int w = 64, h = 112;
            t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            Color a = UIStyle.RegionColor(def.Region) * 0.55f;
            a.a = 1f;
            Color b = UIStyle.Ink;
            int seed = def.Id.GetHashCode();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float v = (float)y / h;
                    float dx = Mathf.Abs(x - w * 0.5f) / (w * 0.5f);
                    float dy = Mathf.Abs(y - h * 0.6f) / (h * 0.5f);
                    float diamond = Mathf.Clamp01(1f - (dx + dy) * 1.3f);
                    float grain = ((x * 73856093) ^ (y * 19349663) ^ seed) % 7 / 160f;
                    var c = Color.Lerp(b, a, v * 0.8f + diamond * 0.5f) + new Color(grain, grain, grain, 0f);
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            }
            t.Apply();
            cache[key] = t;
            return t;
        }
    }
}
