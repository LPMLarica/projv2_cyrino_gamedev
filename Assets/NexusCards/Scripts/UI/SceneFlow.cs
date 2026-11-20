using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    public class SceneFlow : MonoBehaviour
    {
        public const string MenuScene = "Menu";
        public const string ArenaScene = "Arena";

        public CanvasGroup fader;
        public float fadeTime = 0.45f;

        static SceneFlow active;
        bool loading;

        void Awake()
        {
            active = this;
            Time.timeScale = 1f;
            if (fader != null)
            {
                fader.alpha = 1f;
                fader.blocksRaycasts = true;
            }
        }

        void OnDestroy()
        {
            if (active == this) active = null;
        }

        IEnumerator Start()
        {
            if (fader == null) yield break;
            yield return Fade(1f, 0f);
            fader.blocksRaycasts = false;
        }

        public static void Load(string scene)
        {
            if (active != null) active.Go(scene);
            else SceneManager.LoadScene(scene);
        }

        void Go(string scene)
        {
            if (loading) return;
            loading = true;
            StartCoroutine(GoRoutine(scene));
        }

        IEnumerator GoRoutine(string scene)
        {
            if (fader != null)
            {
                fader.blocksRaycasts = true;
                yield return Fade(fader.alpha, 1f);
            }
            SceneManager.LoadScene(scene);
        }

        IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                fader.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            fader.alpha = to;
        }
    }
}
