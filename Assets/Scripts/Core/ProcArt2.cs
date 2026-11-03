using UnityEngine;

namespace KaraPati
{
    /// <summary>Second half of the placeholder art: walking cats, shop props, stickers, icons.</summary>
    public static partial class ProcArt
    {
        // fur, eyes and coat pattern for each KittenColors index (0 none, 1 stripes, 2 black patches, 3 calico)
        static readonly Color[] catFur =
        {
            new Color(0.96f, 0.63f, 0.32f), new Color(0.64f, 0.66f, 0.74f), new Color(0.97f, 0.95f, 0.91f),
            new Color(0.96f, 0.95f, 0.93f), new Color(0.97f, 0.95f, 0.91f), new Color(0.2f, 0.15f, 0.26f)
        };
        static readonly Color[] catEye =
        {
            new Color(0.45f, 0.8f, 0.35f), new Color(0.4f, 0.7f, 1f), new Color(0.4f, 0.7f, 1f),
            new Color(0.75f, 0.85f, 0.3f), new Color(1f, 0.75f, 0.25f), new Color(1f, 0.85f, 0.2f)
        };
        static readonly int[] catPattern = { 1, 0, 0, 2, 3, 0 };
        static readonly Color patchDark = new Color(0.2f, 0.15f, 0.24f), patchOrange = new Color(0.96f, 0.6f, 0.3f);

        /// <summary>Side-view cat. level 0 = Kara Pati, 1 = kitten, 2 = apprentice, 3 = master.
        /// frame 0-3 walk cycle, 4 sit, 5 sleep. color = KittenColors index (-1: the old look for that level).</summary>
        public static Sprite SideCat(int level, int frame, int color = -1)
        {
            if (level <= 0) color = KittenColors.Gece;
            else if (color < 0 || color > KittenColors.Gece) color = KittenColors.Legacy(level);
            int c = color;
            return Make("sidecat_" + level + "_" + frame + "_" + c, Size, p =>
            {
                int hat = level <= 1 ? 0 : level == 2 ? 1 : level == 3 ? 2 : 3;   // 4 = Mini Apprentice: a little red cap
                DrawSideCat(p, catFur[c], catEye[c], catPattern[c], hat, frame);
            });
        }

        static void Leg(Painter p, float x, float swing, Color c) =>
            p.FillOutlined(q => S.Seg(q, new Vector2(x, -0.25f), new Vector2(x + swing, -0.64f), 0.065f), c, 0.045f);

        /// <summary>Two coat patches painted inside the body ellipse (black-and-white or calico cats).</summary>
        static void Patches(Painter p, int pattern, Vector2 bc, Vector2 br, Vector2 a, float ra, Vector2 b, float rb)
        {
            if (pattern < 2) return;
            p.Fill(q => S.I(S.Ellipse(q, bc, br * 0.97f), S.Circle(q, a, ra)), pattern == 3 ? patchOrange : patchDark);
            p.Fill(q => S.I(S.Ellipse(q, bc, br * 0.97f), S.Circle(q, b, rb)), patchDark);
        }

        static void DrawSideCat(Painter p, Color fur, Color eye, int pattern, int hat, int frame)
        {
            var furDark = new Color(fur.r * 0.72f, fur.g * 0.68f, fur.b * 0.7f);
            bool black = fur.r < 0.3f;
            bool stripes = pattern == 1;
            // the tail and legs of patched cats take the patch colour, so they read as two-tone
            var limb = pattern == 2 ? patchDark : pattern == 3 ? patchOrange : fur;
            Color? headPatch = pattern == 2 ? patchDark : pattern == 3 ? patchOrange : (Color?)null;
            if (frame <= 3)
            {
                float ph = frame * Mathf.PI * 0.5f;
                float[] lx = { -0.36f, -0.2f, 0.14f, 0.3f };
                float[] off = { 0f, Mathf.PI, Mathf.PI, 0f };
                for (int i = 1; i < 4; i += 2) Leg(p, lx[i], Mathf.Sin(ph + off[i]) * 0.1f, furDark);
                float sw = Mathf.Sin(ph) * 0.06f;
                p.FillOutlined(q => S.U(S.Seg(q, new Vector2(-0.5f, -0.05f), new Vector2(-0.74f, 0.2f + sw), 0.065f),
                    S.Seg(q, new Vector2(-0.74f, 0.2f + sw), new Vector2(-0.66f + sw, 0.5f), 0.06f)), limb, 0.045f);
                p.FillOutlined(q => S.Ellipse(q, new Vector2(-0.05f, -0.12f), new Vector2(0.52f, 0.26f)), fur, 0.05f);
                Patches(p, pattern, new Vector2(-0.05f, -0.12f), new Vector2(0.52f, 0.26f), new Vector2(-0.28f, 0.02f), 0.2f, new Vector2(0.14f, -0.24f), 0.14f);
                if (stripes)
                    p.Fill(q => S.U(S.Seg(q, new Vector2(-0.32f, 0.12f), new Vector2(-0.3f, -0.02f), 0.035f),
                        S.U(S.Seg(q, new Vector2(-0.14f, 0.14f), new Vector2(-0.12f, 0f), 0.035f),
                            S.Seg(q, new Vector2(0.04f, 0.13f), new Vector2(0.06f, 0f), 0.035f))), furDark);
                for (int i = 0; i < 4; i += 2) Leg(p, lx[i], Mathf.Sin(ph + off[i]) * 0.1f, fur);
                SideHead(p, new Vector2(0.44f, 0.14f), 1f, fur, eye, false, hat, black, headPatch);
            }
            else if (frame == 4)
            {
                p.FillOutlined(q => S.Seg(q, new Vector2(-0.4f, -0.58f), new Vector2(0.08f, -0.66f), 0.06f), limb, 0.045f);
                p.FillOutlined(q => S.Ellipse(q, new Vector2(-0.08f, -0.24f), new Vector2(0.36f, 0.4f)), fur, 0.05f);
                Patches(p, pattern, new Vector2(-0.08f, -0.24f), new Vector2(0.36f, 0.4f), new Vector2(-0.24f, -0.06f), 0.19f, new Vector2(0.04f, -0.46f), 0.14f);
                if (stripes)
                    p.Fill(q => S.U(S.Seg(q, new Vector2(-0.3f, -0.05f), new Vector2(-0.2f, -0.12f), 0.035f),
                        S.Seg(q, new Vector2(-0.32f, -0.25f), new Vector2(-0.2f, -0.3f), 0.035f)), furDark);
                p.FillOutlined(q => S.U(S.Seg(q, new Vector2(0.1f, -0.3f), new Vector2(0.12f, -0.66f), 0.065f),
                    S.Seg(q, new Vector2(0.22f, -0.3f), new Vector2(0.25f, -0.66f), 0.065f)), fur, 0.045f);
                SideHead(p, new Vector2(0.18f, 0.28f), 1f, fur, eye, false, hat, black, headPatch);
            }
            else
            {
                p.FillOutlined(q => S.Ellipse(q, new Vector2(-0.02f, -0.42f), new Vector2(0.6f, 0.26f)), fur, 0.05f);
                Patches(p, pattern, new Vector2(-0.02f, -0.42f), new Vector2(0.6f, 0.26f), new Vector2(-0.26f, -0.3f), 0.2f, new Vector2(0.16f, -0.5f), 0.13f);
                if (stripes)
                    p.Fill(q => S.U(S.Seg(q, new Vector2(-0.3f, -0.2f), new Vector2(-0.28f, -0.32f), 0.035f),
                        S.Seg(q, new Vector2(-0.1f, -0.18f), new Vector2(-0.08f, -0.3f), 0.035f)), furDark);
                p.FillOutlined(q => S.Seg(q, new Vector2(-0.55f, -0.55f), new Vector2(0.18f, -0.62f), 0.07f), limb, 0.045f);
                SideHead(p, new Vector2(0.38f, -0.3f), 0.85f, fur, eye, true, hat, black, headPatch);
            }
        }

        static void SideHead(Painter p, Vector2 hc, float s, Color fur, Color eye, bool sleeping, int hat, bool black, Color? patch = null)
        {
            Vector2 P(float x, float y) => hc + new Vector2(x, y) * s;
            var earA = new[] { P(-0.18f, 0.15f), P(-0.14f, 0.44f), P(-0.01f, 0.25f) };
            var earB = new[] { P(0.02f, 0.26f), P(0.16f, 0.44f), P(0.22f, 0.15f) };
            var inA = new[] { P(-0.13f, 0.2f), P(-0.12f, 0.36f), P(-0.04f, 0.25f) };
            var inB = new[] { P(0.06f, 0.26f), P(0.14f, 0.36f), P(0.17f, 0.2f) };
            var pink = new Color(1f, 0.62f, 0.68f);
            var dark = new Color(0.12f, 0.08f, 0.15f);

            p.FillOutlined(q => S.U(S.Circle(q, hc, 0.27f * s), S.U(S.Poly(q, earA), S.Poly(q, earB))), fur, 0.05f);
            if (patch.HasValue)   // a patch over the back ear and eye side of the head
                p.Fill(q => S.I(S.U(S.Circle(q, hc, 0.25f * s), S.U(S.Poly(q, earA), S.Poly(q, earB))), S.Circle(q, P(-0.14f, 0.16f), 0.17f * s)), patch.Value);
            p.Fill(q => S.U(S.Poly(q, inA), S.Poly(q, inB)), black ? new Color(0.5f, 0.3f, 0.45f) : pink);
            if (sleeping)
                p.Fill(q => S.Seg(q, P(0.03f, 0.02f), P(0.16f, 0f), 0.025f * s), dark);
            else
            {
                p.Fill(q => S.Circle(q, P(0.1f, 0.03f), 0.065f * s), eye);
                p.Fill(q => S.Ellipse(q, P(0.115f, 0.03f), new Vector2(0.025f, 0.048f) * s), dark);
                p.Fill(q => S.Circle(q, P(0.09f, 0.065f), 0.018f * s), Color.white);
            }
            p.Fill(q => S.Circle(q, P(0.26f, -0.04f), 0.035f * s), pink);
            p.Fill(q => S.Circle(q, P(0.12f, -0.11f), 0.05f * s), new Color(1f, 0.55f, 0.6f, 0.4f));
            var wh = black ? new Color(0.75f, 0.7f, 0.8f) : new Color(1f, 1f, 1f, 0.9f);
            p.Fill(q => S.U(S.Seg(q, P(0.2f, -0.08f), P(0.42f, -0.04f), 0.012f), S.Seg(q, P(0.2f, -0.11f), P(0.42f, -0.15f), 0.012f)), wh);

            if (hat == 1)
            {
                var cone = new[] { P(-0.15f, 0.2f), P(0.15f, 0.2f), P(0.03f, 0.6f) };
                p.FillOutlined(q => S.Poly(q, cone), new Color(0.3f, 0.5f, 0.9f), 0.035f);
                p.FillOutlined(q => S.Ellipse(q, P(0f, 0.2f), new Vector2(0.23f, 0.045f) * s), new Color(0.25f, 0.4f, 0.8f), 0.035f);
            }
            else if (hat == 3)
            {
                // Mini Apprentice: small round red cap with a white pompom
                p.FillOutlined(q => S.U(S.Ellipse(q, P(0.0f, 0.24f), new Vector2(0.17f, 0.11f) * s), S.Box(q, P(0f, 0.17f), new Vector2(0.2f, 0.035f) * s, 0.02f)), new Color(0.88f, 0.3f, 0.32f), 0.035f);
                p.Fill(q => S.Circle(q, P(0.02f, 0.36f), 0.055f * s), new Color(1f, 0.95f, 0.9f));
            }
            else if (hat == 2)
            {
                var cone = new[] { P(-0.2f, 0.22f), P(0.2f, 0.22f), P(0.1f, 0.68f) };
                p.FillOutlined(q => S.Poly(q, cone), new Color(0.5f, 0.28f, 0.75f), 0.035f);
                p.FillOutlined(q => S.Ellipse(q, P(0f, 0.22f), new Vector2(0.32f, 0.055f) * s), new Color(0.42f, 0.22f, 0.65f), 0.035f);
                var c0 = P(0.04f, 0.38f);
                p.Fill(S.Xf(q => S.Poly(q, S.Star(5, 1f, 0.45f)), c0, 0.07f * s), new Color(1f, 0.85f, 0.3f));
            }
        }

        // ---------- portraits ----------
        /// <summary>App icon: Kara Pati in a witch hat under the moon. layer 0 = full, 1 = background only, 2 = cat only (adaptive foreground).</summary>
        public static Texture2D AppIcon(int size, int layer = 0)
        {
            var p = new Painter(size, size);
            if (layer != 2)
            {
                p.VerticalGradient(new Color(0.16f, 0.08f, 0.26f), new Color(0.38f, 0.2f, 0.52f));
                p.Glow(new Vector2(0f, -0.1f), 0.95f, new Color(1f, 0.6f, 0.25f, 0.55f));
                p.Fill(q => S.Circle(q, new Vector2(0.58f, 0.6f), 0.2f), new Color(1f, 0.95f, 0.75f));
                p.Fill(q => S.Circle(q, new Vector2(0.66f, 0.66f), 0.17f), new Color(0.3f, 0.16f, 0.44f));
                p.Fill(q => S.Poly(q - new Vector2(-0.3f, 0.8f), S.Star(4, 0.07f, 0.02f)), new Color(1f, 1f, 0.85f, 0.9f));
                p.Fill(q => S.Poly(q - new Vector2(-0.68f, 0.45f), S.Star(4, 0.06f, 0.02f)), new Color(1f, 1f, 0.85f, 0.8f));
            }
            if (layer == 0) CatHead(p, new Vector2(0f, -0.2f), 0.72f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 2);
            // adaptive icons are cropped to the centre ~66%, so the cat is drawn smaller there
            else if (layer == 2) CatHead(p, new Vector2(0f, -0.14f), 0.5f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 2);
            var tex = p.ToTexture(true);
            tex.name = "app_icon_" + layer;
            return tex;
        }

        public static Sprite KaraPatiHead => Make("karapati_head", Size, p =>
            CatHead(p, new Vector2(0f, -0.05f), 0.95f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 0));

        public static Sprite GhostCat => Make("ghostcat", Size, p =>
        {
            p.Glow(Vector2.zero, 1f, new Color(0.6f, 0.85f, 1f, 0.5f));
            CatHead(p, new Vector2(0f, -0.05f), 0.9f, new Color(0.88f, 0.95f, 1f, 0.9f), new Color(0.4f, 0.75f, 1f), false, false, 0);
        });

        // ---------- currency / icons ----------
        public static Sprite Stamp => Make("ui_stamp", Size, p =>
        {
            p.FillOutlined(q => S.Poly(q, S.Star(14, 0.92f, 0.8f)), new Color(0.82f, 0.25f, 0.32f), 0.05f);
            p.Fill(q => S.Ring(S.Circle(q, Vector2.zero, 0.64f), 0.035f), new Color(0.6f, 0.12f, 0.2f));
            var cream = new Color(1f, 0.9f, 0.8f);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.17f), new Vector2(0.26f, 0.2f)), cream);
            p.Fill(q => S.U(S.U(S.Circle(q, new Vector2(-0.28f, 0.1f), 0.1f), S.Circle(q, new Vector2(-0.1f, 0.28f), 0.1f)),
                S.U(S.Circle(q, new Vector2(0.1f, 0.28f), 0.1f), S.Circle(q, new Vector2(0.28f, 0.1f), 0.1f))), cream);
        });

        public static Sprite House => Make("ui_house", Size, p =>
        {
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.35f), new Vector2(0.58f, 0.42f), 0.06f), new Color(1f, 0.9f, 0.75f), 0.06f);
            p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.82f, 0.02f), new Vector2(0f, 0.8f), new Vector2(0.82f, 0.02f) }),
                new Color(0.85f, 0.4f, 0.35f), 0.06f);
            p.Fill(q => S.Box(q, new Vector2(0f, -0.5f), new Vector2(0.16f, 0.27f), 0.06f), new Color(0.55f, 0.35f, 0.25f));
        });

        static void Book(Painter p, Color cover)
        {
            var spine = new Color(cover.r * 0.7f, cover.g * 0.7f, cover.b * 0.7f, 1f);
            p.FillOutlined(q => S.Box(q, new Vector2(0.06f, -0.06f), new Vector2(0.6f, 0.75f), 0.08f), new Color(1f, 0.95f, 0.85f), 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(-0.04f, 0f), new Vector2(0.6f, 0.75f), 0.08f), cover, 0.05f);
            p.Fill(q => S.Box(q, new Vector2(-0.56f, 0f), new Vector2(0.06f, 0.73f), 0f), spine);
            p.Fill(q => S.Poly(q, S.Star(5, 0.3f, 0.13f)), new Color(1f, 0.85f, 0.35f));
        }

        public static Sprite AlbumIcon => Make("ui_album", Size, p => Book(p, new Color(0.55f, 0.3f, 0.8f)));

        public static Sprite GlowSprite => Make("ui_glow", Size, p => p.Glow(Vector2.zero, 1f, Color.white));

        public static Sprite Moon => Make("moon", Size, p =>
            p.Fill(q => S.Sub(S.Circle(q, Vector2.zero, 0.8f), S.Circle(q, new Vector2(0.38f, 0.26f), 0.68f)), new Color(1f, 0.95f, 0.7f)));

        static readonly Color[] potionColors =
        {
            new Color(1f, 0.45f, 0.65f), new Color(0.45f, 0.95f, 0.5f), new Color(0.45f, 0.7f, 1f),
            new Color(1f, 0.7f, 0.3f), new Color(0.75f, 0.45f, 1f)
        };

        public static Sprite Bottle(int i) => Make("bottle_" + (i % potionColors.Length), Size, p =>
        {
            var liq = potionColors[i % potionColors.Length];
            p.FillOutlined(q => S.U(S.Circle(q, new Vector2(0f, -0.32f), 0.5f), S.Box(q, new Vector2(0f, 0.3f), new Vector2(0.16f, 0.3f), 0.04f)),
                new Color(0.82f, 0.9f, 1f, 0.6f), 0.05f);
            p.Fill(q => S.I(S.Circle(q, new Vector2(0f, -0.32f), 0.44f), q.y + 0.25f), liq);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.66f), new Vector2(0.13f, 0.1f), 0.03f), new Color(0.6f, 0.4f, 0.25f), 0.04f);
            p.Fill(q => S.Ellipse(q, new Vector2(-0.18f, -0.2f), new Vector2(0.07f, 0.15f)), new Color(1f, 1f, 1f, 0.6f));
        });

        public static Sprite Lantern(bool lit) => Make("lantern_" + (lit ? 1 : 0), Size, p =>
        {
            var metal = new Color(0.3f, 0.26f, 0.32f);
            p.FillOutlined(q => S.Ring(S.Circle(q, new Vector2(0f, 0.74f), 0.12f), 0.04f), metal, 0.03f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.52f), new Vector2(0.34f, 0.08f), 0.04f), metal, 0.04f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.3f, 0.52f), 0.1f),
                lit ? new Color(1f, 0.85f, 0.45f) : new Color(0.35f, 0.33f, 0.4f), 0.05f);
            if (lit) p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.12f), new Vector2(0.1f, 0.18f)), new Color(1f, 0.98f, 0.85f));
            p.Fill(q => S.U(S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.03f, 0.52f), 0f), S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.3f, 0.03f), 0f)), metal);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.68f), new Vector2(0.36f, 0.07f), 0.04f), metal, 0.04f);
        });

        // ---------- witch items (also stickers) ----------
        public static Sprite WitchItem(string id) => Make("witem_" + id, Size, p =>
        {
            switch (id)
            {
                case "w_sapka":
                    p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, -0.4f), new Vector2(0.88f, 0.18f)), new Color(0.42f, 0.22f, 0.65f), 0.05f);
                    p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.45f, -0.38f), new Vector2(0.45f, -0.38f), new Vector2(0.2f, 0.88f) }),
                        new Color(0.5f, 0.28f, 0.75f), 0.05f);
                    p.Fill(q => S.Box(q, new Vector2(0f, -0.25f), new Vector2(0.4f, 0.08f), 0f), new Color(1f, 0.75f, 0.3f));
                    p.Fill(q => S.Poly(q, S.Star(5, 0.13f, 0.06f)), new Color(1f, 0.9f, 0.4f));
                    break;
                case "w_supurge":
                    p.FillOutlined(q => S.Seg(q, new Vector2(-0.72f, 0.78f), new Vector2(0.22f, -0.18f), 0.06f), new Color(0.6f, 0.4f, 0.25f), 0.045f);
                    p.FillOutlined(q => S.Poly(q, new[] { new Vector2(0.1f, -0.14f), new Vector2(0.3f, 0.06f), new Vector2(0.88f, -0.55f), new Vector2(0.5f, -0.9f) }),
                        new Color(0.9f, 0.72f, 0.38f), 0.05f);
                    p.Fill(q => S.Seg(q, new Vector2(0.12f, -0.02f), new Vector2(0.26f, -0.16f), 0.07f), new Color(0.82f, 0.28f, 0.32f));
                    break;
                case "w_kitap":
                    Book(p, new Color(0.3f, 0.55f, 0.38f));
                    break;
                case "w_kure":
                    p.Glow(new Vector2(0f, 0.05f), 0.95f, new Color(0.6f, 0.85f, 1f, 0.5f));
                    p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.42f, -0.85f), new Vector2(0.42f, -0.85f), new Vector2(0.26f, -0.5f), new Vector2(-0.26f, -0.5f) }),
                        new Color(0.55f, 0.36f, 0.25f), 0.05f);
                    p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.05f), 0.58f), new Color(0.65f, 0.85f, 1f, 0.92f), 0.05f);
                    p.Fill(q => S.Circle(q, new Vector2(0.12f, 0.12f), 0.22f), new Color(0.9f, 0.75f, 1f, 0.6f));
                    p.Fill(q => S.Ellipse(q, new Vector2(-0.25f, 0.3f), new Vector2(0.1f, 0.16f)), new Color(1f, 1f, 1f, 0.7f));
                    break;
                case "w_mektup":
                    p.FillOutlined(q => S.Box(q, Vector2.zero, new Vector2(0.78f, 0.52f), 0.06f), new Color(1f, 0.94f, 0.8f), 0.05f);
                    p.Fill(q => S.U(S.Seg(q, new Vector2(-0.72f, 0.46f), new Vector2(0f, -0.02f), 0.03f), S.Seg(q, new Vector2(0f, -0.02f), new Vector2(0.72f, 0.46f), 0.03f)),
                        new Color(0.7f, 0.55f, 0.45f));
                    p.FillOutlined(q => S.Circle(q, new Vector2(0f, -0.02f), 0.15f), new Color(0.82f, 0.25f, 0.32f), 0.03f);
                    break;
                default:
                    Cauldron(p);
                    break;
            }
        });

        /// <summary>Icon used for a sticker id.</summary>
        public static Sprite StickerIcon(string id)
        {
            if (id.StartsWith("g") && int.TryParse(id.Substring(1), out int gi)) return Ghost(gi);
            if (id.StartsWith("r") && int.TryParse(id.Substring(1), out int ci)) return SideCat(2, 4, ci);   // coloured apprentices
            switch (id)
            {
                case "c_yavru": return Item(Chains.Kedi, 1);
                case "c_uykucu": return SleepCat;
                case "c_cirak": return Item(Chains.Kedi, 2);
                case "c_usta": return Item(Chains.Kedi, 3);
                case "c_karapati": return KaraPatiHead;
                case "c_hayaletkedi": return GhostCat;
                case "w_kazan": return Item(Chains.Kazan, 1);
                default: return WitchItem(id);
            }
        }
    }
}
