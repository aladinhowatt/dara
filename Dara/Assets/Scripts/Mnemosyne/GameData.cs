using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mnemosyne
{
    public enum Phase { MorningClass, Free1, AfternoonClass, Free2, Night }
    public enum ObjKind { Explore, Rest }

    public class ObjectDef
    {
        public string Id, Name, Desc, Codex, Locked;
        public Vector2 Pos, Size;
        public ObjKind Kind = ObjKind.Explore;
        public int Time = 1, Ap = 1, ApGain = 0, NeedWit = 0, Suspicion = 0;
    }

    public class NpcDef
    {
        public string Id, Name;
        public bool Female;
        public string First;
        public string[] Repeat;
    }

    public class LocationDef
    {
        public string Id, Name;
        public Vector2 Size;
        // Optional Resources path (e.g. "Mnemosyne/classroom_topdown") of a top-down painted map.
        // When present it replaces the blockout floor/walls/decor; Size must match the image aspect.
        public string Background;
        public int TravelTime = 1;
        public bool Free2Only, Closed;
        public string ClosedReason;
        public List<ObjectDef> Objects = new List<ObjectDef>();
        public List<Vector4> Decor = new List<Vector4>(); // x,y,w,h
        public string[] NpcPoses; // per slot: "stand" (default) or "sit"
        public Vector2[] NpcSlots = { new Vector2(-2, 0), new Vector2(1, -2), new Vector2(4, -1), new Vector2(-5, 2.5f) };
    }

    public static class GameData
    {
        public static readonly string[] LocationOrder = { "classroom", "canteen", "library", "infirmary", "rooftop", "dorm", "staff" };
        public static readonly Dictionary<string, LocationDef> Locations = new Dictionary<string, LocationDef>();
        public static readonly Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();
        static readonly string[] NpcOrder = { "nara", "mei", "lin", "khun", "pim", "ton" };

        static GameData()
        {
            var classroom = new LocationDef { Id = "classroom", Name = "ห้องเรียน ม.5/3", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/classroom_cctv", TravelTime = 0 };
            // Positions below are matched to the painted map (4x4 desk grid, board on the north wall, calendar on the east wall).
            classroom.NpcSlots = new[] { new Vector2(0.23f, 0.34f), new Vector2(1.76f, -1.66f), new Vector2(2.33f, -0.59f), new Vector2(-1.86f, -0.68f) };
            classroom.NpcPoses = new[] { "stand", "sit", "stand", "sit" };
            classroom.Objects.Add(new ObjectDef { Id = "desk", Name = "โต๊ะของฉัน", Kind = ObjKind.Rest, Pos = new Vector2(-4.24f, -1.42f), Size = new Vector2(2.17f, 1.8f), Time = 2, Ap = 0, ApGain = 2, Desc = "คุณฟุบหน้าลงกับโต๊ะริมหน้าต่าง หมอกด้านนอกแนบติดกระจก" });
            classroom.Objects.Add(new ObjectDef { Id = "emptydesk", Name = "โต๊ะว่าง", Pos = new Vector2(1.93f, -0.39f), Size = new Vector2(1.66f, 1.06f), Time = 1, Ap = 1, Codex = "นักเรียนไม่มีใครสนใจโต๊ะว่าง แต่ครูทำเครื่องหมายไว้ทุกตัว", Desc = "โต๊ะที่ไม่มีชื่อใครติดอยู่ ฝุ่นบนโต๊ะเพิ่งถูกเช็ดไปไม่นาน" });
            classroom.Objects.Add(new ObjectDef { Id = "calendar", Name = "ปฏิทินบนผนัง", Pos = new Vector2(4.91f, 2.73f), Size = new Vector2(0.61f, 1.36f), Time = 1, Ap = 1, Codex = "ปฏิทินถูกขีดทับซ้ำแล้วซ้ำเล่า", Desc = "รอยปากกาแดงทับกันหลายชั้น ขีดฆ่าสัปดาห์เดิมเป็นครั้งแล้วครั้งเล่า" });
            classroom.Objects.Add(new ObjectDef { Id = "board", Name = "กระดานดำ", Pos = new Vector2(1.34f, 3.08f), Size = new Vector2(5.12f, 1.37f), Time = 1, Ap = 0, Desc = "เนื้อหาคาบเมื่อกี้ยังอยู่บนกระดาน ใต้ตัวหนังสือมีรอยชอล์กจางๆ เขียนว่า 'ใครหายไป?'" });
            Add(classroom);

            var canteen = new LocationDef { Id = "canteen", Name = "โรงอาหาร", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/canteen_cctv" };
            canteen.Objects.Add(new ObjectDef { Id = "counter", Name = "เคาน์เตอร์อาหาร", Pos = new Vector2(0.9f, 1.54f), Size = new Vector2(6.38f, 2.13f), Time = 1, Ap = 1, Desc = "แม่ครัวเรียกชื่อทุกคนได้ ยกเว้นเก้าอี้ที่ว่างอยู่" });
            canteen.Objects.Add(new ObjectDef { Id = "tank", Name = "ถังน้ำดื่ม", Pos = new Vector2(5.56f, 1.81f), Size = new Vector2(1.6f, 3.46f), Time = 1, Ap = 1, Suspicion = 5, Codex = "น้ำดื่มทั้งโรงเรียนมาจากถังที่โรงอาหาร", Desc = "ถังสเตนเลสขนาดใหญ่ น้ำจากก๊อกมีรสหวานจางๆ เจ้าหน้าที่คอยดูว่าใครดื่มและใครไม่ดื่ม" });
            canteen.Objects.Add(new ObjectDef { Id = "notice", Name = "ป้ายประกาศ", Pos = new Vector2(7.91f, -0.32f), Size = new Vector2(2.05f, 5.32f), Time = 1, Ap = 1, Desc = "ใบปลิวชมรมต่างๆ และประกาศใหม่: 'ตรวจคุณภาพน้ำ เย็นวันพุธ'" });
            canteen.Decor.Add(new Vector4(-3.6f, 1.8f, 4, 1)); canteen.Decor.Add(new Vector4(1.4f, 1.8f, 4, 1));
            canteen.Decor.Add(new Vector4(-3.6f, -0.4f, 4, 1)); canteen.Decor.Add(new Vector4(1.4f, -0.4f, 4, 1));
            canteen.Decor.Add(new Vector4(-3.6f, -2.6f, 4, 1)); canteen.Decor.Add(new Vector4(1.4f, -2.6f, 4, 1));
            canteen.NpcSlots = new[] { new Vector2(-0.69f, 0.0f), new Vector2(4.36f, -1.65f), new Vector2(3.43f, -3.38f), new Vector2(-5.61f, -3.64f) };
            Add(canteen);

            var library = new LocationDef { Id = "library", Name = "ห้องสมุด", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/library_cctv" };
            library.Objects.Add(new ObjectDef { Id = "shelves", Name = "ชั้นหนังสือเก่า", Pos = new Vector2(2.63f, 2.34f), Size = new Vector2(7.18f, 3.99f), Time = 2, Ap = 2, Codex = "สมุดรุ่นเก่าถูกฉีกหน้าปกออกทุกเล่ม", Desc = "สมุดรุ่นของปีก่อนๆ หลายหน้าถูกตัดออกอย่างเรียบร้อย" });
            library.Objects.Add(new ObjectDef { Id = "registry", Name = "ทะเบียนนักเรียน", Pos = new Vector2(3.53f, -2.18f), Size = new Vector2(5.39f, 4.26f), Time = 2, Ap = 2, NeedWit = 3, Suspicion = 5, Codex = "ทะเบียนมีชื่อนักเรียนที่ไม่มีใครจำได้", Desc = "คุณถอดรหัสแฟ้มได้ มีชื่อหลายชื่อที่มีเครื่องหมายสีแดงกำกับอยู่ข้างๆ" });
            library.Objects.Add(new ObjectDef { Id = "cabinet", Name = "ตู้ล็อก", Pos = new Vector2(7.35f, 0.74f), Size = new Vector2(1.99f, 4.26f), Locked = "ล็อกแน่น ต้องมีกุญแจ" });
            library.Decor.Add(new Vector4(-2, -0.5f, 5, 1));
            library.NpcSlots = new[] { new Vector2(0.37f, -1.91f), new Vector2(-2.95f, -3.51f), new Vector2(-4.95f, -2.18f), new Vector2(-6.54f, -3.51f) };
            Add(library);

            var infirmary = new LocationDef { Id = "infirmary", Name = "ห้องพยาบาล", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/infirmary_cctv" };
            infirmary.Objects.Add(new ObjectDef { Id = "meds", Name = "ตู้ยา", Pos = new Vector2(-6.61f, -0.19f), Size = new Vector2(4.12f, 5.59f), Time = 1, Ap = 1, Codex = "ยาชนิดหนึ่งถูกสต็อกไว้มากเกินความจำเป็น", Desc = "ขวดยาหน้าตาเหมือนกันเรียงเป็นแถว มีขวดหนึ่งเขียนฉลากด้วยลายมือ ลงชื่อ 'ล'" });
            infirmary.Objects.Add(new ObjectDef { Id = "bed", Name = "เตียงพยาบาล", Kind = ObjKind.Rest, Pos = new Vector2(5.03f, -1.98f), Size = new Vector2(3.99f, 4.65f), Time = 2, Ap = 0, ApGain = 3, Desc = "พยาบาลแกล้งทำเป็นไม่เห็น คุณหลับสนิท" });
            infirmary.Decor.Add(new Vector4(0, 3, 2, 1));
            infirmary.NpcSlots = new[] { new Vector2(-1.49f, -2.45f), new Vector2(0.37f, -1.65f), new Vector2(-2.95f, -3.64f), new Vector2(1.44f, -3.51f) };
            Add(infirmary);

            var rooftop = new LocationDef { Id = "rooftop", Name = "ดาดฟ้า", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/rooftop_cctv", Free2Only = true };
            rooftop.Objects.Add(new ObjectDef { Id = "rail", Name = "ราวกั้น", Pos = new Vector2(0.57f, 1.41f), Size = new Vector2(11.04f, 1.86f), Time = 1, Ap = 1, Desc = "หมอกเต็มหุบเขา โรงเรียนดูเหมือนลอยอยู่กลางอากาศ" });
            rooftop.Objects.Add(new ObjectDef { Id = "roofank", Name = "ถังน้ำบนดาดฟ้า", Pos = new Vector2(-6.41f, 0.74f), Size = new Vector2(5.05f, 5.85f), Time = 1, Ap = 1, Codex = "ท่อจากดาดฟ้าวิ่งลงไปใต้ดิน ต่ำกว่าชั้นใต้ถุน", Desc = "ท่อจากถังวิ่งเข้าอาคาร แล้วทอดลงไปข้างล่างเรื่อยๆ" });
            rooftop.NpcSlots = new[] { new Vector2(-0.96f, -1.38f), new Vector2(2.37f, -0.72f), new Vector2(0.37f, -3.64f), new Vector2(4.36f, -2.45f) };
            Add(rooftop);

            var dorm = new LocationDef { Id = "dorm", Name = "หอพัก", Size = new Vector2(17.87f, 10f), Background = "Mnemosyne/dorm_cctv", Free2Only = true };
            dorm.Objects.Add(new ObjectDef { Id = "bed", Name = "เตียงของคุณ", Kind = ObjKind.Rest, Pos = new Vector2(-6.54f, -0.98f), Size = new Vector2(4.79f, 7.18f), Time = 2, Ap = 0, ApGain = 2, Desc = "คุณนอนลง ไฟทางเดินส่งเสียงหึ่งเบาๆ" });
            dorm.Objects.Add(new ObjectDef { Id = "shelf", Name = "ชั้นวางของรวม", Pos = new Vector2(5.85f, 0.15f), Size = new Vector2(6.17f, 6.78f), Time = 1, Ap = 1, Desc = "น้ำขวด บะหมี่สำเร็จรูป และภาพสเกตช์ที่วาดค้างไว้ครึ่งหนึ่ง" });
            dorm.NpcSlots = new[] { new Vector2(-2.02f, -0.98f), new Vector2(0.37f, -2.45f), new Vector2(-1.49f, 1.01f), new Vector2(1.7f, -0.59f) };
            Add(dorm);

            var staff = new LocationDef { Id = "staff", Name = "ห้องพักครู", Size = new Vector2(12, 8), Free2Only = false, Closed = true, ClosedReason = "ปิดอยู่ ครูกำลังประชุม" };
            Add(staff);

            Npcs["nara"] = new NpcDef { Id = "nara", Female = true, Name = "นารา", First = "เฮ้! เธอใช่ไหมที่เป็นคนใหม่ ฉันชื่อนารา เป็นเด็กย้ายมาเหมือนกัน... ก็ประมาณนั้นนะ", Repeat = new[] { "เธอเคยรู้สึกบ้างไหมว่าวันนี้เหมือนเคยผ่านมาแล้ว?", "ฉันวาดทางเดินเดิมซ้ำๆ ตลกดีเนอะ", "...ขอโทษ ฉันลืมว่ากำลังพูดอะไรอยู่" } };
            Npcs["mei"] = new NpcDef { Id = "mei", Female = true, Name = "เหมย", First = "อ๋อ สวัสดี ฉันเหมย อย่าทำตัวเด่นนักล่ะ ที่นี่", Repeat = new[] { "น้ำช่วงนี้รสแปลกๆ หรือว่าเป็นที่ฉันเอง", "อย่าไปถามครูเรื่องโต๊ะว่างนะ" } };
            Npcs["lin"] = new NpcDef { Id = "lin", Female = true, Name = "ลิน", First = "ลินนะ ชมรมศิลปะ อย่าสนใจยาที่ฉันกินเลย แค่วิตามิน", Repeat = new[] { "วิตามิน จริงๆ นะ", "เธอลองดูปฏิทินดีๆ หรือยัง?" } };
            Npcs["khun"] = new NpcDef { Id = "khun", Name = "ขุนพล", First = "...เธอไม่ควรมาอยู่ตรงนี้ นี่เป็นเรื่องของสภานักเรียน", Repeat = new[] { "อย่าดื่มน้ำก๊อกเด็ดขาด ลืมที่ฉันพูดไปซะ" } };
            Npcs["pim"] = new NpcDef { Id = "pim", Female = true, Name = "พิม", First = "พิมเอง แม่ครัวรู้จักชื่อทุกคนเลยนะ ยกเว้นโต๊ะที่ว่างอยู่", Repeat = new[] { "เมนูเดิมทุกวันเลยเนอะ" } };
            Npcs["ton"] = new NpcDef { Id = "ton", Name = "ต้น", First = "ต้นนะ อย่าถามครูเรื่องคนที่หายไป เชื่อฉัน", Repeat = new[] { "..." } };
        }

        static void Add(LocationDef l) { Locations[l.Id] = l; }

        public static string Where(string id, int day, Phase ph)
        {
            if (ph == Phase.Free1)
                switch (id)
                {
                    case "nara": return day == 3 ? "canteen" : "classroom";
                    case "mei": return "canteen";
                    case "lin": return "classroom";
                    case "khun": return "library";
                    case "pim": return "canteen";
                    case "ton": return "classroom";
                }
            if (ph == Phase.Free2)
                switch (id)
                {
                    case "nara": return day == 2 ? "dorm" : "rooftop";
                    case "mei": return "library";
                    case "lin": return "infirmary";
                    case "khun": return day % 2 == 0 ? "rooftop" : "library";
                    case "pim": return "classroom";
                    case "ton": return "canteen";
                }
            return null;
        }

        public static List<string> NpcsAt(string loc, int day, Phase ph)
        {
            return NpcOrder.Where(n => Where(n, day, ph) == loc).ToList();
        }
    }
}
