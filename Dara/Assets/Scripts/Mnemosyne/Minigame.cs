using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UIImage = UnityEngine.UI.Image;

namespace Mnemosyne
{
    // A minigame stage that a cutscene node can run. Placeholder mechanic: a marker sweeps along a bar,
    // press Space/click while it is inside the green zone. Rounds hit >= PassHits means the stage is passed.
    public class MinigameDef
    {
        public string Id, Title, Hint;
        public int Rounds = 3, PassHits = 2;
        public float Zone = 0.16f;      // width of the target zone (0..1 of the bar)
        public float Speed = 0.9f;      // sweeps per second
        public string BonusItem;        // if carried in Prep Slots, widens the zone by BonusZone
        public float BonusZone = 0.08f;
    }

    public static class MinigameData
    {
        public static readonly Dictionary<string, MinigameDef> All = new Dictionary<string, MinigameDef>();

        static void Add(MinigameDef d) { All[d.Id] = d; }

        static MinigameData()
        {
            Add(new MinigameDef { Id = "hide", Title = "ซ่อนตัว", Hint = "กดตอนเงาผ่านช่องว่าง เพื่อหลบสายตาของมัน",
                Zone = 0.18f, Speed = 0.8f, BonusItem = "distilled_water" });
            Add(new MinigameDef { Id = "run", Title = "วิ่งหนี", Hint = "กดให้ตรงจังหวะก้าว อย่าให้มันตามทัน",
                Zone = 0.14f, Speed = 1.1f, BonusItem = "keycard" });
            Add(new MinigameDef { Id = "stand", Title = "ยืนหยัด", Hint = "ต้านเสียงสะท้อนในหัว กดตอนเสียงเงียบ",
                Zone = 0.12f, Speed = 1.3f, BonusItem = "has_pills" });
        }
    }

    // Builds its own panel; CutscenePlayer owns one and calls Run(). Reports pass/fail through the callback.
    public class MinigamePanel : MonoBehaviour
    {
        enum Stage { Idle, Intro, Moving, Feedback, Result }

        TMP_FontAsset font;
        GameObject panel;
        TextMeshProUGUI title, hint, status;
        RectTransform zone, marker;

        MinigameDef def;
        GameState S;
        Action<bool> done;
        Stage stage = Stage.Idle;
        float stageAt, phase, zoneCenter, zoneWidth;
        int round, hits;

        public bool Active { get { return stage != Stage.Idle; } }

        public void Init(Transform canvasRoot, TMP_FontAsset thaiFont)
        {
            font = thaiFont;
            var p = UiKit.Panel(canvasRoot, "MinigamePanel", new Color(0.02f, 0.04f, 0.04f, 0.97f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel = p.gameObject;

            title = UiKit.Text(p, font, "", 56, TextAlignmentOptions.Center, UiKit.Teal);
            Place(title.rectTransform, 0.1f, 0.72f, 0.9f, 0.88f);
            hint = UiKit.Text(p, font, "", 30, TextAlignmentOptions.Center, UiKit.TextC);
            Place(hint.rectTransform, 0.1f, 0.58f, 0.9f, 0.70f);

            var bar = UiKit.Panel(p, "Bar", new Color(0.12f, 0.18f, 0.18f, 1), new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.49f), Vector2.zero, Vector2.zero);
            zone = UiKit.Panel(bar, "Zone", new Color(0.3f, 0.85f, 0.55f, 0.9f), new Vector2(0.4f, 0), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);
            marker = UiKit.Panel(bar, "Marker", Color.white, new Vector2(0, -0.35f), new Vector2(0.01f, 1.35f), Vector2.zero, Vector2.zero);

            status = UiKit.Text(p, font, "", 36, TextAlignmentOptions.Center, Color.white);
            Place(status.rectTransform, 0.1f, 0.18f, 0.9f, 0.36f);

            panel.SetActive(false);
        }

        static void Place(RectTransform r, float x0, float y0, float x1, float y1)
        {
            r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        public void Run(MinigameDef d, GameState state, Action<bool> onDone)
        {
            def = d; S = state; done = onDone;
            round = 0; hits = 0;
            zoneWidth = d.Zone + (d.BonusItem != null && state.Equipped.Contains(d.BonusItem) ? d.BonusZone : 0f);
            title.text = ThaiShaper.Fix(d.Title);
            string bonus = d.BonusItem != null && state.Equipped.Contains(d.BonusItem) ? "\n(" + ItemData.NameOf(d.BonusItem) + " ช่วยให้ง่ายขึ้น)" : "";
            hint.text = ThaiShaper.Fix(d.Hint + bonus);
            status.text = ThaiShaper.Fix("กด Space หรือคลิกเพื่อเริ่ม");
            panel.SetActive(true);
            SetMarker(0f);
            SetZone(0.5f);
            Enter(Stage.Intro);
        }

        void Enter(Stage s) { stage = s; stageAt = Time.unscaledTime; }

        void SetZone(float center)
        {
            zoneCenter = center;
            zone.anchorMin = new Vector2(Mathf.Clamp01(center - zoneWidth / 2f), 0);
            zone.anchorMax = new Vector2(Mathf.Clamp01(center + zoneWidth / 2f), 1);
        }

        void SetMarker(float x)
        {
            marker.anchorMin = new Vector2(Mathf.Clamp01(x - 0.005f), marker.anchorMin.y);
            marker.anchorMax = new Vector2(Mathf.Clamp01(x + 0.005f), marker.anchorMax.y);
        }

        void StartRound()
        {
            SetZone(UnityEngine.Random.Range(0.2f, 0.8f));
            phase = 0f;
            status.text = ThaiShaper.Fix("รอบ " + (round + 1) + "/" + def.Rounds + "   ถูก " + hits);
            Enter(Stage.Moving);
        }

        void Update()
        {
            if (stage == Stage.Idle) return;
            bool pressed = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            bool guarded = Time.unscaledTime - stageAt < 0.25f;

            switch (stage)
            {
                case Stage.Intro:
                    if (pressed && !guarded) StartRound();
                    break;
                case Stage.Moving:
                    phase += Time.unscaledDeltaTime * def.Speed;
                    float x = Mathf.PingPong(phase, 1f);
                    SetMarker(x);
                    if (pressed && !guarded)
                    {
                        bool hit = Mathf.Abs(x - zoneCenter) <= zoneWidth / 2f;
                        if (hit) hits++;
                        round++;
                        status.text = ThaiShaper.Fix(hit ? "ตรงจังหวะ!" : "พลาด...");
                        Enter(Stage.Feedback);
                    }
                    break;
                case Stage.Feedback:
                    if (Time.unscaledTime - stageAt > 0.7f)
                    {
                        if (round >= def.Rounds)
                        {
                            bool pass = hits >= def.PassHits;
                            status.text = ThaiShaper.Fix((pass ? "ผ่าน" : "ไม่ผ่าน") + "   (" + hits + "/" + def.Rounds + ")\n\nคลิกเพื่อต่อไป");
                            Enter(Stage.Result);
                        }
                        else StartRound();
                    }
                    break;
                case Stage.Result:
                    if (pressed && !guarded) Finish(hits >= def.PassHits);
                    break;
            }
        }

        void Finish(bool pass)
        {
            stage = Stage.Idle;
            panel.SetActive(false);
            var cb = done; done = null;
            if (cb != null) cb(pass);
        }
    }
}
