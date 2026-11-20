using System;
using System.Collections.Generic;

namespace Nexus.Core
{
    public enum Side { Player, Enemy }
    public enum Region { Ionia, Demacia, Noxus, PiltoverZaun, Freljord, Ixtal, Bandopolis, Neutro, Feitico }
    public enum CardKind { Champion, Spell, Minion }
    public enum SpellEffect { None, Ignite, Heal, Exhaust }

    [Flags]
    public enum Keyword { None = 0, Guardiao = 1, Impeto = 2 }

    public sealed class CardDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Region Region;
        public readonly CardKind Kind;
        public readonly int Cost;
        public readonly int Attack;
        public readonly int Health;
        public readonly Keyword Keywords;
        public readonly SpellEffect Spell;
        public readonly bool RemoteArt;

        public CardDef(string id, string name, Region region, CardKind kind, int cost, int attack, int health,
            Keyword keywords = Keyword.None, SpellEffect spell = SpellEffect.None, bool remoteArt = true)
        {
            Id = id;
            Name = name;
            Region = region;
            Kind = kind;
            Cost = cost;
            Attack = attack;
            Health = health;
            Keywords = keywords;
            Spell = spell;
            RemoteArt = remoteArt;
        }

        public bool Has(Keyword k) { return (Keywords & k) != 0; }
        public bool IsUnit { get { return Kind != CardKind.Spell; } }
        public bool NeedsTarget { get { return Spell == SpellEffect.Ignite || Spell == SpellEffect.Exhaust; } }

        public string RulesText
        {
            get
            {
                switch (Spell)
                {
                    case SpellEffect.Ignite: return "Causa 3 de dano a um campeão inimigo.";
                    case SpellEffect.Heal: return "Cura 3 de seus campeões e 4 do seu Nexus.";
                    case SpellEffect.Exhaust: return "Atordoa um inimigo: ele não ataca no próximo turno.";
                }
                var parts = new List<string>();
                if (Has(Keyword.Guardiao)) parts.Add("Guardião");
                if (Has(Keyword.Impeto)) parts.Add("Ímpeto");
                var combo = SynergyRules.ComboFor(Id);
                if (combo != null) parts.Add("Combo: " + combo.Name);
                return string.Join("  ·  ", parts.ToArray());
            }
        }
    }

    public static class CardDatabase
    {
        public static readonly List<CardDef> Champions = new List<CardDef>
        {
            new CardDef("Teemo", "Teemo", Region.Bandopolis, CardKind.Champion, 1, 1, 2, Keyword.Impeto),
            new CardDef("Katarina", "Katarina", Region.Noxus, CardKind.Champion, 2, 3, 1, Keyword.Impeto),
            new CardDef("Ekko", "Ekko", Region.PiltoverZaun, CardKind.Champion, 2, 2, 2, Keyword.Impeto),
            new CardDef("Poppy", "Poppy", Region.Demacia, CardKind.Champion, 2, 1, 4, Keyword.Guardiao),
            new CardDef("Draven", "Draven", Region.Noxus, CardKind.Champion, 3, 4, 2),
            new CardDef("Ahri", "Ahri", Region.Ionia, CardKind.Champion, 3, 3, 3),
            new CardDef("Irelia", "Irelia", Region.Ionia, CardKind.Champion, 3, 3, 3),
            new CardDef("Xayah", "Xayah", Region.Ionia, CardKind.Champion, 3, 3, 2, Keyword.Impeto),
            new CardDef("Rakan", "Rakan", Region.Ionia, CardKind.Champion, 3, 2, 4, Keyword.Guardiao),
            new CardDef("Lux", "Lux", Region.Demacia, CardKind.Champion, 3, 3, 3),
            new CardDef("Vi", "Vi", Region.PiltoverZaun, CardKind.Champion, 3, 3, 4),
            new CardDef("Caitlyn", "Caitlyn", Region.PiltoverZaun, CardKind.Champion, 3, 4, 2),
            new CardDef("Ashe", "Ashe", Region.Freljord, CardKind.Champion, 3, 3, 3),
            new CardDef("Yasuo", "Yasuo", Region.Ionia, CardKind.Champion, 4, 4, 3, Keyword.Impeto),
            new CardDef("Zed", "Zed", Region.Ionia, CardKind.Champion, 4, 5, 2),
            new CardDef("Garen", "Garen", Region.Demacia, CardKind.Champion, 4, 3, 6, Keyword.Guardiao),
            new CardDef("Fiora", "Fiora", Region.Demacia, CardKind.Champion, 4, 4, 4),
            new CardDef("Jinx", "Jinx", Region.PiltoverZaun, CardKind.Champion, 4, 4, 3),
            new CardDef("Braum", "Braum", Region.Freljord, CardKind.Champion, 4, 2, 6, Keyword.Guardiao),
            new CardDef("JarvanIV", "Jarvan IV", Region.Demacia, CardKind.Champion, 5, 5, 5),
            new CardDef("Darius", "Darius", Region.Noxus, CardKind.Champion, 5, 5, 5),
            new CardDef("Sejuani", "Sejuani", Region.Freljord, CardKind.Champion, 5, 4, 6, Keyword.Guardiao),
            new CardDef("Malphite", "Malphite", Region.Ixtal, CardKind.Champion, 5, 3, 7, Keyword.Guardiao),
            new CardDef("Swain", "Swain", Region.Noxus, CardKind.Champion, 6, 6, 6),
        };

        public static readonly CardDef Ignite = new CardDef("Incendiar", "Incendiar", Region.Feitico, CardKind.Spell, 2, 0, 0, Keyword.None, SpellEffect.Ignite, false);
        public static readonly CardDef Heal = new CardDef("Curar", "Curar", Region.Feitico, CardKind.Spell, 2, 0, 0, Keyword.None, SpellEffect.Heal, false);
        public static readonly CardDef Exhaust = new CardDef("Exaurir", "Exaurir", Region.Feitico, CardKind.Spell, 1, 0, 0, Keyword.None, SpellEffect.Exhaust, false);
        public static readonly CardDef Minion = new CardDef("Tropa", "Tropa do Nexus", Region.Neutro, CardKind.Minion, 0, 1, 2, Keyword.Guardiao, SpellEffect.None, false);

        public static CardDef Find(string id)
        {
            foreach (var c in Champions) if (c.Id == id) return c;
            if (id == Ignite.Id) return Ignite;
            if (id == Heal.Id) return Heal;
            if (id == Exhaust.Id) return Exhaust;
            if (id == Minion.Id) return Minion;
            return null;
        }

        public static List<CardDef> PlayerDeck()
        {
            var deck = new List<CardDef>(Champions);
            for (int i = 0; i < 2; i++)
            {
                deck.Add(Ignite);
                deck.Add(Heal);
                deck.Add(Exhaust);
            }
            return deck;
        }

        public static List<CardDef> EnemyDeck()
        {
            return new List<CardDef>(Champions);
        }

        public static string RegionName(Region r)
        {
            switch (r)
            {
                case Region.Ionia: return "Ionia";
                case Region.Demacia: return "Demacia";
                case Region.Noxus: return "Noxus";
                case Region.PiltoverZaun: return "Piltover & Zaun";
                case Region.Freljord: return "Freljord";
                case Region.Ixtal: return "Ixtal";
                case Region.Bandopolis: return "Bandópolis";
                case Region.Feitico: return "Feitiço";
                default: return "Nexus";
            }
        }
    }
}
