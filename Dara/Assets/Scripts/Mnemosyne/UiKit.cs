using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace Mnemosyne
{
    // Small uGUI builders shared by screens that are created from code.
    public static class UiKit
    {
        public static readonly Color Teal = new Color(0.45f, 0.95f, 0.85f);
        public static readonly Color TextC = new Color(0.85f, 0.92f, 0.9f);
        public static readonly Color ButtonBg = new Color(0.14f, 0.26f, 0.25f);
        public static readonly Color ButtonText = new Color(0.7f, 0.97f, 0.9f);

        // Painted UI pieces live in Resources/Mnemosyne. Every skin call silently keeps the flat colors
        // if the sprite is missing, so the game still runs without the art.
        static Sprite Art(string name) { return Resources.Load<Sprite>("Mnemosyne/" + name); }

        public static void SkinBar(UIImage img, string spriteName)
        {
            var s = Art(spriteName);
            if (s == null) return;
            img.sprite = s; img.type = UIImage.Type.Simple; img.color = Color.white;
        }

        public static void SkinButton(UIImage img)
        {
            var s = Art("ui_button");
            if (s == null) return;
            img.sprite = s; img.type = UIImage.Type.Sliced; img.color = Color.white;
            img.pixelsPerUnitMultiplier = 1.5f; // keeps the 44px corners small enough for 64px-high buttons
        }

        public static void SkinPanel(UIImage img)
        {
            var s = Art("ui_panel");
            if (s == null) return;
            img.sprite = s; img.type = UIImage.Type.Sliced; img.color = Color.white;
        }

        public static void ApplyFeatures(TMP_Text t)
        {
            t.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>
            {
                UnityEngine.TextCore.OTL_FeatureTag.kern,
                UnityEngine.TextCore.OTL_FeatureTag.mark,
                UnityEngine.TextCore.OTL_FeatureTag.mkmk
            };
        }

        public static RectTransform Panel(Transform parent, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            g.AddComponent<UIImage>().color = c;
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
            return r;
        }

        public static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string s, float size, TextAlignmentOptions a, Color c)
        {
            var g = new GameObject("Text", typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font;
            ApplyFeatures(t);
            t.text = ThaiShaper.Fix(s); t.fontSize = size; t.alignment = a; t.color = c;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(16, 6); r.offsetMax = new Vector2(-16, -6);
            return t;
        }

        public static UnityEngine.UI.Button MakeButton(Transform parent, TMP_FontAsset font, string label, UnityAction onClick)
        {
            var g = new GameObject("Btn_" + label, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var bimg = g.AddComponent<UIImage>();
            bimg.color = ButtonBg;
            SkinButton(bimg);
            var b = g.AddComponent<UnityEngine.UI.Button>();
            g.AddComponent<LayoutElement>();
            Text(g.transform, font, label, 26, TextAlignmentOptions.Center, ButtonText);
            b.onClick.AddListener(onClick);
            return b;
        }
    }
}
