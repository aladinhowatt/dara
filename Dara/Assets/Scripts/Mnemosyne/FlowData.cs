using System.Collections.Generic;

namespace Mnemosyne
{
    public enum SegmentKind { Cutscene, Day, Night }

    // One step of a chapter. GameController runs the list in order.
    // - Cutscene : video + choices that change the game state
    // - Day      : morning class, free period 1, afternoon class, free period 2 (AP/time loop). Ends at nightfall.
    // - Night    : the night of a day. Plays NightCutsceneId if given, otherwise shows the built-in night summary.
    public class SegmentDef
    {
        public SegmentKind Kind;
        public string CutsceneId;
        public int Day;

        public static SegmentDef Cutscene(string id) { return new SegmentDef { Kind = SegmentKind.Cutscene, CutsceneId = id }; }
        public static SegmentDef DayLoop(int day) { return new SegmentDef { Kind = SegmentKind.Day, Day = day }; }

        // Cutscenes that play in the middle of a day, right before the given phase begins.
        public Dictionary<Phase, string> PhaseCutscenes;
        public SegmentDef Before(Phase phase, string cutsceneId)
        {
            if (PhaseCutscenes == null) PhaseCutscenes = new Dictionary<Phase, string>();
            PhaseCutscenes[phase] = cutsceneId;
            return this;
        }
        public static SegmentDef Night(int day, string cutsceneId = null) { return new SegmentDef { Kind = SegmentKind.Night, Day = day, CutsceneId = cutsceneId }; }
    }

    public static class FlowData
    {
        // To insert a cutscene between segments, add SegmentDef.Cutscene("id") at that spot
        // and register the cutscene in CutsceneData.
        public static readonly List<SegmentDef> Chapter1 = new List<SegmentDef>
        {
            // Monday
            SegmentDef.Cutscene("mon_intro_nara"),
            SegmentDef.DayLoop(1).Before(Phase.Free2, "mon_corridor"),
            SegmentDef.Cutscene("mon_ritual_invite"),
            SegmentDef.Night(1, "mon_ritual_night"),
            SegmentDef.DayLoop(2), SegmentDef.Night(2),
            SegmentDef.DayLoop(3), SegmentDef.Night(3),
            SegmentDef.DayLoop(4), SegmentDef.Night(4),
            SegmentDef.DayLoop(5), SegmentDef.Night(5),
        };
    }
}
