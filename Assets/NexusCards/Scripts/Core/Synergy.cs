using System.Collections.Generic;

namespace Nexus.Core
{
    public enum ComboBurst { None, DamageAllEnemies, StunAllEnemies, HealNexus, Draw, ReadyBoth, HitStructure }

    public sealed class ComboDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string A;
        public readonly string B;
        public readonly string Text;
        public readonly ComboBurst Burst;

        public ComboDef(string id, string name, string a, string b, ComboBurst burst, string text)
        {
            Id = id;
            Name = name;
            A = a;
            B = b;
            Burst = burst;
            Text = text;
        }
    }

    public static class SynergyRules
    {
        public const int RegionThreshold = 2;

        public static readonly ComboDef[] Combos =
        {
            new ComboDef("celestial", "Par Celestial", "Xayah", "Rakan", ComboBurst.HitStructure,
                "+2 ATQ para ambos. Ao formar: 2 de dano na Torre (ou no Nexus, com o portão aberto)."),
            new ComboDef("zaun", "Irmãs de Zaun", "Jinx", "Vi", ComboBurst.DamageAllEnemies,
                "+1 ATQ para ambas. Ao formar: 2 de dano em todos os campeões inimigos."),
            new ComboDef("wombo", "Wombo Combo", "Yasuo", "Malphite", ComboBurst.StunAllEnemies,
                "Ao formar: 1 de dano e atordoa todos os campeões inimigos."),
            new ComboDef("crownguard", "Luz de Demacia", "Garen", "Lux", ComboBurst.HealNexus,
                "Garen +0/+2, Lux +1 ATQ. Ao formar: cura 5 do seu Nexus."),
            new ComboDef("avalanche", "Coração do Freljord", "Ashe", "Braum", ComboBurst.Draw,
                "Braum +0/+2. Ao formar: compre 2 cartas."),
            new ComboDef("noxus", "Sangue de Noxus", "Darius", "Draven", ComboBurst.ReadyBoth,
                "+1 ATQ para ambos. Ao formar: os dois podem atacar imediatamente."),
        };

        public static ComboDef ComboFor(string cardId)
        {
            foreach (var c in Combos)
                if (c.A == cardId || c.B == cardId) return c;
            return null;
        }

        public static bool RegionHasBonus(Region r)
        {
            return r == Region.Ionia || r == Region.Demacia || r == Region.Noxus || r == Region.PiltoverZaun || r == Region.Freljord;
        }

        public static string RegionBonusText(Region r)
        {
            switch (r)
            {
                case Region.Ionia: return "Ionianos +1/+1";
                case Region.Demacia: return "Demacianos +0/+2";
                case Region.Noxus: return "Noxianos +2 ATQ";
                case Region.PiltoverZaun: return "+1 ATQ e dano dobrado em estruturas";
                case Region.Freljord: return "+0/+1 e atordoa quem atingem";
                default: return "";
            }
        }
    }
}
