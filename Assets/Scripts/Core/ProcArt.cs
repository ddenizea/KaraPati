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

        public void Fill(Sdf f, Color c, float soft = 1f)
        {
            float pix = 2f / w;
            float edge = pix * soft;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                float d = f(p);
                if (d > edge) continue;
                float cov = Mathf.Clamp01(0.5f - d / edge);
                if (cov <= 0f) continue;
                Blend(x, y, c, cov);
            }
        }

        public void FillOutlined(Sdf f, Color c, float ow = 0.05f)
        {
            Fill(p => f(p) - ow, ProcArt.Outline);
            Fill(f, c);
        }

        public void Glow(Vector2 c, float r, Color col)
        {
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
        public static readonly Color Outline = new Color(0.12f, 0.07f, 0.19f, 1f);
        const int Size = 128;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int size, System.Action<Painter> draw, int border = 0)
        {
            if (cache.TryGetValue(key, out var s) && s) return s;
            var p = new Painter(size, size);
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

        public static Sprite Gradient => Make("ui_gradient", 64,
            p => p.VerticalGradient(new Color(0.09f, 0.06f, 0.14f), new Color(0.22f, 0.13f, 0.3f)));

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
        public static Sprite Item(string chain, int level) =>
            Make("item_" + chain + "_" + level, Size, p => DrawItem(p, chain, level));

        public static Color ChainColor(string chain)
        {
            switch (chain)
            {
                case Chains.Ot: return new Color(0.5f, 0.85f, 0.4f);
                case Chains.Mantar: return new Color(0.95f, 0.45f, 0.4f);
                case Chains.Kristal: return new Color(0.45f, 0.85f, 1f);
                case Chains.Ag: return new Color(0.8f, 0.7f, 1f);
                case Chains.Kedi: return new Color(1f, 0.7f, 0.35f);
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
                case Chains.Sandik: Chest(p); break;
                case Chains.Simya: SimyaPot(p); break;
                case Chains.Iksir: PotionFlask(p, level); break;
            }
        }

        static void Sparkles(Painter p, int count, Color c)
        {
            var pos = new[] { new Vector2(0.72f, 0.7f), new Vector2(-0.74f, 0.55f), new Vector2(0.68f, -0.72f) };
            var sc = new[] { 0.2f, 0.15f, 0.13f };
            for (int i = 0; i < Mathf.Min(count, pos.Length); i++)
            {
                var c0 = pos[i]; var s0 = sc[i];
                p.Fill(S.Xf(q => S.Poly(q, S.Star(4, 1f, 0.25f)), c0, s0), c);
            }
        }

        // --- herb chain ---
        static void Leaf(Painter p, Vector2 c, float s, float deg, Color body, Color vein)
        {
            Sdf leaf = q => S.I(S.Circle(q, new Vector2(-0.55f, 0f), 1f), S.Circle(q, new Vector2(0.55f, 0f), 1f));
            Sdf stem = q => S.Seg(q, new Vector2(0f, -0.75f), new Vector2(0f, -1.1f), 0.07f);
            Sdf rib = q => S.Seg(q, new Vector2(0f, -0.75f), new Vector2(0f, 0.65f), 0.05f);
            p.FillOutlined(S.Xf(stem, c, s, deg), vein, 0.04f);
            p.FillOutlined(S.Xf(leaf, c, s, deg), body, 0.05f);
            p.Fill(S.Xf(rib, c, s, deg), vein);
        }

        static void DrawOt(Painter p, int level)
        {
            var g = new Color(0.47f, 0.8f, 0.36f); var gv = new Color(0.25f, 0.52f, 0.2f);
            switch (level)
            {
                case 1: Leaf(p, new Vector2(0f, 0f), 0.72f, -25f, g, gv); break;
                case 2:
                    Leaf(p, new Vector2(-0.24f, 0.02f), 0.6f, 25f, g, gv);
                    Leaf(p, new Vector2(0.24f, 0.02f), 0.6f, -25f, g, gv);
                    break;
                case 3:
                case 4:
                {
                    bool magic = level == 4;
                    if (magic) p.Glow(Vector2.zero, 1f, new Color(0.3f, 1f, 0.85f, 0.55f));
                    var body = magic ? new Color(0.35f, 0.92f, 0.78f) : new Color(0.6f, 0.78f, 0.55f);
                    var vein = magic ? new Color(0.1f, 0.5f, 0.45f) : new Color(0.32f, 0.5f, 0.3f);
                    Leaf(p, new Vector2(-0.3f, 0.02f), 0.56f, 32f, body, vein);
                    Leaf(p, new Vector2(0.3f, 0.02f), 0.56f, -32f, body, vein);
                    Leaf(p, new Vector2(0f, 0.12f), 0.6f, 0f, body, vein);
                    p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.52f), new Vector2(0.24f, 0.09f), 0.05f),
                        new Color(0.62f, 0.36f, 0.82f), 0.04f);
                    if (magic) Sparkles(p, 2, new Color(0.8f, 1f, 0.95f));
                    break;
                }
                default:
                {
                    p.Glow(Vector2.zero, 1f, new Color(1f, 0.9f, 0.45f, 0.55f));
                    Leaf(p, new Vector2(-0.42f, -0.45f), 0.38f, 50f, g, gv);
                    Leaf(p, new Vector2(0.42f, -0.45f), 0.38f, -50f, g, gv);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        var pc = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.38f + new Vector2(0f, 0.08f);
                        p.FillOutlined(q => S.Circle(q, pc, 0.3f), new Color(0.98f, 0.95f, 0.82f), 0.04f);
                    }
                    p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.08f), 0.24f), new Color(1f, 0.78f, 0.3f), 0.04f);
                    Sparkles(p, 3, new Color(1f, 0.97f, 0.7f));
                    break;
                }
            }
        }

        // --- mushroom chain ---
        static void Mushroom(Painter p, Vector2 c, float s, Color cap, Color spot, bool spots)
        {
            Sdf stem = q => S.Box(q, new Vector2(0f, -0.38f), new Vector2(0.3f, 0.42f), 0.14f);
            Sdf capS = q => S.I(S.Ellipse(q, Vector2.zero, new Vector2(0.95f, 0.72f)), -(q.y + 0.02f));
            p.FillOutlined(S.Xf(stem, c, s), new Color(0.96f, 0.9f, 0.78f), 0.05f);
            p.FillOutlined(S.Xf(capS, c, s), cap, 0.05f);
            if (!spots) return;
            p.Fill(S.Xf(q => S.Circle(q, new Vector2(-0.42f, 0.22f), 0.13f), c, s), spot);
            p.Fill(S.Xf(q => S.Circle(q, new Vector2(0.12f, 0.45f), 0.11f), c, s), spot);
            p.Fill(S.Xf(q => S.Circle(q, new Vector2(0.48f, 0.15f), 0.1f), c, s), spot);
        }

        static void DrawMantar(Painter p, int level)
        {
            var brown = new Color(0.68f, 0.44f, 0.3f);
            var red = new Color(0.88f, 0.27f, 0.27f);
            switch (level)
            {
                case 1: Mushroom(p, new Vector2(0f, 0.05f), 0.75f, brown, Color.white, false); break;
                case 2: Mushroom(p, new Vector2(0f, 0.05f), 0.8f, red, Color.white, true); break;
                case 3:
                    Mushroom(p, new Vector2(-0.4f, -0.2f), 0.45f, brown, Color.white, false);
                    Mushroom(p, new Vector2(0.4f, -0.2f), 0.45f, brown, Color.white, false);
                    Mushroom(p, new Vector2(0f, 0.12f), 0.58f, red, Color.white, true);
                    break;
                case 4:
                    p.Glow(Vector2.zero, 1f, new Color(0.4f, 0.65f, 1f, 0.55f));
                    Mushroom(p, new Vector2(0f, 0.05f), 0.82f, new Color(0.38f, 0.62f, 1f), new Color(0.85f, 0.97f, 1f), true);
                    Sparkles(p, 2, new Color(0.85f, 0.95f, 1f));
                    break;
                default:
                    p.Glow(Vector2.zero, 1f, new Color(0.8f, 0.45f, 1f, 0.6f));
                    Mushroom(p, new Vector2(0f, 0.05f), 0.88f, new Color(0.72f, 0.42f, 0.95f), new Color(1f, 0.85f, 0.35f), true);
                    Sparkles(p, 3, new Color(1f, 0.92f, 0.6f));
                    break;
            }
        }

        // --- crystal chain ---
        static readonly Vector2[] hex =
        {
            new Vector2(0f, 1f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, -0.45f),
            new Vector2(0f, -1f), new Vector2(-0.5f, -0.45f), new Vector2(-0.5f, 0.45f)
        };
        static readonly Vector2[] facet =
        {
            new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-0.5f, -0.45f), new Vector2(-0.5f, 0.45f)
        };

        static void Crystal(Painter p, Vector2 c, float s, float deg, Color col)
        {
            p.FillOutlined(S.Xf(q => S.Poly(q, hex), c, s, deg), col, 0.05f);
            p.Fill(S.Xf(q => S.Poly(q, facet), c, s, deg), new Color(1f, 1f, 1f, 0.35f));
        }

        static void DrawKristal(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                {
                    var sand = new Color(0.92f, 0.8f, 0.55f);
                    p.FillOutlined(q => S.Circle(q, new Vector2(-0.3f, -0.2f), 0.2f), sand, 0.05f);
                    p.FillOutlined(q => S.Circle(q, new Vector2(0.27f, -0.25f), 0.17f), sand, 0.05f);
                    p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.2f), 0.19f), sand, 0.05f);
                    break;
                }
                case 2:
                    p.FillOutlined(q => S.Ellipse(q, Vector2.zero, new Vector2(0.7f, 0.5f)), new Color(0.6f, 0.6f, 0.68f), 0.05f);
                    p.Fill(q => S.Ellipse(q, new Vector2(-0.22f, 0.18f), new Vector2(0.22f, 0.1f)), new Color(0.82f, 0.82f, 0.88f));
                    break;
                case 3: Crystal(p, Vector2.zero, 0.8f, 0f, new Color(0.4f, 0.85f, 0.95f)); break;
                case 4:
                    p.Glow(Vector2.zero, 1f, new Color(0.8f, 0.7f, 1f, 0.6f));
                    Crystal(p, new Vector2(-0.42f, -0.22f), 0.45f, 25f, new Color(0.7f, 0.62f, 1f));
                    Crystal(p, new Vector2(0.42f, -0.22f), 0.45f, -25f, new Color(0.7f, 0.62f, 1f));
                    Crystal(p, new Vector2(0f, 0.05f), 0.75f, 0f, new Color(0.82f, 0.75f, 1f));
                    Sparkles(p, 2, Color.white);
                    break;
                default:
                {
                    p.Glow(Vector2.zero, 1f, new Color(1f, 0.85f, 0.35f, 0.6f));
                    var big = S.Star(5, 0.85f, 0.38f);
                    var small = S.Star(5, 0.45f, 0.2f);
                    p.FillOutlined(q => S.Poly(q, big), new Color(1f, 0.82f, 0.28f), 0.06f);
                    p.Fill(q => S.Poly(q, small), new Color(1f, 0.95f, 0.65f));
                    Sparkles(p, 3, Color.white);
                    break;
                }
            }
        }

        // --- web / fabric chain ---
        static void DrawAg(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                    p.Fill(q =>
                    {
                        float d = 9f;
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.3f), 0.035f));
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.58f), 0.035f));
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.86f), 0.035f));
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * 45f * Mathf.Deg2Rad;
                            d = S.U(d, S.Seg(q, Vector2.zero, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.9f, 0.03f));
                        }
                        return d;
                    }, new Color(0.9f, 0.9f, 0.97f));
                    p.FillOutlined(q => S.Circle(q, new Vector2(0.3f, -0.3f), 0.12f), new Color(0.2f, 0.15f, 0.25f), 0.03f);
                    break;
                case 2: Yarn(p, 0.62f, new Color(0.78f, 0.72f, 0.88f)); break;
                case 3: Yarn(p, 0.85f, new Color(0.66f, 0.45f, 0.88f)); break;
                case 4:
                    p.Glow(Vector2.zero, 1f, new Color(0.55f, 0.3f, 0.8f, 0.6f));
                    p.FillOutlined(q => S.Box(q, Vector2.zero, new Vector2(0.75f, 0.58f), 0.12f), new Color(0.36f, 0.22f, 0.52f), 0.05f);
                    p.Fill(q => S.Poly(q, new[] { new Vector2(0.75f, 0.58f), new Vector2(0.75f, 0f), new Vector2(0.1f, 0.58f) }),
                        new Color(0.52f, 0.36f, 0.7f));
                    p.Fill(q => S.Box(q, new Vector2(0f, -0.3f), new Vector2(0.75f, 0.05f), 0f), new Color(0.75f, 0.6f, 0.95f));
                    Sparkles(p, 2, new Color(0.9f, 0.8f, 1f));
                    break;
                default:
                    p.Glow(Vector2.zero, 1f, new Color(0.7f, 0.9f, 1f, 0.6f));
                    GhostBody(p, new Vector2(0f, 0f), 0.9f, new Color(0.92f, 0.96f, 1f, 0.95f), false);
                    p.Fill(q => S.Box(q, new Vector2(0f, -0.1f), new Vector2(0.6f, 0.05f), 0f), new Color(0.6f, 0.8f, 1f));
                    p.Fill(q => S.Box(q, new Vector2(0f, -0.35f), new Vector2(0.6f, 0.05f), 0f), new Color(0.6f, 0.8f, 1f));
                    Sparkles(p, 3, Color.white);
                    break;
            }
        }

        static void Yarn(Painter p, float s, Color col)
        {
            var dark = col * 0.7f; dark.a = 1f;
            p.FillOutlined(q => S.Seg(q, new Vector2(0.5f * s, -0.55f * s), new Vector2(0.95f, -0.9f), 0.04f), col, 0.03f);
            p.FillOutlined(q => S.Circle(q, Vector2.zero, 0.85f * s), col, 0.05f);
            p.Fill(q =>
            {
                float ball = S.Circle(q, Vector2.zero, 0.8f * s);
                float st = S.Ring(S.Circle(q, new Vector2(0.35f, 0.35f) * s, 0.55f * s), 0.035f);
                st = S.U(st, S.Ring(S.Circle(q, new Vector2(-0.4f, 0.1f) * s, 0.7f * s), 0.035f));
                st = S.U(st, S.Ring(S.Circle(q, new Vector2(0.1f, -0.5f) * s, 0.6f * s), 0.035f));
                return S.I(st, ball);
            }, dark);
        }

        // --- cats ---
        static void DrawKedi(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                    CatHead(p, new Vector2(0f, -0.05f), 0.95f, new Color(0.96f, 0.63f, 0.32f), new Color(0.45f, 0.8f, 0.35f), false, true, 0);
                    break;
                case 2:
                    CatHead(p, new Vector2(0f, -0.12f), 0.88f, new Color(0.64f, 0.66f, 0.74f), new Color(0.4f, 0.7f, 1f), false, false, 1);
                    break;
                default:
                    p.Glow(Vector2.zero, 1f, new Color(0.7f, 0.4f, 1f, 0.55f));
                    CatHead(p, new Vector2(0f, -0.14f), 0.86f, new Color(0.2f, 0.15f, 0.26f), new Color(1f, 0.85f, 0.2f), false, false, 2);
                    Sparkles(p, 2, new Color(1f, 0.95f, 0.6f));
                    break;
            }
        }

        static void CatHead(Painter p, Vector2 c, float s, Color fur, Color eye, bool sleeping, bool stripes, int hat)
        {
            var earL = new[] { new Vector2(-0.64f, 0.06f), new Vector2(-0.5f, 0.82f), new Vector2(-0.06f, 0.42f) };
            var earR = new[] { new Vector2(0.64f, 0.06f), new Vector2(0.5f, 0.82f), new Vector2(0.06f, 0.42f) };
            var inL = new[] { new Vector2(-0.5f, 0.22f), new Vector2(-0.46f, 0.64f), new Vector2(-0.22f, 0.42f) };
            var inR = new[] { new Vector2(0.5f, 0.22f), new Vector2(0.46f, 0.64f), new Vector2(0.22f, 0.42f) };
            var pink = new Color(1f, 0.62f, 0.68f);
            var dark = new Color(0.12f, 0.08f, 0.15f);
            var furDark = new Color(fur.r * 0.7f, fur.g * 0.65f, fur.b * 0.65f);
            bool blackCat = fur.r < 0.3f;

            p.FillOutlined(S.Xf(q => S.U(S.Circle(q, new Vector2(0f, -0.1f), 0.62f), S.U(S.Poly(q, earL), S.Poly(q, earR))), c, s), fur, 0.05f);
            p.Fill(S.Xf(q => S.U(S.Poly(q, inL), S.Poly(q, inR)), c, s), blackCat ? new Color(0.5f, 0.3f, 0.45f) : pink);

            if (stripes)
                p.Fill(S.Xf(q => S.U(S.Seg(q, new Vector2(0f, 0.44f), new Vector2(0f, 0.28f), 0.04f),
                    S.U(S.Seg(q, new Vector2(-0.16f, 0.42f), new Vector2(-0.13f, 0.28f), 0.035f),
                        S.Seg(q, new Vector2(0.16f, 0.42f), new Vector2(0.13f, 0.28f), 0.035f))), c, s), furDark);

            // cheeks
            p.Fill(S.Xf(q => S.U(S.Circle(q, new Vector2(-0.38f, -0.26f), 0.09f), S.Circle(q, new Vector2(0.38f, -0.26f), 0.09f)), c, s),
                new Color(1f, 0.55f, 0.6f, blackCat ? 0.35f : 0.45f));

            if (sleeping)
            {
                p.Fill(S.Xf(q => S.U(S.Seg(q, new Vector2(-0.36f, -0.06f), new Vector2(-0.14f, -0.1f), 0.035f),
                    S.Seg(q, new Vector2(0.36f, -0.06f), new Vector2(0.14f, -0.1f), 0.035f)), c, s), dark);
            }
            else
            {
                p.Fill(S.Xf(q => S.U(S.Circle(q, new Vector2(-0.24f, -0.04f), 0.12f), S.Circle(q, new Vector2(0.24f, -0.04f), 0.12f)), c, s), eye);
                p.Fill(S.Xf(q => S.U(S.Ellipse(q, new Vector2(-0.24f, -0.04f), new Vector2(0.045f, 0.1f)),
                    S.Ellipse(q, new Vector2(0.24f, -0.04f), new Vector2(0.045f, 0.1f))), c, s), dark);
                p.Fill(S.Xf(q => S.U(S.Circle(q, new Vector2(-0.2f, 0.01f), 0.035f), S.Circle(q, new Vector2(0.28f, 0.01f), 0.035f)), c, s), Color.white);
            }

            var nose = new[] { new Vector2(-0.07f, -0.2f), new Vector2(0.07f, -0.2f), new Vector2(0f, -0.28f) };
            p.Fill(S.Xf(q => S.Poly(q, nose), c, s), pink);
            p.Fill(S.Xf(q => S.U(S.Seg(q, new Vector2(0f, -0.28f), new Vector2(-0.08f, -0.35f), 0.022f),
                S.Seg(q, new Vector2(0f, -0.28f), new Vector2(0.08f, -0.35f), 0.022f)), c, s), blackCat ? new Color(0.6f, 0.5f, 0.6f) : dark);

            var whisk = blackCat ? new Color(0.75f, 0.7f, 0.8f) : new Color(1f, 1f, 1f, 0.9f);
            p.Fill(S.Xf(q =>
            {
                float d = S.Seg(q, new Vector2(0.32f, -0.22f), new Vector2(0.78f, -0.15f), 0.015f);
                d = S.U(d, S.Seg(q, new Vector2(0.32f, -0.28f), new Vector2(0.78f, -0.34f), 0.015f));
                d = S.U(d, S.Seg(q, new Vector2(-0.32f, -0.22f), new Vector2(-0.78f, -0.15f), 0.015f));
                d = S.U(d, S.Seg(q, new Vector2(-0.32f, -0.28f), new Vector2(-0.78f, -0.34f), 0.015f));
                return d;
            }, c, s), whisk);

            if (hat == 1)
            {
                var cone = new[] { new Vector2(-0.28f, 0.46f), new Vector2(0.3f, 0.46f), new Vector2(0.18f, 1.0f) };
                p.FillOutlined(S.Xf(q => S.Poly(q, cone), c, s), new Color(0.3f, 0.5f, 0.9f), 0.04f);
                p.FillOutlined(S.Xf(q => S.Ellipse(q, new Vector2(0f, 0.46f), new Vector2(0.42f, 0.08f)), c, s), new Color(0.25f, 0.4f, 0.8f), 0.04f);
            }
            else if (hat == 2)
            {
                var cone = new[] { new Vector2(-0.38f, 0.48f), new Vector2(0.38f, 0.48f), new Vector2(0.24f, 1.12f) };
                p.FillOutlined(S.Xf(q => S.Poly(q, cone), c, s), new Color(0.5f, 0.28f, 0.75f), 0.04f);
                p.FillOutlined(S.Xf(q => S.Ellipse(q, new Vector2(0f, 0.48f), new Vector2(0.64f, 0.1f)), c, s), new Color(0.42f, 0.22f, 0.65f), 0.04f);
                p.Fill(S.Xf(q => S.Poly(q, S.Star(5, 0.11f, 0.05f)), c + new Vector2(0.02f, 0.72f) * s, s), new Color(1f, 0.85f, 0.3f));
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

        // --- generators ---
        static void Cauldron(Painter p)
        {
            var iron = new Color(0.25f, 0.22f, 0.3f);
            p.FillOutlined(q => S.U(S.Box(q, new Vector2(-0.45f, -0.75f), new Vector2(0.08f, 0.15f), 0.04f),
                S.Box(q, new Vector2(0.45f, -0.75f), new Vector2(0.08f, 0.15f), 0.04f)), iron, 0.04f);
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, -0.18f), new Vector2(0.82f, 0.62f)), iron, 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(-0.35f, -0.15f), new Vector2(0.12f, 0.3f)), new Color(0.4f, 0.36f, 0.48f));
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.36f), new Vector2(0.86f, 0.11f), 0.09f), new Color(0.34f, 0.3f, 0.4f), 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.38f), new Vector2(0.72f, 0.07f)), new Color(0.45f, 0.95f, 0.45f));
            var bub = new Color(0.6f, 1f, 0.55f);
            p.FillOutlined(q => S.Circle(q, new Vector2(-0.25f, 0.6f), 0.13f), bub, 0.035f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0.15f, 0.74f), 0.1f), bub, 0.035f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0.38f, 0.55f), 0.07f), bub, 0.035f);
        }

        static void Chest(Painter p)
        {
            var wood = new Color(0.62f, 0.4f, 0.25f);
            var woodL = new Color(0.74f, 0.5f, 0.32f);
            var gold = new Color(1f, 0.8f, 0.3f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.28f), new Vector2(0.8f, 0.45f), 0.1f), wood, 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.3f), new Vector2(0.82f, 0.26f), 0.18f), woodL, 0.05f);
            p.Fill(q => S.U(S.Box(q, new Vector2(-0.5f, -0.05f), new Vector2(0.06f, 0.62f), 0f),
                S.Box(q, new Vector2(0.5f, -0.05f), new Vector2(0.06f, 0.62f), 0f)), gold);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.05f), new Vector2(0.13f, 0.17f), 0.04f), gold, 0.04f);
            p.Fill(q => S.Circle(q, new Vector2(0f, 0.06f), 0.045f), Outline);
        }
    }
}
