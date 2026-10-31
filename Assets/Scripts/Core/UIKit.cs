using System;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Helpers for building uGUI hierarchies from code.</summary>
    public static class UIKit
    {
        public static readonly Color Cream = new Color(1f, 0.96f, 0.88f);
        public static readonly Color Orange = new Color(1f, 0.66f, 0.3f);
        public static readonly Color Panel = new Color(0.2f, 0.14f, 0.29f, 0.96f);
        public static readonly Color PanelDark = new Color(0.13f, 0.09f, 0.19f, 0.95f);
        public static readonly Color Green = new Color(0.42f, 0.78f, 0.38f);
        public static readonly Color Disabled = new Color(0.33f, 0.29f, 0.4f);

        static Font font;
        public static Font Font
        {
            get
            {
                if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform At(this RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 anchor, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced) img.type = Image.Type.Sliced;
            return img;
        }

        public static Text Txt(string name, Transform parent, string s, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = s;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.08f, 0.04f, 0.12f, 0.85f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Button Btn(string name, Transform parent, string label, Color bg, Action onClick, int fontSize = 36)
        {
            var img = Img(name, parent, ProcArt.Rounded, bg, true);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick?.Invoke());
            var t = Txt("Label", img.transform, label, fontSize, Cream);
            Stretch(t.rectTransform, 6, 6, 4, 4);
            return b;
        }

        public static Text Label(this Button b) => b.GetComponentInChildren<Text>();
    }
}
