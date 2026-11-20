using System.Collections;
using System.Collections.Generic;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class HUDController : MonoBehaviour
    {
        [Header("Pontuação")]
        public Text scoreText;
        public Text scoreDeltaText;
        public CanvasGroup scoreDeltaGroup;
        public Text bestText;

        [Header("Jogador")]
        public Image playerNexusFill;
        public Text playerNexusText;
        public Image[] manaGems;
        public Text manaText;
        public Text playerDeckText;

        [Header("Inimigo")]
        public Image enemyNexusFill;
        public Text enemyNexusText;
        public Image turretFill;
        public Text turretText;
        public CanvasGroup turretGroup;
        public Text enemyHandText;

        [Header("Estado da partida")]
        public Text turnText;
        public Text phaseTitle;
        public Text phaseDesc;
        public Image phaseAccent;
        public Button endTurnButton;
        public Text endTurnLabel;
        public Image endTurnGlow;

        [Header("Sinergias")]
        public GameObject[] synergyChips;
        public Text[] synergyTexts;
        public Image[] synergyDots;
        public Text synergyEmpty;

        [Header("Mensagens")]
        public CanvasGroup toastGroup;
        public Text toastText;
        public CanvasGroup bannerGroup;
        public Text bannerTitle;
        public Text bannerSub;
        public CanvasGroup infoGroup;
        public Text infoTitle;
        public Text infoBody;
        public GameObject helpPanel;

        [Header("Resultado")]
        public ResultPanel result;

        Coroutine toastRoutine;
        Coroutine bannerRoutine;
        Coroutine deltaRoutine;
        bool endTurnHint;

        void Awake()
        {
            if (toastGroup != null) toastGroup.alpha = 0f;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
            if (scoreDeltaGroup != null) scoreDeltaGroup.alpha = 0f;
            if (infoGroup != null) infoGroup.alpha = 0f;
            if (helpPanel != null) helpPanel.SetActive(false);
        }

        void Update()
        {
            if (endTurnGlow != null)
            {
                float a = endTurnHint ? 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
                endTurnGlow.color = new Color(UIStyle.Teal.r, UIStyle.Teal.g, UIStyle.Teal.b, a);
            }
        }

        public void SetScore(int total, int delta, string label)
        {
            scoreText.text = total.ToString("0000");
            if (delta <= 0 || scoreDeltaText == null) return;
            scoreDeltaText.text = "+" + delta + "  " + label;
            if (deltaRoutine != null) StopCoroutine(deltaRoutine);
            deltaRoutine = StartCoroutine(FadeOut(scoreDeltaGroup, 1.6f));
        }

        public void SetBest(int best)
        {
            if (bestText != null) bestText.text = "RECORDE " + best.ToString("0000");
        }

        public void SetNexus(Side side, int hp, int max)
        {
            var fill = side == Side.Player ? playerNexusFill : enemyNexusFill;
            var text = side == Side.Player ? playerNexusText : enemyNexusText;
            if (fill != null) fill.fillAmount = max > 0 ? (float)hp / max : 0f;
            if (text != null) text.text = hp + " / " + max;
        }

        public void SetTurret(int hp, int max)
        {
            if (turretFill != null) turretFill.fillAmount = max > 0 ? (float)hp / max : 0f;
            if (turretText != null) turretText.text = hp > 0 ? hp + " / " + max : "DESTRUÍDA";
            if (turretGroup != null) turretGroup.alpha = hp > 0 ? 1f : 0.45f;
        }

        public void SetMana(int mana, int max)
        {
            for (int i = 0; i < manaGems.Length; i++)
            {
                var g = manaGems[i];
                if (i < mana) g.color = UIStyle.Teal;
                else if (i < max) g.color = new Color(0.25f, 0.35f, 0.4f, 1f);
                else g.color = new Color(1f, 1f, 1f, 0.08f);
            }
            if (manaText != null) manaText.text = mana + "/" + max;
        }

        public void SetCounts(int playerDeck, int enemyHand, int enemyDeck)
        {
            if (playerDeckText != null) playerDeckText.text = "BARALHO " + playerDeck;
            if (enemyHandText != null) enemyHandText.text = "MÃO " + enemyHand + "   ·   BARALHO " + enemyDeck;
        }

        public void SetTurn(Side side, int turn)
        {
            if (turnText == null) return;
            turnText.text = side == Side.Player ? "TURNO " + turn + "  ·  SUA VEZ" : "TURNO " + turn + "  ·  VEZ DO INIMIGO";
            turnText.color = side == Side.Player ? UIStyle.Teal : UIStyle.Crimson;
        }

        public void SetPhase(GamePhase phase)
        {
            if (phase == GamePhase.SiegeTower)
            {
                phaseTitle.text = "DESAFIO 1  ·  CERCO À TORRE";
                phaseDesc.text = "O Portão protege o Nexus. Destrua a Torre para abri-lo.";
                if (phaseAccent != null) phaseAccent.color = UIStyle.Gold;
            }
            else
            {
                phaseTitle.text = "DESAFIO 2  ·  NEXUS EXPOSTO";
                phaseDesc.text = "O Nexus convoca uma Tropa Guardiã por turno. Derrube-o!";
                if (phaseAccent != null) phaseAccent.color = UIStyle.Crimson;
            }
        }

        public void SetEndTurn(bool interactable, bool hint)
        {
            endTurnHint = interactable && hint;
            if (endTurnButton != null) endTurnButton.interactable = interactable;
            if (endTurnLabel != null)
            {
                endTurnLabel.text = interactable ? "ENCERRAR TURNO" : "AGUARDE";
                endTurnLabel.color = interactable ? UIStyle.GoldLight : UIStyle.Muted;
            }
        }

        public void SetSynergies(Match match)
        {
            var entries = new List<KeyValuePair<string, Color>>();
            foreach (var r in match.Player.ActiveRegions)
                entries.Add(new KeyValuePair<string, Color>(CardDatabase.RegionName(r) + "  —  " + SynergyRules.RegionBonusText(r), UIStyle.RegionColor(r)));
            foreach (var c in match.Player.ActiveCombos)
                entries.Add(new KeyValuePair<string, Color>("COMBO " + c.Name, UIStyle.Gold));
            foreach (var r in match.Enemy.ActiveRegions)
                entries.Add(new KeyValuePair<string, Color>("Inimigo: " + CardDatabase.RegionName(r), UIStyle.Crimson));
            foreach (var c in match.Enemy.ActiveCombos)
                entries.Add(new KeyValuePair<string, Color>("Inimigo: " + c.Name, UIStyle.Crimson));

            for (int i = 0; i < synergyChips.Length; i++)
            {
                bool on = i < entries.Count;
                synergyChips[i].SetActive(on);
                if (!on) continue;
                synergyTexts[i].text = entries[i].Key;
                synergyDots[i].color = entries[i].Value;
            }
            if (synergyEmpty != null) synergyEmpty.gameObject.SetActive(entries.Count == 0);
        }

        public void SetInfo(string title, string body)
        {
            if (infoGroup == null) return;
            if (string.IsNullOrEmpty(title))
            {
                infoGroup.alpha = Mathf.MoveTowards(infoGroup.alpha, 0f, Time.unscaledDeltaTime * 6f);
                return;
            }
            infoTitle.text = title;
            infoBody.text = body;
            infoGroup.alpha = Mathf.MoveTowards(infoGroup.alpha, 1f, Time.unscaledDeltaTime * 8f);
        }

        public void Toast(string message, Color color)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.color = color;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(FadeOut(toastGroup, 2.2f));
        }

        public void Toast(string message)
        {
            Toast(message, UIStyle.GoldLight);
        }

        public void Banner(string title, string subtitle, Color accent, float seconds = 2.2f)
        {
            if (bannerTitle == null) return;
            bannerTitle.text = title;
            bannerTitle.color = accent;
            bannerSub.text = subtitle;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(FadeOut(bannerGroup, seconds));
        }

        public void ToggleHelp()
        {
            if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);
        }

        public void CloseHelp()
        {
            if (helpPanel != null) helpPanel.SetActive(false);
        }

        IEnumerator FadeOut(CanvasGroup group, float hold)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = t / 0.18f;
                yield return null;
            }
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - t / 0.5f;
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
