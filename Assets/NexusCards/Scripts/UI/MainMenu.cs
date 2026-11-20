using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class MainMenu : MonoBehaviour
    {
        public GameObject howToPanel;
        public GameObject creditsPanel;
        public Text bestText;
        public Text difficultyLabel;

        void Start()
        {
            AudioManager.Music(MusicTrack.Menu);
            ClosePanels();
            if (bestText != null)
                bestText.text = GameSettings.BestScore > 0 ? "RECORDE  " + GameSettings.BestScore.ToString("0000") : "";
            RefreshDifficulty();
        }

        void Update()
        {
            if (InputShim.KeyDown(GameKey.Escape)) ClosePanels();
        }

        public void Play()
        {
            AudioManager.Fire(Sfx.Click);
            SceneFlow.Load(SceneFlow.ArenaScene);
        }

        public void ShowHowTo()
        {
            AudioManager.Fire(Sfx.Click);
            howToPanel.SetActive(true);
            creditsPanel.SetActive(false);
        }

        public void ShowCredits()
        {
            AudioManager.Fire(Sfx.Click);
            creditsPanel.SetActive(true);
            howToPanel.SetActive(false);
        }

        public void ClosePanels()
        {
            if (howToPanel != null) howToPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void ToggleDifficulty()
        {
            AudioManager.Fire(Sfx.Click);
            GameSettings.Hard = !GameSettings.Hard;
            RefreshDifficulty();
        }

        void RefreshDifficulty()
        {
            if (difficultyLabel != null)
                difficultyLabel.text = GameSettings.Hard ? "DIFICULDADE: DIFÍCIL" : "DIFICULDADE: NORMAL";
        }

        public void Quit()
        {
            AudioManager.Fire(Sfx.Click);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
