using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Video;
using UIImage = UnityEngine.UI.Image;

namespace Mnemosyne
{
    public class Hotspot : MonoBehaviour
    {
        public ObjectDef Obj;
        public string NpcId;
        public Renderer R;
        public Color Base;
    }

    public class GameController : MonoBehaviour
    {
        enum Mode { None, Talk, Explore }

        [SerializeField] TMP_FontAsset thaiFont;

        GameState S = new GameState();
        Camera cam;
        Transform world;
        Shader unlit;
        Mode mode = Mode.None;
        bool modal;
        Hotspot hover;
        readonly List<Hotspot> hotspots = new List<Hotspot>();
        readonly List<Button> cmdButtons = new List<Button>();

        TextMeshProUGUI hud, hint, msgText, msgBtnLabel, locTitle;
        GameObject msgPanel, locPanel;
        Button msgBtn;
        Transform locList;
        Action onMsgClose;

        GameObject titlePanel, topGo, hintGo, barGo;
        CutscenePlayer cutscene;
        int flowIndex;

        GameObject chapterPanel;
        TextMeshProUGUI chapterTitle, chapterBody, chapterHint;
        int chapterState; // 0 off, 1 typing, 2 finished
        float chapterT;
        int chapterTotal;
        const string ChapterTitleText = "Chapter 1 : กวิน";
        const string ChapterBodyText =
            "โรงเรียนประจำดาราวลัย\nกลางหุบเขาที่หมอกไม่เคยจางหาย\n\n" +
            "ทุกคนที่นี่บอกว่าที่นี่ปลอดภัย\nทุกคนที่นี่บอกว่าไม่มีใครหายไป\n\n" +
            "แต่ทำไม โต๊ะริมหน้าต่างตัวนั้นถึงว่างอยู่เสมอ\n\n" +
            "วันเปิดเทอมวันหนึ่ง กวินกำลังจะได้รู้\nว่าคำถามนั้นมีราคาที่ต้องจ่าย";

        static readonly Color Teal = new Color(0.45f, 0.95f, 0.85f);
        static readonly Color Amber = new Color(0.93f, 0.72f, 0.32f);
        static readonly Color TextC = new Color(0.85f, 0.92f, 0.9f);

        LocationDef Loc { get { return GameData.Locations[S.Location]; } }

        public GameState State { get { return S; } }

        void Awake()
        {
            if (thaiFont == null) thaiFont = Resources.Load<TMP_FontAsset>("NotoSansThai SDF");
            if (thaiFont == null) Debug.LogWarning("GameController: thaiFont is not assigned. Thai text will not render.");
            WarmUpFont();
            unlit = Shader.Find("Universal Render Pipeline/Lit");
            if (unlit == null) unlit = Shader.Find("Standard");
            if (FindFirstObjectByType<Light>() == null)
            {
                var lg = new GameObject("Directional Light");
                var l = lg.AddComponent<Light>();
                l.type = LightType.Directional;
                lg.transform.rotation = Quaternion.Euler(70, -20, 0);
            }
            SetupCamera();
            SetupEventSystem();
            BuildUI();
        }

        void Start() { ShowTitle(); }

        // TMP loads OpenType mark-positioning data lazily, so make sure every Thai glyph is in the
        // dynamic atlas and has been laid out once before any real text is shown.
        void WarmUpFont()
        {
            if (thaiFont == null) return;
            var sb = new System.Text.StringBuilder();
            for (char c = 'ก'; c <= '๛'; c++) sb.Append(c);
            for (char c = ''; c <= ''; c++) sb.Append(c);
            for (char c = ''; c <= ''; c++) sb.Append(c);
            for (char c = ' '; c <= '~'; c++) sb.Append(c);
            sb.Append("“”…");
            string missing;
            thaiFont.TryAddCharacters(sb.ToString(), out missing);
            var go = new GameObject("FontWarmup");
            go.transform.position = new Vector3(0, -1000, 0);
            var t = go.AddComponent<TextMeshPro>();
            t.font = thaiFont;
            ApplyFeatures(t);
            t.text = ThaiShaper.Fix("ที่นี่ตั้งซึ่งป่าผู้โต๊ะน้ำ");
            t.ForceMeshUpdate(true, true);
            t.ForceMeshUpdate(true, true);
            go.SetActive(false);
            Destroy(go);
        }

        // --------------------------------------------------------- title / intro

        void SetGameUi(bool on)
        {
            topGo.SetActive(on); hintGo.SetActive(on); barGo.SetActive(on);
        }

        void ShowTitle()
        {
            SetGameUi(false);
            chapterPanel.SetActive(false);
            titlePanel.SetActive(true);
        }

        void OnStartPressed()
        {
            titlePanel.SetActive(false);
            ShowChapter();
        }

        // ------------------------------------------------------- chapter intro

        void ShowChapter()
        {
            chapterPanel.SetActive(true);
            chapterState = 1;
            chapterT = 0f;
            chapterTitle.text = ChapterTitleText;
            chapterTitle.alpha = 0f;
            chapterBody.text = ThaiShaper.Fix(ChapterBodyText);
            chapterBody.ForceMeshUpdate();
            chapterTotal = chapterBody.textInfo.characterCount;
            chapterBody.maxVisibleCharacters = 0;
            chapterHint.gameObject.SetActive(false);
        }

        void UpdateChapter(bool pressed)
        {
            chapterT += Time.unscaledDeltaTime;
            chapterTitle.alpha = Mathf.Clamp01(chapterT / 1.5f);
            bool skip = pressed && chapterT > 0.3f;
            if (chapterState == 1)
            {
                int typed = (int)(Mathf.Max(0f, chapterT - 1.8f) * 24f);
                chapterBody.maxVisibleCharacters = typed;
                if (skip || typed >= chapterTotal)
                {
                    chapterBody.maxVisibleCharacters = 99999;
                    chapterTitle.alpha = 1f;
                    chapterState = 2;
                    chapterHint.gameObject.SetActive(true);
                }
            }
            else if (chapterState == 2 && skip)
            {
                EndChapter();
            }
        }

        void EndChapter()
        {
            chapterState = 0;
            chapterPanel.SetActive(false);
            StartFlow();
        }

        // ---------------------------------------------------------------- flow

        // A chapter is an ordered list of segments (FlowData): cutscenes (video + choices that change
        // the game state) and day loops (classes, free periods spending AP/time, night).
        void StartFlow()
        {
            flowIndex = -1;
            NextSegment();
        }

        bool IsLastSegment { get { return flowIndex >= FlowData.Chapter1.Count - 1; } }

        void NextSegment()
        {
            flowIndex++;
            if (flowIndex >= FlowData.Chapter1.Count)
            {
                S = new GameState();
                StartFlow();
                return;
            }
            var seg = FlowData.Chapter1[flowIndex];
            if (seg.Kind != SegmentKind.Day && !string.IsNullOrEmpty(seg.CutsceneId))
            {
                // Cutscene segment, or a Night segment that plays its own cutscene.
                if (seg.Kind == SegmentKind.Night) S.Day = seg.Day;
                SetGameUi(false);
                ClearWorld();
                modal = false;
                cutscene.Play(CutsceneData.All[seg.CutsceneId], S, NextSegment);
            }
            else if (seg.Kind == SegmentKind.Night)
            {
                // Built-in night summary.
                S.Day = seg.Day;
                S.Phase = Phase.Night;
                SetGameUi(true);
                ShowPhase();
            }
            else
            {
                S.Day = seg.Day;
                SetGameUi(true);
                StartDay();
            }
        }

        void StartDay()
        {
            S.Ap = Mathf.Max(1, GameState.MaxAp - S.NextDayApPenalty);
            S.NextDayApPenalty = 0;
            S.Phase = Phase.MorningClass;
            EnterPhase();
        }

        void NextPhase()
        {
            S.Phase = (Phase)((int)S.Phase + 1);
            EnterPhase();
        }

        // Plays the cutscene attached to this phase of the current day (SegmentDef.Before), then shows the phase.
        void EnterPhase()
        {
            var seg = FlowData.Chapter1[flowIndex];
            string id;
            if (seg.Kind == SegmentKind.Day && seg.PhaseCutscenes != null && seg.PhaseCutscenes.TryGetValue(S.Phase, out id))
            {
                SetGameUi(false);
                ClearWorld();
                modal = false;
                cutscene.Play(CutsceneData.All[id], S, () => { SetGameUi(true); ShowPhase(); });
            }
            else ShowPhase();
        }

        void ShowPhase()
        {
            mode = Mode.None;
            string day = GameState.DayNames[S.Day];
            switch (S.Phase)
            {
                case Phase.MorningClass:
                    ClearWorld();
                    Refresh();
                    ShowMsg(day + " - คาบเช้า\n\nครูเช็คชื่อ ปากกาของเธอหยุดที่โต๊ะว่างตัวหนึ่ง แล้วขีดเส้นสีแดงลงไป", "ต่อไป", NextPhase);
                    break;
                case Phase.AfternoonClass:
                    ClearWorld();
                    Refresh();
                    ShowMsg(day + " - คาบบ่าย\n\nบทเรียนยืดเยื้อ ข้างนอกหมอกหนาขึ้นเรื่อยๆ", "ต่อไป", NextPhase);
                    break;
                case Phase.Free1:
                case Phase.Free2:
                    S.Time = GameState.FreeTime;
                    S.Location = "classroom";
                    BuildWorld();
                    Refresh();
                    break;
                case Phase.Night:
                    // Reached by finishing a Day segment: the day loop ends here, the next segment
                    // (usually the Night segment of this day) takes over.
                    if (FlowData.Chapter1[flowIndex].Kind == SegmentKind.Day) { ClearWorld(); NextSegment(); break; }
                    ClearWorld();
                    Refresh();
                    ShowMsg(NightText(), IsLastSegment ? "เริ่มใหม่" : "วันถัดไป", () =>
                    {
                        if (IsLastSegment) { S = new GameState(); StartFlow(); }
                        else NextSegment();
                    });
                    break;
            }
        }

        string NightText()
        {
            string head = "กลางคืน" + GameState.DayNames[S.Day] + "\n\n";
            switch (S.Day)
            {
                case 1:
                    return head + "นาราพาคุณไปที่อาคารไม้เก่า วงเทียนและบทสวดแปลกๆ เมื่อพิธีจบลง บางอย่างในอกของคุณบีบแน่นขึ้น\n\nคุณถูกตีตราแล้ว เหลือเวลาถึงวันศุกร์";
                case 2:
                    if (S.Aff("nara") >= 1)
                    {
                        S.AddCodex("นาราจำรอบก่อนไม่ได้ แต่ภาพวาดของเธอจำได้");
                        return head + "ที่โถงหอพัก นาราวาดทางเดินที่เธอไม่เคยไป เธอจ้องภาพนั้นอย่างสับสน\n\nแฟ้มความรู้: นาราจำรอบก่อนไม่ได้ แต่ภาพวาดของเธอจำได้";
                    }
                    return head + "คุณได้ยินเสียงใครสักคนวาดรูปที่โถงหอพักกลางดึก แต่ยังไม่สนิทพอที่จะเข้าไปหา";
                case 3:
                    if (S.Suspicion >= 85) { S.NextDayApPenalty = 2; return head + "ตรวจน้ำ คุณถูกเรียกสอบถามเกือบทั้งคืน\n\nพรุ่งนี้เริ่มวันด้วย AP -2"; }
                    if (S.Suspicion >= 30)
                    {
                        if (S.Composure >= 3) { S.Suspicion -= 5; return head + "ตรวจน้ำ คุณแกล้งดื่มแล้วเทน้ำลงในแขนเสื้อ\n\nความสงสัย -5"; }
                        return head + "ตรวจน้ำ คุณดื่มลงไป รายละเอียดบางอย่างของสัปดาห์นี้เริ่มเลือนไปจากความทรงจำ";
                    }
                    return head + "ยามเดินตรวจทางเดิน คุณรออยู่ในความมืดจนเสียงฝีเท้าจางหายไป";
                case 4:
                    return head + "คุณเตรียมตัวสำหรับพรุ่งนี้\n\nแฟ้มความรู้: " + S.Codex.Count + " รายการ\nคนที่รู้จัก: " + S.KnownNpcs.Count + " คน";
                default:
                    return head + "หมอกกดทับหน้าต่าง ตรงนี้คือจุดที่ฉากเอาตัวรอดจะเริ่มขึ้น\n\n(โปรโตไทป์จบเพียงเท่านี้)";
            }
        }

        // ------------------------------------------------------------- actions

        public void Talk(string npcId)
        {
            if (modal) return;
            if (!S.CanAfford(1, 1)) { ShowMsg("เวลาหรือ AP ไม่พอที่จะคุย", "ตกลง", null); return; }
            var npc = GameData.Npcs[npcId];
            bool first = !S.KnownNpcs.Contains(npcId);
            S.Time -= 1; S.Ap -= 1;
            string line;
            if (first) { S.KnownNpcs.Add(npcId); line = npc.First; }
            else line = npc.Repeat[Mathf.Clamp(S.Aff(npcId) - 1, 0, npc.Repeat.Length - 1)];
            S.AddAff(npcId, 1);
            Refresh(); BuildWorld();
            ShowMsg(npc.Name + (first ? "  (คนใหม่)" : "") + "\n\n“" + line + "”\n\nความสนิท " + S.Aff(npcId), "ต่อไป", AfterAction);
        }

        public void ExploreObj(ObjectDef o)
        {
            if (modal) return;
            string key = S.Location + "." + o.Id;
            if (o.Locked != null) { ShowMsg(o.Name + "\n\n" + o.Locked, "ตกลง", null); return; }
            if (o.NeedWit > S.Wit) { ShowMsg(o.Name + "\n\nต้องใช้ Wit " + o.NeedWit + " (คุณมี " + S.Wit + ")", "ตกลง", null); return; }
            if (o.Kind == ObjKind.Explore && S.Explored.Contains(key))
            {
                ShowMsg(o.Name + "\n\n" + o.Desc + "\n\n(ตรวจแล้ว ไม่เสียค่าใช้จ่าย)", "ตกลง", null);
                return;
            }
            if (!S.CanAfford(o.Time, o.Ap)) { ShowMsg("เวลาหรือ AP ไม่พอ\nต้องใช้ เวลา " + o.Time + ", AP " + o.Ap, "ตกลง", null); return; }
            S.Time -= o.Time; S.Ap -= o.Ap;
            string extra = "";
            if (o.Kind == ObjKind.Rest)
            {
                S.Ap = Mathf.Min(GameState.MaxAp, S.Ap + o.ApGain);
                extra = "\n\nAP +" + o.ApGain;
            }
            else
            {
                S.Explored.Add(key);
                if (!string.IsNullOrEmpty(o.Codex)) { bool isNew = !S.Codex.Contains(o.Codex); S.AddCodex(o.Codex); if (isNew) extra += "\n\nแฟ้มความรู้: " + o.Codex; }
            }
            if (o.Suspicion != 0) { S.Suspicion = Mathf.Clamp(S.Suspicion + o.Suspicion, 0, 100); extra += "\nความสงสัย +" + o.Suspicion; }
            Refresh(); BuildWorld();
            ShowMsg(o.Name + "\n\n" + o.Desc + extra, "ต่อไป", AfterAction);
        }

        public void Wait()
        {
            if (modal) return;
            if (!S.CanAfford(1, 0)) { ShowMsg("ไม่มีเวลาเหลือแล้ว", "ตกลง", null); return; }
            S.Time -= 1;
            string text;
            if (S.Phase == Phase.Free1 || S.Phase == Phase.Free2)
            {
                Phase next = S.Phase == Phase.Free1 ? Phase.Free2 : Phase.Night;
                if (next == Phase.Night) text = "คุณมองฝูงชนค่อยๆ บางลง กลางคืนใกล้เข้ามาแล้ว";
                else
                {
                    var lines = new List<string>();
                    foreach (var id in new[] { "nara", "mei", "lin", "khun", "pim", "ton" })
                    {
                        string loc = GameData.Where(id, S.Day, next);
                        if (loc == null) continue;
                        string who = S.KnownNpcs.Contains(id) ? GameData.Npcs[id].Name : "คนที่คุณไม่รู้จัก";
                        lines.Add(who + " ไปที่" + GameData.Locations[loc].Name);
                    }
                    text = "คุณคอยดูว่าใครมาใครไป\n\nหลังเลิกเรียน:\n" + string.Join("\n", lines);
                }
            }
            else text = "คุณรออยู่";
            Refresh();
            ShowMsg("รอและสังเกต\n\n" + text, "ต่อไป", AfterAction);
        }

        void AfterAction()
        {
            if (S.Time <= 0) EndPeriod();
            else Refresh();
        }

        void EndPeriod()
        {
            ShowMsg("กริ่งดังขึ้น หมดช่วงเวลานี้แล้ว", "ต่อไป", NextPhase);
        }

        void OpenLeave()
        {
            if (modal) return;
            locPanel.SetActive(true);
            modal = true;
            foreach (Transform c in locList) Destroy(c.gameObject);
            locTitle.text = ThaiShaper.Fix("จะไปที่ไหน? (เหลือเวลา " + S.Time + ")");
            foreach (var id in GameData.LocationOrder)
            {
                var l = GameData.Locations[id];
                string reason = null;
                if (l.Id == S.Location) reason = "คุณอยู่ที่นี่";
                else if (l.Closed) reason = l.ClosedReason;
                else if (l.Free2Only && S.Phase != Phase.Free2) reason = "เปิดหลังคาบบ่าย";
                else if (S.Suspicion >= 60 && l.Id == "library") reason = "ยามประจำการ (ความสงสัยสูง)";
                else if (S.Time < l.TravelTime) reason = "เวลาไม่พอ";
                string label = l.Name + "   [เวลา " + l.TravelTime + "]" + (reason != null ? "   - " + reason : "");
                var loc = l;
                var captured = reason;
                var b = MakeButton(locList, label, () =>
                {
                    if (captured != null) { locTitle.text = ThaiShaper.Fix(loc.Name + ": " + captured); return; }
                    Travel(loc);
                });
                b.GetComponent<LayoutElement>().preferredHeight = 54;
                if (reason != null) b.GetComponent<UIImage>().color = new Color(0.12f, 0.17f, 0.17f);
            }
            var close = MakeButton(locList, "ยกเลิก", () => { locPanel.SetActive(false); modal = false; });
            close.GetComponent<LayoutElement>().preferredHeight = 54;
        }

        public void Travel(LocationDef l)
        {
            locPanel.SetActive(false);
            modal = false;
            S.Time -= l.TravelTime;
            S.Location = l.Id;
            mode = Mode.None;
            BuildWorld();
            Refresh();
            if (S.Time <= 0) EndPeriod();
        }

        // ---------------------------------------------------------------- input

        void Update()
        {
            if (chapterState != 0)
            {
                bool pressed = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                UpdateChapter(pressed);
                return;
            }
            if (cutscene != null && cutscene.Active) return;
            if (Mouse.current == null || cam == null) return;
            hover = null;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!modal && !overUi && S.IsFree)
            {
                var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100f)) hover = hit.collider.GetComponent<Hotspot>();
                if (Mouse.current.leftButton.wasPressedThisFrame && hover != null)
                {
                    if (hover.NpcId != null) Talk(hover.NpcId);
                    else if (hover.Obj != null) ExploreObj(hover.Obj);
                }
            }
            UpdateColors();
            UpdateHint();
        }

        void UpdateColors()
        {
            float pulse = 0.18f + 0.12f * Mathf.Sin(Time.time * 4f);
            foreach (var h in hotspots)
            {
                if (h == null) continue;
                Color c = h.Base;
                bool isNpc = h.NpcId != null;
                if ((mode == Mode.Talk && isNpc) || (mode == Mode.Explore && !isNpc)) c = Color.Lerp(c, Color.white, pulse + 0.1f);
                if (h == hover) c = Color.Lerp(c, Color.white, 0.5f);
                h.R.material.SetColor("_BaseColor", c);
                h.R.material.color = c;
            }
        }

        void SetHint(string s)
        {
            if (hint == null) return;
            string f = ThaiShaper.Fix(s);
            if (hint.text != f) hint.text = f;
        }

        void UpdateHint()
        {
            if (hint == null) return;
            if (hover == null) { SetHint(S.IsFree ? "เลือกคำสั่ง หรือคลิกที่คนหรือของในห้อง" : ""); return; }
            if (hover.NpcId != null)
            {
                var n = GameData.Npcs[hover.NpcId];
                SetHint((S.KnownNpcs.Contains(n.Id) ? n.Name : "คนที่ไม่รู้จัก") + "  -  คุย: เวลา 1, AP -1");
            }
            else
            {
                var o = hover.Obj;
                bool explored = S.Explored.Contains(S.Location + "." + o.Id);
                string name = (o.Kind == ObjKind.Rest || explored) ? o.Name : "ยังไม่ได้สำรวจ";
                string cost = o.Locked != null ? "ล็อก" : o.Kind == ObjKind.Rest ? ("เวลา " + o.Time + ", AP +" + o.ApGain) : explored ? "ตรวจแล้ว" : ("เวลา " + o.Time + ", AP -" + o.Ap + (o.NeedWit > 0 ? ", Wit " + o.NeedWit : ""));
                SetHint(name + "  -  " + cost);
            }
        }

        // ---------------------------------------------------------------- world

        void ClearWorld()
        {
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            hotspots.Clear();
            world = null;
        }

        GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Color c, bool collider)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(world, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            r.material = new Material(unlit);
            r.material.SetColor("_BaseColor", c);
            r.material.color = c;
            if (!collider) Destroy(g.GetComponent<Collider>());
            return g;
        }

        static void ApplyFeatures(TMP_Text t)
        {
            t.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>
            {
                UnityEngine.TextCore.OTL_FeatureTag.kern,
                UnityEngine.TextCore.OTL_FeatureTag.mark,
                UnityEngine.TextCore.OTL_FeatureTag.mkmk
            };
        }

        void Label(string text, Vector3 pos, Color c, float size)
        {
            var g = new GameObject("Label");
            g.transform.SetParent(world, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var tm = g.AddComponent<TextMeshPro>();
            tm.font = thaiFont;
            ApplyFeatures(tm);
            tm.text = ThaiShaper.Fix(text);
            tm.fontSize = size;
            tm.color = c;
            tm.alignment = TextAlignmentOptions.Center;
            tm.textWrappingMode = TextWrappingModes.NoWrap;
            tm.rectTransform.sizeDelta = new Vector2(8, 2);
        }

        void BuildWorld()
        {
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            hotspots.Clear();
            world = new GameObject("World").transform;
            var L = Loc;
            float w = L.Size.x, d = L.Size.y;
            // A painted top-down map (LocationDef.Background) replaces the blockout floor/walls/decor.
            Texture2D art = string.IsNullOrEmpty(L.Background) ? null : Resources.Load<Texture2D>(L.Background);
            if (!string.IsNullOrEmpty(L.Background) && art == null)
                Debug.LogWarning("Background not found in Resources: " + L.Background + " (falling back to blockout)");
            if (art != null)
            {
                BuildBackdrop(art, w, d);
            }
            else
            {
                var wallC = new Color(0.16f, 0.19f, 0.19f);
                Prim(PrimitiveType.Cube, "Floor", new Vector3(0, -0.1f, 0), new Vector3(w, 0.1f, d), new Color(0.30f, 0.34f, 0.33f), false);
                Prim(PrimitiveType.Cube, "WallN", new Vector3(0, 0, d / 2 + 0.1f), new Vector3(w + 0.4f, 0.3f, 0.2f), wallC, false);
                Prim(PrimitiveType.Cube, "WallS", new Vector3(0, 0, -d / 2 - 0.1f), new Vector3(w + 0.4f, 0.3f, 0.2f), wallC, false);
                Prim(PrimitiveType.Cube, "WallE", new Vector3(w / 2 + 0.1f, 0, 0), new Vector3(0.2f, 0.3f, d), wallC, false);
                Prim(PrimitiveType.Cube, "WallW", new Vector3(-w / 2 - 0.1f, 0, 0), new Vector3(0.2f, 0.3f, d), wallC, false);

                foreach (var dc in L.Decor)
                    Prim(PrimitiveType.Cube, "Decor", new Vector3(dc.x, 0.2f, dc.y), new Vector3(dc.z, 0.4f, dc.w), new Color(0.36f, 0.30f, 0.24f), false);
            }

            foreach (var o in L.Objects)
            {
                string key = L.Id + "." + o.Id;
                bool explored = S.Explored.Contains(key);
                Color baseC = o.Kind == ObjKind.Rest ? new Color(0.45f, 0.36f, 0.27f)
                    : o.Locked != null ? new Color(0.45f, 0.25f, 0.25f)
                    : explored ? new Color(0.32f, 0.40f, 0.42f) : new Color(0.40f, 0.58f, 0.64f);
                // On a painted map the desk art is already there: use an invisible hotspot that only lights up on hover/mode.
                if (art != null) baseC = new Color(baseC.r, baseC.g, baseC.b, 0f);
                var g = art != null
                    ? Overlay("Obj_" + o.Id, new Vector3(o.Pos.x, 0.05f, o.Pos.y), new Vector3(o.Size.x, 0.05f, o.Size.y), baseC)
                    : Prim(PrimitiveType.Cube, "Obj_" + o.Id, new Vector3(o.Pos.x, 0.4f, o.Pos.y), new Vector3(o.Size.x, 0.8f, o.Size.y), baseC, true);
                var h = g.AddComponent<Hotspot>(); h.Obj = o; h.R = g.GetComponent<Renderer>(); h.Base = baseC;
                hotspots.Add(h);
                bool showName = o.Kind == ObjKind.Rest || explored;
                Label(showName ? o.Name : "?", new Vector3(o.Pos.x, 1.3f, o.Pos.y + (o.Size.y / 2 + 0.5f)), Teal, showName ? 3f : 6f);
            }

            var npcs = GameData.NpcsAt(L.Id, S.Day, S.Phase);
            for (int i = 0; i < npcs.Count && i < L.NpcSlots.Length; i++)
            {
                var id = npcs[i];
                bool known = S.KnownNpcs.Contains(id);
                var slot = L.NpcSlots[i];
                Color c = known ? Amber : new Color(0.50f, 0.54f, 0.54f);
                var g = Prim(PrimitiveType.Cylinder, "Npc_" + id, new Vector3(slot.x, 0.9f, slot.y), new Vector3(0.8f, 0.9f, 0.8f), c, true);
                var h = g.AddComponent<Hotspot>(); h.NpcId = id; h.R = g.GetComponent<Renderer>(); h.Base = c;
                hotspots.Add(h);
                Label(known ? GameData.Npcs[id].Name : "?", new Vector3(slot.x, 2.0f, slot.y), known ? new Color(1f, 0.85f, 0.5f) : Teal, known ? 3.6f : 7f);
            }

            float aspect = cam.aspect;
            if (art != null)
            {
                // Fit the map between the HUD top bar (70px) and the hint + command bars (148px) of a 1080px-high screen.
                float size = Mathf.Max(d / 2f / (862f / 1080f), (w / 2f + 0.3f) / aspect);
                cam.orthographicSize = size;
                cam.transform.position = new Vector3(0, 20, -0.2f - size * 2f * (39f / 1080f));
            }
            else
            {
                cam.orthographicSize = Mathf.Max(d / 2f + 2.0f, (w / 2f + 0.8f) / aspect);
                cam.transform.position = new Vector3(0, 20, -0.2f);
            }
        }

        void BuildBackdrop(Texture2D tex, float w, float d)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Backdrop";
            q.transform.SetParent(world, false);
            q.transform.localPosition = new Vector3(0, -0.1f, 0);
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(w, d, 1);
            Destroy(q.GetComponent<Collider>());
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Texture");
            var m = new Material(sh);
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            q.GetComponent<Renderer>().material = m;
        }

        // Transparent flat box used as an invisible click target over painted art.
        GameObject Overlay(string name, Vector3 pos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(world, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = unlit;
            var m = new Material(sh);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            m.SetColor("_BaseColor", c);
            m.color = c;
            g.GetComponent<Renderer>().material = m;
            return g;
        }

        // ------------------------------------------------------------------- UI

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var g = new GameObject("Main Camera"); g.tag = "MainCamera";
                cam = g.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.09f);
        }

        void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        RectTransform Panel(Transform parent, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            g.AddComponent<UIImage>().color = c;
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
            return r;
        }

        TextMeshProUGUI MakeText(Transform parent, string s, float size, TextAlignmentOptions a, Color c)
        {
            var g = new GameObject("Text", typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = thaiFont;
            ApplyFeatures(t);
            t.text = ThaiShaper.Fix(s); t.fontSize = size; t.alignment = a; t.color = c;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(16, 6); r.offsetMax = new Vector2(-16, -6);
            return t;
        }

        Button MakeButton(Transform parent, string label, UnityAction onClick)
        {
            var g = new GameObject("Btn_" + label, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var bimg = g.AddComponent<UIImage>();
            bimg.color = new Color(0.14f, 0.26f, 0.25f);
            UiKit.SkinButton(bimg);
            var b = g.AddComponent<Button>();
            g.AddComponent<LayoutElement>();
            MakeText(g.transform, label, 26, TextAlignmentOptions.Center, new Color(0.7f, 0.97f, 0.9f));
            b.onClick.AddListener(onClick);
            return b;
        }

        void BuildUI()
        {
            var cgo = new GameObject("UI_Canvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            cgo.AddComponent<GraphicRaycaster>();
            var dark = new Color(0.05f, 0.08f, 0.08f, 0.9f);

            var top = Panel(cgo.transform, "TopBar", dark, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), Vector2.zero);
            UiKit.SkinBar(top.GetComponent<UIImage>(), "ui_bar_top");
            hud = MakeText(top, "", 26, TextAlignmentOptions.MidlineLeft, TextC);
            topGo = top.gameObject;

            var hintP = Panel(cgo.transform, "HintBar", new Color(0, 0, 0, 0.35f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 100), new Vector2(0, 148));
            hint = MakeText(hintP, "", 24, TextAlignmentOptions.Center, TextC);
            hintGo = hintP.gameObject;

            var bar = Panel(cgo.transform, "CommandBar", dark, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 100));
            UiKit.SkinBar(bar.GetComponent<UIImage>(), "ui_bar_bottom");
            barGo = bar.gameObject;
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(30, 30, 14, 14); hl.spacing = 20;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            cmdButtons.Add(MakeButton(bar, "คุย", () => { if (!modal) mode = Mode.Talk; }));
            cmdButtons.Add(MakeButton(bar, "สำรวจ", () => { if (!modal) mode = Mode.Explore; }));
            cmdButtons.Add(MakeButton(bar, "รอและสังเกต", Wait));
            cmdButtons.Add(MakeButton(bar, "ออกจากห้อง", OpenLeave));
            cmdButtons.Add(MakeButton(bar, "จบช่วงนี้", () => { if (!modal && S.IsFree) EndPeriod(); }));

            var mp = Panel(cgo.transform, "MsgPanel", new Color(0.05f, 0.08f, 0.08f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500, -250), new Vector2(500, 250));
            UiKit.SkinPanel(mp.GetComponent<UIImage>());
            msgPanel = mp.gameObject;
            var mt = MakeText(mp, "", 28, TextAlignmentOptions.TopLeft, TextC);
            mt.enableAutoSizing = true; mt.fontSizeMin = 18; mt.fontSizeMax = 28;
            var mtr = mt.GetComponent<RectTransform>(); mtr.offsetMin = new Vector2(30, 100); mtr.offsetMax = new Vector2(-30, -24);
            msgText = mt;
            msgBtn = MakeButton(mp, "ต่อไป", CloseMsg);
            var br = msgBtn.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0); br.anchorMax = new Vector2(0.5f, 0); br.pivot = new Vector2(0.5f, 0);
            br.sizeDelta = new Vector2(280, 64); br.anchoredPosition = new Vector2(0, 24);
            msgBtnLabel = msgBtn.GetComponentInChildren<TextMeshProUGUI>();
            msgPanel.SetActive(false);

            var lp = Panel(cgo.transform, "LocPanel", new Color(0.05f, 0.08f, 0.08f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -340), new Vector2(440, 340));
            UiKit.SkinPanel(lp.GetComponent<UIImage>());
            locPanel = lp.gameObject;
            locTitle = MakeText(lp, "จะไปที่ไหน?", 30, TextAlignmentOptions.Center, Teal);
            var ltr = locTitle.GetComponent<RectTransform>();
            ltr.anchorMin = new Vector2(0, 1); ltr.anchorMax = new Vector2(1, 1); ltr.pivot = new Vector2(0.5f, 1);
            ltr.offsetMin = new Vector2(16, -70); ltr.offsetMax = new Vector2(-16, -6);
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(lp, false);
            var lr = listGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(24, 20); lr.offsetMax = new Vector2(-24, -80);
            var vl = listGo.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            locList = listGo.transform;
            locPanel.SetActive(false);

            var cp = Panel(cgo.transform, "ChapterPanel", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            chapterPanel = cp.gameObject;
            chapterTitle = MakeText(cp, "", 84, TextAlignmentOptions.Center, Color.white);
            chapterTitle.gameObject.name = "ChapterTitle";
            var ctr = chapterTitle.GetComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0, 0.72f); ctr.anchorMax = new Vector2(1, 0.9f); ctr.offsetMin = Vector2.zero; ctr.offsetMax = Vector2.zero;
            chapterBody = MakeText(cp, "", 36, TextAlignmentOptions.Top, new Color(0.82f, 0.88f, 0.86f));
            chapterBody.gameObject.name = "ChapterBody";
            chapterBody.lineSpacing = 18;
            var cbr = chapterBody.GetComponent<RectTransform>();
            cbr.anchorMin = new Vector2(0.15f, 0.12f); cbr.anchorMax = new Vector2(0.85f, 0.68f); cbr.offsetMin = Vector2.zero; cbr.offsetMax = Vector2.zero;
            chapterHint = MakeText(cp, "คลิกหรือกด Space เพื่อต่อไป", 24, TextAlignmentOptions.BottomRight, new Color(1, 1, 1, 0.5f));
            chapterHint.gameObject.name = "ChapterHint";
            chapterPanel.SetActive(false);

            var tp = Panel(cgo.transform, "TitlePanel", new Color(0.04f, 0.06f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titlePanel = tp.gameObject;
            var tTitle = MakeText(tp, "MNEMOSYNE", 110, TextAlignmentOptions.Center, new Color(0.78f, 0.95f, 0.9f));
            var ttr2 = tTitle.GetComponent<RectTransform>();
            ttr2.anchorMin = new Vector2(0, 0.55f); ttr2.anchorMax = new Vector2(1, 0.8f); ttr2.offsetMin = Vector2.zero; ttr2.offsetMax = Vector2.zero;
            var tSub = MakeText(tp, "รอยแยกแห่งความทรงจำที่สาบสูญ", 40, TextAlignmentOptions.Center, new Color(0.55f, 0.7f, 0.67f));
            var tsr = tSub.GetComponent<RectTransform>();
            tsr.anchorMin = new Vector2(0, 0.44f); tsr.anchorMax = new Vector2(1, 0.56f); tsr.offsetMin = Vector2.zero; tsr.offsetMax = Vector2.zero;
            var startB = MakeButton(tp, "เริ่มเกม", OnStartPressed);
            var sbr = startB.GetComponent<RectTransform>();
            sbr.anchorMin = new Vector2(0.5f, 0.3f); sbr.anchorMax = new Vector2(0.5f, 0.3f); sbr.pivot = new Vector2(0.5f, 0.5f);
            sbr.sizeDelta = new Vector2(360, 84); sbr.anchoredPosition = Vector2.zero;
            titlePanel.SetActive(false);

            // Created last so the cutscene panel is drawn on top of every other screen.
            cutscene = gameObject.AddComponent<CutscenePlayer>();
            cutscene.Init(cgo.transform, thaiFont);
        }

        void ShowMsg(string text, string button, Action onClose)
        {
            modal = true;
            msgPanel.SetActive(true);
            msgText.text = ThaiShaper.Fix(text);
            msgBtnLabel.text = ThaiShaper.Fix(button);
            onMsgClose = onClose;
        }

        public void CloseMsg()
        {
            msgPanel.SetActive(false);
            modal = false;
            var a = onMsgClose; onMsgClose = null;
            if (a != null) a();
        }

        void Refresh()
        {
            if (hud == null) return;
            string pips = "";
            if (S.IsFree) for (int i = 0; i < GameState.FreeTime; i++) pips += i < S.Time ? "[#]" : "[ ]";
            string phase = S.Phase == Phase.Free1 ? "ช่วงอิสระ 1" : S.Phase == Phase.Free2 ? "ช่วงอิสระ 2" : S.Phase == Phase.MorningClass ? "คาบเช้า" : S.Phase == Phase.AfternoonClass ? "คาบบ่าย" : "กลางคืน";
            string where = S.IsFree ? Loc.Name : "โรงเรียน";
            hud.text = ThaiShaper.Fix(where + "  |  " + GameState.DayNames[S.Day] + "  |  " + phase + "     เวลา " + pips + "     AP " + S.Ap + "/" + GameState.MaxAp + "     ความสงสัย " + S.Suspicion + "     แฟ้มความรู้ " + S.Codex.Count);
        }
    }
}
