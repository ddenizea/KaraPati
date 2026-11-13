using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Colour themes for the menus and the board. The UI code was first written for a dark purple look; instead of
    /// rewriting every colour, UIKit passes each one through <see cref="Ui"/> (backgrounds) or <see cref="Ink"/> (texts),
    /// which turn the old purple shades into the current theme's shades. Pictures (items, cats, story scenes) are never
    /// touched: they are drawn white-tinted or built with <see cref="UIKit.ImgArt"/>.
    /// The choice is a device setting (PlayerPrefs), not part of the save.
    /// </summary>
    public static class Theme
    {
        public enum Id { Krem = 0, Seftali = 1, Mor = 2 }

        const string PrefKey = "karapati_theme";
        static bool loaded;
        static Id current;

        public static Id Current
        {
            get
            {
                if (!loaded) { current = (Id)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)Id.Krem), 0, 2); loaded = true; }
                return current;
            }
        }

        public static void Set(Id id)
        {
            current = id;
            loaded = true;
            PlayerPrefs.SetInt(PrefKey, (int)id);
            PlayerPrefs.Save();
        }

        /// <summary>The themes a player can pick in the settings (the old purple one stays only for comparison).</summary>
        public static readonly Id[] Choices = { Id.Krem, Id.Seftali };

        public static string Name(Id id) => id == Id.Krem ? L.T("Krem") : id == Id.Seftali ? L.T("Şeftali") : L.T("Mor");

        /// <summary>The old purple look: every mapping returns the colour it gets.</summary>
        public static bool Legacy => Current == Id.Mor;

        // ------------------------------------------------------------------ palettes
        class Pal
        {
            public Color low, high;          // the purple panel shades map onto this ramp (inset ... raised)
            public Color accent2;            // strong purple buttons and badges
            public Color disabled;
            public Color go, orange;         // the green and orange buttons
            public Color text, title, outline;
            public Color dim;                // full-screen dimmer and toasts (alpha comes from the old colour)
            public Color bgTop, bgBottom, camera;
            public Color star;
            // board
            public Color table, tableTop, cloth, cellA, cellB, counter, counterEdge, grain;
        }

        static readonly Pal krem = new Pal
        {
            low = C(0.89f, 0.78f, 0.6f), high = C(1f, 0.98f, 0.93f),
            accent2 = C(0.79f, 0.54f, 0.31f), disabled = C(0.7f, 0.62f, 0.52f),
            go = C(0.47f, 0.77f, 0.4f), orange = C(0.95f, 0.6f, 0.27f),
            text = C(0.35f, 0.22f, 0.13f), title = C(0.72f, 0.38f, 0.15f), outline = new Color(0.29f, 0.165f, 0.094f, 0.85f),
            dim = C(0.22f, 0.13f, 0.06f),
            bgTop = C(0.97f, 0.9f, 0.77f), bgBottom = C(0.92f, 0.79f, 0.58f), camera = C(0.92f, 0.79f, 0.58f),
            star = new Color(1f, 1f, 1f, 0.5f),
            table = C(0.56f, 0.36f, 0.22f), tableTop = C(0.74f, 0.52f, 0.31f), cloth = C(0.88f, 0.76f, 0.55f),
            cellA = C(0.98f, 0.93f, 0.8f), cellB = C(0.94f, 0.87f, 0.71f),
            counter = C(0.8f, 0.57f, 0.34f), counterEdge = C(0.6f, 0.38f, 0.22f), grain = C(0.7f, 0.48f, 0.28f),
        };

        static readonly Pal seftali = new Pal
        {
            low = C(0.98f, 0.79f, 0.69f), high = C(1f, 0.98f, 0.96f),
            accent2 = C(0.95f, 0.5f, 0.45f), disabled = C(0.78f, 0.66f, 0.62f),
            go = C(0.36f, 0.78f, 0.62f), orange = C(1f, 0.55f, 0.47f),
            text = C(0.38f, 0.23f, 0.2f), title = C(0.86f, 0.4f, 0.34f), outline = new Color(0.36f, 0.19f, 0.16f, 0.85f),
            dim = C(0.28f, 0.14f, 0.12f),
            bgTop = C(1f, 0.92f, 0.86f), bgBottom = C(1f, 0.8f, 0.7f), camera = C(1f, 0.8f, 0.7f),
            star = new Color(1f, 1f, 1f, 0.6f),
            table = C(0.62f, 0.4f, 0.33f), tableTop = C(0.95f, 0.66f, 0.55f), cloth = C(0.72f, 0.9f, 0.8f),
            cellA = C(1f, 0.96f, 0.93f), cellB = C(0.99f, 0.89f, 0.83f),
            counter = C(0.98f, 0.72f, 0.6f), counterEdge = C(0.8f, 0.5f, 0.42f), grain = C(0.92f, 0.62f, 0.52f),
        };

        static Pal P => Current == Id.Seftali ? seftali : krem;
        static Color C(float r, float g, float b) => new Color(r, g, b);

        // the colours the old purple UI used for these roles
        static readonly Color oldOrange = new Color(1f, 0.66f, 0.3f), oldGreen = new Color(0.42f, 0.78f, 0.38f), oldDisabled = new Color(0.33f, 0.29f, 0.4f);
        static readonly Color oldTeal = new Color(0.3f, 0.55f, 0.6f);   // storage / home buttons

        // ------------------------------------------------------------------ mapping
        /// <summary>A background colour from the purple UI, in the current theme.</summary>
        public static Color Ui(Color c)
        {
            if (Legacy) return c;
            if (Same(c, oldGreen)) return WithA(P.go, c.a);
            if (Same(c, oldOrange)) return WithA(P.orange, c.a);
            if (Same(c, oldDisabled)) return WithA(P.disabled, c.a);
            if (Same(c, oldTeal)) return WithA(P.accent2, c.a);
            Color.RGBToHSV(c, out float h, out float s, out float v);
            bool purple = (h >= 0.68f && h <= 0.92f) || (h >= 0.5f && h < 0.68f && s < 0.55f && v < 0.45f);   // the purple shades, and the storage's dark teal
            bool nearBlack = v < 0.16f;
            if (!purple && !nearBlack) return c;   // not one of the purple UI shades: a real colour, keep it
            if (c.a < 0.9f && v < 0.2f) return WithA(P.dim, c.a * 0.8f);   // dimmers, toasts
            if (v >= 0.52f && s >= 0.45f) return WithA(P.accent2, c.a);       // strong purple buttons / badges
            float t = Mathf.Clamp01((v - 0.12f) / (0.5f - 0.12f));            // darker purple -> inset, lighter -> raised
            return WithA(Color.Lerp(P.low, P.high, t), c.a);
        }

        /// <summary>
        /// A text colour from the purple UI. On a light panel, light text turns into the theme's dark text (and orange
        /// titles into its darker title colour); on a coloured or dark background (buttons, badges, toasts) it stays light.
        /// <paramref name="onLight"/> is false when the text sits on such a background.
        /// </summary>
        public static Color Ink(Color c, bool onLight)
        {
            if (Legacy || !onLight) return c;
            if (Same(c, oldOrange)) return WithA(P.title, c.a);
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (v > 0.7f && s < 0.38f) return WithA(P.text, c.a);   // white / cream / pale lavender
            if (v > 0.75f) return WithA(Color.HSVToRGB(h, Mathf.Min(1f, s * 1.15f), 0.55f), c.a);   // bright yellow / green / pink: a deeper shade reads on cream
            return c;
        }

        /// <summary>True when a text under this parent sits on a light panel: the nearest visible image above it is light
        /// and not strongly coloured. No image at all counts as the light page background.</summary>
        public static bool OnLight(Transform parent)
        {
            if (Legacy) return false;
            for (var t = parent; t != null; t = t.parent)
            {
                var img = t.GetComponent<UnityEngine.UI.Image>();
                if (img == null || img.color.a < 0.3f) continue;
                Color.RGBToHSV(img.color, out float h, out float s, out float v);
                if (img.sprite != null && img.color.r > 0.97f && img.color.g > 0.97f && img.color.b > 0.97f) continue;   // a picture, look further up
                return v > 0.8f && s < 0.42f;
            }
            return true;
        }

        /// <summary>Rich-text colour tags written for the dark UI (bright yellow, light green, pink) turned into
        /// shades that read on the light panels.</summary>
        public static string Rich(string text)
        {
            if (Legacy || string.IsNullOrEmpty(text) || text.IndexOf("<color=#", System.StringComparison.Ordinal) < 0) return text;
            return text.Replace("<color=#FFD24D>", "<color=#B07A10>").Replace("<color=#9BF28C>", "<color=#3E8E3A>").Replace("<color=#FF9A8A>", "<color=#C0473A>");
        }

        /// <summary>Outline colour for light text.</summary>
        public static Color Outline => Legacy ? new Color(0.08f, 0.04f, 0.12f, 0.85f) : P.outline;

        /// <summary>Level badges on the board items: a small dark circle with a light number.</summary>
        public static Color Badge => Legacy ? new Color(0.14f, 0.09f, 0.2f, 0.92f) : WithA(P.text, 0.92f);

        /// <summary>Main dark text colour on panels (for code that sets text colours itself).</summary>
        public static Color Text => Legacy ? new Color(1f, 0.96f, 0.88f) : P.text;

        public static Color BgTop => Legacy ? new Color(0.22f, 0.13f, 0.3f) : P.bgTop;
        public static Color BgBottom => Legacy ? new Color(0.09f, 0.06f, 0.14f) : P.bgBottom;
        public static Color Camera => Legacy ? new Color(0.09f, 0.06f, 0.14f) : P.camera;
        public static Color Star => Legacy ? new Color(1f, 0.95f, 0.8f, 0.3f) : P.star;

        // board
        public static Color Table => P.table;
        public static Color TableTop => P.tableTop;
        public static Color Cloth => P.cloth;
        public static Color CellA => P.cellA;
        public static Color CellB => P.cellB;
        public static Color Counter => P.counter;
        public static Color CounterEdge => P.counterEdge;
        public static Color Grain => P.grain;

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;
        static Color WithA(Color c, float a) { c.a = a; return c; }
    }
}
