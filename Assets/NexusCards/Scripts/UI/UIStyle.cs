using Nexus.Core;
using UnityEngine;

namespace Nexus
{
    public static class UIStyle
    {
        public static readonly Color Gold = new Color32(200, 170, 110, 255);
        public static readonly Color GoldDark = new Color32(120, 90, 40, 255);
        public static readonly Color GoldLight = new Color32(240, 230, 210, 255);
        public static readonly Color Ink = new Color32(1, 10, 19, 255);
        public static readonly Color Navy = new Color32(10, 20, 40, 255);
        public static readonly Color Teal = new Color32(10, 200, 185, 255);
        public static readonly Color TealDeep = new Color32(5, 120, 130, 255);
        public static readonly Color Crimson = new Color32(210, 60, 60, 255);
        public static readonly Color Heal = new Color32(90, 220, 140, 255);
        public static readonly Color Stun = new Color32(170, 140, 255, 255);
        public static readonly Color Muted = new Color32(160, 155, 140, 255);

        public static Color RegionColor(Region r)
        {
            switch (r)
            {
                case Region.Ionia: return new Color32(232, 124, 170, 255);
                case Region.Demacia: return new Color32(236, 214, 150, 255);
                case Region.Noxus: return new Color32(208, 58, 58, 255);
                case Region.PiltoverZaun: return new Color32(242, 162, 60, 255);
                case Region.Freljord: return new Color32(130, 200, 246, 255);
                case Region.Ixtal: return new Color32(96, 194, 112, 255);
                case Region.Bandopolis: return new Color32(184, 134, 232, 255);
                case Region.Feitico: return new Color32(10, 200, 185, 255);
                default: return new Color32(150, 150, 160, 255);
            }
        }

        public static string Hex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }

    public static class GameSettings
    {
        const string BestKey = "nexus.best";
        const string HardKey = "nexus.hard";

        public static bool Hard
        {
            get { return PlayerPrefs.GetInt(HardKey, 0) == 1; }
            set { PlayerPrefs.SetInt(HardKey, value ? 1 : 0); }
        }

        public static int BestScore
        {
            get { return PlayerPrefs.GetInt(BestKey, 0); }
        }

        public static bool SubmitScore(int score)
        {
            if (score <= BestScore) return false;
            PlayerPrefs.SetInt(BestKey, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
