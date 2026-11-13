using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>Signed distance function in normalized [-1,1] space (negative = inside).</summary>
    public delegate float Sdf(Vector2 p);

    /// <summary>Tiny CPU rasterizer used to draw placeholder art at runtime.</summary>
    public class Painter
    {
        readonly int w, h;
        readonly Color[] px;

        public Painter(int w, int h)
        {
            this.w = w; this.h = h;
            px = new Color[w * h];
        }

        /// <summary>Hand-drawn look (the cozy board art): shapes wobble a little like a felt-tip line,
        /// every outline has the same width, and soft glows are left out (flat colours only).</summary>
        public Color[] Pixels => px;

        public bool Cozy;
        public float WobbleSeed;
        const float Wobble = 0.011f;

        Vector2 Warp(Vector2 p)
        {
            if (!Cozy) return p;
            float s = WobbleSeed;
            return new Vector2(
                p.x + (Mathf.Sin(p.y * 7.3f + s) + Mathf.Sin(p.x * 5.1f + p.y * 3.7f + s * 1.7f)) * 0.5f * Wobble,
                p.y + (Mathf.Sin(p.x * 6.9f + s * 2.3f) + Mathf.Sin(p.x * 4.3f - p.y * 6.1f + s * 0.6f)) * 0.5f * Wobble);
        }

        /// <summary>Fills a shape. Works in 8x8 blocks: a block far outside is skipped, one deep inside is painted
        /// without testing every pixel, so bigger sprites stay cheap.</summary>
        public void Fill(Sdf f, Color c, float soft = 1f)
        {
            const int B = 8;
            float pix = 2f / w;
            float edge = pix * soft;
            float reach = pix * B * 0.75f + (Cozy ? Wobble * 2f : 0f);
            for (int by = 0; by < h; by += B)
            for (int bx = 0; bx < w; bx += B)
            {
                var bc = new Vector2((bx + B * 0.5f) / w * 2f - 1f, (by + B * 0.5f) / h * 2f - 1f);
                float bd = f(Warp(bc));
                if (bd > edge + reach) continue;
                bool inside = bd < -(edge + reach);
                int y1 = Mathf.Min(by + B, h), x1 = Mathf.Min(bx + B, w);
                for (int y = by; y < y1; y++)
                for (int x = bx; x < x1; x++)
                {
                    if (inside) { Blend(x, y, c, 1f); continue; }
                    var p = new Vector2((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                    float d = f(Warp(p));
                    if (d > edge) continue;
                    float cov = Mathf.Clamp01(0.5f - d / edge);
                    if (cov <= 0f) continue;
                    Blend(x, y, c, cov);
                }
            }
        }

        public void FillOutlined(Sdf f, Color c, float ow = 0.05f)
        {
            if (Cozy) ow = ProcArt.LineW;
            Fill(p => f(p) - ow, ProcArt.Outline);
            Fill(f, c);
        }

        public void Glow(Vector2 c, float r, Color col)
        {
            if (Cozy) return;   // flat colours only
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                float d = (p - c).magnitude;
                if (d >= r) continue;
                float a = 1f - d / r;
                Blend(x, y, col, a * a);
            }
        }

        public void VerticalGradient(Color bottom, Color top)
        {
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
        }

        void Blend(int x, int y, Color c, float cov)
        {
            int i = y * w + x;
            var dst = px[i];
            float sa = c.a * cov;
            float oa = sa + dst.a * (1f - sa);
            if (oa <= 0.0001f) return;
            float k = dst.a * (1f - sa);
            px[i] = new Color(
                (c.r * sa + dst.r * k) / oa,
                (c.g * sa + dst.g * k) / oa,
                (c.b * sa + dst.b * k) / oa,
                oa);
        }

        /// <summary>Paint everything drawn so far in one flat colour, keeping its shape (a silhouette).</summary>
        public void Silhouette(Color c)
        {
            for (int i = 0; i < px.Length; i++)
                if (px[i].a > 0f) px[i] = new Color(c.r, c.g, c.b, px[i].a * c.a);
        }

        public Texture2D ToTexture() => ToTexture(false);

        public Texture2D ToTexture(bool readable)
        {
            // Bleed colour into fully transparent pixels to avoid dark halos when filtered.
            for (int i = 0; i < px.Length; i++)
                if (px[i].a <= 0f) px[i] = new Color(0.5f, 0.45f, 0.6f, 0f);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            t.SetPixels(px);
            t.Apply(false, !readable);
            return t;
        }
    }

    /// <summary>SDF helpers.</summary>
    public static class S
    {
        public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
        {
            var q = p - c;
            q.x /= r.x; q.y /= r.y;
            return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        }

        public static float Box(Vector2 p, Vector2 c, Vector2 half, float rad)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(rad, rad);
            var m = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return m.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - rad;
        }

        public static float Seg(Vector2 p, Vector2 a, Vector2 b, float th)
        {
            var pa = p - a; var ba = b - a;
            float hh = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * hh).magnitude - th;
        }

        public static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            int n = v.Length;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                var e = v[j] - v[i];
                var wv = p - v[i];
                var b = wv - e * Mathf.Clamp01(Vector2.Dot(wv, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * wv.y > e.y * wv.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public static float Ring(float d, float th) => Mathf.Abs(d) - th;
        public static float U(float a, float b) => Mathf.Min(a, b);
        public static float I(float a, float b) => Mathf.Max(a, b);
        public static float Sub(float a, float b) => Mathf.Max(a, -b);

        public static Vector2 Rot(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        /// <summary>Place a unit-space shape at centre c with scale s and rotation deg.</summary>
        public static Sdf Xf(Sdf f, Vector2 c, float s, float deg = 0f) => p => f(Rot((p - c) / s, -deg)) * s;

        public static Vector2[] Star(int points, float outer, float inner, float rotDeg = 90f)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (rotDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outer : inner;
                v[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return v;
        }
    }

    /// <summary>All placeholder sprites, generated once and cached.</summary>
    public static partial class ProcArt
    {
        public static readonly Color Outline = new Color(0.29f, 0.165f, 0.094f, 1f);   // warm brown felt-tip line
        public const float LineW = 0.048f;                                             // the one line width of the cozy art
        const int Size = 160;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int size, System.Action<Painter> draw, int border = 0)
        {
            if (cache.TryGetValue(key, out var s) && s) return s;
            var p = new Painter(size, size);
            if (IsCozy(key)) { p.Cozy = true; p.WobbleSeed = (key.GetHashCode() & 1023) * 0.37f; }
            draw(p);
            var tex = p.ToTexture();
            tex.name = key;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = key;
            cache[key] = s;
            return s;
        }

        // ---------- UI primitives ----------
        public static Sprite Rounded => Make("ui_rounded", 64,
            p => p.Fill(q => S.Box(q, Vector2.zero, new Vector2(0.98f, 0.98f), 0.62f), Color.white), 24);

        public static Sprite Frame => Make("ui_frame", 64,
            p => p.Fill(q => S.Ring(S.Box(q, Vector2.zero, new Vector2(0.9f, 0.9f), 0.55f), 0.08f), Color.white), 24);

        public static Sprite Circle => Make("ui_circle", Size,
            p => p.Fill(q => S.Circle(q, Vector2.zero, 0.97f), Color.white));

        public static Sprite Gradient => Make("ui_gradient_" + (int)Theme.Current, 64,
            p => p.VerticalGradient(Theme.BgBottom, Theme.BgTop));

        public static Sprite Sparkle => Make("ui_sparkle", 64,
            p => p.Fill(q => S.Poly(q, S.Star(4, 0.95f, 0.22f)), Color.white));

        public static Sprite Coin => Make("ui_coin", Size, p =>
        {
            p.FillOutlined(q => S.Circle(q, Vector2.zero, 0.82f), new Color(1f, 0.78f, 0.25f), 0.07f);
            p.Fill(q => S.Ring(S.Circle(q, Vector2.zero, 0.58f), 0.05f), new Color(0.85f, 0.55f, 0.12f));
            p.Fill(q => S.Poly(q, S.Star(5, 0.35f, 0.15f)), new Color(1f, 0.92f, 0.55f));
        });

        public static Sprite Bolt => Make("ui_bolt", Size, p =>
        {
            var v = new[]
            {
                new Vector2(0.18f, 0.95f), new Vector2(-0.55f, -0.1f), new Vector2(-0.05f, -0.1f),
                new Vector2(-0.22f, -0.95f), new Vector2(0.55f, 0.15f), new Vector2(0.05f, 0.15f)
            };
            p.FillOutlined(q => S.Poly(q, v), new Color(1f, 0.85f, 0.25f), 0.07f);
        });

        public static Sprite Check => Make("ui_check", Size, p =>
        {
            p.Fill(q => S.Circle(q, Vector2.zero, 0.95f), new Color(0.3f, 0.72f, 0.35f));
            p.Fill(q => S.U(S.Seg(q, new Vector2(-0.45f, 0f), new Vector2(-0.12f, -0.35f), 0.12f),
                S.Seg(q, new Vector2(-0.12f, -0.35f), new Vector2(0.48f, 0.38f), 0.12f)), Color.white);
        });

        public static Sprite SleepCat => Make("sleepcat", Size, p =>
            CatHead(p, new Vector2(0f, -0.08f), 0.95f, new Color(0.96f, 0.63f, 0.32f), Color.black, true, true, 0));

        static readonly Color[] ghostTints =
        {
            new Color(0.95f, 0.97f, 1f), new Color(0.8f, 0.95f, 0.9f), new Color(0.95f, 0.85f, 0.95f),
            new Color(0.85f, 0.9f, 1f), new Color(1f, 0.95f, 0.8f), new Color(0.9f, 0.85f, 1f),
            new Color(1f, 0.85f, 0.85f), new Color(0.85f, 1f, 0.8f)
        };
        public static int GhostTintCount => ghostTints.Length;

        public static Sprite Ghost(int tint) => Make("ghost_" + tint, Size, p =>
        {
            var col = ghostTints[Mathf.Abs(tint) % ghostTints.Length];
            p.Glow(Vector2.zero, 1f, new Color(col.r, col.g, col.b, 0.25f));
            GhostBody(p, Vector2.zero, 0.95f, col, true);
        });

        // ---------- Items ----------
        /// <summary>Board item picture. Uses the hand-drawn art in Resources/Items ("chain_level.png") when there is one,
        /// otherwise falls back to the procedural drawing (cats, and anything not drawn yet).</summary>
        public static Sprite Item(string chain, int level)
        {
            string key = "item_" + chain + "_" + level;
            if (cache.TryGetValue(key, out var s) && s) return s;
            s = LoadArt("Items/" + chain + "_" + level, key);
            return s ? s : Make(key, Size, p => DrawItem(p, chain, level));
        }

        /// <summary>The procedural version of an item, for screens that keep the old look for now
        /// (the cauldron and chest on the home screen, gift chests, decorations).</summary>
        public static Sprite Classic(string chain, int level) =>
            Make("classic_" + chain + "_" + level, Size, p => DrawItem(p, chain, level));

        static Sprite LoadArt(string path, string key)
        {
            var s = Resources.Load<Sprite>(path);
            if (!s)
            {
                // imported as a plain texture (import settings not applied yet): wrap it ourselves
                var tex = Resources.Load<Texture2D>(path);
                if (!tex) return null;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            s.name = key;
            cache[key] = s;
            return s;
        }

        public static Color ChainColor(string chain)
        {
            switch (chain)
            {
                case Chains.Ot: return new Color(0.5f, 0.85f, 0.4f);
                case Chains.Mantar: return new Color(0.95f, 0.45f, 0.4f);
                case Chains.Kristal: return new Color(0.45f, 0.85f, 1f);
                case Chains.Ag: return new Color(0.8f, 0.7f, 1f);
                case Chains.Kedi: return new Color(1f, 0.7f, 0.35f);
                case Chains.Sihir: return new Color(1f, 0.56f, 0.8f);
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }

        static void DrawItem(Painter p, string chain, int level)
        {
            switch (chain)
            {
                case Chains.Ot: DrawOt(p, level); break;
                case Chains.Mantar: DrawMantar(p, level); break;
                case Chains.Kristal: DrawKristal(p, level); break;
                case Chains.Ag: DrawAg(p, level); break;
                case Chains.Kedi: DrawKedi(p, level); break;
                case Chains.Kazan: Cauldron(p); break;
                case Chains.KaraKazan: Cauldron(p); break;
                case Chains.Sandik: Chest(p); break;
                case Chains.Simya: SimyaPot(p); break;
                case Chains.KediEvi: CatHouse(p); break;
                case Chains.Iksir: PotionFlask(p, level); break;
            }
        }

        // --- ghosts ---
        static void GhostBody(Painter p, Vector2 c, float s, Color col, bool face)
        {
            p.FillOutlined(S.Xf(q =>
            {
                float d = S.U(S.Circle(q, new Vector2(0f, 0.2f), 0.62f), S.Box(q, new Vector2(0f, -0.25f), new Vector2(0.62f, 0.45f), 0f));
                d = S.U(d, S.Circle(q, new Vector2(-0.41f, -0.68f), 0.21f));
                d = S.U(d, S.Circle(q, new Vector2(0f, -0.68f), 0.21f));
                d = S.U(d, S.Circle(q, new Vector2(0.41f, -0.68f), 0.21f));
                return d;
            }, c, s), col, 0.05f);
            if (!face) return;
            var dark = new Color(0.15f, 0.1f, 0.22f);
            p.Fill(S.Xf(q => S.U(S.Ellipse(q, new Vector2(-0.22f, 0.22f), new Vector2(0.09f, 0.13f)),
                S.Ellipse(q, new Vector2(0.22f, 0.22f), new Vector2(0.09f, 0.13f))), c, s), dark);
            p.Fill(S.Xf(q => S.U(S.Circle(q, new Vector2(-0.19f, 0.27f), 0.03f), S.Circle(q, new Vector2(0.25f, 0.27f), 0.03f)), c, s), Color.white);
            p.Fill(S.Xf(q => S.Ellipse(q, new Vector2(0f, -0.05f), new Vector2(0.09f, 0.07f)), c, s), dark);
            p.Fill(S.Xf(q => S.U(S.Circle(q, new Vector2(-0.38f, 0.02f), 0.08f), S.Circle(q, new Vector2(0.38f, 0.02f), 0.08f)), c, s),
                new Color(1f, 0.6f, 0.7f, 0.5f));
        }

    }
}
