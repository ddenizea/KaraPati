using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Customer portraits (front view). Each character has a plain look and a "full" look with its signature
    /// items (hat, sword, goggles...), which it shows once the friendship is deep enough. A silhouette version is
    /// what the player sees through the fog before they have met.
    /// </summary>
    public static partial class ProcArt
    {
        const int PortraitSize = 192;
        static readonly Color Dark = new Color(0.16f, 0.1f, 0.2f);
        static readonly Color Cheek = new Color(1f, 0.5f, 0.62f, 0.55f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.28f);

        /// <summary>Portrait of a customer; full = with its signature items.</summary>
        public static Sprite Customer(string id, bool full) =>
            Make("cust_" + id + (full ? "_f" : "_b"), PortraitSize, p => DrawCustomer(p, id, full));

        /// <summary>Flat pale shape of the customer, for the fog before you have met.</summary>
        public static Sprite CustomerSilhouette(string id) =>
            Make("cust_" + id + "_s", PortraitSize, p => { DrawCustomer(p, id, false); p.Silhouette(new Color(0.6f, 0.52f, 0.8f, 1f)); });

        /// <summary>Soft drifting fog puffs laid over a customer who is still a stranger.</summary>
        public static Sprite Fog => Make("ui_fog", Size, p =>
        {
            var c = new Color(0.86f, 0.84f, 0.95f, 0.9f);
            p.Glow(new Vector2(-0.45f, -0.35f), 0.75f, c);
            p.Glow(new Vector2(0.4f, -0.4f), 0.7f, c);
            p.Glow(new Vector2(0.05f, 0.25f), 0.8f, c);
            p.Glow(new Vector2(-0.35f, 0.45f), 0.55f, c);
            p.Glow(new Vector2(0.5f, 0.35f), 0.5f, c);
        });

        /// <summary>Apprentice seen from behind, standing on its hind paws and stirring the pot with both front paws.
        /// frame 0-3 is one round of the spoon.</summary>
        public static Sprite BackStirCat(int level, int frame, int color)
        {
            if (color < 0 || color > KittenColors.Gece) color = KittenColors.Legacy(level);
            int c = color;
            int hat = level == 2 ? 1 : level == 3 ? 2 : level == 4 ? 3 : 0;
            return Make("backstir_" + level + "_" + frame + "_" + c, Size, p => DrawBackStir(p, catFur[c], catPattern[c], hat, frame));
        }

        static void DrawBackStir(Painter p, Color fur, int pattern, int hat, int frame)
        {
            var furDark = new Color(fur.r * 0.72f, fur.g * 0.68f, fur.b * 0.7f);
            var limb = pattern == 2 ? patchDark : pattern == 3 ? patchOrange : fur;
            var wood = new Color(0.62f, 0.42f, 0.26f);
            float a = frame * Mathf.PI * 0.5f;
            float sway = Mathf.Sin(a) * 0.035f;                                     // the whole cat leans with the spoon
            var hand = new Vector2(0.2f + Mathf.Cos(a) * 0.09f, 0.0f + Mathf.Sin(a) * 0.05f);
            var bc = new Vector2(sway, -0.4f);
            var br = new Vector2(0.31f, 0.38f);
            var hc = new Vector2(sway * 1.4f, 0.13f);

            // the spoon is held in front of the cat (away from us): only the handle shows above its shoulder
            var top = new Vector2(hand.x + 0.14f, 0.86f);
            p.FillOutlined(q => S.Seg(q, hand, top, 0.045f), wood, 0.035f);

            // feet peeking out under the body
            p.FillOutlined(q => S.U(S.Ellipse(q, new Vector2(-0.17f, -0.79f), new Vector2(0.13f, 0.07f)),
                S.Ellipse(q, new Vector2(0.17f, -0.79f), new Vector2(0.13f, 0.07f))), furDark, 0.04f);
            // both front paws reach up to the spoon
            p.FillOutlined(q => S.U(S.Seg(q, new Vector2(bc.x - 0.24f, -0.14f), hand + new Vector2(-0.06f, 0f), 0.07f),
                S.Seg(q, new Vector2(bc.x + 0.24f, -0.14f), hand + new Vector2(0.03f, -0.02f), 0.07f)), fur, 0.045f);
            p.FillOutlined(q => S.U(S.Circle(q, hand + new Vector2(-0.06f, 0f), 0.075f), S.Circle(q, hand + new Vector2(0.04f, -0.02f), 0.075f)), fur, 0.04f);

            // body
            p.FillOutlined(q => S.Ellipse(q, bc, br), fur, 0.05f);
            Patches(p, pattern, bc, br, bc + new Vector2(-0.14f, 0.12f), 0.19f, bc + new Vector2(0.12f, -0.18f), 0.15f);
            if (pattern == 1)   // tabby stripes across the back
                p.Fill(q => S.U(S.Seg(q, bc + new Vector2(-0.2f, 0.18f), bc + new Vector2(0.2f, 0.18f), 0.03f),
                    S.U(S.Seg(q, bc + new Vector2(-0.25f, 0.02f), bc + new Vector2(0.25f, 0.02f), 0.03f),
                        S.Seg(q, bc + new Vector2(-0.24f, -0.14f), bc + new Vector2(0.24f, -0.14f), 0.03f))), furDark);
            // a little line down the spine
            p.Fill(q => S.Seg(q, bc + new Vector2(0f, 0.26f), bc + new Vector2(0f, -0.2f), 0.012f), new Color(furDark.r, furDark.g, furDark.b, 0.6f));

            // back of the head: round, two ears, no face
            var earA = new[] { hc + new Vector2(-0.25f, 0.08f), hc + new Vector2(-0.2f, 0.4f), hc + new Vector2(-0.04f, 0.2f) };
            var earB = new[] { hc + new Vector2(0.04f, 0.2f), hc + new Vector2(0.2f, 0.4f), hc + new Vector2(0.25f, 0.08f) };
            p.FillOutlined(q => S.U(S.Circle(q, hc, 0.27f), S.U(S.Poly(q, earA), S.Poly(q, earB))), fur, 0.05f);
            if (pattern >= 2)
                p.Fill(q => S.I(S.Circle(q, hc, 0.25f), S.Circle(q, hc + new Vector2(0.12f, 0.12f), 0.15f)), pattern == 3 ? patchOrange : patchDark);
            if (pattern == 1)
                p.Fill(q => S.U(S.Seg(q, hc + new Vector2(-0.06f, 0.2f), hc + new Vector2(-0.06f, 0.06f), 0.025f),
                    S.Seg(q, hc + new Vector2(0.06f, 0.2f), hc + new Vector2(0.06f, 0.06f), 0.025f)), furDark);

            // hat seen from behind
            if (hat == 1 || hat == 2)
            {
                var hatCol = hat == 1 ? new Color(0.3f, 0.5f, 0.9f) : new Color(0.5f, 0.28f, 0.75f);
                var brim = hat == 1 ? new Color(0.25f, 0.4f, 0.8f) : new Color(0.42f, 0.22f, 0.65f);
                var cone = new[] { hc + new Vector2(-0.17f, 0.2f), hc + new Vector2(0.17f, 0.2f), hc + new Vector2(0.05f - sway, 0.62f) };
                p.FillOutlined(q => S.Poly(q, cone), hatCol, 0.035f);
                p.FillOutlined(q => S.Ellipse(q, hc + new Vector2(0f, 0.2f), new Vector2(0.27f, 0.05f)), brim, 0.035f);
            }
            else if (hat == 3)
            {
                p.FillOutlined(q => S.Ellipse(q, hc + new Vector2(0f, 0.24f), new Vector2(0.17f, 0.11f)), new Color(0.88f, 0.3f, 0.32f), 0.035f);
                p.Fill(q => S.Circle(q, hc + new Vector2(0f, 0.36f), 0.055f), new Color(1f, 0.95f, 0.9f));
            }

            // the tail curls out towards us and swishes while it stirs
            float tw = Mathf.Sin(a + 1f) * 0.08f;
            p.FillOutlined(q => S.U(S.Seg(q, bc + new Vector2(0.05f, -0.3f), new Vector2(0.42f, -0.62f), 0.065f),
                S.Seg(q, new Vector2(0.42f, -0.62f), new Vector2(0.55f + tw, -0.28f), 0.06f)), limb, 0.045f);
        }

        /// <summary>The Mini Apprentice's little red cap with a white pompom (graduation).</summary>
        public static Sprite RedCap => Make("ui_redcap", 96, p =>
        {
            p.FillOutlined(q => S.U(S.Ellipse(q, new Vector2(0f, -0.05f), new Vector2(0.62f, 0.42f)),
                S.Box(q, new Vector2(0f, -0.32f), new Vector2(0.72f, 0.13f), 0.08f)), new Color(0.88f, 0.3f, 0.32f), 0.08f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0.06f, 0.48f), 0.2f), new Color(1f, 0.95f, 0.9f), 0.06f);
        });

        static void DrawCustomer(Painter p, string id, bool full)
        {
            switch (id)
            {
                case "kurbaga": Frog(p, full); break;
                case "bocek": Ladybug(p, full); break;
                case "tirtil": Caterpillar(p, full); break;
                case "kartal": Eagle(p, full); break;
                case "baykus": Owl(p, full); break;
                case "ejderha": Dragon(p, full); break;
                case "boo": GhostBoo(p, full); break;
                case "sis": GhostSis(p, full); break;
                case "pus": GhostPus(p, full); break;
                case "ugultu": GhostUgultu(p, full); break;
            }
        }

        // ---------------------------------------------------------------- little helpers
        static void DrawEye(Painter p, Vector2 c, float r)
        {
            p.FillOutlined(q => S.Ellipse(q, c, new Vector2(r, r * 1.15f)), Color.white, 0.025f);
            p.Fill(q => S.Ellipse(q, c + new Vector2(0.01f, -0.015f), new Vector2(r * 0.56f, r * 0.68f)), Dark);
            p.Fill(q => S.Circle(q, c + new Vector2(-r * 0.18f, r * 0.32f), r * 0.24f), Color.white);
        }

        /// <summary>The lower half of a ring: a smile.</summary>
        static void Smile(Painter p, Vector2 c, float r, float th, Color col) =>
            p.Fill(q => S.I(S.Ring(S.Circle(q, c, r), th), q.y - c.y + r * 0.25f), col);

        static void Cheeks(Painter p, Vector2 a, Vector2 b, float r)
        {
            p.Fill(q => S.Ellipse(q, a, new Vector2(r, r * 0.6f)), Cheek);
            p.Fill(q => S.Ellipse(q, b, new Vector2(r, r * 0.6f)), Cheek);
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        // ---------------------------------------------------------------- Kurbağa Prens
        static void Frog(Painter p, bool full)
        {
            var green = new Color(0.45f, 0.78f, 0.36f);
            var belly = new Color(0.78f, 0.92f, 0.58f);
            if (full)   // a little red cape behind the body
                p.FillOutlined(q => S.Poly(q, new[] { V(-0.42f, -0.12f), V(0.42f, -0.12f), V(0.62f, -0.86f), V(-0.62f, -0.86f) }), new Color(0.78f, 0.22f, 0.28f), 0.035f);
            p.FillOutlined(q => S.U(S.Ellipse(q, V(-0.38f, -0.84f), V(0.2f, 0.08f)), S.Ellipse(q, V(0.38f, -0.84f), V(0.2f, 0.08f))), green, 0.035f);
            p.FillOutlined(q => S.Ellipse(q, V(0f, -0.46f), V(0.5f, 0.4f)), green, 0.04f);
            p.Fill(q => S.Ellipse(q, V(0f, -0.52f), V(0.32f, 0.26f)), belly);
            p.FillOutlined(q => S.U(S.Ellipse(q, V(0f, 0.02f), V(0.7f, 0.42f)),
                S.U(S.Circle(q, V(-0.36f, 0.4f), 0.22f), S.Circle(q, V(0.36f, 0.4f), 0.22f))), green, 0.04f);
            DrawEye(p, V(-0.36f, 0.42f), 0.14f);
            DrawEye(p, V(0.36f, 0.42f), 0.14f);
            Smile(p, V(0f, 0.1f), 0.38f, 0.025f, Dark);
            Cheeks(p, V(-0.5f, -0.04f), V(0.5f, -0.04f), 0.1f);
            if (!full) return;
            // golden crown between the eyes
            p.FillOutlined(q => S.Poly(q, new[] { V(-0.2f, 0.56f), V(-0.2f, 0.8f), V(-0.1f, 0.68f), V(0f, 0.86f), V(0.1f, 0.68f), V(0.2f, 0.8f), V(0.2f, 0.56f) }), Gold, 0.03f);
            p.Fill(q => S.Circle(q, V(0f, 0.63f), 0.045f), new Color(0.9f, 0.25f, 0.35f));
        }

        // ---------------------------------------------------------------- Bay Böcek
        static void Ladybug(Painter p, bool full)
        {
            var red = new Color(0.88f, 0.22f, 0.24f);
            var head = new Color(0.2f, 0.15f, 0.24f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                float k = sx;
                p.Fill(q => S.U(S.Seg(q, V(k * 0.32f, -0.72f), V(k * 0.42f, -0.92f), 0.035f), S.Seg(q, V(k * 0.5f, -0.4f), V(k * 0.72f, -0.52f), 0.035f)), head);
            }
            p.FillOutlined(q => S.Circle(q, V(0f, -0.32f), 0.58f), red, 0.04f);
            p.Fill(q => S.Seg(q, V(0f, 0.2f), V(0f, -0.88f), 0.022f), head);
            p.Fill(q => S.U(S.U(S.Circle(q, V(-0.3f, -0.16f), 0.1f), S.Circle(q, V(0.3f, -0.13f), 0.09f)),
                S.U(S.U(S.Circle(q, V(-0.35f, -0.52f), 0.085f), S.Circle(q, V(0.34f, -0.54f), 0.1f)),
                    S.U(S.Circle(q, V(-0.12f, -0.74f), 0.065f), S.Circle(q, V(0.14f, -0.73f), 0.06f)))), head);
            // antennae (the hat sits between them)
            {
                p.Fill(q => S.U(S.Seg(q, V(-0.16f, 0.55f), V(-0.32f, 0.86f), 0.028f), S.Seg(q, V(0.16f, 0.55f), V(0.32f, 0.86f), 0.028f)), head);
                p.FillOutlined(q => S.U(S.Circle(q, V(-0.33f, 0.89f), 0.06f), S.Circle(q, V(0.33f, 0.89f), 0.06f)), Gold, 0.025f);
            }
            p.FillOutlined(q => S.Circle(q, V(0f, 0.28f), 0.38f), head, 0.04f);
            DrawEye(p, V(-0.14f, 0.31f), 0.11f);
            DrawEye(p, V(0.14f, 0.31f), 0.11f);
            Smile(p, V(0f, 0.17f), 0.1f, 0.02f, Color.white);
            Cheeks(p, V(-0.26f, 0.15f), V(0.26f, 0.15f), 0.07f);
            if (!full) return;
            var hat = new Color(0.5f, 0.28f, 0.65f);
            p.FillOutlined(q => S.U(S.Box(q, V(0f, 0.8f), V(0.16f, 0.15f), 0.03f), S.Ellipse(q, V(0f, 0.63f), V(0.3f, 0.06f))), hat, 0.03f);
            p.Fill(q => S.Box(q, V(0f, 0.69f), V(0.16f, 0.035f), 0f), Gold);
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(0f, -0.06f), V(-0.2f, 0.03f), V(-0.2f, -0.15f) }), S.Poly(q, new[] { V(0f, -0.06f), V(0.2f, 0.03f), V(0.2f, -0.15f) })), Gold, 0.025f);
            p.Fill(q => S.Circle(q, V(0f, -0.06f), 0.045f), new Color(0.9f, 0.6f, 0.15f));
        }

        // ---------------------------------------------------------------- Korsan Tırtıl
        static void Caterpillar(Painter p, bool full)
        {
            var green = new Color(0.43f, 0.78f, 0.35f);
            var belly = new Color(0.7f, 0.92f, 0.55f);
            var foot = new Color(0.25f, 0.5f, 0.2f);
            var segs = new[] { V(-0.62f, -0.62f), V(-0.36f, -0.7f), V(-0.1f, -0.66f), V(0.12f, -0.5f) };
            float[] rs = { 0.24f, 0.25f, 0.26f, 0.27f };
            for (int i = 0; i < segs.Length; i++)
            {
                var c = segs[i];
                float r = rs[i];
                p.Fill(q => S.U(S.Ellipse(q, c + V(-0.1f, -r), V(0.06f, 0.04f)), S.Ellipse(q, c + V(0.1f, -r), V(0.06f, 0.04f))), foot);
                p.FillOutlined(q => S.Circle(q, c, r), green, 0.035f);
                p.Fill(q => S.Ellipse(q, c + V(0f, -r * 0.35f), V(r * 0.5f, r * 0.3f)), belly);
            }
            if (full)
            {
                // little arm holding the sword
                p.FillOutlined(q => S.Seg(q, V(-0.08f, -0.5f), V(-0.3f, -0.26f), 0.05f), green, 0.03f);
                p.FillOutlined(q => S.Seg(q, V(-0.35f, -0.2f), V(-0.38f, 0.32f), 0.03f), new Color(0.85f, 0.88f, 0.92f), 0.025f);
                p.FillOutlined(q => S.Seg(q, V(-0.47f, -0.22f), V(-0.23f, -0.22f), 0.03f), Gold, 0.02f);
                p.FillOutlined(q => S.Circle(q, V(-0.31f, -0.26f), 0.06f), green, 0.025f);
            }
            var hc = V(0.3f, -0.04f);
            p.FillOutlined(q => S.Circle(q, hc, 0.4f), green, 0.04f);
            DrawEye(p, hc + V(-0.14f, 0.06f), 0.1f);
            DrawEye(p, hc + V(0.12f, 0.08f), 0.11f);
            Smile(p, hc + V(0f, -0.12f), 0.14f, 0.022f, Dark);
            Cheeks(p, hc + V(-0.27f, -0.12f), hc + V(0.27f, -0.12f), 0.07f);
            if (!full) return;
            // eye patch, tricorn hat with a skull, golden earring
            p.Fill(q => S.Seg(q, hc + V(-0.38f, 0.22f), hc + V(0.36f, -0.06f), 0.022f), Dark);
            p.Fill(q => S.Ellipse(q, hc + V(-0.14f, 0.06f), V(0.12f, 0.1f)), Dark);
            var hat = new Color(0.22f, 0.16f, 0.2f);
            p.FillOutlined(q => S.Poly(q, new[] { hc + V(-0.5f, 0.26f), hc + V(0f, 0.74f), hc + V(0.5f, 0.26f), hc + V(0.24f, 0.32f), hc + V(0f, 0.28f), hc + V(-0.24f, 0.32f) }), hat, 0.03f);
            p.Fill(q => S.U(S.Seg(q, hc + V(-0.5f, 0.26f), hc + V(-0.24f, 0.32f), 0.018f), S.U(S.Seg(q, hc + V(-0.24f, 0.32f), hc + V(0f, 0.28f), 0.018f),
                S.U(S.Seg(q, hc + V(0f, 0.28f), hc + V(0.24f, 0.32f), 0.018f), S.Seg(q, hc + V(0.24f, 0.32f), hc + V(0.5f, 0.26f), 0.018f)))), Gold);
            p.Fill(q => S.Circle(q, hc + V(0f, 0.47f), 0.07f), Color.white);
            p.Fill(q => S.U(S.Circle(q, hc + V(-0.025f, 0.48f), 0.017f), S.Circle(q, hc + V(0.025f, 0.48f), 0.017f)), Dark);
            p.Fill(q => S.Ring(S.Circle(q, hc + V(-0.4f, -0.1f), 0.05f), 0.018f), Gold);
        }

        // ---------------------------------------------------------------- Pilot Kartal
        static void Eagle(Painter p, bool full)
        {
            var brown = new Color(0.56f, 0.36f, 0.22f);
            var dark = new Color(0.42f, 0.26f, 0.16f);
            var tan = new Color(0.8f, 0.6f, 0.4f);
            var yellow = new Color(0.98f, 0.75f, 0.2f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                float k = sx;
                p.FillOutlined(q => S.Poly(q, new[] { V(k * 0.3f, -0.08f), V(k * 0.86f, -0.22f), V(k * 0.84f, -0.62f), V(k * 0.68f, -0.52f), V(k * 0.62f, -0.74f), V(k * 0.46f, -0.6f), V(k * 0.32f, -0.72f) }), dark, 0.035f);
                p.Fill(q => S.U(S.Seg(q, V(k * 0.14f, -0.84f), V(k * 0.1f, -0.96f), 0.025f), S.Seg(q, V(k * 0.14f, -0.84f), V(k * 0.2f, -0.96f), 0.025f)), yellow);
            }
            p.FillOutlined(q => S.Ellipse(q, V(0f, -0.42f), V(0.44f, 0.45f)), brown, 0.04f);
            p.Fill(q => S.Ellipse(q, V(0f, -0.5f), V(0.25f, 0.27f)), tan);
            if (full)   // red scarf with a fluttering tail
            {
                var red = new Color(0.86f, 0.22f, 0.26f);
                p.FillOutlined(q => S.Poly(q, new[] { V(0.18f, -0.02f), V(0.66f, -0.2f), V(0.6f, -0.3f), V(0.68f, -0.38f), V(0.14f, -0.14f) }), red, 0.03f);
                p.FillOutlined(q => S.Box(q, V(0f, -0.04f), V(0.34f, 0.07f), 0.06f), red, 0.03f);
            }
            p.FillOutlined(q => S.Circle(q, V(0f, 0.3f), 0.38f), Color.white, 0.04f);
            DrawEye(p, V(-0.15f, 0.34f), 0.1f);
            DrawEye(p, V(0.15f, 0.34f), 0.1f);
            p.Fill(q => S.U(S.Seg(q, V(-0.28f, 0.5f), V(-0.07f, 0.46f), 0.025f), S.Seg(q, V(0.28f, 0.5f), V(0.07f, 0.46f), 0.025f)), Dark);
            p.FillOutlined(q => S.Poly(q, new[] { V(-0.12f, 0.2f), V(0.12f, 0.2f), V(0.04f, -0.02f), V(0f, -0.07f), V(-0.02f, 0.06f) }), yellow, 0.025f);
            if (!full) return;
            // flying goggles on the forehead
            var strap = new Color(0.45f, 0.28f, 0.18f);
            p.Fill(q => S.Seg(q, V(-0.38f, 0.56f), V(0.38f, 0.56f), 0.04f), strap);
            p.FillOutlined(q => S.U(S.Circle(q, V(-0.15f, 0.58f), 0.12f), S.Circle(q, V(0.15f, 0.58f), 0.12f)), strap, 0.025f);
            p.Fill(q => S.U(S.Circle(q, V(-0.15f, 0.58f), 0.085f), S.Circle(q, V(0.15f, 0.58f), 0.085f)), new Color(0.6f, 0.85f, 0.97f));
            p.Fill(q => S.U(S.Circle(q, V(-0.18f, 0.62f), 0.025f), S.Circle(q, V(0.12f, 0.62f), 0.025f)), Color.white);
        }

        // ---------------------------------------------------------------- Baykuş Kütüphaneci
        static void Owl(Painter p, bool full)
        {
            var body = new Color(0.56f, 0.44f, 0.36f);
            var wing = new Color(0.44f, 0.33f, 0.27f);
            var face = new Color(0.96f, 0.88f, 0.74f);
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(-0.5f, 0.2f), V(-0.44f, 0.66f), V(-0.22f, 0.36f) }), S.Poly(q, new[] { V(0.5f, 0.2f), V(0.44f, 0.66f), V(0.22f, 0.36f) })), wing, 0.035f);
            p.FillOutlined(q => S.Ellipse(q, V(0f, -0.25f), V(0.55f, 0.65f)), body, 0.04f);
            p.FillOutlined(q => S.U(S.Ellipse(q, V(-0.5f, -0.35f), V(0.14f, 0.36f)), S.Ellipse(q, V(0.5f, -0.35f), V(0.14f, 0.36f))), wing, 0.03f);
            p.Fill(q => S.Ellipse(q, V(0f, -0.48f), V(0.3f, 0.32f)), new Color(0.82f, 0.72f, 0.6f));
            for (int i = 0; i < 3; i++)   // little feather marks on the belly
            {
                float y = -0.35f - i * 0.14f;
                p.Fill(q => S.I(S.Ring(S.Circle(q, V(0f, y), 0.08f), 0.015f), q.y - y), wing);
            }
            p.Fill(q => S.U(S.Circle(q, V(-0.2f, 0.1f), 0.24f), S.Circle(q, V(0.2f, 0.1f), 0.24f)), face);
            DrawEye(p, V(-0.2f, 0.1f), 0.14f);
            DrawEye(p, V(0.2f, 0.1f), 0.14f);
            p.FillOutlined(q => S.Poly(q, new[] { V(-0.06f, 0.0f), V(0.06f, 0.0f), V(0f, -0.1f) }), new Color(0.95f, 0.6f, 0.2f), 0.02f);
            p.FillOutlined(q => S.U(S.Ellipse(q, V(-0.16f, -0.86f), V(0.1f, 0.05f)), S.Ellipse(q, V(0.16f, -0.86f), V(0.1f, 0.05f))), new Color(0.95f, 0.6f, 0.2f), 0.025f);
            if (!full) return;
            // round reading glasses and a red book under the wing
            p.Fill(q => S.U(S.Ring(S.Circle(q, V(-0.2f, 0.1f), 0.19f), 0.022f), S.U(S.Ring(S.Circle(q, V(0.2f, 0.1f), 0.19f), 0.022f), S.Seg(q, V(-0.02f, 0.14f), V(0.02f, 0.14f), 0.02f))), Dark);
            p.FillOutlined(q => S.Box(q, V(0.46f, -0.58f), V(0.2f, 0.26f), 0.03f), new Color(0.75f, 0.22f, 0.28f), 0.03f);
            p.Fill(q => S.Box(q, V(0.6f, -0.58f), V(0.04f, 0.22f), 0f), new Color(1f, 0.95f, 0.85f));
            p.Fill(q => S.Box(q, V(0.44f, -0.5f), V(0.1f, 0.025f), 0f), Gold);
        }

        // ---------------------------------------------------------------- Ejderha Yavrusu
        static void Dragon(Painter p, bool full)
        {
            var teal = new Color(0.38f, 0.72f, 0.6f);
            var belly = new Color(0.98f, 0.86f, 0.56f);
            var wing = new Color(0.6f, 0.45f, 0.78f);
            var horn = new Color(1f, 0.93f, 0.8f);
            p.FillOutlined(q => S.U(S.Seg(q, V(0.25f, -0.72f), V(0.7f, -0.56f), 0.09f), S.Poly(q, new[] { V(0.66f, -0.44f), V(0.92f, -0.5f), V(0.72f, -0.7f) })), teal, 0.035f);
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(-0.32f, -0.2f), V(-0.8f, 0.05f), V(-0.62f, -0.2f), V(-0.82f, -0.32f), V(-0.4f, -0.42f) }),
                S.Poly(q, new[] { V(0.32f, -0.2f), V(0.8f, 0.05f), V(0.62f, -0.2f), V(0.82f, -0.32f), V(0.4f, -0.42f) })), wing, 0.035f);
            p.FillOutlined(q => S.U(S.Ellipse(q, V(-0.24f, -0.86f), V(0.14f, 0.08f)), S.Ellipse(q, V(0.24f, -0.86f), V(0.14f, 0.08f))), teal, 0.03f);
            p.FillOutlined(q => S.Ellipse(q, V(0f, -0.45f), V(0.42f, 0.42f)), teal, 0.04f);
            p.Fill(q => S.Ellipse(q, V(0f, -0.5f), V(0.25f, 0.3f)), belly);
            p.Fill(q => S.U(S.Seg(q, V(-0.18f, -0.4f), V(0.18f, -0.4f), 0.012f), S.Seg(q, V(-0.2f, -0.56f), V(0.2f, -0.56f), 0.012f)), new Color(0.85f, 0.7f, 0.42f));
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(-0.32f, 0.44f), V(-0.4f, 0.82f), V(-0.16f, 0.52f) }), S.Poly(q, new[] { V(0.32f, 0.44f), V(0.4f, 0.82f), V(0.16f, 0.52f) })), horn, 0.03f);
            p.FillOutlined(q => S.Ellipse(q, V(0f, 0.2f), V(0.45f, 0.4f)), teal, 0.04f);
            p.Fill(q => S.Ellipse(q, V(0f, 0.04f), V(0.28f, 0.17f)), new Color(0.55f, 0.84f, 0.72f));
            p.Fill(q => S.U(S.Circle(q, V(-0.08f, 0.08f), 0.025f), S.Circle(q, V(0.08f, 0.08f), 0.025f)), Dark);
            DrawEye(p, V(-0.18f, 0.3f), 0.11f);
            DrawEye(p, V(0.18f, 0.3f), 0.11f);
            Smile(p, V(0f, -0.02f), 0.1f, 0.02f, Dark);
            Cheeks(p, V(-0.32f, 0.08f), V(0.32f, 0.08f), 0.07f);
            if (!full) return;
            // a hiccup flame puff and spikes along the head
            p.FillOutlined(q => S.U(S.Circle(q, V(0.45f, -0.02f), 0.1f), S.U(S.Circle(q, V(0.62f, 0.06f), 0.08f), S.Circle(q, V(0.76f, 0.16f), 0.055f))), new Color(1f, 0.55f, 0.2f), 0.025f);
            p.Fill(q => S.U(S.Circle(q, V(0.45f, -0.02f), 0.055f), S.Circle(q, V(0.62f, 0.06f), 0.04f)), new Color(1f, 0.88f, 0.35f));
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(-0.08f, 0.58f), V(0f, 0.72f), V(0.08f, 0.58f) }), S.Poly(q, new[] { V(-0.06f, 0.48f), V(0f, 0.6f), V(0.06f, 0.48f) })), wing, 0.02f);
        }

        // ---------------------------------------------------------------- ghost customers
        static void GhostBoo(Painter p, bool full)
        {
            var c = V(0f, -0.2f);
            GhostBody(p, c, 0.72f, new Color(0.95f, 0.88f, 0.98f), true);
            if (!full) return;
            // pink bow on the head and a little teddy bear
            var pink = new Color(1f, 0.5f, 0.68f);
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(0f, 0.43f), V(-0.24f, 0.56f), V(-0.24f, 0.32f) }), S.Poly(q, new[] { V(0f, 0.43f), V(0.24f, 0.56f), V(0.24f, 0.32f) })), pink, 0.03f);
            p.Fill(q => S.Circle(q, V(0f, 0.43f), 0.06f), new Color(0.9f, 0.35f, 0.5f));
            var bear = new Color(0.72f, 0.5f, 0.32f);
            p.FillOutlined(q => S.U(S.Circle(q, V(0.5f, -0.48f), 0.17f), S.U(S.Circle(q, V(0.38f, -0.33f), 0.06f), S.Circle(q, V(0.62f, -0.33f), 0.06f))), bear, 0.03f);
            p.Fill(q => S.Ellipse(q, V(0.5f, -0.53f), V(0.07f, 0.05f)), new Color(0.92f, 0.78f, 0.6f));
            p.Fill(q => S.U(S.Circle(q, V(0.44f, -0.44f), 0.022f), S.Circle(q, V(0.56f, -0.44f), 0.022f)), Dark);
        }

        static void GhostSis(Painter p, bool full)
        {
            var c = V(0f, -0.05f);
            GhostBody(p, c, 0.88f, new Color(0.88f, 0.86f, 1f), true);
            if (!full) return;
            // knitted shawl and knitting needles with a ball of yarn
            var shawl = new Color(0.62f, 0.42f, 0.78f);
            p.FillOutlined(q => S.U(S.Box(q, V(0f, -0.3f), V(0.56f, 0.1f), 0.06f), S.Poly(q, new[] { V(-0.2f, -0.35f), V(0f, -0.62f), V(0.2f, -0.35f) })), shawl, 0.03f);
            for (int i = -4; i <= 4; i++)
            {
                float x = i * 0.12f;
                p.Fill(q => S.Seg(q, V(x, -0.26f), V(x + 0.04f, -0.34f), 0.012f), new Color(0.78f, 0.6f, 0.9f));
            }
            p.FillOutlined(q => S.Circle(q, V(0.55f, -0.62f), 0.14f), new Color(0.95f, 0.5f, 0.55f), 0.03f);
            p.Fill(q => S.U(S.Seg(q, V(0.3f, -0.4f), V(0.72f, -0.86f), 0.02f), S.Seg(q, V(0.78f, -0.4f), V(0.36f, -0.86f), 0.02f)), new Color(0.75f, 0.6f, 0.45f));
        }

        static void GhostPus(Painter p, bool full)
        {
            var c = V(0f, -0.12f);
            GhostBody(p, c, 0.84f, new Color(0.72f, 0.82f, 0.94f), true);
            if (!full) return;
            // captain's hat and a pipe
            var navy = new Color(0.16f, 0.2f, 0.4f);
            p.FillOutlined(q => S.Box(q, V(0f, 0.63f), V(0.38f, 0.17f), 0.12f), navy, 0.03f);
            p.Fill(q => S.Box(q, V(0f, 0.5f), V(0.38f, 0.05f), 0f), new Color(0.95f, 0.95f, 0.98f));
            p.FillOutlined(q => S.Ellipse(q, V(0f, 0.44f), V(0.56f, 0.06f)), new Color(0.1f, 0.12f, 0.28f), 0.03f);
            p.Fill(q => S.Circle(q, V(0f, 0.66f), 0.07f), Gold);
            var wood = new Color(0.5f, 0.3f, 0.18f);
            p.Fill(q => S.Seg(q, V(0.1f, -0.2f), V(0.42f, -0.24f), 0.035f), wood);
            p.FillOutlined(q => S.Box(q, V(0.46f, -0.16f), V(0.07f, 0.1f), 0.03f), wood, 0.025f);
            p.Fill(q => S.U(S.Circle(q, V(0.5f, 0.04f), 0.05f), S.Circle(q, V(0.58f, 0.16f), 0.065f)), new Color(0.92f, 0.94f, 0.98f, 0.7f));
        }

        static void GhostUgultu(Painter p, bool full)
        {
            var c = V(0f, -0.1f);
            GhostBody(p, c, 0.9f, new Color(1f, 0.86f, 0.84f), true);
            if (!full) return;
            // round glasses, a red bow tie and a thick old book
            var e1 = c + V(-0.22f, 0.22f) * 0.9f;
            var e2 = c + V(0.22f, 0.22f) * 0.9f;
            p.Fill(q => S.U(S.Ring(S.Circle(q, e1, 0.15f), 0.022f), S.U(S.Ring(S.Circle(q, e2, 0.15f), 0.022f), S.Seg(q, e1 + V(0.15f, 0.02f), e2 - V(0.15f, -0.02f), 0.018f))), Dark);
            var red = new Color(0.82f, 0.2f, 0.26f);
            p.FillOutlined(q => S.U(S.Poly(q, new[] { V(0f, -0.2f), V(-0.2f, -0.1f), V(-0.2f, -0.3f) }), S.Poly(q, new[] { V(0f, -0.2f), V(0.2f, -0.1f), V(0.2f, -0.3f) })), red, 0.025f);
            p.Fill(q => S.Circle(q, V(0f, -0.2f), 0.045f), new Color(0.6f, 0.12f, 0.18f));
            p.FillOutlined(q => S.Box(q, V(-0.5f, -0.6f), V(0.22f, 0.17f), 0.03f), new Color(0.3f, 0.45f, 0.7f), 0.03f);
            p.Fill(q => S.Box(q, V(-0.5f, -0.74f), V(0.2f, 0.035f), 0f), new Color(1f, 0.95f, 0.85f));
            p.Fill(q => S.Box(q, V(-0.5f, -0.56f), V(0.1f, 0.025f), 0f), Gold);
        }
    }
}
