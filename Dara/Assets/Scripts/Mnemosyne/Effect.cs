using System;
using System.Collections.Generic;
using System.Text;

namespace Mnemosyne
{
    // Everything a choice (or any scripted beat) can change in the game state.
    // Data only: add a field here when a new kind of consequence is needed.
    public class Effect
    {
        // Pools of the CURRENT day. Note: StartDay() refills Ap, so a cutscene that runs
        // right before a Day segment should use NextDayApPenalty instead of Ap.
        public int Ap, Time;
        public int NextDayApPenalty;

        public int Suspicion;
        public int Composure, Wit, Strength;

        public Dictionary<string, int> Affection = new Dictionary<string, int>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public string Codex;
        public string[] SetFlags, ClearFlags;
        public string[] GiveItems, TakeItems;

        public Effect Aff(string npc, int delta) { Affection[npc] = delta; return this; }
        public Effect Flag(params string[] flags) { SetFlags = flags; return this; }
        public Effect Unflag(params string[] flags) { ClearFlags = flags; return this; }
        public Effect Give(params string[] items) { GiveItems = items; return this; }
        public Effect Take(params string[] items) { TakeItems = items; return this; }
        public Effect Count(string counter, int delta) { Counters[counter] = delta; return this; }

        public void Apply(GameState s)
        {
            s.Ap = Clamp(s.Ap + Ap, 0, GameState.MaxAp);
            s.Time = Math.Max(0, s.Time + Time);
            s.NextDayApPenalty = Math.Max(0, s.NextDayApPenalty + NextDayApPenalty);
            s.Suspicion = Clamp(s.Suspicion + Suspicion, 0, 100);
            s.Composure = Math.Max(0, s.Composure + Composure);
            s.Wit = Math.Max(0, s.Wit + Wit);
            s.Strength = Math.Max(0, s.Strength + Strength);
            foreach (var kv in Affection) s.AddAff(kv.Key, kv.Value);
            foreach (var kv in Counters) s.AddCounter(kv.Key, kv.Value);
            if (!string.IsNullOrEmpty(Codex)) s.AddCodex(Codex);
            if (SetFlags != null) foreach (var f in SetFlags) s.Flags.Add(f);
            if (ClearFlags != null) foreach (var f in ClearFlags) s.Flags.Remove(f);
            if (GiveItems != null) foreach (var i in GiveItems) s.Items.Add(i);
            if (TakeItems != null) foreach (var i in TakeItems) s.RemoveItem(i);
        }

        // For logs and debug overlays. Players do not see numbers (see design notes).
        public string Describe()
        {
            var sb = new StringBuilder();
            Add(sb, "AP", Ap); Add(sb, "Time", Time); Add(sb, "NextDayAP-", NextDayApPenalty);
            Add(sb, "Suspicion", Suspicion);
            Add(sb, "Composure", Composure); Add(sb, "Wit", Wit); Add(sb, "Strength", Strength);
            foreach (var kv in Affection) Add(sb, "Aff:" + kv.Key, kv.Value);
            foreach (var kv in Counters) Add(sb, "#" + kv.Key, kv.Value);
            if (!string.IsNullOrEmpty(Codex)) sb.Append("Codex+ ");
            if (SetFlags != null && SetFlags.Length > 0) sb.Append("Flags:" + string.Join(",", SetFlags) + " ");
            if (ClearFlags != null && ClearFlags.Length > 0) sb.Append("Unflag:" + string.Join(",", ClearFlags) + " ");
            if (GiveItems != null && GiveItems.Length > 0) sb.Append("Item+" + string.Join(",", GiveItems) + " ");
            if (TakeItems != null && TakeItems.Length > 0) sb.Append("Item-" + string.Join(",", TakeItems) + " ");
            return sb.Length == 0 ? "(no effect)" : sb.ToString().Trim();
        }

        static void Add(StringBuilder sb, string name, int v)
        {
            if (v != 0) sb.Append(name).Append(v > 0 ? "+" : "").Append(v).Append(' ');
        }

        static int Clamp(int v, int lo, int hi) { return v < lo ? lo : v > hi ? hi : v; }
    }

    // A condition on the game state. Used to lock a choice (shown dimmed with its Label) and to pick a branch.
    // Combine with Requirement.All(...) / Requirement.Not(...).
    public class Requirement
    {
        enum Kind { Composure, Wit, Strength, Flag, Affection, Item, Equipped, Counter, SuspicionMin, SuspicionBelow, Not, All, Any }

        Kind kind;
        int min;
        string key;
        string label;
        Requirement[] subs;

        public static Requirement Composure(int n) { return new Requirement { kind = Kind.Composure, min = n }; }
        public static Requirement Wit(int n) { return new Requirement { kind = Kind.Wit, min = n }; }
        public static Requirement Strength(int n) { return new Requirement { kind = Kind.Strength, min = n }; }
        public static Requirement HasFlag(string flag, string label = null) { return new Requirement { kind = Kind.Flag, key = flag, label = label }; }
        public static Requirement Aff(string npc, int n) { return new Requirement { kind = Kind.Affection, key = npc, min = n }; }
        public static Requirement HasItem(string item) { return new Requirement { kind = Kind.Item, key = item }; }
        public static Requirement Equipped(string item) { return new Requirement { kind = Kind.Equipped, key = item }; }
        public static Requirement Counter(string counter, int atLeast) { return new Requirement { kind = Kind.Counter, key = counter, min = atLeast }; }
        public static Requirement SuspicionAtLeast(int n) { return new Requirement { kind = Kind.SuspicionMin, min = n }; }
        public static Requirement SuspicionBelow(int n) { return new Requirement { kind = Kind.SuspicionBelow, min = n }; }
        public static Requirement Not(Requirement r) { return new Requirement { kind = Kind.Not, subs = new[] { r } }; }
        public static Requirement All(params Requirement[] rs) { return new Requirement { kind = Kind.All, subs = rs }; }
        public static Requirement Any(params Requirement[] rs) { return new Requirement { kind = Kind.Any, subs = rs }; }

        public bool Met(GameState s)
        {
            switch (kind)
            {
                case Kind.Composure: return s.Composure >= min;
                case Kind.Wit: return s.Wit >= min;
                case Kind.Strength: return s.Strength >= min;
                case Kind.Flag: return s.Flags.Contains(key);
                case Kind.Affection: return s.Aff(key) >= min;
                case Kind.Item: return s.Items.Contains(key);
                case Kind.Equipped: return s.Equipped.Contains(key);
                case Kind.Counter: return s.Counter(key) >= min;
                case Kind.SuspicionMin: return s.Suspicion >= min;
                case Kind.SuspicionBelow: return s.Suspicion < min;
                case Kind.Not: return !subs[0].Met(s);
                case Kind.All: foreach (var r in subs) if (!r.Met(s)) return false; return true;
                case Kind.Any: foreach (var r in subs) if (r.Met(s)) return true; return subs.Length == 0;
            }
            return true;
        }

        public string Label
        {
            get
            {
                switch (kind)
                {
                    case Kind.Composure: return "Composure " + min;
                    case Kind.Wit: return "Wit " + min;
                    case Kind.Strength: return "Strength " + min;
                    case Kind.Flag: return label ?? key;
                    case Kind.Affection:
                        GameData.Npcs.TryGetValue(key, out NpcDef n);
                        return "สนิทกับ" + (n != null ? n.Name : key) + " " + min;
                    case Kind.Item: return "ต้องมี " + ItemData.NameOf(key);
                    case Kind.Equipped: return "ต้องพก " + ItemData.NameOf(key);
                    case Kind.Counter: return key + " " + min;
                    case Kind.SuspicionMin: return "ความสงสัย " + min + "+";
                    case Kind.SuspicionBelow: return "ความสงสัยต่ำกว่า " + min;
                    case Kind.Not: return "ต้องไม่ใช่: " + subs[0].Label;
                    case Kind.All: case Kind.Any:
                        var parts = new List<string>();
                        foreach (var r in subs) parts.Add(r.Label);
                        return string.Join(kind == Kind.All ? " + " : " / ", parts);
                }
                return "";
            }
        }
    }

    // Conditional jump: the first branch whose condition holds wins; otherwise the default Next is used.
    public class Branch
    {
        public Requirement If;
        public string Next;

        public Branch(Requirement cond, string next) { If = cond; Next = next; }

        public static string Resolve(string defaultNext, List<Branch> branches, GameState s)
        {
            if (branches != null)
                foreach (var b in branches)
                    if (b.If == null || b.If.Met(s)) return b.Next;
            return defaultNext;
        }
    }
}
