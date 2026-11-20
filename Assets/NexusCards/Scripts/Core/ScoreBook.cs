using System.Collections.Generic;

namespace Nexus.Core
{
    public sealed class ScoreBook
    {
        public const int Kill = 100;
        public const int MinionKill = 40;
        public const int StructurePerPoint = 10;
        public const int Turret = 400;
        public const int Combo = 150;
        public const int Region = 50;
        public const int Victory = 1000;
        public const int VictoryPerNexusHp = 20;

        readonly HashSet<string> registered = new HashSet<string>();

        public int Total { get; private set; }

        public bool Has(string eventId) { return registered.Contains(eventId); }

        public bool Register(string eventId, int points, string label, List<GameEvent> ev)
        {
            if (points <= 0 || !registered.Add(eventId)) return false;
            Total += points;
            if (ev != null) ev.Add(new ScoreEvent(Total, points, label));
            return true;
        }
    }
}
