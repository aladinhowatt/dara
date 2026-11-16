using System.Collections.Generic;

namespace Mnemosyne
{
    // Round 1 (Kwin), Monday. Source of truth for the script: MNEMOSYNE_round1_choice_map.md section 2.
    // Video nodes carry the spoken script in Lines as a fallback: it is shown as text when the clip file
    // is not in StreamingAssets yet (CS-M3 / CS-M4 clips still have to be produced).
    public static class CutsceneMonday
    {
        static CutsceneNode Say(string id, string next, params string[] lines)
        {
            return new CutsceneNode { Id = id, Next = next, Lines = lines };
        }

        public static IEnumerable<CutsceneDef> Build()
        {
            yield return M1();
            yield return M2();
            yield return M3();
            yield return M4();
        }

        // CS-M1  Start of Monday: Nara asks the new student's name (the name is fixed: Kwin).
        static CutsceneDef M1()
        {
            var d = new CutsceneDef { Id = "mon_intro_nara", Start = "ask" };
            d.Add(new CutsceneNode
            {
                Id = "ask", Video = "Video/mon_intro_nara.mp4",
                Lines = new[] { "นารา: นายมาใหม่นี่ นายชื่ออะไร" },
                Prompt = "นายมาใหม่นี่ นายชื่ออะไร",
                Choices =
                {
                    new ChoiceDef { Id = "friendly", Text = "“กวิน เรียกกวินก็ได้”", Next = "a",
                        Fx = new Effect().Aff("nara", 1).Flag("told_name") },
                    new ChoiceDef { Id = "look_desk", Text = "“กวิน” แล้วหันไปมองโต๊ะว่างริมหน้าต่าง", Next = "b",
                        Fx = new Effect { Composure = 1 }.Flag("noticed_empty_desk") },
                    new ChoiceDef { Id = "ask_back", Text = "“เธอเคยเจอฉันมาก่อนหรือเปล่า”", Next = "c", Need = Requirement.Wit(3),
                        Fx = new Effect { Codex = "นารารู้สึกว่าเคยคุยกับกวินมาก่อน ทั้งที่เธอไม่ควรจำได้" }.Aff("nara", 1).Flag("asked_nara_before") },
                }
            });
            d.Add(Say("a", null,
                "นารา: กวิน... ชื่อเพราะจัง ฉันจะจำให้ได้นะ",
                "(เธอก้มดูสมุดในอ้อมแขน แล้วเงยขึ้นมาอีกครั้ง)",
                "นารา: แปลกจัง รู้สึกเหมือนเคยพูดประโยคนี้มาแล้ว"));
            d.Add(Say("b", null,
                "นารา: (มองตามสายตากวินไปที่โต๊ะว่าง) โต๊ะนั้นเหรอ ว่างอยู่ตั้งแต่ฉันมา",
                "นารา: แต่ฝุ่นไม่หนาเลยนะ ตลกดีเนอะ",
                "(เธอยิ้ม แต่ไม่กะพริบตา)"));
            d.Add(Say("c", null,
                "(รอยยิ้มของนาราค้างไปครู่หนึ่ง)",
                "นารา: ...เคยเหรอ",
                "นารา: (เบาลง) ฉันก็รู้สึกแบบนั้น แต่คิดว่าเป็นเพราะตื่นเต้นมากกว่า"));
            return d;
        }

        // CS-M2  After the afternoon class, before free period 2: Kwin and Lin in the locker corridor.
        static CutsceneDef M2()
        {
            var d = new CutsceneDef { Id = "mon_corridor", Start = "talk" };
            d.Add(new CutsceneNode
            {
                Id = "talk", Video = "Video/mon_corridor_kwin_lin.mp4",
                Lines = new[]
                {
                    "ลิน: (กระซิบ) กวิน... โต๊ะริมหน้าต่างตัวนั้น เมื่อวานมีคนนั่งอยู่ใช่ไหม",
                    "กวิน: ...ใช่ ฉันก็จำได้",
                },
                Prompt = "กวินจะบอกลินว่าอย่างไร",
                Choices =
                {
                    new ChoiceDef { Id = "investigate", Text = "“เราต้องหาให้ได้ว่าเกิดอะไรขึ้น”", Next = "a",
                        Fx = new Effect().Aff("lin", 1).Flag("investigating") },
                    new ChoiceDef { Id = "secret", Text = "“อย่าบอกใครเรื่องนี้ ฉันกลัวมีคนฟังอยู่”", Next = "b",
                        Fx = new Effect { Suspicion = -3, Composure = 1 }.Flag("secret_pact") },
                    new ChoiceDef { Id = "pills", Text = "“เธอกินยาอะไรอยู่ ทำไมเธอจำได้”", Next = "c", Need = Requirement.Aff("lin", 2),
                        Fx = new Effect { Codex = "ยาของลินดูเหมือนจะทำให้เธอจำได้" }.Aff("lin", 1).Flag("lin_pills_known") },
                }
            });
            d.Add(Say("a", null,
                "ลิน: (พยักหน้าช้า ๆ) ฉันไม่รู้จะเริ่มจากตรงไหน แต่ถ้ามีนายอยู่ด้วย ฉันกล้าขึ้นหน่อย",
                "กวิน: เริ่มจากโต๊ะนั้นก่อน"));
            d.Add(Say("b", null,
                "ลิน: (มองไปรอบทางเดิน) ...นายก็ระแวงเหมือนกันเหรอ",
                "กวิน: ลุงฉันบอกเสมอว่าที่นี่มีหูอยู่ทุกที่ ฉันเพิ่งเริ่มเข้าใจว่าท่านไม่ได้พูดเล่น"));
            d.Add(Say("c", null,
                "ลิน: (กำพวงกุญแจขวดยาแน่น) ...วิตามิน",
                "กวิน: ลิน",
                "ลิน: (เสียงสั่น) หมอบอกว่าห้ามหยุดกิน ฉันเลยไม่เคยหยุด แล้วฉันก็จำเรื่องที่คนอื่นลืมได้ ฉันไม่เคยบอกใครมาก่อน"));
            return d;
        }

        // CS-M3  End of free period 2: Nara invites the group to the ritual.
        static CutsceneDef M3()
        {
            var d = new CutsceneDef { Id = "mon_ritual_invite", Start = "invite" };
            d.Add(new CutsceneNode
            {
                Id = "invite", Video = "Video/mon_ritual_invite.mp4",
                Lines = new[]
                {
                    "นารา: ถ้าอยากรู้ว่าคนที่หายไปคือใคร ฉันรู้วิธีนะ",
                    "ลิน: วิธีอะไร",
                    "นารา: พิธีติดต่ออีกฝั่ง ไม่อันตรายหรอก แค่ถามว่า “เขาคือใคร” แล้วฟังคำตอบ",
                    "ขุนพล: (ขมวดคิ้ว) พิธีแบบนี้ไม่ควรทำโดยไม่รู้ว่าใครเป็นคนสอนเธอ",
                    "นารา: (ยิ้ม) ฉันอ่านเจอ ในห้องสมุด",
                },
                Prompt = "กวินจะไปพิธีหรือไม่",
                Choices =
                {
                    new ChoiceDef { Id = "go", Text = "“ไปสิ ฉันอยากรู้”", Next = "a",
                        Fx = new Effect().Aff("nara", 1).Flag("ritual_eager") },
                    new ChoiceDef { Id = "check_first", Text = "“ได้ แต่ให้ขุนพลตรวจดูก่อน”", Next = "b", Need = Requirement.Aff("khun", 1),
                        Fx = new Effect().Aff("khun", 1).Flag("khun_researching") },
                    new ChoiceDef { Id = "refuse", Text = "“ไม่ไป มันไม่ปลอดภัย”", Next = "c",
                        Fx = new Effect { Suspicion = 3 }.Aff("lin", -1).Aff("nara", -1).Flag("ritual_reluctant") },
                }
            });
            d.Add(Say("a", null,
                "นารา: (ตาเป็นประกาย) ดีเลย คืนนี้นะ ที่อาคารไม้เก่า",
                "ขุนพล: ...ฉันจะไปด้วย แต่ถ้ามีอะไรผิดปกติ หยุดทันที"));
            d.Add(Say("b", null,
                "ขุนพล: (เงียบครู่หนึ่ง แล้วพยักหน้า) ให้เวลาฉันถึงค่ำ ฉันจะเช็กว่าพิธีแบบนี้มีบันทึกที่ไหนบ้าง",
                "นารา: (ยิ้มแข็ง) แน่นอน ฉันไม่รีบ",
                "(สีหน้านาราเปลี่ยนไปเล็กน้อยตอนเอ่ยคำว่า “ไม่รีบ”)"));
            d.Add(Say("c", null,
                "ลิน: ถ้านายไม่ไป ฉันก็ไปคนเดียวได้ ฉันอยากรู้จริง ๆ",
                "กวิน: ...ลิน",
                "ลิน: ขอโทษ ฉันไม่ได้ตั้งใจจะตัดสินใจแทนนาย แต่ฉันจะไม่ถอย",
                "(กวินถอนหายใจ แล้วเดินตามไป ไม่พูดอะไรอีก)"));
            return d;
        }

        // CS-M4  Monday night: the ritual at the old wooden building. Everyone ends up Marked.
        static CutsceneDef M4()
        {
            var d = new CutsceneDef { Id = "mon_ritual_night", Start = "ritual" };
            d.Add(new CutsceneNode
            {
                Id = "ritual", Video = "Video/mon_ritual_night.mp4",
                Lines = new[]
                {
                    "นารา: (เสียงเบา) ทุกคนจับมือกัน คิดถึงคนที่หายไปแล้วถามในใจ “เขาคือใคร”",
                    "(เปลวเทียนเอนไปทางมุมมืด กระจกมือแตกเป็นเส้น เสียงสะท้อนซ้อนเสียงนารา)",
                    "เสียงสะท้อน: ...เขาคือใคร... เขาคือใคร... เขาคือใคร...",
                    "ขุนพล: (สะดุ้ง) หยุด!",
                },
                Prompt = "เสียงสะท้อนรอคำตอบจากกวิน",
                Choices =
                {
                    new ChoiceDef { Id = "truth", Text = "“ฉันไม่รู้ชื่อเขา แต่ฉันจำเขาได้”", Next = "a",
                        Fx = new Effect { Composure = 1, Codex = "พิธีต้องการชื่อ แต่เอนทิตีรับคำถามเป็นคำเรียก" } },
                    new ChoiceDef { Id = "name_him", Text = "ตั้งชื่อให้ตามความรู้สึก", Next = "b", Need = Requirement.Wit(3),
                        Fx = new Effect().Flag("ritual_named") },
                    new ChoiceDef { Id = "hold_lin", Text = "ไม่พูด จับมือลินไว้แน่น", Next = "c",
                        Fx = new Effect().Aff("lin", 2).Flag("held_lin") },
                }
            });
            d.Add(Say("a", "close",
                "กวิน: ฉันไม่รู้ชื่อเขา แต่ฉันจำเขาได้ เขาเคยนั่งโต๊ะริมหน้าต่าง",
                "(เสียงสะท้อนหยุด ทุกคนนิ่งไปชั่วอึดใจ)",
                "นารา: (กระซิบ) ...พอแล้วค่ะ"));
            d.Add(Say("b", "close",
                "กวิน: (ตั้งสติ) ฉันขอเรียกเขาว่า “ผู้ที่นั่งริมหน้าต่าง”",
                "(เสียงสะท้อนทวนประโยคนั้นหลายครั้ง ช้าลงเรื่อย ๆ จนกลายเป็นเสียงกระซิบ)",
                "ขุนพล: (แผ่วเบา) ทำไมมันถึงทวนตามนาย..."));
            d.Add(Say("c", "close",
                "(มือของลินเย็นเฉียบ เธอบีบตอบแรง ๆ)",
                "ลิน: (กระซิบ) กวิน ฉันอยู่ตรงนี้",
                "กวิน: ฉันก็อยู่"));
            d.Add(new CutsceneNode
            {
                Id = "close", Video = "Video/mon_marked.mp4", Next = "marked",
                Lines = new[]
                {
                    "(เทียนดับพร้อมกันทุกเล่ม ข้อมือซ้ายของกวินและลินมีรอยสีซีดประทับ นาราจ้องมองรอยนั้นแล้วยิ้มเบา ๆ)",
                    "นารา: ขอบคุณที่มานะ",
                    "ขุนพล: นารา เธอ...",
                    "(นาราเดินออกจากวงเทียนโดยไม่หันกลับมา)",
                },
            });
            d.Add(new CutsceneNode
            {
                Id = "marked", Title = "ตีตราแล้ว",
                Lines = new[] { "รอยจาง ๆ บนข้อมือซ้ายยังไม่บอกอะไรคุณ" },
                Fx = new Effect().Flag("marked"),
            });
            return d;
        }
    }
}
