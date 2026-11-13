using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// The cozy board art (Neko Atsume-like, "yurukawa"): one warm brown felt-tip line of the same width everywhere,
    /// flat warm colours without glows, a slight hand-drawn wobble, and very simple cats (dot eyes, a little "w" mouth,
    /// round coat patches). A higher level looks more valuable through size, extra parts, a pale flat halo and little
    /// outlined stars. Painter.Cozy (set in Make for the keys below) turns the line, wobble and no-glow rules on.
    /// </summary>
    public static partial class ProcArt
    {
        static bool IsCozy(string key) =>
            key.StartsWith("item_") || key.StartsWith("classic_") || key.StartsWith("sidecat_") || key.StartsWith("backstir_") ||
            key == "karapati_head" || key == "sleepcat" || key == "ui_crate" || key == "mg_yarn";

        static readonly Color Ink = new Color(0.29f, 0.165f, 0.094f);
        static readonly Color SparkCol = new Color(1f, 0.83f, 0.3f);

        static void Shape(Painter p, Sdf f, Color c) => p.FillOutlined(f, c, LineW);
        static void InkLine(Painter p, Vector2 a, Vector2 b, float w = 0.026f) => p.Fill(q => S.Seg(q, a, b, w), Ink);
        static void Halo(Painter p, Vector2 c, float r, Color col) => p.Fill(q => S.Circle(q, c, r), col);
        static void Spark(Painter p, Vector2 c, float r) => Shape(p, S.Xf(q => S.Poly(q, S.Star(4, 1f, 0.42f)), c, r), SparkCol);

        static void Sparkles(Painter p, int count, Color c)
        {
            var pos = new[] { new Vector2(0.7f, 0.68f), new Vector2(-0.72f, 0.52f), new Vector2(0.7f, -0.66f) };
            var sc = new[] { 0.17f, 0.13f, 0.11f };
            for (int i = 0; i < Mathf.Min(count, pos.Length); i++) Spark(p, pos[i], sc[i]);
        }

        /// <summary>Lower half of a ring: a "u" stroke (closed eyes, the cat's "w" mouth).</summary>
        static float Cup(Vector2 q, Vector2 c, float r, float w) => S.I(S.Ring(S.Circle(q, c, r), w), q.y - c.y);

        // ================================================================== cats
        // coat colour, eye colour (shown on dark cats) and pattern for each KittenColors index
        // (0 none, 1 stripes, 2 black patches, 3 calico)
        static readonly Color[] catFur =
        {
            new Color(0.94f, 0.63f, 0.29f), new Color(0.62f, 0.62f, 0.68f), new Color(0.96f, 0.87f, 0.69f),
            new Color(0.99f, 0.97f, 0.92f), new Color(0.99f, 0.97f, 0.92f), new Color(0.25f, 0.23f, 0.28f)
        };
        static readonly Color[] catEye =
        {
            new Color(0.45f, 0.8f, 0.35f), new Color(0.4f, 0.7f, 1f), new Color(0.4f, 0.7f, 1f),
            new Color(0.75f, 0.85f, 0.3f), new Color(1f, 0.75f, 0.25f), new Color(1f, 0.82f, 0.25f)
        };
        static readonly int[] catPattern = { 1, 0, 0, 2, 3, 0 };
        static readonly Color patchDark = new Color(0.25f, 0.23f, 0.28f), patchOrange = new Color(0.94f, 0.63f, 0.29f);
        static readonly Color earPink = new Color(1f, 0.7f, 0.68f);

        static bool IsDark(Color fur) => fur.r + fur.g + fur.b < 1.2f;
        static Color Darker(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        /// <summary>Dot eyes (dark, or the eye colour on a dark cat), closed "u" eyes when sleeping, and the "w" mouth.
        /// c = face centre, r = head radius.</summary>
        static void SimpleFace(Painter p, Vector2 c, float r, Color fur, Color eye, bool sleeping)
        {
            bool dark = IsDark(fur);
            var ec = dark ? eye : Ink;
            var l = c + new Vector2(-0.42f, 0.04f) * r;
            var rr = c + new Vector2(0.42f, 0.04f) * r;
            float lw = Mathf.Max(0.018f, 0.055f * r);
            if (sleeping)
                p.Fill(q => S.U(Cup(q, l + new Vector2(0f, 0.08f * r), 0.13f * r, lw), Cup(q, rr + new Vector2(0f, 0.08f * r), 0.13f * r, lw)), ec);
            else
                p.Fill(q => S.U(S.Ellipse(q, l, new Vector2(0.08f, 0.1f) * r), S.Ellipse(q, rr, new Vector2(0.08f, 0.1f) * r)), ec);
            var m = c + new Vector2(0f, -0.2f) * r;
            var mc = dark ? new Color(0.86f, 0.8f, 0.82f) : Ink;
            p.Fill(q => S.U(Cup(q, m + new Vector2(-0.085f, 0.03f) * r, 0.085f * r, lw * 0.8f), Cup(q, m + new Vector2(0.085f, 0.03f) * r, 0.085f * r, lw * 0.8f)), mc);
        }

        /// <summary>A cat head facing the player: a wide round head with two small ears. Returns its shape so coat
        /// patches can be painted inside it. hc = centre, r = half the head width.</summary>
        static Sdf CatHeadShape(Vector2 hc, float r)
        {
            var el = new[] { hc + new Vector2(-0.96f, 0.1f) * r, hc + new Vector2(-0.74f, 0.98f) * r, hc + new Vector2(-0.22f, 0.66f) * r };
            var er = new[] { hc + new Vector2(0.96f, 0.1f) * r, hc + new Vector2(0.74f, 0.98f) * r, hc + new Vector2(0.22f, 0.66f) * r };
            return q => S.U(S.Ellipse(q, hc, new Vector2(1f, 0.8f) * r), S.U(S.Poly(q, el), S.Poly(q, er)) - 0.03f * r);
        }

        static void HeadAt(Painter p, Vector2 hc, float r, Color fur, Color eye, bool sleeping, int pattern, int hat)
        {
            var head = CatHeadShape(hc, r);
            Shape(p, head, fur);
            // pink inside the ears
            var il = new[] { hc + new Vector2(-0.78f, 0.36f) * r, hc + new Vector2(-0.7f, 0.78f) * r, hc + new Vector2(-0.42f, 0.6f) * r };
            var ir = new[] { hc + new Vector2(0.78f, 0.36f) * r, hc + new Vector2(0.7f, 0.78f) * r, hc + new Vector2(0.42f, 0.6f) * r };
            p.Fill(q => S.U(S.Poly(q, il), S.Poly(q, ir)), IsDark(fur) ? new Color(0.55f, 0.42f, 0.5f) : earPink);
            if (pattern == 2 || pattern == 3)   // a patch over one ear and eye
                p.Fill(q => S.I(head(q) + 0.004f, S.Circle(q, hc + new Vector2(-0.6f, 0.5f) * r, 0.52f * r)), pattern == 3 ? patchOrange : patchDark);
            if (pattern == 3)
                p.Fill(q => S.I(head(q) + 0.004f, S.Circle(q, hc + new Vector2(0.85f, 0.55f) * r, 0.34f * r)), patchDark);
            if (pattern == 1)   // tabby: three short stripes on the forehead
            {
                var sc = Darker(fur, 0.78f);
                float w = Mathf.Max(0.016f, 0.05f * r);
                for (int i = -1; i <= 1; i++)
                    p.Fill(q => S.Seg(q, hc + new Vector2(i * 0.2f, 0.72f) * r, hc + new Vector2(i * 0.18f, 0.44f) * r, w), sc);
            }
            SimpleFace(p, hc + new Vector2(0f, -0.08f) * r, r, fur, eye, sleeping);
            Hat(p, hc, r, hat);
        }

        /// <summary>hat: 1 the apprentice's blue cone, 2 the master's witch hat, 3 the Mini Apprentice's red cap.</summary>
        static void Hat(Painter p, Vector2 hc, float r, int hat)
        {
            if (hat == 1)
            {
                Shape(p, q => S.Poly(q, new[] { hc + new Vector2(-0.42f, 0.62f) * r, hc + new Vector2(0.42f, 0.62f) * r, hc + new Vector2(0.06f, 1.5f) * r }) - 0.02f * r, new Color(0.38f, 0.62f, 0.95f));
                Shape(p, q => S.Ellipse(q, hc + new Vector2(0f, 0.62f) * r, new Vector2(0.62f, 0.13f) * r), new Color(0.3f, 0.5f, 0.86f));
            }
            else if (hat == 2)
            {
                Shape(p, q => S.Poly(q, new[] { hc + new Vector2(-0.5f, 0.64f) * r, hc + new Vector2(0.5f, 0.64f) * r, hc + new Vector2(0.32f, 1.7f) * r }) - 0.02f * r, new Color(0.56f, 0.36f, 0.8f));
                Shape(p, q => S.Ellipse(q, hc + new Vector2(0f, 0.64f) * r, new Vector2(0.9f, 0.16f) * r), new Color(0.46f, 0.28f, 0.7f));
                p.Fill(q => S.Box(q, hc + new Vector2(0.02f, 0.82f) * r, new Vector2(0.46f, 0.07f) * r, 0f), new Color(1f, 0.78f, 0.32f));
                Spark(p, hc + new Vector2(0.26f, 1.16f) * r, 0.16f * r);
            }
            else if (hat == 3)
            {
                Shape(p, q => S.U(S.Ellipse(q, hc + new Vector2(0f, 0.66f) * r, new Vector2(0.5f, 0.32f) * r), S.Box(q, hc + new Vector2(0f, 0.58f) * r, new Vector2(0.58f, 0.09f) * r, 0.04f * r)), new Color(0.9f, 0.36f, 0.38f));
                Shape(p, q => S.Circle(q, hc + new Vector2(0.05f, 1.02f) * r, 0.14f * r), new Color(1f, 0.97f, 0.9f));
            }
        }

        /// <summary>Big head (item icons, Kara Pati's portrait, the app icon).</summary>
        static void CatHead(Painter p, Vector2 c, float s, Color fur, Color eye, bool sleeping, bool stripes, int hat) =>
            HeadAt(p, c + new Vector2(0f, -0.1f) * s, 0.7f * s, fur, eye, sleeping, stripes ? 1 : 0, hat);

        static void Patches(Painter p, Sdf body, int pattern, Vector2 a, float ra, Vector2 b, float rb)
        {
            if (pattern < 2) return;
            p.Fill(q => S.I(body(q) + 0.004f, S.Circle(q, a, ra)), pattern == 3 ? patchOrange : patchDark);
            p.Fill(q => S.I(body(q) + 0.004f, S.Circle(q, b, rb)), patchDark);
        }

        /// <summary>Old signature (the back-view stirring cat): patches inside an ellipse body.</summary>
        static void Patches(Painter p, int pattern, Vector2 bc, Vector2 br, Vector2 a, float ra, Vector2 b, float rb) =>
            Patches(p, q => S.Ellipse(q, bc, br * 0.97f), pattern, a, ra, b, rb);

        static void Tail(Painter p, Vector2 a, Vector2 b, Vector2 c, Color col) =>
            Shape(p, q => S.U(S.Seg(q, a, b, 0.055f), S.Seg(q, b, c, 0.055f)), col);

        static void BackStripes(Painter p, Color fur, params Vector2[] pts)
        {
            var sc = Darker(fur, 0.78f);
            for (int i = 0; i + 1 < pts.Length; i += 2) { var a = pts[i]; var b = pts[i + 1]; p.Fill(q => S.Seg(q, a, b, 0.03f), sc); }
        }

        /// <summary>Board cat seen from the side with its face turned to the player.
        /// frame 0-3 walk cycle, 4 sit, 5 sleep. hat: 0 none, 1 apprentice, 2 master, 3 Mini Apprentice.</summary>
        static void DrawSideCat(Painter p, Color fur, Color eye, int pattern, int hat, int frame)
        {
            var limb = pattern == 2 ? patchDark : pattern == 3 ? patchOrange : fur;
            if (frame <= 3)
            {
                float ph = frame * Mathf.PI * 0.5f;
                float[] lx = { -0.4f, -0.22f, 0.1f, 0.28f };
                float[] off = { 0f, Mathf.PI, Mathf.PI, 0f };
                var far = Darker(fur, 0.86f);
                float sw = Mathf.Sin(ph) * 0.05f;
                Tail(p, new Vector2(-0.5f, -0.06f), new Vector2(-0.72f, 0.16f + sw), new Vector2(-0.64f + sw, 0.44f), limb);
                for (int i = 1; i < 4; i += 2) { float x = lx[i], d = Mathf.Sin(ph + off[i]) * 0.08f; Shape(p, q => S.Seg(q, new Vector2(x, -0.22f), new Vector2(x + d, -0.6f), 0.07f), far); }
                Sdf body = q => S.Ellipse(q, new Vector2(-0.08f, -0.14f), new Vector2(0.54f, 0.28f));
                Shape(p, body, fur);
                Patches(p, body, pattern, new Vector2(-0.3f, 0.02f), 0.2f, new Vector2(0.1f, -0.3f), 0.14f);
                if (pattern == 1) BackStripes(p, fur, new Vector2(-0.34f, 0.1f), new Vector2(-0.32f, -0.04f), new Vector2(-0.16f, 0.13f), new Vector2(-0.14f, -0.01f), new Vector2(0.02f, 0.13f), new Vector2(0.04f, -0.01f));
                for (int i = 0; i < 4; i += 2) { float x = lx[i], d = Mathf.Sin(ph + off[i]) * 0.08f; Shape(p, q => S.Seg(q, new Vector2(x, -0.22f), new Vector2(x + d, -0.62f), 0.07f), fur); }
                HeadAt(p, new Vector2(0.44f, 0.14f), 0.31f, fur, eye, false, pattern, hat);
            }
            else if (frame == 4)
            {
                // sitting, facing the player
                Tail(p, new Vector2(0.18f, -0.66f), new Vector2(0.5f, -0.62f), new Vector2(0.56f, -0.32f), limb);
                Sdf body = q => S.Ellipse(q, new Vector2(0f, -0.36f), new Vector2(0.34f, 0.36f));
                Shape(p, body, fur);
                Patches(p, body, pattern, new Vector2(-0.2f, -0.22f), 0.17f, new Vector2(0.18f, -0.5f), 0.12f);
                if (pattern == 1) BackStripes(p, fur, new Vector2(-0.3f, -0.2f), new Vector2(-0.18f, -0.24f), new Vector2(0.3f, -0.2f), new Vector2(0.18f, -0.24f));
                InkLine(p, new Vector2(-0.1f, -0.4f), new Vector2(-0.1f, -0.7f), 0.022f);   // front legs, Neko style
                InkLine(p, new Vector2(0.1f, -0.4f), new Vector2(0.1f, -0.7f), 0.022f);
                HeadAt(p, new Vector2(0f, 0.18f), 0.36f, fur, eye, false, pattern, hat);
            }
            else
            {
                // sleeping loaf, tail wrapped in front
                Sdf body = q => S.Ellipse(q, new Vector2(-0.06f, -0.42f), new Vector2(0.62f, 0.27f));
                Shape(p, body, fur);
                Patches(p, body, pattern, new Vector2(-0.3f, -0.26f), 0.2f, new Vector2(0.12f, -0.52f), 0.13f);
                if (pattern == 1) BackStripes(p, fur, new Vector2(-0.36f, -0.2f), new Vector2(-0.34f, -0.32f), new Vector2(-0.16f, -0.17f), new Vector2(-0.14f, -0.3f));
                Shape(p, q => S.Seg(q, new Vector2(-0.6f, -0.56f), new Vector2(0.08f, -0.64f), 0.055f), limb);
                HeadAt(p, new Vector2(0.4f, -0.3f), 0.29f, fur, eye, true, pattern, hat);
            }
        }

        // the board's cat items: wild kitten, apprentice, master
        static void DrawKedi(Painter p, int level)
        {
            switch (level)
            {
                case 1: CatHead(p, new Vector2(0f, -0.08f), 1f, catFur[0], catEye[0], false, true, 0); break;
                case 2: CatHead(p, new Vector2(0f, -0.2f), 0.92f, catFur[1], catEye[1], false, false, 1); break;
                default:
                    Halo(p, new Vector2(0f, 0f), 0.92f, new Color(0.93f, 0.86f, 1f));
                    CatHead(p, new Vector2(0f, -0.22f), 0.9f, catFur[5], catEye[5], false, false, 2);
                    Spark(p, new Vector2(0.74f, -0.1f), 0.14f);
                    break;
            }
        }

        // ================================================================== herb chain
        static readonly Color LeafG = new Color(0.55f, 0.8f, 0.36f), LeafV = new Color(0.33f, 0.58f, 0.24f);

        static void Leaf(Painter p, Vector2 c, float s, float deg, Color body, Color vein)
        {
            Sdf leaf = q => S.I(S.Circle(q, new Vector2(-0.5f, 0f), 1f), S.Circle(q, new Vector2(0.5f, 0f), 1f));
            Shape(p, S.Xf(leaf, c, s, deg), body);
            p.Fill(S.Xf(q => S.Seg(q, new Vector2(0f, -0.7f), new Vector2(0f, 0.45f), 0.022f / s), c, s, deg), vein);
        }

        static void Stems(Painter p, Vector2 root, Color col, params Vector2[] tips)
        {
            foreach (var t in tips) { var tt = t; Shape(p, q => S.Seg(q, root, tt, 0.03f), col); }
        }

        static void Bunch(Painter p, Color body, Color vein, Color tie, int leaves)
        {
            var root = new Vector2(0f, -0.76f);
            if (leaves == 3)
            {
                Stems(p, root, vein, new Vector2(-0.24f, -0.16f), new Vector2(0.24f, -0.16f), new Vector2(0f, -0.06f));
                Leaf(p, new Vector2(-0.34f, 0.1f), 0.42f, 30f, body, vein);
                Leaf(p, new Vector2(0.34f, 0.1f), 0.42f, -30f, body, vein);
                Leaf(p, new Vector2(0f, 0.3f), 0.48f, 0f, body, vein);
            }
            else
            {
                Stems(p, root, vein, new Vector2(-0.3f, -0.2f), new Vector2(0.3f, -0.2f), new Vector2(0f, -0.1f));
                Leaf(p, new Vector2(-0.52f, -0.02f), 0.36f, 58f, body, vein);
                Leaf(p, new Vector2(0.52f, -0.02f), 0.36f, -58f, body, vein);
                Leaf(p, new Vector2(-0.27f, 0.22f), 0.43f, 24f, body, vein);
                Leaf(p, new Vector2(0.27f, 0.22f), 0.43f, -24f, body, vein);
                Leaf(p, new Vector2(0f, 0.34f), 0.5f, 0f, body, vein);
            }
            Shape(p, q => S.Box(q, new Vector2(0f, -0.5f), new Vector2(0.17f, 0.075f), 0.05f), tie);
        }

        static void DrawOt(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                    Shape(p, q => S.Seg(q, new Vector2(0.14f, -0.74f), new Vector2(0.05f, -0.42f), 0.03f), LeafV);
                    Leaf(p, new Vector2(-0.04f, 0.04f), 0.66f, 14f, LeafG, LeafV);
                    break;
                case 2: Bunch(p, LeafG, LeafV, new Color(0.9f, 0.72f, 0.45f), 3); break;
                case 3: Bunch(p, new Color(0.66f, 0.78f, 0.6f), new Color(0.4f, 0.55f, 0.38f), new Color(0.7f, 0.52f, 0.86f), 5); break;
                case 4:
                    Halo(p, new Vector2(0f, 0.04f), 0.9f, new Color(0.84f, 0.95f, 0.9f));
                    Bunch(p, new Color(0.36f, 0.8f, 0.7f), new Color(0.16f, 0.5f, 0.48f), new Color(0.96f, 0.5f, 0.62f), 5);
                    Sparkles(p, 2, SparkCol);
                    break;
                default:   // Ay Çiçeği
                {
                    Halo(p, new Vector2(0f, 0.12f), 0.92f, new Color(1f, 0.95f, 0.78f));
                    Shape(p, q => S.Seg(q, new Vector2(0f, -0.86f), new Vector2(0f, 0.1f), 0.032f), LeafV);
                    Leaf(p, new Vector2(-0.32f, -0.52f), 0.3f, 60f, LeafG, LeafV);
                    Leaf(p, new Vector2(0.32f, -0.52f), 0.3f, -60f, LeafG, LeafV);
                    var fc = new Vector2(0f, 0.2f);
                    Shape(p, q =>
                    {
                        float d = 9f;
                        for (int i = 0; i < 6; i++)
                        {
                            float a = (90f + i * 60f) * Mathf.Deg2Rad;
                            d = S.U(d, S.Circle(q, fc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.34f, 0.24f));
                        }
                        return d;
                    }, new Color(0.99f, 0.97f, 0.9f));
                    Shape(p, q => S.Circle(q, fc, 0.2f), new Color(1f, 0.78f, 0.3f));
                    Sparkles(p, 3, SparkCol);
                    break;
                }
            }
        }

        // ================================================================== mushroom chain
        static void Mushroom(Painter p, Vector2 c, float s, Color cap, Color spot, bool spots)
        {
            Shape(p, S.Xf(q => S.Box(q, new Vector2(0f, -0.4f), new Vector2(0.28f, 0.42f), 0.14f), c, s), new Color(0.98f, 0.93f, 0.82f));
            Sdf capS = q => S.I(S.Ellipse(q, Vector2.zero, new Vector2(0.95f, 0.74f)), -(q.y + 0.02f));
            Shape(p, S.Xf(capS, c, s), cap);
            if (!spots) return;
            p.Fill(S.Xf(q => S.U(S.U(S.Circle(q, new Vector2(-0.44f, 0.22f), 0.13f), S.Circle(q, new Vector2(0.1f, 0.48f), 0.12f)), S.Circle(q, new Vector2(0.48f, 0.16f), 0.11f)), c, s), spot);
        }

        static void DrawMantar(Painter p, int level)
        {
            var brown = new Color(0.82f, 0.56f, 0.36f);
            var red = new Color(0.92f, 0.36f, 0.36f);
            switch (level)
            {
                case 1: Mushroom(p, new Vector2(0f, 0.04f), 0.74f, brown, Color.white, false); break;
                case 2: Mushroom(p, new Vector2(0f, 0.04f), 0.8f, red, Color.white, true); break;
                case 3:
                    Mushroom(p, new Vector2(-0.42f, -0.24f), 0.44f, brown, Color.white, false);
                    Mushroom(p, new Vector2(0.42f, -0.24f), 0.44f, brown, Color.white, false);
                    Mushroom(p, new Vector2(0f, 0.1f), 0.58f, red, Color.white, true);
                    break;
                case 4:
                    Halo(p, Vector2.zero, 0.9f, new Color(0.84f, 0.92f, 1f));
                    Mushroom(p, new Vector2(0f, 0.04f), 0.82f, new Color(0.42f, 0.64f, 0.95f), new Color(0.88f, 0.97f, 1f), true);
                    Sparkles(p, 2, SparkCol);
                    break;
                default:
                    Halo(p, Vector2.zero, 0.92f, new Color(0.93f, 0.86f, 1f));
                    Mushroom(p, new Vector2(0f, 0.04f), 0.86f, new Color(0.68f, 0.45f, 0.9f), new Color(1f, 0.85f, 0.35f), true);
                    Sparkles(p, 3, SparkCol);
                    break;
            }
        }

        // ================================================================== crystal chain
        static readonly Vector2[] hex =
        {
            new Vector2(0f, 1f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, -0.45f),
            new Vector2(0f, -1f), new Vector2(-0.5f, -0.45f), new Vector2(-0.5f, 0.45f)
        };
        static readonly Vector2[] facet = { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-0.5f, -0.45f), new Vector2(-0.5f, 0.45f) };

        static void Crystal(Painter p, Vector2 c, float s, float deg, Color col)
        {
            Shape(p, S.Xf(q => S.Poly(q, hex), c, s, deg), col);
            p.Fill(S.Xf(q => S.Poly(q, facet), c, s, deg), Color.Lerp(col, Color.white, 0.45f));
            p.Fill(S.Xf(q => S.Seg(q, new Vector2(0f, 0.9f), new Vector2(0f, -0.9f), 0.025f / s), c, s, deg), Ink);
        }

        static void DrawKristal(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                {
                    var sand = new Color(0.95f, 0.83f, 0.6f);
                    Shape(p, q => S.Circle(q, new Vector2(-0.3f, -0.22f), 0.2f), sand);
                    Shape(p, q => S.Circle(q, new Vector2(0.28f, -0.26f), 0.17f), sand);
                    Shape(p, q => S.Circle(q, new Vector2(0f, 0.18f), 0.19f), sand);
                    break;
                }
                case 2:
                    Shape(p, q => S.Ellipse(q, new Vector2(0f, -0.06f), new Vector2(0.72f, 0.5f)), new Color(0.68f, 0.68f, 0.74f));
                    p.Fill(q => S.Ellipse(q, new Vector2(-0.24f, 0.12f), new Vector2(0.22f, 0.1f)), new Color(0.86f, 0.86f, 0.9f));
                    break;
                case 3: Crystal(p, Vector2.zero, 0.8f, 0f, new Color(0.45f, 0.84f, 0.95f)); break;
                case 4:
                    Halo(p, Vector2.zero, 0.9f, new Color(0.92f, 0.88f, 1f));
                    Crystal(p, new Vector2(-0.42f, -0.24f), 0.45f, 25f, new Color(0.72f, 0.64f, 0.98f));
                    Crystal(p, new Vector2(0.42f, -0.24f), 0.45f, -25f, new Color(0.72f, 0.64f, 0.98f));
                    Crystal(p, new Vector2(0f, 0.04f), 0.74f, 0f, new Color(0.82f, 0.76f, 1f));
                    Sparkles(p, 2, SparkCol);
                    break;
                default:
                    Halo(p, Vector2.zero, 0.92f, new Color(1f, 0.94f, 0.75f));
                    Shape(p, q => S.Poly(q, S.Star(5, 0.82f, 0.38f)) - 0.04f, new Color(1f, 0.8f, 0.3f));
                    p.Fill(q => S.Poly(q, S.Star(5, 0.4f, 0.18f)), new Color(1f, 0.93f, 0.6f));
                    Sparkles(p, 3, SparkCol);
                    break;
            }
        }

        // ================================================================== web / fabric chain
        static void Yarn(Painter p, float s, Color col)
        {
            var dark = Darker(col, 0.74f);
            // a loose thread curling away from the ball
            var t0 = new Vector2(0.52f * s, -0.6f * s);
            p.Fill(q => S.U(S.Seg(q, t0, t0 + new Vector2(0.16f, -0.14f), 0.022f), S.I(S.Ring(S.Circle(q, t0 + new Vector2(0.28f, -0.1f), 0.14f), 0.022f), q.y - (t0.y - 0.1f))), Ink);
            Shape(p, q => S.Circle(q, Vector2.zero, 0.84f * s), col);
            p.Fill(q =>
            {
                float ball = S.Circle(q, Vector2.zero, 0.78f * s);
                float st = S.Ring(S.Circle(q, new Vector2(0.35f, 0.35f) * s, 0.55f * s), 0.028f);
                st = S.U(st, S.Ring(S.Circle(q, new Vector2(-0.4f, 0.1f) * s, 0.7f * s), 0.028f));
                st = S.U(st, S.Ring(S.Circle(q, new Vector2(0.1f, -0.5f) * s, 0.6f * s), 0.028f));
                return S.I(st, ball);
            }, dark);
        }

        static void DrawAg(Painter p, int level)
        {
            switch (level)
            {
                case 1:
                    p.Fill(q =>
                    {
                        float d = 9f;
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.3f), 0.022f));
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.58f), 0.022f));
                        d = S.U(d, S.Ring(S.Circle(q, Vector2.zero, 0.84f), 0.022f));
                        for (int i = 0; i < 8; i++)
                        {
                            float a = (i * 45f + 10f) * Mathf.Deg2Rad;
                            d = S.U(d, S.Seg(q, Vector2.zero, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.88f, 0.022f));
                        }
                        return d;
                    }, Ink);
                    Shape(p, q => S.Circle(q, new Vector2(0.32f, -0.3f), 0.12f), new Color(0.3f, 0.28f, 0.34f));
                    break;
                case 2: Yarn(p, 0.62f, new Color(0.8f, 0.74f, 0.92f)); break;
                case 3: Yarn(p, 0.84f, new Color(0.68f, 0.5f, 0.9f)); break;
                case 4:
                    Halo(p, Vector2.zero, 0.92f, new Color(0.9f, 0.86f, 1f));
                    Shape(p, q => S.Box(q, new Vector2(0f, -0.04f), new Vector2(0.74f, 0.56f), 0.12f), new Color(0.44f, 0.32f, 0.62f));
                    p.Fill(q => S.Poly(q, new[] { new Vector2(0.72f, 0.5f), new Vector2(0.72f, -0.02f), new Vector2(0.14f, 0.5f) }), new Color(0.6f, 0.46f, 0.78f));
                    InkLine(p, new Vector2(-0.6f, -0.34f), new Vector2(0.6f, -0.34f), 0.022f);
                    Sparkles(p, 2, SparkCol);
                    break;
                default:
                    Halo(p, Vector2.zero, 0.92f, new Color(0.86f, 0.94f, 1f));
                    GhostBody(p, Vector2.zero, 0.88f, new Color(0.97f, 0.98f, 1f), false);
                    p.Fill(q => S.U(S.Box(q, new Vector2(0f, -0.1f), new Vector2(0.52f, 0.045f), 0f), S.Box(q, new Vector2(0f, -0.34f), new Vector2(0.52f, 0.045f), 0f)), new Color(0.55f, 0.75f, 0.98f));
                    Sparkles(p, 3, SparkCol);
                    break;
            }
        }

        // ================================================================== generators and stations
        static void Cauldron(Painter p)
        {
            var iron = new Color(0.4f, 0.37f, 0.46f);
            Shape(p, q => S.U(S.Box(q, new Vector2(-0.45f, -0.76f), new Vector2(0.08f, 0.14f), 0.04f), S.Box(q, new Vector2(0.45f, -0.76f), new Vector2(0.08f, 0.14f), 0.04f)), iron);
            Shape(p, q => S.Ellipse(q, new Vector2(0f, -0.18f), new Vector2(0.82f, 0.6f)), iron);
            p.Fill(q => S.Ellipse(q, new Vector2(-0.4f, -0.16f), new Vector2(0.09f, 0.24f)), new Color(0.54f, 0.5f, 0.6f));
            Shape(p, q => S.Box(q, new Vector2(0f, 0.34f), new Vector2(0.86f, 0.11f), 0.09f), new Color(0.5f, 0.46f, 0.56f));
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.36f), new Vector2(0.7f, 0.06f)), new Color(0.55f, 0.85f, 0.42f));
            var bub = new Color(0.78f, 0.95f, 0.66f);
            Shape(p, q => S.Circle(q, new Vector2(-0.24f, 0.6f), 0.12f), bub);
            Shape(p, q => S.Circle(q, new Vector2(0.16f, 0.74f), 0.09f), bub);
            Shape(p, q => S.Circle(q, new Vector2(0.4f, 0.56f), 0.06f), bub);
        }

        static void SimyaPot(Painter p)
        {
            var body = new Color(0.5f, 0.38f, 0.66f);
            var gold = new Color(1f, 0.78f, 0.32f);
            Shape(p, q => S.U(S.Box(q, new Vector2(-0.48f, -0.76f), new Vector2(0.09f, 0.14f), 0.04f), S.Box(q, new Vector2(0.48f, -0.76f), new Vector2(0.09f, 0.14f), 0.04f)), body);
            Shape(p, q => S.Ellipse(q, new Vector2(0f, -0.2f), new Vector2(0.84f, 0.58f)), body);
            p.Fill(q => S.Ring(S.Ellipse(q, new Vector2(0f, -0.22f), new Vector2(0.64f, 0.38f)), 0.03f), gold);
            Shape(p, q => S.Poly(q - new Vector2(0f, -0.22f), S.Star(5, 0.18f, 0.08f)), gold);
            Shape(p, q => S.Box(q, new Vector2(0f, 0.32f), new Vector2(0.9f, 0.11f), 0.09f), gold);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.34f), new Vector2(0.72f, 0.06f)), new Color(0.82f, 0.58f, 1f));
            var bub = new Color(0.9f, 0.78f, 1f);
            Shape(p, q => S.Circle(q, new Vector2(-0.22f, 0.6f), 0.11f), bub);
            Shape(p, q => S.Circle(q, new Vector2(0.2f, 0.74f), 0.08f), bub);
        }

        static void CatHouse(Painter p)
        {
            var wall = new Color(0.92f, 0.74f, 0.48f);
            var roof = new Color(0.86f, 0.42f, 0.34f);
            var dark = new Color(0.4f, 0.26f, 0.18f);
            Shape(p, q => S.Box(q, new Vector2(0f, -0.3f), new Vector2(0.62f, 0.5f), 0.06f), wall);
            InkLine(p, new Vector2(-0.56f, -0.08f), new Vector2(0.56f, -0.08f), 0.016f);
            InkLine(p, new Vector2(-0.56f, -0.52f), new Vector2(0.56f, -0.52f), 0.016f);
            Shape(p, q => S.Poly(q, new[] { new Vector2(-0.8f, 0.14f), new Vector2(0f, 0.84f), new Vector2(0.8f, 0.14f) }) - 0.03f, roof);
            Shape(p, q => S.U(S.Circle(q, new Vector2(0f, -0.38f), 0.27f), S.Box(q, new Vector2(0f, -0.62f), new Vector2(0.27f, 0.24f), 0f)), dark);
            Shape(p, q => S.Circle(q, new Vector2(0f, 0.36f), 0.18f), new Color(1f, 0.93f, 0.8f));
            p.Fill(q => S.Circle(q, new Vector2(0f, 0.36f), 0.11f), dark);
        }

        static void Chest(Painter p)
        {
            var wood = new Color(0.8f, 0.55f, 0.32f);
            var woodL = new Color(0.88f, 0.65f, 0.4f);
            var gold = new Color(1f, 0.8f, 0.32f);
            Shape(p, q => S.Box(q, new Vector2(0f, -0.28f), new Vector2(0.8f, 0.45f), 0.08f), wood);
            Shape(p, q => S.Box(q, new Vector2(0f, 0.3f), new Vector2(0.82f, 0.26f), 0.16f), woodL);
            p.Fill(q => S.U(S.Box(q, new Vector2(-0.5f, -0.05f), new Vector2(0.06f, 0.62f), 0f), S.Box(q, new Vector2(0.5f, -0.05f), new Vector2(0.06f, 0.62f), 0f)), gold);
            Shape(p, q => S.Box(q, new Vector2(0f, 0.05f), new Vector2(0.13f, 0.17f), 0.04f), gold);
            p.Fill(q => S.Circle(q, new Vector2(0f, 0.06f), 0.045f), Ink);
        }

        /// <summary>Potion bottles: each recipe has its own colour and flask shape; stars mark the stronger ones.</summary>
        static void PotionFlask(Painter p, int id)
        {
            var liq = PotionColor(id);
            var glass = new Color(0.93f, 0.97f, 1f);
            var cork = new Color(0.78f, 0.55f, 0.34f);
            int shape = (id - 1) % 3;
            if (shape == 0)
            {
                Shape(p, q => S.U(S.Circle(q, new Vector2(0f, -0.3f), 0.52f), S.Box(q, new Vector2(0f, 0.32f), new Vector2(0.15f, 0.28f), 0.04f)), glass);
                p.Fill(q => S.I(S.Circle(q, new Vector2(0f, -0.3f), 0.52f), q.y + 0.22f), liq);
            }
            else if (shape == 1)
            {
                Shape(p, q => S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.3f, 0.66f), 0.22f), glass);
                p.Fill(q => S.I(S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.3f, 0.66f), 0.22f), q.y + 0.06f), liq);
            }
            else
            {
                var tri = new[] { new Vector2(-0.6f, -0.78f), new Vector2(0.6f, -0.78f), new Vector2(0.16f, 0.2f), new Vector2(-0.16f, 0.2f) };
                Sdf flask = q => S.U(S.Poly(q, tri) - 0.04f, S.Box(q, new Vector2(0f, 0.38f), new Vector2(0.15f, 0.22f), 0.04f));
                Shape(p, flask, glass);
                p.Fill(q => S.I(flask(q), q.y + 0.24f), liq);
            }
            Shape(p, q => S.Box(q, new Vector2(0f, 0.68f), new Vector2(0.14f, 0.1f), 0.03f), cork);
            p.Fill(q => S.Ellipse(q, new Vector2(-0.2f, -0.18f), new Vector2(0.05f, 0.12f)), Color.white);
            int stars = (id + 1) / 2;
            for (int i = 0; i < stars; i++)
            {
                var c = new Vector2(-0.12f * (stars - 1) + i * 0.24f, -0.46f);
                p.Fill(q => S.Poly(q - c, S.Star(5, 0.1f, 0.045f)), new Color(1f, 0.97f, 0.75f));
            }
        }

        // ================================================================== preview (editor: "Çizim Önizleme Kaydet")
        /// <summary>Every board drawing on the board's cream cells, so the look can be checked outside the game.</summary>
        public static Texture2D PreviewSheet()
        {
            const int cell = Size, cols = 7, rows = 9;
            var cA = new Color(0.98f, 0.93f, 0.8f); var cB = new Color(0.94f, 0.87f, 0.71f);
            var sheet = new Color[cols * cell * rows * cell];
            for (int r = 0; r < rows; r++)
            for (int cx = 0; cx < cols; cx++)
            {
                var bg = (r + cx) % 2 == 0 ? cA : cB;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++) sheet[((rows - 1 - r) * cell + y) * cols * cell + cx * cell + x] = bg;
            }
            int n = 0;
            void Put(string key, System.Action<Painter> draw)
            {
                int col = n % cols, row = n / cols; n++;
                if (row >= rows) return;
                var p = new Painter(cell, cell);
                if (IsCozy(key)) { p.Cozy = true; p.WobbleSeed = (key.GetHashCode() & 1023) * 0.37f; }
                draw(p);
                var px = p.Pixels;
                int ox = col * cell, oy = (rows - 1 - row) * cell;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    var c = px[y * cell + x];
                    int k = (oy + y) * cols * cell + ox + x;
                    sheet[k] = Color.Lerp(sheet[k], new Color(c.r, c.g, c.b, 1f), c.a);
                }
            }
            foreach (var ch in new[] { Chains.Ot, Chains.Mantar, Chains.Kristal, Chains.Ag })
            {
                for (int l = 1; l <= 5; l++) { int lv = l; string cc = ch; Put("item_" + cc + "_" + lv, p => DrawItem(p, cc, lv)); }
                n += 2;
            }
            for (int l = 1; l <= 6; l++) { int lv = l; Put("item_iksir_" + lv, p => PotionFlask(p, lv)); }
            n += 1;
            Put("item_kazan_1", p => Cauldron(p)); Put("item_sandik_1", p => Chest(p)); Put("item_simya_1", p => SimyaPot(p)); Put("item_kedievi_1", p => CatHouse(p));
            for (int l = 1; l <= 3; l++) { int lv = l; Put("item_kedi_" + lv, p => DrawKedi(p, lv)); }
            for (int c = 0; c < 6; c++) { int cc = c; Put("sidecat_w" + cc, p => DrawSideCat(p, catFur[cc], catEye[cc], catPattern[cc], 0, 1)); }
            n += 1;
            for (int c = 0; c < 6; c++) { int cc = c; Put("sidecat_s" + cc, p => DrawSideCat(p, catFur[cc], catEye[cc], catPattern[cc], 0, 4)); }
            n += 1;
            Put("sidecat_h1", p => DrawSideCat(p, catFur[1], catEye[1], 0, 1, 4));
            Put("sidecat_h2", p => DrawSideCat(p, catFur[5], catEye[5], 0, 2, 4));
            Put("sidecat_h3", p => DrawSideCat(p, catFur[0], catEye[0], 1, 3, 0));
            Put("sidecat_z0", p => DrawSideCat(p, catFur[0], catEye[0], 1, 0, 5));
            Put("sidecat_z3", p => DrawSideCat(p, catFur[3], catEye[3], 2, 0, 5));
            Put("karapati_head", p => CatHead(p, new Vector2(0f, -0.05f), 0.95f, catFur[5], catEye[5], false, false, 0));
            Put("backstir_x", p => DrawBackStir(p, catFur[0], catPattern[0], 2, 0));

            var t = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, false);
            t.SetPixels(sheet);
            t.Apply();
            return t;
        }
    }
}
