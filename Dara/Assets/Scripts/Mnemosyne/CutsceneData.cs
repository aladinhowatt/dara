using System.Collections.Generic;

namespace Mnemosyne
{
    // One selectable answer. Next = id of the node to continue with (null = the cutscene ends).
    // Branches are checked first (in order); the first whose condition holds overrides Next.
    public class ChoiceDef
    {
        public string Id, Text, Next;
        public Effect Fx = new Effect();
        public Requirement Need;
        public List<Branch> Branches;
    }

    // One beat of a cutscene. What it shows, in order of priority:
    // - Minigame  : runs MinigameData[Minigame]; on pass applies PassFx, otherwise FailFx; adds to Counter if set.
    // - PrepSlots : the item-loadout screen (pick up to PrepSlots owned items to carry into the night).
    // - Video     : path relative to StreamingAssets, e.g. "Video/nara_intro.mp4".
    // - Title/Lines : a text card (a node with neither video nor lines nor choices is a silent "gate" that only routes).
    // After it: Choices (if any), else the node continues to Branches/Next (null = the cutscene ends).
    // - Fx      : applied once when the node is entered.
    // - PauseAt : seconds into the clip at which the choices appear (-1 = when the clip ends).
    public class CutsceneNode
    {
        public string Id, Video, Prompt, Next, Title;
        public string[] Lines;
        public float PauseAt = -1f;
        public Effect Fx;
        public int PrepSlots;
        public string Minigame, Counter;
        public Effect PassFx, FailFx;
        public List<Branch> Branches;
        public List<ChoiceDef> Choices = new List<ChoiceDef>();
    }

    public class CutsceneDef
    {
        public string Id, Start;
        public readonly Dictionary<string, CutsceneNode> Nodes = new Dictionary<string, CutsceneNode>();

        public CutsceneDef Add(CutsceneNode n)
        {
            for (int i = 0; i < n.Choices.Count; i++)
                if (string.IsNullOrEmpty(n.Choices[i].Id)) n.Choices[i].Id = "c" + (i + 1);
            Nodes[n.Id] = n;
            return this;
        }
    }

    // All cutscenes of the game. Placeholder content: the Thai lines and effects below are a vertical
    // slice that proves the system works, not final story text.
    public static class CutsceneData
    {
        public static readonly Dictionary<string, CutsceneDef> All = new Dictionary<string, CutsceneDef>();

        static CutsceneData()
        {
            var intro = new CutsceneDef { Id = "intro_nara", Start = "greet" };

            intro.Add(new CutsceneNode
            {
                Id = "greet",
                Video = "Video/nara_intro.mp4",
                Prompt = "แล้วเธอล่ะ ชื่ออะไร?",
                Choices =
                {
                    new ChoiceDef { Id = "name", Text = "ตอบว่า “กอฟ”", Next = "after_name",
                        Fx = new Effect { Composure = 0 }.Aff("nara", 1).Flag("told_name") },
                    new ChoiceDef { Id = "silent", Text = "นิ่งเงียบ ไม่ตอบ", Next = "after_silent",
                        Fx = new Effect { Suspicion = 5, Composure = 1 }.Flag("stayed_silent") },
                    new ChoiceDef { Id = "askback", Text = "ถามกลับว่า เธอเคยเจอฉันมาก่อนหรือเปล่า?", Next = "after_ask",
                        Need = Requirement.Wit(3),
                        Fx = new Effect { Wit = 1, Codex = "นารารู้สึกว่าเคยคุยกับคุณมาก่อน ทั้งที่เธอไม่ควรจำได้" }.Aff("nara", 1).Flag("asked_nara") },
                }
            });

            intro.Add(new CutsceneNode { Id = "after_name", Video = "Video/nara_after_name.mp4" });

            intro.Add(new CutsceneNode
            {
                Id = "after_silent",
                Lines = new[]
                {
                    "นาราเอียงหัว มองคุณอยู่ครู่หนึ่ง",
                    "“ไม่เป็นไร ไว้ค่อยบอกก็ได้”",
                    "เธอยิ้ม แต่ไม่กะพริบตา",
                }
            });

            intro.Add(new CutsceneNode
            {
                Id = "after_ask",
                Lines = new[]
                {
                    "รอยยิ้มของนาราค้างไปครู่หนึ่ง",
                    "“...แปลกจัง ฉันก็รู้สึกแบบนั้นเหมือนกัน”",
                }
            });

            All[intro.Id] = intro;

            All["engine_demo"] = BuildEngineDemo();
        }

        // Exercises every engine feature without video: items, prep slots, minigame stages with a counter,
        // conditional branches and a text card. Not part of the story flow; play it by adding
        // SegmentDef.Cutscene("engine_demo") to FlowData while testing.
        static CutsceneDef BuildEngineDemo()
        {
            var d = new CutsceneDef { Id = "engine_demo", Start = "give" };

            d.Add(new CutsceneNode
            {
                Id = "give", Title = "ทดสอบเอนจิน",
                Lines = new[] { "คุณได้รับของสามชิ้นก่อนคืนวันศุกร์" },
                Fx = new Effect().Give("has_pills", "distilled_water", "keycard").Flag("demo_started"),
                Next = "prep",
            });
            d.Add(new CutsceneNode { Id = "prep", PrepSlots = ItemData.PrepSlots, Next = "stage1" });
            d.Add(new CutsceneNode { Id = "stage1", Minigame = "hide", Counter = "survival", Next = "stage2" });
            d.Add(new CutsceneNode { Id = "stage2", Minigame = "run", Counter = "survival", Next = "stage3" });
            d.Add(new CutsceneNode
            {
                Id = "stage3", Minigame = "stand", Counter = "survival",
                Next = "end_low",
                Branches = new List<Branch>
                {
                    new Branch(Requirement.Counter("survival", 3), "end_full"),
                    new Branch(Requirement.Counter("survival", 2), "end_mid"),
                },
            });
            d.Add(new CutsceneNode { Id = "end_full", Lines = new[] { "ผ่านครบทั้งสามด่าน", "ขุนพลหนีออกไปพร้อมของทุกชิ้น" } });
            d.Add(new CutsceneNode { Id = "end_mid", Lines = new[] { "ผ่านสองด่าน", "ขุนพลหนีออกไปได้ แต่เสียของไปหนึ่งชิ้น" } });
            d.Add(new CutsceneNode { Id = "end_low", Lines = new[] { "ผ่านไม่ถึงสองด่าน", "ขุนพลหนีออกไปด้วยมือเปล่า" } });
            return d;
        }
    }
}
