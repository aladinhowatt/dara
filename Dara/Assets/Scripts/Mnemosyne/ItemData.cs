using System.Collections.Generic;

namespace Mnemosyne
{
    public class ItemDef
    {
        public string Id, Name, Desc;
    }

    // Carry-over objects. Flag convention for the choice map: has_<item> is expressed through GameState.Items.
    public static class ItemData
    {
        public const int PrepSlots = 3;

        public static readonly Dictionary<string, ItemDef> All = new Dictionary<string, ItemDef>();

        static void Add(string id, string name, string desc) { All[id] = new ItemDef { Id = id, Name = name, Desc = desc }; }

        static ItemData()
        {
            Add("has_pin", "เข็มกลัดเงิน", "ของที่ผูกกับความทรงจำ ขุนพลจะเก็บมันไว้");
            Add("has_pills", "ยาของลิน", "ลดผลของเสียงสะท้อน 1 ครั้ง ต้านสารลบความจำ");
            Add("has_nara_drawing", "ภาพวาดนารา", "ภาพทางเดินที่นาราไม่เคยไป");
            Add("distilled_water", "น้ำกลั่น", "ช่วยฟื้น Composure ระหว่างหนี");
            Add("keycard", "คีย์การ์ด", "เปิดประตูที่ปกติล็อกอยู่");
        }

        public static string NameOf(string id)
        {
            ItemDef d;
            return All.TryGetValue(id, out d) ? d.Name : id;
        }
    }
}
