using System.Collections.Generic;

namespace Nexus.Core
{
    public enum AIActionKind { Play, Attack, EndTurn }

    public sealed class AIAction
    {
        public AIActionKind Kind;
        public CardInstance Card;
        public int Slot = -1;
        public TargetRef Target;

        public static readonly AIAction End = new AIAction { Kind = AIActionKind.EndTurn };
    }

    public static class AIPlanner
    {
        public static AIAction Plan(Match m, Side side)
        {
            if (m.IsOver || m.Turn != side) return AIAction.End;
            var play = BestPlay(m, side);
            if (play != null) return play;
            var attack = BestAttack(m, side);
            if (attack != null) return attack;
            return AIAction.End;
        }

        static AIAction BestPlay(Match m, Side side)
        {
            var s = m.Get(side);
            var opp = m.Get(Match.Other(side));
            AIAction best = null;
            int bestScore = 0;
            foreach (var c in s.Hand)
            {
                string reason;
                if (!m.CanPlay(side, c, out reason)) continue;
                int score = 0;
                var t = default(TargetRef);
                if (c.IsUnit)
                {
                    score = 10 + c.Def.Cost * 10 + c.Def.Attack + c.Def.Health;
                    var combo = SynergyRules.ComboFor(c.Def.Id);
                    if (combo != null)
                    {
                        string partner = combo.A == c.Def.Id ? combo.B : combo.A;
                        if (s.FindOnBoard(partner) != null) score += 45;
                    }
                    if (SynergyRules.RegionHasBonus(c.Def.Region))
                    {
                        foreach (var u in s.Units())
                            if (u.Def.Region == c.Def.Region) { score += 12; break; }
                    }
                    if (c.IsGuardian && s.NexusHp < s.NexusMax / 2) score += 15;
                }
                else
                {
                    switch (c.Def.Spell)
                    {
                        case SpellEffect.Ignite:
                            foreach (var u in opp.Units())
                            {
                                var tr = TargetRef.Champion(u);
                                if (!m.IsValidSpellTarget(c, tr)) continue;
                                int v = u.Health <= 3 ? 20 + u.Def.Cost * 10 + u.Attack * 4 : u.Attack * 3;
                                if (v > score) { score = v; t = tr; }
                            }
                            break;
                        case SpellEffect.Exhaust:
                            foreach (var u in opp.Units())
                            {
                                var tr = TargetRef.Champion(u);
                                if (!m.IsValidSpellTarget(c, tr) || u.Attack < 3) continue;
                                int v = u.Attack * 7;
                                if (v > score) { score = v; t = tr; }
                            }
                            break;
                        case SpellEffect.Heal:
                            int missing = s.NexusMax - s.NexusHp;
                            foreach (var u in s.Units()) missing += System.Math.Min(3, u.Damage);
                            score = missing >= 5 ? missing * 6 : 0;
                            break;
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new AIAction { Kind = AIActionKind.Play, Card = c, Slot = s.FreeSlot(), Target = t };
                }
            }
            return best;
        }

        static AIAction BestAttack(Match m, Side side)
        {
            var s = m.Get(side);
            var opp = m.Get(Match.Other(side));
            var targets = new List<TargetRef>();
            foreach (var u in opp.Units()) targets.Add(TargetRef.Champion(u));
            if (side == Side.Player && m.TurretHp > 0) targets.Add(TargetRef.Turret());
            targets.Add(TargetRef.NexusOf(opp.Side));

            AIAction best = null;
            int bestScore = 0;
            foreach (var a in s.Units())
            {
                if (!a.CanAttack) continue;
                foreach (var t in targets)
                {
                    string reason;
                    bool blocked;
                    if (!m.ValidateAttack(a, t, out reason, out blocked) || blocked) continue;
                    int score = Evaluate(m, a, t, opp);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new AIAction { Kind = AIActionKind.Attack, Card = a, Target = t };
                    }
                }
            }
            return best;
        }

        static int Evaluate(Match m, CardInstance a, TargetRef t, SideState opp)
        {
            switch (t.Kind)
            {
                case TargetKind.Champion:
                {
                    var d = t.Card;
                    bool kills = a.Attack >= d.Health;
                    bool dies = d.Attack >= a.Health;
                    int v = kills ? 40 + d.Def.Cost * 8 + d.Attack * 3 : a.Attack * 2 + 4;
                    if (dies) v -= 12 + a.Def.Cost * 6;
                    if (d.IsGuardian) v += 6;
                    return v;
                }
                case TargetKind.Turret:
                {
                    int dmg = a.Attack;
                    int v = 18 + dmg * 5;
                    if (dmg >= m.TurretHp) v += 60;
                    return v;
                }
                default:
                {
                    int dmg = a.Attack;
                    int v = 14 + dmg * 4;
                    if (dmg >= opp.NexusHp) v += 500;
                    return v;
                }
            }
        }
    }
}
