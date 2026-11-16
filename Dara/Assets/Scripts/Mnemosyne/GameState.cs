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
        public int Composure = 2, Wit = 3, Strength = 1; // Kwin (round 1) starting stats
        public int NextDayApPenalty;
        public string Location = "classroom";

        // Story flags set by cutscene effects and choices ("choice:<cutscene>:<node>:<choice>", "told_name", ...).
        public HashSet<string> Flags = new HashSet<string>();

        // Items owned, and the (up to ItemData.PrepSlots) items carried into the night (set on the Prep Slots screen).
        public HashSet<string> Items = new HashSet<string> { "has_pin" };
        public List<string> Equipped = new List<string>();

        // Named numeric counters (e.g. "survival" = minigame stages passed).
        public Dictionary<string, int> Counters = new Dictionary<string, int>();

        public HashSet<string> KnownNpcs = new HashSet<string>();
        public HashSet<string> Explored = new HashSet<string>();
        public List<string> Codex = new List<string>();
        public Dictionary<string, int> Affection = new Dictionary<string, int> { { "lin", 2 }, { "khun", 1 } };

        public int Aff(string npc) { int v; return Affection.TryGetValue(npc, out v) ? v : 0; }
        public void AddAff(string npc, int d) { Affection[npc] = Aff(npc) + d; }
        public int Counter(string key) { int v; return Counters.TryGetValue(key, out v) ? v : 0; }
        public void AddCounter(string key, int d) { Counters[key] = Counter(key) + d; }
        public void RemoveItem(string id) { Items.Remove(id); Equipped.Remove(id); }
        public void AddCodex(string entry) { if (!string.IsNullOrEmpty(entry) && !Codex.Contains(entry)) Codex.Add(entry); }
        public bool CanAfford(int time, int ap) { return Time >= time && Ap >= ap; }
        public bool IsFree { get { return Phase == Phase.Free1 || Phase == Phase.Free2; } }
    }
}
