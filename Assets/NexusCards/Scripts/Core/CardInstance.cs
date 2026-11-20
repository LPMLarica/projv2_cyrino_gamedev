using System;

namespace Nexus.Core
{
    public sealed class CardInstance
    {
        public readonly int Uid;
        public readonly CardDef Def;
        public readonly Side Owner;

        public int Slot = -1;
        public int BonusAtk;
        public int BonusHp;
        public int Damage;
        public int StunTurns;
        public bool Summoned;
        public bool HasAttacked;

        public CardInstance(int uid, CardDef def, Side owner)
        {
            Uid = uid;
            Def = def;
            Owner = owner;
        }

        public int Attack { get { return Math.Max(0, Def.Attack + BonusAtk); } }
        public int MaxHealth { get { return Def.Health + BonusHp; } }
        public int Health { get { return MaxHealth - Damage; } }
        public bool IsUnit { get { return Def.IsUnit; } }
        public bool OnBoard { get { return Slot >= 0; } }
        public bool IsDead { get { return IsUnit && OnBoard && Health <= 0; } }
        public bool IsStunned { get { return StunTurns > 0; } }
        public bool IsGuardian { get { return Def.Has(Keyword.Guardiao); } }

        public bool CanAttack
        {
            get
            {
                return IsUnit && OnBoard && !HasAttacked && !IsStunned && Attack > 0
                       && (!Summoned || Def.Has(Keyword.Impeto));
            }
        }

        public bool WaitingSummon { get { return OnBoard && Summoned && !Def.Has(Keyword.Impeto); } }
    }

    public enum TargetKind { Champion, Turret, Nexus }

    public struct TargetRef
    {
        public TargetKind Kind;
        public Side Side;
        public CardInstance Card;

        public static TargetRef Champion(CardInstance c) { return new TargetRef { Kind = TargetKind.Champion, Side = c.Owner, Card = c }; }
        public static TargetRef Turret() { return new TargetRef { Kind = TargetKind.Turret, Side = Side.Enemy }; }
        public static TargetRef NexusOf(Side s) { return new TargetRef { Kind = TargetKind.Nexus, Side = s }; }
    }
}
