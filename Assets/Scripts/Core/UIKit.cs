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

        // Fonts (Resources/Fonts, SIL Open Font License): Sour Gummy, a soft rounded face that suits the crayon art,
        // Medium for texts and Bold for titles and buttons. If a file is missing the game falls back to the fonts it
        // used before (Nunito, Baloo 2) and then to Unity's built-in font.
        static Font font, titleFont;

        /// <summary>Body text font (Sour Gummy Medium).</summary>
        public static Font Font
        {
            get
            {
                if (!font) font = Resources.Load<Font>("Fonts/SourGummy-Medium");
                if (!font) font = Resources.Load<Font>("Fonts/Nunito-SemiBold");
                if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Title and button font (Sour Gummy Bold): big texts and button labels use it.</summary>
        public static Font TitleFont
        {
            get
            {
                if (!titleFont) titleFont = Resources.Load<Font>("Fonts/SourGummy-Bold");
                if (!titleFont) titleFont = Resources.Load<Font>("Fonts/Baloo2-Bold");
                if (!titleFont) titleFont = Font;
                return titleFont;
            }
        }

        /// <summary>Texts this big or bigger are titles.</summary>
        public const int TitleSize = 36;

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

        /// <summary>A UI image; its colour goes through the theme (purple panel shades become the theme's shades).</summary>
        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool sliced = false) =>
            ImgArt(name, parent, sprite, Theme.Ui(color), sliced);

        /// <summary>An image for a picture (scene, decoration): its colour is used as it is, whatever the theme.</summary>
        public static Image ImgArt(string name, Transform parent, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced) img.type = Image.Type.Sliced;
            // pictures are never squashed: they keep their proportions inside whatever box they are given.
            // Plain shapes (panels, circles, light, fog) are meant to stretch. An image that starts empty gets a picture later.
            else img.preserveAspect = ProcArt.IsPicture(sprite);
            return img;
        }

        /// <summary>A flat shape of a scene (wall, rug, shelf, cushion...). Solid ones get the same hand-drawn brown line
        /// as the sprites; see-through ones (shadows, light, grime) and hair-thin strips stay plain.</summary>
        public static Image ScenePart(string name, Transform parent, Sprite sprite, Color color, Vector2 size, bool sliced = false)
        {
            if (sprite != null && color.a >= 0.95f && Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) >= 12f)
            {
                if (sprite == ProcArt.Rounded) sprite = sliced ? ProcArt.InkRounded : ProcArt.InkBlob(size);
                else if (sprite == ProcArt.Circle) sprite = ProcArt.InkCircle(size);
            }
            return ImgArt(name, parent, sprite, color, sliced);
        }

        public static Text Txt(string name, Transform parent, string s, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            // on a light theme panel light text turns dark and loses its outline; on buttons, badges and dark boxes it stays as it was
            bool onLight = Theme.OnLight(parent);
            var mapped = Theme.Ink(color, onLight);
            if (mapped != color) outline = false;
            return TxtRaw(name, parent, s, size, mapped, align, outline);
        }

        /// <summary>A text whose colour is used as it is (light text on a dark or coloured box, whatever the theme).</summary>
        public static Text TxtRaw(string name, Transform parent, string s, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = size >= TitleSize ? TitleFont : Font;
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
                o.effectColor = Theme.Outline;
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
            b.onClick.AddListener(() => { Sfx.Play("click"); onClick?.Invoke(); });
            // labels stay light with an outline on coloured buttons; a pale panel-coloured button gets the dark panel text
            var t = Theme.OnLight(img.transform) ? Txt("Label", img.transform, label, fontSize, Cream) : TxtRaw("Label", img.transform, label, fontSize, Cream);
            t.font = TitleFont;
            Stretch(t.rectTransform, 6, 6, 4, 4);
            return b;
        }

        public static Text Label(this Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Set a text's colour later on, through the theme like a new text.</summary>
        public static void SetInk(this Text t, Color c) => t.color = Theme.Ink(c, Theme.OnLight(t.transform.parent));

        /// <summary>Full-width horizontal band measured from the top of a parent.</summary>
        public static RectTransform Section(Transform parent, string name, float top, float height)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name)
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            return rt;
        }
    }
}
