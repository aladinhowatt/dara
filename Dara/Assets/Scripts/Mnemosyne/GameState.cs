using System.Collections.Generic;

namespace Mnemosyne
{
    public class GameState
    {
        public const int MaxAp = 6;
        public const int FreeTime = 4;
        public static readonly string[] DayNames = { "", "วันจันทร์", "วันอังคาร", "วันพุธ", "วันพฤหัสบดี", "วันศุกร์" };

        public int Day = 1;
        public Phase Phase = Phase.MorningClass;
        public int Time;
        public int Ap = MaxAp;
        public int Suspicion = 10;
        public int Composure = 2, Wit = 2, Strength = 1;
        public int NextDayApPenalty;
        public string Location = "classroom";

        public HashSet<string> KnownNpcs = new HashSet<string>();
        public HashSet<string> Explored = new HashSet<string>();
        public List<string> Codex = new List<string>();
        public Dictionary<string, int> Affection = new Dictionary<string, int>();

        public int Aff(string npc) { int v; return Affection.TryGetValue(npc, out v) ? v : 0; }
        public void AddAff(string npc, int d) { Affection[npc] = Aff(npc) + d; }
        public void AddCodex(string entry) { if (!string.IsNullOrEmpty(entry) && !Codex.Contains(entry)) Codex.Add(entry); }
        public bool CanAfford(int time, int ap) { return Time >= time && Ap >= ap; }
        public bool IsFree { get { return Phase == Phase.Free1 || Phase == Phase.Free2; } }
    }
}
