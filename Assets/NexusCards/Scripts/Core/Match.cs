using System;
using System.Collections.Generic;

namespace Nexus.Core
{
    public sealed class SideState
    {
        public readonly Side Side;
        public readonly List<CardDef> Deck = new List<CardDef>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly CardInstance[] Board = new CardInstance[Match.BoardSize];
        public readonly HashSet<Region> ActiveRegions = new HashSet<Region>();
        public readonly List<ComboDef> ActiveCombos = new List<ComboDef>();
        public readonly HashSet<string> FormedPairs = new HashSet<string>();
        public int Mana;
        public int MaxMana;
        public int NexusHp;
        public int NexusMax;
        public int Fatigue;

        public SideState(Side side) { Side = side; }

        public IEnumerable<CardInstance> Units()
        {
            for (int i = 0; i < Board.Length; i++)
                if (Board[i] != null) yield return Board[i];
        }

        public int FreeSlot()
        {
            for (int i = 0; i < Board.Length; i++)
                if (Board[i] == null) return i;
            return -1;
        }

        public bool HasGuardian()
        {
            foreach (var u in Units()) if (u.IsGuardian) return true;
            return false;
        }

        public CardInstance FindOnBoard(string id)
        {
            foreach (var u in Units()) if (u.Def.Id == id) return u;
            return null;
        }
    }

    public sealed class Match
    {
        public const int BoardSize = 5;
        public const int HandLimit = 8;
        public const int ManaCap = 10;
        public const int StartHand = 4;
        public int TurretDamage = 1;

        public int PlayerNexusMax = 30;
        public int EnemyNexusMax = 18;
        public int TurretMax = 10;
        public int EnemyManaDelay = 1;

        public readonly SideState Player = new SideState(Side.Player);
        public readonly SideState Enemy = new SideState(Side.Enemy);
        public readonly ScoreBook Score = new ScoreBook();

        public int TurretHp { get; private set; }
        public bool GateOpen { get; private set; }
        public GamePhase Phase { get; private set; }
        public Side Turn { get; private set; }
        public int TurnNumber { get; private set; }
        public MatchResult Result { get; private set; }
        public bool IsOver { get { return Result != MatchResult.None; } }

        readonly Random rng;
        int nextUid = 1;
        int hitSeq;

        public Match(int seed)
        {
            rng = new Random(seed);
        }

        public SideState Get(Side s) { return s == Side.Player ? Player : Enemy; }
        public static Side Other(Side s) { return s == Side.Player ? Side.Enemy : Side.Player; }

        public List<GameEvent> StartMatch()
        {
            var ev = new List<GameEvent>();
            Player.NexusMax = Player.NexusHp = PlayerNexusMax;
            Enemy.NexusMax = Enemy.NexusHp = EnemyNexusMax;
            TurretHp = TurretMax;
            GateOpen = false;
            Phase = GamePhase.SiegeTower;
            Result = MatchResult.None;
            Player.Deck.AddRange(Shuffle(CardDatabase.PlayerDeck()));
            Enemy.Deck.AddRange(Shuffle(CardDatabase.EnemyDeck()));
            for (int i = 0; i < StartHand; i++)
            {
                Draw(Side.Player, 1, ev);
                Draw(Side.Enemy, 1, ev);
            }
            ev.Add(new PhaseChangedEvent(Phase));
            return ev;
        }

        List<CardDef> Shuffle(List<CardDef> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
            return list;
        }

        public List<GameEvent> BeginTurn(Side side)
        {
            var ev = new List<GameEvent>();
            if (IsOver) return ev;
            Turn = side;
            if (side == Side.Player) TurnNumber++;
            var s = Get(side);
            s.MaxMana = Math.Min(ManaCap, s.MaxMana + 1);
            if (side == Side.Enemy) s.MaxMana = Math.Max(1, Math.Min(s.MaxMana, TurnNumber - EnemyManaDelay));
            s.Mana = s.MaxMana;
            ev.Add(new TurnStartedEvent(side, TurnNumber));
            ev.Add(new ManaChangedEvent(side, s.Mana, s.MaxMana));
            foreach (var u in s.Units())
            {
                u.Summoned = false;
                u.HasAttacked = false;
            }
            if (side == Side.Enemy && Phase == GamePhase.NexusExposed) SummonMinion(ev);
            Draw(side, 1, ev);
            ev.Add(new StatsChangedEvent(side));
            CheckGameOver(ev);
            return ev;
        }

        public List<GameEvent> EndTurn(Side side)
        {
            var ev = new List<GameEvent>();
            foreach (var u in Get(side).Units())
                if (u.StunTurns > 0) u.StunTurns--;
            ev.Add(new TurnEndedEvent(side));
            ev.Add(new StatsChangedEvent(side));
            return ev;
        }

        void SummonMinion(List<GameEvent> ev)
        {
            int slot = Enemy.FreeSlot();
            if (slot < 0) return;
            var c = new CardInstance(nextUid++, CardDatabase.Minion, Side.Enemy);
            Place(Enemy, c, slot);
            ev.Add(new MinionSummonedEvent(Side.Enemy, c, slot));
            RecalculateSynergy(Side.Enemy, ev);
        }

        void Draw(Side side, int count, List<GameEvent> ev)
        {
            var s = Get(side);
            for (int i = 0; i < count; i++)
            {
                if (s.Deck.Count == 0)
                {
                    s.Fatigue++;
                    DamageNexus(side, s.Fatigue, ev, false);
                    ev.Add(new FatigueEvent(side, s.Fatigue));
                    continue;
                }
                var def = s.Deck[s.Deck.Count - 1];
                s.Deck.RemoveAt(s.Deck.Count - 1);
                if (s.Hand.Count >= HandLimit)
                {
                    ev.Add(new CardBurnedEvent(side, def));
                    continue;
                }
                var card = new CardInstance(nextUid++, def, side);
                s.Hand.Add(card);
                ev.Add(new CardDrawnEvent(side, card));
            }
        }

        void Place(SideState s, CardInstance c, int slot)
        {
            s.Board[slot] = c;
            c.Slot = slot;
            c.Summoned = true;
            c.HasAttacked = false;
        }

        public bool CanPlay(Side side, CardInstance card, out string reason)
        {
            reason = null;
            var s = Get(side);
            if (IsOver) { reason = "A partida terminou."; return false; }
            if (Turn != side) { reason = "Aguarde o seu turno."; return false; }
            if (!s.Hand.Contains(card)) { reason = "Carta indisponível."; return false; }
            if (card.Def.Cost > s.Mana) { reason = "Mana insuficiente."; return false; }
            if (card.IsUnit && s.FreeSlot() < 0) { reason = "Mesa cheia (máximo 5 campeões)."; return false; }
            if (card.Def.NeedsTarget)
            {
                bool any = false;
                foreach (var u in Get(Other(side)).Units()) if (IsValidSpellTarget(card, TargetRef.Champion(u))) any = true;
                if (!any) { reason = "Não há alvo válido para este feitiço."; return false; }
            }
            return true;
        }

        public bool IsValidSpellTarget(CardInstance spell, TargetRef t)
        {
            if (!spell.Def.NeedsTarget) return false;
            if (t.Kind != TargetKind.Champion || t.Card == null || !t.Card.OnBoard) return false;
            if (t.Card.Owner == spell.Owner) return false;
            if (spell.Def.Spell == SpellEffect.Exhaust && t.Card.IsStunned) return false;
            return true;
        }

        public List<GameEvent> PlayCard(Side side, CardInstance card, int slot, TargetRef target)
        {
            var ev = new List<GameEvent>();
            string reason;
            if (!CanPlay(side, card, out reason)) return ev;
            var s = Get(side);
            if (card.Def.NeedsTarget && !IsValidSpellTarget(card, target)) return ev;
            if (card.IsUnit)
            {
                if (slot < 0 || slot >= BoardSize || s.Board[slot] != null) slot = s.FreeSlot();
            }
            s.Mana -= card.Def.Cost;
            s.Hand.Remove(card);
            ev.Add(new ManaChangedEvent(side, s.Mana, s.MaxMana));
            if (card.IsUnit)
            {
                Place(s, card, slot);
                ev.Add(new CardPlayedEvent(side, card, slot));
                RecalculateSynergy(side, ev);
            }
            else
            {
                ev.Add(new SpellCastEvent(side, card, card.Def.NeedsTarget, target));
                ResolveSpell(side, card, target, ev);
                CleanupDeaths(ev);
            }
            CheckGameOver(ev);
            return ev;
        }

        void ResolveSpell(Side side, CardInstance spell, TargetRef t, List<GameEvent> ev)
        {
            switch (spell.Def.Spell)
            {
                case SpellEffect.Ignite:
                    DealDamage(t.Card, 3, ev);
                    break;
                case SpellEffect.Exhaust:
                    Stun(t.Card, ev);
                    break;
                case SpellEffect.Heal:
                    foreach (var u in Get(side).Units()) HealCard(u, 3, ev);
                    HealNexus(side, 4, ev);
                    break;
            }
        }

        public bool ValidateAttack(CardInstance attacker, TargetRef t, out string reason, out bool willBeBlocked)
        {
            reason = null;
            willBeBlocked = false;
            if (IsOver) { reason = "A partida terminou."; return false; }
            if (attacker == null || attacker.Owner != Turn) { reason = "Não é o turno deste campeão."; return false; }
            if (!attacker.CanAttack)
            {
                if (attacker.IsStunned) reason = "Atordoado: não pode atacar neste turno.";
                else if (attacker.WaitingSummon) reason = "Recém-invocado: ataca a partir do próximo turno.";
                else reason = "Este campeão já atacou.";
                return false;
            }
            var opp = Get(Other(attacker.Owner));
            if (t.Side == attacker.Owner) { reason = "Escolha um alvo inimigo."; return false; }
            bool guarded = opp.HasGuardian();
            switch (t.Kind)
            {
                case TargetKind.Champion:
                    if (t.Card == null || !t.Card.OnBoard || t.Card.Owner == attacker.Owner) { reason = "Alvo inválido."; return false; }
                    if (guarded && !t.Card.IsGuardian) { reason = "Um Guardião protege este alvo."; return false; }
                    return true;
                case TargetKind.Turret:
                    if (t.Side != Side.Enemy || TurretHp <= 0) { reason = "A Torre já caiu."; return false; }
                    if (guarded) { reason = "Derrote os Guardiões antes de atacar a Torre."; return false; }
                    return true;
                case TargetKind.Nexus:
                    if (guarded) { reason = "Derrote os Guardiões antes de atacar o Nexus."; return false; }
                    willBeBlocked = t.Side == Side.Enemy && !GateOpen;
                    return true;
            }
            reason = "Alvo inválido.";
            return false;
        }

        public bool ValidateAttack(CardInstance attacker, TargetRef t)
        {
            string r;
            bool b;
            return ValidateAttack(attacker, t, out r, out b);
        }

        public List<GameEvent> ResolveAttack(CardInstance attacker, TargetRef t, bool blocked)
        {
            var ev = new List<GameEvent>();
            if (IsOver || attacker == null || !attacker.OnBoard) return ev;
            attacker.HasAttacked = true;
            if (blocked)
            {
                ev.Add(new AttackBlockedEvent(attacker));
                ev.Add(new StatsChangedEvent(attacker.Owner));
                return ev;
            }
            var mine = Get(attacker.Owner);
            switch (t.Kind)
            {
                case TargetKind.Champion:
                    if (t.Card == null || !t.Card.OnBoard) break;
                    int toTarget = attacker.Attack;
                    int toAttacker = t.Card.Attack;
                    DealDamage(t.Card, toTarget, ev);
                    DealDamage(attacker, toAttacker, ev);
                    if (attacker.Def.Region == Region.Freljord && mine.ActiveRegions.Contains(Region.Freljord) && t.Card.Health > 0)
                        Stun(t.Card, ev);
                    break;
                case TargetKind.Turret:
                    DamageTurret(StructureDamage(attacker), attacker.Owner, ev);
                    break;
                case TargetKind.Nexus:
                    DamageNexus(t.Side, StructureDamage(attacker), ev, true);
                    break;
            }
            CleanupDeaths(ev);
            ev.Add(new StatsChangedEvent(attacker.Owner));
            CheckGameOver(ev);
            return ev;
        }

        int StructureDamage(CardInstance attacker)
        {
            bool pz = attacker.Def.Region == Region.PiltoverZaun && Get(attacker.Owner).ActiveRegions.Contains(Region.PiltoverZaun);
            return attacker.Attack * (pz ? 2 : 1);
        }

        public bool TurretActive { get { return Phase == GamePhase.SiegeTower && TurretHp > 0; } }

        public TargetRef ChooseTurretTarget()
        {
            CardInstance best = null;
            foreach (var u in Player.Units())
            {
                if (best == null || u.Attack > best.Attack || (u.Attack == best.Attack && u.Health > best.Health)) best = u;
            }
            return best != null ? TargetRef.Champion(best) : TargetRef.NexusOf(Side.Player);
        }

        public List<GameEvent> ResolveTurretShot(TargetRef t)
        {
            var ev = new List<GameEvent>();
            if (IsOver || !TurretActive) return ev;
            if (t.Kind == TargetKind.Champion && t.Card != null && t.Card.OnBoard) DealDamage(t.Card, TurretDamage, ev);
            else DamageNexus(Side.Player, TurretDamage, ev, false);
            CleanupDeaths(ev);
            CheckGameOver(ev);
            return ev;
        }

        void DealDamage(CardInstance c, int amount, List<GameEvent> ev)
        {
            if (c == null || amount <= 0 || !c.OnBoard) return;
            c.Damage += amount;
            ev.Add(new CardDamagedEvent(c, amount));
        }

        void HealCard(CardInstance c, int amount, List<GameEvent> ev)
        {
            int healed = Math.Min(amount, c.Damage);
            if (healed <= 0) return;
            c.Damage -= healed;
            ev.Add(new CardHealedEvent(c, healed));
        }

        void Stun(CardInstance c, List<GameEvent> ev)
        {
            if (c == null || !c.OnBoard) return;
            c.StunTurns = Math.Max(c.StunTurns, 1);
            ev.Add(new StunnedEvent(c));
        }

        void DamageTurret(int amount, Side source, List<GameEvent> ev)
        {
            if (amount <= 0 || TurretHp <= 0) return;
            int dealt = Math.Min(amount, TurretHp);
            TurretHp -= dealt;
            ev.Add(new StructureChangedEvent(Side.Enemy, TargetKind.Turret, -dealt, TurretHp, TurretMax));
            if (source == Side.Player) Score.Register("hit:" + (++hitSeq), dealt * ScoreBook.StructurePerPoint, "Dano na Torre", ev);
            if (TurretHp <= 0)
            {
                ev.Add(new TurretDestroyedEvent());
                Score.Register("turret", ScoreBook.Turret, "Torre destruída", ev);
                GateOpen = true;
                ev.Add(new GateOpenedEvent());
                Phase = GamePhase.NexusExposed;
                ev.Add(new PhaseChangedEvent(Phase));
            }
        }

        void DamageNexus(Side side, int amount, List<GameEvent> ev, bool fromPlayerAttack)
        {
            var s = Get(side);
            if (amount <= 0 || s.NexusHp <= 0) return;
            int dealt = Math.Min(amount, s.NexusHp);
            s.NexusHp -= dealt;
            ev.Add(new StructureChangedEvent(side, TargetKind.Nexus, -dealt, s.NexusHp, s.NexusMax));
            if (side == Side.Enemy && fromPlayerAttack)
                Score.Register("hit:" + (++hitSeq), dealt * ScoreBook.StructurePerPoint, "Dano no Nexus", ev);
        }

        void HealNexus(Side side, int amount, List<GameEvent> ev)
        {
            var s = Get(side);
            int healed = Math.Min(amount, s.NexusMax - s.NexusHp);
            if (healed <= 0) return;
            s.NexusHp += healed;
            ev.Add(new StructureChangedEvent(side, TargetKind.Nexus, healed, s.NexusHp, s.NexusMax));
        }

        void CleanupDeaths(List<GameEvent> ev)
        {
            bool any = false;
            foreach (var s in new[] { Player, Enemy })
            {
                for (int i = 0; i < s.Board.Length; i++)
                {
                    var c = s.Board[i];
                    if (c == null || c.Health > 0) continue;
                    ev.Add(new CardDiedEvent(c));
                    s.Board[i] = null;
                    c.Slot = -1;
                    any = true;
                    if (s.Side == Side.Enemy)
                    {
                        bool minion = c.Def.Kind == CardKind.Minion;
                        Score.Register("kill:" + c.Uid, minion ? ScoreBook.MinionKill : ScoreBook.Kill,
                            minion ? "Tropa abatida" : "Abate: " + c.Def.Name, ev);
                    }
                }
            }
            if (any)
            {
                RecalculateSynergy(Side.Player, ev);
                RecalculateSynergy(Side.Enemy, ev);
            }
        }

        void CheckGameOver(List<GameEvent> ev)
        {
            if (IsOver) return;
            if (Enemy.NexusHp <= 0)
            {
                Result = MatchResult.Victory;
                Score.Register("victory", ScoreBook.Victory + Player.NexusHp * ScoreBook.VictoryPerNexusHp, "Vitória", ev);
                ev.Add(new GameOverEvent(Result));
            }
            else if (Player.NexusHp <= 0)
            {
                Result = MatchResult.Defeat;
                ev.Add(new GameOverEvent(Result));
            }
        }

        void RecalculateSynergy(Side side, List<GameEvent> ev)
        {
            var s = Get(side);
            var before = new HashSet<Region>(s.ActiveRegions);
            s.ActiveRegions.Clear();
            s.ActiveCombos.Clear();
            var counts = new Dictionary<Region, int>();
            foreach (var u in s.Units())
            {
                u.BonusAtk = 0;
                u.BonusHp = 0;
                if (u.Def.Kind != CardKind.Champion) continue;
                int n;
                counts.TryGetValue(u.Def.Region, out n);
                counts[u.Def.Region] = n + 1;
            }
            foreach (var kv in counts)
                if (kv.Value >= SynergyRules.RegionThreshold && SynergyRules.RegionHasBonus(kv.Key)) s.ActiveRegions.Add(kv.Key);

            foreach (var u in s.Units())
            {
                if (!s.ActiveRegions.Contains(u.Def.Region) || u.Def.Kind != CardKind.Champion) continue;
                switch (u.Def.Region)
                {
                    case Region.Ionia: u.BonusAtk += 1; u.BonusHp += 1; break;
                    case Region.Demacia: u.BonusHp += 2; break;
                    case Region.Noxus: u.BonusAtk += 2; break;
                    case Region.PiltoverZaun: u.BonusAtk += 1; break;
                    case Region.Freljord: u.BonusHp += 1; break;
                }
            }

            var fresh = new List<ComboEvent>();
            foreach (var combo in SynergyRules.Combos)
            {
                var a = s.FindOnBoard(combo.A);
                var b = s.FindOnBoard(combo.B);
                if (a == null || b == null) continue;
                s.ActiveCombos.Add(combo);
                switch (combo.Id)
                {
                    case "celestial": a.BonusAtk += 2; b.BonusAtk += 2; break;
                    case "zaun": a.BonusAtk += 1; b.BonusAtk += 1; break;
                    case "crownguard": a.BonusHp += 2; b.BonusAtk += 1; break;
                    case "avalanche": b.BonusHp += 2; break;
                    case "noxus": a.BonusAtk += 1; b.BonusAtk += 1; break;
                }
                string key = combo.Id + ":" + Math.Min(a.Uid, b.Uid) + "-" + Math.Max(a.Uid, b.Uid);
                if (s.FormedPairs.Add(key)) fresh.Add(new ComboEvent(side, combo, a, b));
            }

            foreach (var u in s.Units())
                if (u.Health <= 0 && u.Damage > 0) u.Damage = Math.Max(0, u.MaxHealth - 1);

            ev.Add(new StatsChangedEvent(side));
            foreach (var r in s.ActiveRegions)
            {
                if (before.Contains(r)) continue;
                ev.Add(new RegionSynergyEvent(side, r));
                if (side == Side.Player)
                    Score.Register("region:" + r, ScoreBook.Region, "Sinergia " + CardDatabase.RegionName(r), ev);
            }

            if (fresh.Count == 0) return;
            foreach (var c in fresh)
            {
                ev.Add(c);
                if (side == Side.Player)
                    Score.Register("combo:" + c.Combo.Id + ":" + c.A.Uid + "-" + c.B.Uid, ScoreBook.Combo, "Combo: " + c.Combo.Name, ev);
                ApplyBurst(side, c, ev);
            }
            CleanupDeaths(ev);
        }

        void ApplyBurst(Side side, ComboEvent c, List<GameEvent> ev)
        {
            var opp = Get(Other(side));
            switch (c.Combo.Burst)
            {
                case ComboBurst.DamageAllEnemies:
                    foreach (var u in new List<CardInstance>(opp.Units())) DealDamage(u, 2, ev);
                    break;
                case ComboBurst.StunAllEnemies:
                    foreach (var u in new List<CardInstance>(opp.Units()))
                    {
                        DealDamage(u, 1, ev);
                        if (u.Health > 0) Stun(u, ev);
                    }
                    break;
                case ComboBurst.HealNexus:
                    HealNexus(side, 5, ev);
                    break;
                case ComboBurst.Draw:
                    Draw(side, 2, ev);
                    break;
                case ComboBurst.ReadyBoth:
                    c.A.Summoned = false; c.A.HasAttacked = false;
                    c.B.Summoned = false; c.B.HasAttacked = false;
                    break;
                case ComboBurst.HitStructure:
                    if (side == Side.Player)
                    {
                        if (TurretHp > 0) DamageTurret(2, side, ev);
                        else DamageNexus(Side.Enemy, 2, ev, true);
                    }
                    else DamageNexus(Side.Player, 2, ev, false);
                    break;
            }
            ev.Add(new StatsChangedEvent(Side.Player));
            ev.Add(new StatsChangedEvent(Side.Enemy));
            CheckGameOver(ev);
        }
    }
}
