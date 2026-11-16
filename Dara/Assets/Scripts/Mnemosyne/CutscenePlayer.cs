using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Mnemosyne
{
    // Plays a CutsceneDef: full-screen video clips, pauses at the decision point, shows the choices,
    // applies the chosen Effect to GameState and continues with the next node.
    // Owns its own full-screen panel; GameController only calls Play() and waits for the callback.
    public class CutscenePlayer : MonoBehaviour
    {
        enum Stage { Idle, Loading, Playing, Lines, Choosing, Minigame, Prep }

        const float SkipGuard = 0.35f; // ignore clicks right after a transition (the click that picked a choice)

        TMP_FontAsset font;
        GameObject panel;
        RawImage raw;
        RenderTexture rt;
        VideoPlayer vp;
        TextMeshProUGUI promptText, linesText, hintText, titleText;
        MinigamePanel minigame;
        readonly List<string> prepPick = new List<string>();
        RectTransform choiceRoot;
        readonly List<GameObject> choiceObjs = new List<GameObject>();

        CutsceneDef def;
        CutsceneNode node;
        GameState S;
        Action onDone;
        Stage stage = Stage.Idle;
        float enteredAt;

        public bool Active { get { return stage != Stage.Idle; } }

        // Raised after a choice is applied (for analytics, achievements, debug overlays).
        public event Action<CutsceneNode, ChoiceDef> ChoiceMade;

        public void Init(Transform canvasRoot, TMP_FontAsset thaiFont)
        {
            font = thaiFont;

            var p = UiKit.Panel(canvasRoot, "CutscenePanel", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel = p.gameObject;

            var rawGo = new GameObject("Video", typeof(RectTransform));
            rawGo.transform.SetParent(p, false);
            raw = rawGo.AddComponent<RawImage>();
            var rr = rawGo.GetComponent<RectTransform>();
            rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = Vector2.zero; rr.offsetMax = Vector2.zero;
            var fit = rawGo.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;

            rt = new RenderTexture(1920, 1080, 0);
            raw.texture = rt;

            titleText = UiKit.Text(p, font, "", 56, TextAlignmentOptions.Center, UiKit.Teal);
            titleText.gameObject.name = "Title";
            var tr = titleText.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.1f, 0.86f); tr.anchorMax = new Vector2(0.9f, 0.96f);
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            titleText.gameObject.SetActive(false);

            linesText = UiKit.Text(p, font, "", 40, TextAlignmentOptions.Center, UiKit.TextC);
            var lr = linesText.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.15f, 0.5f); lr.anchorMax = new Vector2(0.85f, 0.85f);
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
            linesText.lineSpacing = 18;
            linesText.enableAutoSizing = true;
            linesText.fontSizeMin = 24; linesText.fontSizeMax = 40;
            linesText.gameObject.SetActive(false);

            promptText = UiKit.Text(p, font, "", 40, TextAlignmentOptions.Center, Color.white);
            promptText.gameObject.name = "Prompt";
            var pr = promptText.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.1f, 0.40f); pr.anchorMax = new Vector2(0.9f, 0.48f);
            pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
            promptText.gameObject.SetActive(false);

            var rootGo = new GameObject("Choices", typeof(RectTransform));
            rootGo.transform.SetParent(p, false);
            choiceRoot = rootGo.GetComponent<RectTransform>();
            choiceRoot.anchorMin = new Vector2(0.2f, 0.05f); choiceRoot.anchorMax = new Vector2(0.8f, 0.38f);
            choiceRoot.offsetMin = Vector2.zero; choiceRoot.offsetMax = Vector2.zero;
            var vl = rootGo.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 14; vl.childAlignment = TextAnchor.LowerCenter;
            vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

            hintText = UiKit.Text(p, font, "คลิกหรือกด Space เพื่อข้าม", 22, TextAlignmentOptions.BottomRight, new Color(1, 1, 1, 0.5f));
            hintText.gameObject.name = "Hint";
            hintText.gameObject.SetActive(false);

            vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.source = VideoSource.Url;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.prepareCompleted += OnPrepared;
            vp.loopPointReached += OnClipEnded;
            vp.errorReceived += OnClipError;

            // Drawn above this panel (created later in the same canvas).
            minigame = gameObject.AddComponent<MinigamePanel>();
            minigame.Init(canvasRoot, font);

            panel.SetActive(false);
        }

        public void Play(CutsceneDef cutscene, GameState state, Action done)
        {
            def = cutscene;
            S = state;
            onDone = done;
            panel.SetActive(true);
            EnterNode(def.Start);
        }

        // ------------------------------------------------------------------ nodes

        void EnterNode(string id)
        {
            ClearChoices();
            promptText.gameObject.SetActive(false);
            linesText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);
            titleText.gameObject.SetActive(false);

            if (id == null) { Finish(); return; }
            if (!def.Nodes.TryGetValue(id, out node))
            {
                Debug.LogError("Cutscene '" + def.Id + "': unknown node '" + id + "'");
                Finish();
                return;
            }
            enteredAt = Time.unscaledTime;

            if (node.Fx != null)
            {
                node.Fx.Apply(S);
                Debug.Log("[Cutscene " + def.Id + "/" + node.Id + "] enter: " + node.Fx.Describe());
            }

            if (!string.IsNullOrEmpty(node.Minigame)) { RunMinigame(); return; }
            if (node.PrepSlots > 0) { ShowPrep(); return; }

            // A clip that is not in StreamingAssets yet falls back to the node's Lines as text.
            if (!string.IsNullOrEmpty(node.Video) && !System.IO.File.Exists(System.IO.Path.Combine(Application.streamingAssetsPath, node.Video)))
            {
                Debug.LogWarning("Cutscene '" + def.Id + "/" + node.Id + "': missing video " + node.Video + ", showing text");
                ShowLines();
            }
            else if (!string.IsNullOrEmpty(node.Video))
            {
                stage = Stage.Loading;
                vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, node.Video);
                vp.Prepare();
            }
            else
            {
                ShowLines();
            }
        }

        void OnPrepared(VideoPlayer v)
        {
            if (stage != Stage.Loading) return;
            stage = Stage.Playing;
            hintText.gameObject.SetActive(true);
            v.Play();
        }

        void OnClipEnded(VideoPlayer v)
        {
            if (stage == Stage.Playing) EndOfClip();
        }

        void OnClipError(VideoPlayer v, string msg)
        {
            Debug.LogError("Cutscene video error (" + (node != null ? node.Video : "?") + "): " + msg);
            if (stage == Stage.Loading || stage == Stage.Playing) EndOfClip();
        }

        void EndOfClip()
        {
            hintText.gameObject.SetActive(false);
            if (node.Choices.Count > 0) ShowChoices();
            else Advance();
        }

        // Leaves a node that has no choice to make: first matching branch, otherwise Next.
        void Advance()
        {
            EnterNode(Branch.Resolve(node.Next, node.Branches, S));
        }

        void ShowLines()
        {
            bool hasLines = node.Lines != null && node.Lines.Length > 0;
            bool hasTitle = !string.IsNullOrEmpty(node.Title);
            if (hasLines)
            {
                linesText.text = ThaiShaper.Fix(string.Join("\n\n", node.Lines));
                linesText.gameObject.SetActive(true);
            }
            if (hasTitle)
            {
                titleText.text = ThaiShaper.Fix(node.Title);
                titleText.gameObject.SetActive(true);
            }
            if (node.Choices.Count > 0)
            {
                ShowChoices();
            }
            else if (!hasLines && !hasTitle)
            {
                Advance(); // silent gate node: only routes
            }
            else
            {
                stage = Stage.Lines;
                hintText.text = ThaiShaper.Fix("คลิกหรือกด Space เพื่อต่อไป");
                hintText.gameObject.SetActive(true);
            }
        }

        // ---------------------------------------------------------------- choices

        void ShowChoices()
        {
            if (vp.isPlaying) vp.Pause();
            stage = Stage.Choosing;
            hintText.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(node.Prompt))
            {
                promptText.text = ThaiShaper.Fix(node.Prompt);
                promptText.gameObject.SetActive(true);
            }

            foreach (var c in node.Choices)
            {
                var choice = c;
                bool ok = choice.Need == null || choice.Need.Met(S);
                string label = ok ? choice.Text : choice.Text + "   [" + choice.Need.Label + "]";
                var b = UiKit.MakeButton(choiceRoot, font, label, () => Choose(choice));
                var le = b.GetComponent<LayoutElement>();
                le.minHeight = 84; le.preferredHeight = 84;
                if (!ok)
                {
                    b.interactable = false;
                    b.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.14f, 0.14f, 0.85f);
                    var t = b.GetComponentInChildren<TextMeshProUGUI>();
                    t.color = new Color(0.55f, 0.6f, 0.58f);
                }
                choiceObjs.Add(b.gameObject);
            }
        }

        // ------------------------------------------------------- minigame / prep slots

        void RunMinigame()
        {
            MinigameDef md;
            if (!MinigameData.All.TryGetValue(node.Minigame, out md))
            {
                Debug.LogError("Cutscene '" + def.Id + "': unknown minigame '" + node.Minigame + "'");
                Advance();
                return;
            }
            stage = Stage.Minigame;
            var runNode = node;
            minigame.Run(md, S, pass =>
            {
                var fx = pass ? runNode.PassFx : runNode.FailFx;
                if (fx != null) fx.Apply(S);
                if (pass && !string.IsNullOrEmpty(runNode.Counter)) S.AddCounter(runNode.Counter, 1);
                S.Flags.Add("minigame:" + def.Id + ":" + runNode.Id + ":" + (pass ? "pass" : "fail"));
                Debug.Log("[Cutscene " + def.Id + "/" + runNode.Id + "] minigame '" + runNode.Minigame + "' " + (pass ? "passed" : "failed")
                          + (string.IsNullOrEmpty(runNode.Counter) ? "" : " (" + runNode.Counter + "=" + S.Counter(runNode.Counter) + ")"));
                node = runNode;
                Advance();
            });
        }

        // Item loadout: toggle owned items (up to node.PrepSlots) then confirm. Result goes to GameState.Equipped.
        void ShowPrep()
        {
            stage = Stage.Prep;
            prepPick.Clear();
            foreach (var e in S.Equipped) if (S.Items.Contains(e)) prepPick.Add(e);
            RebuildPrep();
        }

        void RebuildPrep()
        {
            ClearChoices();
            titleText.text = ThaiShaper.Fix("เลือกของที่จะพกเข้าคืนนี้ (" + prepPick.Count + "/" + node.PrepSlots + ")");
            titleText.gameObject.SetActive(true);

            var owned = new List<string>(S.Items);
            owned.Sort(StringComparer.Ordinal);
            foreach (var id in owned)
            {
                string itemId = id;
                bool on = prepPick.Contains(itemId);
                ItemDef idf;
                string desc = ItemData.All.TryGetValue(itemId, out idf) ? "  —  " + idf.Desc : "";
                string label = (on ? "[พก] " : "[   ] ") + ItemData.NameOf(itemId) + desc;
                var b = UiKit.MakeButton(choiceRoot, font, label, () => TogglePrep(itemId));
                var le = b.GetComponent<LayoutElement>();
                le.minHeight = 70; le.preferredHeight = 70;
                if (on) b.GetComponent<UnityEngine.UI.Image>().color = new Color(0.25f, 0.5f, 0.42f);
                choiceObjs.Add(b.gameObject);
            }
            var ok = UiKit.MakeButton(choiceRoot, font, "ยืนยัน", ConfirmPrep);
            var ole = ok.GetComponent<LayoutElement>();
            ole.minHeight = 70; ole.preferredHeight = 70;
            choiceObjs.Add(ok.gameObject);
        }

        void TogglePrep(string id)
        {
            if (stage != Stage.Prep) return;
            if (!prepPick.Remove(id) && prepPick.Count < node.PrepSlots) prepPick.Add(id);
            RebuildPrep();
        }

        void ConfirmPrep()
        {
            if (stage != Stage.Prep) return;
            S.Equipped.Clear();
            S.Equipped.AddRange(prepPick);
            Debug.Log("[Cutscene " + def.Id + "/" + node.Id + "] equipped: " + string.Join(",", S.Equipped));
            Advance();
        }

        void ClearChoices()
        {
            foreach (var g in choiceObjs) if (g != null) Destroy(g);
            choiceObjs.Clear();
        }

        void Choose(ChoiceDef c)
        {
            if (stage != Stage.Choosing) return;
            if (c.Need != null && !c.Need.Met(S)) return;

            c.Fx.Apply(S);
            S.Flags.Add("choice:" + def.Id + ":" + node.Id + ":" + c.Id);
            Debug.Log("[Cutscene " + def.Id + "/" + node.Id + "] chose '" + c.Id + "': " + c.Fx.Describe());

            var chosenNode = node;
            var handler = ChoiceMade;
            if (handler != null) handler(chosenNode, c);

            EnterNode(Branch.Resolve(c.Next, c.Branches, S));
        }

        // ----------------------------------------------------------------- input

        void Update()
        {
            if (stage == Stage.Idle) return;

            bool pressed = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            bool guarded = Time.unscaledTime - enteredAt < SkipGuard;

            if (stage == Stage.Playing)
            {
                if (node.PauseAt >= 0f && node.Choices.Count > 0 && vp.time >= node.PauseAt)
                {
                    EndOfClip();
                    return;
                }
                if (pressed && !guarded)
                {
                    vp.Pause();
                    EndOfClip(); // skipping a clip jumps to its decision point, never past it
                }
            }
            else if (stage == Stage.Lines && pressed && !guarded)
            {
                Advance();
            }
        }

        void Finish()
        {
            stage = Stage.Idle;
            if (vp != null) vp.Stop();
            ClearChoices();
            titleText.gameObject.SetActive(false);
            panel.SetActive(false);
            var cb = onDone;
            onDone = null;
            if (cb != null) cb();
        }
    }
}
