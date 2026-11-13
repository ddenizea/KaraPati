using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Builds the ornaments that go into decoration slots, from existing procedural sprites.</summary>
    public static class DecorArt
    {
        static Image P(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = UIKit.ScenePart(name, parent, s, c, size, sliced);
            img.rectTransform.At(pos, size);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Small preview picture for the decoration panel.</summary>
        public static Sprite Icon(int slot, int variant)
        {
            if (variant == 0) return ProcArt.Lock;
            switch (slot)
            {
                case 0: return variant == 1 ? ProcArt.Pennant : variant == 2 ? ProcArt.Pumpkin : ProcArt.Sparkle;
                case 1: return variant == 1 ? ProcArt.Classic(Chains.Ot, 5) : variant == 2 ? ProcArt.Classic(Chains.Mantar, 5) : ProcArt.Classic(Chains.Kristal, 5);
                case 2: return variant == 1 ? ProcArt.PawPrint : variant == 2 ? ProcArt.Classic(Chains.Ag, 3) : ProcArt.Crate;
                default: return variant == 1 ? ProcArt.Candle(true) : variant == 2 ? ProcArt.Lantern(true) : ProcArt.WitchItem("w_kure");
            }
        }

        public static Color IconTint(int slot, int variant)
        {
            if (slot == 0 && variant == 1) return new Color(1f, 0.55f, 0.2f);
            if (slot == 0 && variant == 3) return new Color(1f, 0.85f, 0.35f);
            if (slot == 2 && variant == 1) return new Color(0.95f, 0.55f, 0.65f);
            return Color.white;
        }

        /// <summary>Touch area for a placed tower ornament.</summary>
        public static Vector2 HitSize(int slot) => slot == 0 ? new Vector2(440f, 110f) : slot == 2 ? new Vector2(190f, 170f) : new Vector2(170f, 210f);

        /// <summary>Where the touch area sits relative to the ornament's origin.</summary>
        public static Vector2 HitOffset(int slot) => slot == 0 ? new Vector2(0f, -25f) : slot == 1 ? new Vector2(0f, 20f) : slot == 2 ? new Vector2(0f, 10f) : new Vector2(0f, 10f);

        /// <summary>Builds the ornament under `parent` at the slot's place. Returns null for "none".</summary>
        public static RectTransform Build(Transform parent, int slot, int variant, Vector3 place)
        {
            if (variant <= 0) return null;
            var g = UIKit.Rect("Decor_" + slot + "_" + variant, parent).At(new Vector2(place.x, place.y), new Vector2(10f, 10f));
            switch (slot)
            {
                case 0: Garland(g, variant, place.z > 0f ? place.z : 500f); break;
                case 1: Plant(g, variant); break;
                case 2: CatCorner(g, variant); break;
                default: Light(g, variant); break;
            }
            return g;
        }

        static void Garland(RectTransform g, int v, float width)
        {
            int count = Mathf.Max(4, Mathf.RoundToInt(width / 80f));
            float step = width / (count - 1);
            // the string sags a little in the middle
            for (int i = 0; i < count - 1; i++)
            {
                float x0 = -width / 2f + i * step, x1 = x0 + step;
                float y0 = Sag(i, count), y1 = Sag(i + 1, count);
                var seg = P("String", g, null, new Color(0.25f, 0.18f, 0.22f), new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f), new Vector2(step + 2f, 4f));
                seg.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg);
            }
            var colors = new[] { new Color(1f, 0.55f, 0.2f), new Color(0.65f, 0.4f, 0.95f), new Color(0.4f, 0.85f, 0.5f) };
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(-width / 2f + i * step, Sag(i, count) - 22f);
                if (v == 1) P("Flag", g, ProcArt.Pennant, colors[i % 3], pos, new Vector2(46f, 52f));
                else if (v == 2) P("Pumpkin", g, ProcArt.Pumpkin, Color.white, pos + new Vector2(0f, -6f), new Vector2(54f, 54f));
                else
                {
                    P("Glow", g, ProcArt.GlowSprite, new Color(1f, 0.9f, 0.5f, 0.35f), pos, new Vector2(80f, 80f));
                    P("Star", g, ProcArt.Sparkle, new Color(1f, 0.88f, 0.4f), pos, new Vector2(i % 2 == 0 ? 44f : 32f, i % 2 == 0 ? 44f : 32f));
                }
            }
        }

        static float Sag(int i, int count)
        {
            float t = i / (float)(count - 1);
            return -28f * Mathf.Sin(t * Mathf.PI);
        }

        static void Pot(RectTransform g, Color c)
        {
            P("Pot", g, ProcArt.Rounded, c, new Vector2(0f, -30f), new Vector2(84f, 64f), true);
            P("Rim", g, ProcArt.Rounded, new Color(c.r * 1.15f, c.g * 1.15f, c.b * 1.15f), new Vector2(0f, 2f), new Vector2(100f, 18f), true);
        }

        static void Plant(RectTransform g, int v)
        {
            if (v == 1) { P("Glow", g, ProcArt.GlowSprite, new Color(0.9f, 1f, 0.7f, 0.35f), new Vector2(0f, 60f), new Vector2(170f, 170f)); Pot(g, new Color(0.62f, 0.36f, 0.24f)); P("Flower", g, ProcArt.Classic(Chains.Ot, 5), Color.white, new Vector2(0f, 62f), new Vector2(120f, 120f)); }
            else if (v == 2) { Pot(g, new Color(0.35f, 0.3f, 0.5f)); P("Shroom", g, ProcArt.Classic(Chains.Mantar, 5), Color.white, new Vector2(0f, 58f), new Vector2(116f, 116f)); P("Shroom", g, ProcArt.Classic(Chains.Mantar, 2), Color.white, new Vector2(-40f, 26f), new Vector2(56f, 56f)); }
            else
            {
                P("Glow", g, ProcArt.GlowSprite, new Color(0.5f, 0.85f, 1f, 0.45f), new Vector2(0f, 40f), new Vector2(190f, 190f));
                P("Stone", g, ProcArt.Rounded, new Color(0.4f, 0.38f, 0.48f), new Vector2(0f, -36f), new Vector2(110f, 40f), true);
                P("Crystal", g, ProcArt.Classic(Chains.Kristal, 5), Color.white, new Vector2(0f, 30f), new Vector2(110f, 110f));
                P("Crystal", g, ProcArt.Classic(Chains.Kristal, 3), Color.white, new Vector2(42f, -4f), new Vector2(56f, 56f));
            }
        }

        static void CatCorner(RectTransform g, int v)
        {
            if (v == 1)
            {
                P("Shadow", g, ProcArt.Circle, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -30f), new Vector2(170f, 40f));
                P("Cushion", g, ProcArt.Circle, new Color(0.9f, 0.45f, 0.6f), new Vector2(0f, -14f), new Vector2(160f, 64f));
                P("Top", g, ProcArt.Circle, new Color(1f, 0.65f, 0.75f), new Vector2(0f, -6f), new Vector2(124f, 40f));
                P("Paw", g, ProcArt.PawPrint, new Color(1f, 0.9f, 0.95f, 0.8f), new Vector2(0f, -6f), new Vector2(34f, 34f));
            }
            else if (v == 2)
            {
                P("Basket", g, ProcArt.Rounded, new Color(0.7f, 0.5f, 0.3f), new Vector2(0f, -22f), new Vector2(120f, 60f), true);
                P("Yarn", g, ProcArt.Classic(Chains.Ag, 3), Color.white, new Vector2(-18f, 14f), new Vector2(80f, 80f));
                P("Yarn", g, ProcArt.Classic(Chains.Ag, 2), Color.white, new Vector2(30f, 8f), new Vector2(60f, 60f));
                P("Rim", g, ProcArt.Rounded, new Color(0.82f, 0.6f, 0.38f), new Vector2(0f, -2f), new Vector2(128f, 14f), true);
            }
            else
            {
                var wood = new Color(0.55f, 0.38f, 0.26f);
                P("Post", g, null, new Color(0.85f, 0.75f, 0.55f), new Vector2(0f, 30f), new Vector2(20f, 150f));
                P("Base", g, ProcArt.Rounded, wood, new Vector2(0f, -40f), new Vector2(130f, 40f), true);
                P("Shelf", g, ProcArt.Rounded, wood, new Vector2(10f, 30f), new Vector2(100f, 24f), true);
                P("Top", g, ProcArt.Rounded, new Color(0.65f, 0.45f, 0.32f), new Vector2(0f, 104f), new Vector2(96f, 30f), true);
                P("Cushion", g, ProcArt.Circle, new Color(0.9f, 0.45f, 0.6f), new Vector2(0f, 120f), new Vector2(84f, 24f));
                P("Ball", g, ProcArt.Circle, new Color(1f, 0.8f, 0.3f), new Vector2(46f, 4f), new Vector2(24f, 24f));
            }
        }

        static void Light(RectTransform g, int v)
        {
            if (v == 1)
            {
                P("Glow", g, ProcArt.GlowSprite, new Color(1f, 0.75f, 0.35f, 0.55f), new Vector2(0f, 20f), new Vector2(230f, 230f));
                P("Candle", g, ProcArt.Candle(true), Color.white, new Vector2(-24f, 0f), new Vector2(84f, 96f));
                P("Candle", g, ProcArt.Candle(true), Color.white, new Vector2(28f, -14f), new Vector2(66f, 76f));
            }
            else if (v == 2)
            {
                P("Glow", g, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.55f), Vector2.zero, new Vector2(240f, 240f));
                P("Hook", g, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(0f, 62f), new Vector2(4f, 50f));
                P("Lantern", g, ProcArt.Lantern(true), Color.white, Vector2.zero, new Vector2(86f, 96f));
            }
            else
            {
                P("Glow", g, ProcArt.GlowSprite, new Color(0.55f, 0.8f, 1f, 0.6f), new Vector2(0f, 10f), new Vector2(230f, 230f));
                P("Stand", g, ProcArt.Rounded, new Color(0.5f, 0.35f, 0.25f), new Vector2(0f, -40f), new Vector2(70f, 26f), true);
                P("Ball", g, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(0f, 4f), new Vector2(100f, 100f));
            }
        }
    }
}
