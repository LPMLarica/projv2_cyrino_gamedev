using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class ResultPanel : MonoBehaviour
    {
        public CanvasGroup group;
        public Text title;
        public Text subtitle;
        public Text scoreText;
        public Text recordText;
        public Text detailText;
        public Image accent;
        public Button retryButton;
        public Button menuButton;

        public bool Visible { get; private set; }

        void Awake()
        {
            Hide();
        }

        void Hide()
        {
            Visible = false;
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        void Update()
        {
            if (Visible && InputShim.KeyDown(GameKey.R)) Retry();
        }

        public void Show(bool victory, int score, int best, bool newRecord, string details)
        {
            Visible = true;
            title.text = victory ? "VITÓRIA" : "DERROTA";
            title.color = victory ? UIStyle.Gold : UIStyle.Crimson;
            subtitle.text = victory ? "O Nexus inimigo foi destruído." : "Seu Nexus caiu.";
            if (accent != null) accent.color = victory ? UIStyle.Gold : UIStyle.Crimson;
            scoreText.text = score.ToString("0000");
            recordText.text = newRecord ? "NOVO RECORDE!" : "RECORDE " + best.ToString("0000");
            recordText.color = newRecord ? UIStyle.Teal : UIStyle.Muted;
            detailText.text = details;
            StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            group.blocksRaycasts = true;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = t / 0.5f;
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
        }

        public void Retry()
        {
            AudioManager.Fire(Sfx.Click);
            SceneFlow.Load(SceneFlow.ArenaScene);
        }

        public void GoToMenu()
        {
            AudioManager.Fire(Sfx.Click);
            SceneFlow.Load(SceneFlow.MenuScene);
        }
    }
}
