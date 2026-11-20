namespace Nexus.Core
{
    public enum GamePhase { SiegeTower, NexusExposed }
    public enum MatchResult { None, Victory, Defeat }

    public abstract class GameEvent { }

    public sealed class CardDrawnEvent : GameEvent
    {
        public readonly Side Side; public readonly CardInstance Card;
        public CardDrawnEvent(Side s, CardInstance c) { Side = s; Card = c; }
    }

    public sealed class CardBurnedEvent : GameEvent
    {
        public readonly Side Side; public readonly CardDef Def;
        public CardBurnedEvent(Side s, CardDef d) { Side = s; Def = d; }
    }

    public sealed class FatigueEvent : GameEvent
    {
        public readonly Side Side; public readonly int Damage;
        public FatigueEvent(Side s, int d) { Side = s; Damage = d; }
    }

    public sealed class TurnStartedEvent : GameEvent
    {
        public readonly Side Side; public readonly int Turn;
        public TurnStartedEvent(Side s, int t) { Side = s; Turn = t; }
    }

    public sealed class TurnEndedEvent : GameEvent
    {
        public readonly Side Side;
        public TurnEndedEvent(Side s) { Side = s; }
    }

    public sealed class ManaChangedEvent : GameEvent
    {
        public readonly Side Side; public readonly int Mana; public readonly int Max;
        public ManaChangedEvent(Side s, int m, int max) { Side = s; Mana = m; Max = max; }
    }

    public sealed class CardPlayedEvent : GameEvent
    {
        public readonly Side Side; public readonly CardInstance Card; public readonly int Slot;
        public CardPlayedEvent(Side s, CardInstance c, int slot) { Side = s; Card = c; Slot = slot; }
    }

    public sealed class MinionSummonedEvent : GameEvent
    {
        public readonly Side Side; public readonly CardInstance Card; public readonly int Slot;
        public MinionSummonedEvent(Side s, CardInstance c, int slot) { Side = s; Card = c; Slot = slot; }
    }

    public sealed class SpellCastEvent : GameEvent
    {
        public readonly Side Side; public readonly CardInstance Card; public readonly bool HasTarget; public readonly TargetRef Target;
        public SpellCastEvent(Side s, CardInstance c, bool hasTarget, TargetRef t) { Side = s; Card = c; HasTarget = hasTarget; Target = t; }
    }

    public sealed class CardDamagedEvent : GameEvent
    {
        public readonly CardInstance Card; public readonly int Amount;
        public CardDamagedEvent(CardInstance c, int a) { Card = c; Amount = a; }
    }

    public sealed class CardHealedEvent : GameEvent
    {
        public readonly CardInstance Card; public readonly int Amount;
        public CardHealedEvent(CardInstance c, int a) { Card = c; Amount = a; }
    }

    public sealed class StructureChangedEvent : GameEvent
    {
        public readonly Side Side; public readonly TargetKind Kind; public readonly int Delta; public readonly int Hp; public readonly int Max;
        public StructureChangedEvent(Side s, TargetKind k, int delta, int hp, int max) { Side = s; Kind = k; Delta = delta; Hp = hp; Max = max; }
    }

    public sealed class StunnedEvent : GameEvent
    {
        public readonly CardInstance Card;
        public StunnedEvent(CardInstance c) { Card = c; }
    }

    public sealed class CardDiedEvent : GameEvent
    {
        public readonly CardInstance Card;
        public CardDiedEvent(CardInstance c) { Card = c; }
    }

    public sealed class TurretDestroyedEvent : GameEvent { }
    public sealed class GateOpenedEvent : GameEvent { }

    public sealed class PhaseChangedEvent : GameEvent
    {
        public readonly GamePhase Phase;
        public PhaseChangedEvent(GamePhase p) { Phase = p; }
    }

    public sealed class StatsChangedEvent : GameEvent
    {
        public readonly Side Side;
        public StatsChangedEvent(Side s) { Side = s; }
    }

    public sealed class RegionSynergyEvent : GameEvent
    {
        public readonly Side Side; public readonly Region Region;
        public RegionSynergyEvent(Side s, Region r) { Side = s; Region = r; }
    }

    public sealed class ComboEvent : GameEvent
    {
        public readonly Side Side; public readonly ComboDef Combo; public readonly CardInstance A; public readonly CardInstance B;
        public ComboEvent(Side s, ComboDef c, CardInstance a, CardInstance b) { Side = s; Combo = c; A = a; B = b; }
    }

    public sealed class ScoreEvent : GameEvent
    {
        public readonly int Total; public readonly int Delta; public readonly string Label;
        public ScoreEvent(int total, int delta, string label) { Total = total; Delta = delta; Label = label; }
    }

    public sealed class AttackBlockedEvent : GameEvent
    {
        public readonly CardInstance Attacker;
        public AttackBlockedEvent(CardInstance a) { Attacker = a; }
    }

    public sealed class GameOverEvent : GameEvent
    {
        public readonly MatchResult Result;
        public GameOverEvent(MatchResult r) { Result = r; }
    }
}
