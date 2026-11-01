using UnityEngine;

namespace KaraPati
{
    /// <summary>Props for chapters 3-6: garden, ghost town, tower, mirror realm.</summary>
    public static partial class ProcArt
    {
        public static Sprite FullMoon => Make("full_moon", Size, p =>
        {
            p.Glow(Vector2.zero, 1f, new Color(1f, 0.95f, 0.7f, 0.35f));
            p.Fill(q => S.Circle(q, Vector2.zero, 0.7f), new Color(1f, 0.96f, 0.78f));
            var crater = new Color(0.9f, 0.85f, 0.65f);
            p.Fill(q => S.Circle(q, new Vector2(-0.25f, 0.2f), 0.14f), crater);
            p.Fill(q => S.Circle(q, new Vector2(0.2f, -0.18f), 0.1f), crater);
            p.Fill(q => S.Circle(q, new Vector2(0.28f, 0.28f), 0.07f), crater);
        });

        /// <summary>White bush silhouette; tint with the Image colour.</summary>
        public static Sprite Bush => Make("bush", Size, p =>
        {
            p.Fill(q => S.U(S.U(S.Circle(q, new Vector2(-0.45f, -0.25f), 0.45f), S.Circle(q, new Vector2(0.45f, -0.25f), 0.45f)),
                S.U(S.Circle(q, new Vector2(0f, 0.05f), 0.55f), S.Box(q, new Vector2(0f, -0.45f), new Vector2(0.88f, 0.25f), 0.1f))), Color.white);
        });

        public static Sprite Fountain => Make("fountain", Size, p =>
        {
            var stone = new Color(0.62f, 0.62f, 0.7f);
            var dark = new Color(0.45f, 0.45f, 0.52f);
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, -0.55f), new Vector2(0.92f, 0.28f)), stone, 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.5f), new Vector2(0.78f, 0.17f)), dark);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.1f), new Vector2(0.12f, 0.42f), 0.04f), stone, 0.05f);
            p.FillOutlined(q => S.I(S.Ellipse(q, new Vector2(0f, 0.3f), new Vector2(0.5f, 0.22f)), q.y - 0.32f), stone, 0.05f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.38f), 0.09f), stone, 0.04f);
        });

        public static Sprite Scarecrow => Make("scarecrow", Size, p =>
        {
            var wood = new Color(0.55f, 0.38f, 0.22f);
            p.FillOutlined(q => S.U(S.Seg(q, new Vector2(0f, -0.95f), new Vector2(0f, 0.5f), 0.06f), S.Seg(q, new Vector2(-0.7f, 0.15f), new Vector2(0.7f, 0.15f), 0.05f)), wood, 0.04f);
            p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.45f, 0.25f), new Vector2(0.45f, 0.25f), new Vector2(0.32f, -0.45f), new Vector2(-0.32f, -0.45f) }),
                new Color(0.4f, 0.5f, 0.75f), 0.05f);
            p.Fill(q => S.U(S.Seg(q, new Vector2(-0.2f, 0.1f), new Vector2(-0.12f, -0.3f), 0.03f), S.Seg(q, new Vector2(0.18f, 0.12f), new Vector2(0.1f, -0.32f), 0.03f)),
                new Color(0.3f, 0.38f, 0.6f));
            p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.48f), 0.22f), new Color(0.88f, 0.75f, 0.5f), 0.04f);
            var stitch = new Color(0.35f, 0.25f, 0.15f);
            p.Fill(q => S.U(S.Circle(q, new Vector2(-0.08f, 0.52f), 0.035f), S.Circle(q, new Vector2(0.08f, 0.52f), 0.035f)), stitch);
            p.Fill(q => S.Seg(q, new Vector2(-0.08f, 0.4f), new Vector2(0.08f, 0.4f), 0.015f), stitch);
            p.Fill(q => S.U(S.Seg(q, new Vector2(-0.7f, 0.15f), new Vector2(-0.82f, 0.05f), 0.025f), S.Seg(q, new Vector2(0.7f, 0.15f), new Vector2(0.82f, 0.05f), 0.025f)),
                new Color(0.95f, 0.8f, 0.4f));
        });

        public static Sprite ClockFace => Make("clock_face", Size, p =>
        {
            p.FillOutlined(q => S.Circle(q, Vector2.zero, 0.85f), new Color(0.98f, 0.94f, 0.82f), 0.07f);
            p.Fill(q => S.Ring(S.Circle(q, Vector2.zero, 0.72f), 0.025f), new Color(0.6f, 0.5f, 0.4f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                float len = i % 3 == 0 ? 0.14f : 0.07f;
                p.Fill(q => S.Seg(q, d * 0.68f, d * (0.68f - len), 0.025f), new Color(0.3f, 0.22f, 0.18f));
            }
            p.Fill(q => S.Circle(q, Vector2.zero, 0.06f), new Color(0.3f, 0.22f, 0.18f));
        });

        /// <summary>White bell; tint grey or gold.</summary>
        public static Sprite Bell => Make("bell", Size, p =>
        {
            p.FillOutlined(q => S.U(S.U(S.Circle(q, new Vector2(0f, 0.25f), 0.42f), S.Poly(q, new[]
            {
                new Vector2(-0.42f, 0.25f), new Vector2(0.42f, 0.25f), new Vector2(0.7f, -0.55f), new Vector2(-0.7f, -0.55f)
            })), S.Box(q, new Vector2(0f, -0.58f), new Vector2(0.75f, 0.08f), 0.06f)), Color.white, 0.05f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0f, -0.75f), 0.12f), Color.white, 0.04f);
            p.FillOutlined(q => S.Ring(S.Circle(q, new Vector2(0f, 0.75f), 0.1f), 0.04f), Color.white, 0.03f);
        });

        public static Sprite Telescope => Make("telescope", Size, p =>
        {
            var wood = new Color(0.45f, 0.3f, 0.2f);
            p.FillOutlined(q => S.U(S.Seg(q, new Vector2(0f, -0.1f), new Vector2(-0.4f, -0.9f), 0.04f),
                S.U(S.Seg(q, new Vector2(0f, -0.1f), new Vector2(0.4f, -0.9f), 0.04f), S.Seg(q, new Vector2(0f, -0.1f), new Vector2(0f, -0.9f), 0.04f))), wood, 0.03f);
            p.FillOutlined(q => S.Box(q, new Vector2(0.05f, 0.05f), new Vector2(0.62f, 0.13f), 0.05f), new Color(0.85f, 0.65f, 0.3f), 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(0.62f, 0.05f), new Vector2(0.12f, 0.19f), 0.04f), new Color(0.75f, 0.55f, 0.25f), 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(-0.6f, 0.05f), new Vector2(0.08f, 0.09f), 0.03f), new Color(0.75f, 0.55f, 0.25f), 0.04f);
            p.Fill(q => S.Ellipse(q, new Vector2(0.75f, 0.05f), new Vector2(0.03f, 0.13f)), new Color(0.6f, 0.85f, 1f));
        });

        public static Sprite Cage => Make("cage", Size, p =>
        {
            var metal = new Color(0.8f, 0.68f, 0.35f);
            p.FillOutlined(q => S.I(S.Ring(S.Circle(q, new Vector2(0f, 0.25f), 0.7f), 0.05f), -(q.y - 0.25f)), metal, 0.03f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.85f), new Vector2(0.78f, 0.08f), 0.04f), metal, 0.04f);
            for (int i = -3; i <= 3; i++)
            {
                float x = i * 0.22f;
                float top = 0.25f + Mathf.Sqrt(Mathf.Max(0f, 0.49f - x * x));
                p.Fill(q => S.Seg(q, new Vector2(x, -0.8f), new Vector2(x, top), 0.03f), metal);
            }
            p.FillOutlined(q => S.Ring(S.Circle(q, new Vector2(0f, 1f - 0.04f), 0.06f), 0.025f), metal, 0.02f);
        });

        /// <summary>White triangle pennant; tint per flag.</summary>
        public static Sprite Pennant => Make("pennant", 64, p =>
            p.Fill(q => S.Poly(q, new[] { new Vector2(-0.9f, 0.9f), new Vector2(0.9f, 0.9f), new Vector2(0f, -0.9f) }), Color.white));

        public static Sprite Feather => Make("feather", Size, p =>
        {
            p.FillOutlined(S.Xf(q => S.I(S.Circle(q, new Vector2(-0.35f, 0f), 0.6f), S.Circle(q, new Vector2(0.35f, 0f), 0.6f)), Vector2.zero, 1.2f, -35f),
                new Color(0.18f, 0.14f, 0.24f), 0.05f);
            p.Fill(S.Xf(q => S.Seg(q, new Vector2(0f, -0.75f), new Vector2(0f, 0.45f), 0.03f), Vector2.zero, 1.2f, -35f), new Color(0.5f, 0.45f, 0.6f));
        });

        /// <summary>Witch silhouette for the mirror-realm cage and the portrait.</summary>
        public static Sprite Witch => Make("witch", Size, p =>
        {
            var dress = new Color(0.4f, 0.22f, 0.55f);
            p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.5f, -0.9f), new Vector2(0.5f, -0.9f), new Vector2(0.22f, -0.05f), new Vector2(-0.22f, -0.05f) }), dress, 0.05f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0f, 0.12f), 0.22f), new Color(1f, 0.86f, 0.75f), 0.04f);
            p.Fill(q => S.U(S.Circle(q, new Vector2(-0.08f, 0.14f), 0.03f), S.Circle(q, new Vector2(0.08f, 0.14f), 0.03f)), new Color(0.2f, 0.12f, 0.2f));
            p.Fill(q => S.I(S.Ring(S.Circle(q, new Vector2(0f, 0.08f), 0.08f), 0.015f), q.y - 0.06f), new Color(0.6f, 0.25f, 0.3f));
            p.Fill(q => S.U(S.Ellipse(q, new Vector2(-0.22f, -0.05f), new Vector2(0.08f, 0.25f)), S.Ellipse(q, new Vector2(0.22f, -0.05f), new Vector2(0.08f, 0.25f))),
                new Color(0.75f, 0.35f, 0.2f));
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, 0.3f), new Vector2(0.48f, 0.08f)), new Color(0.3f, 0.16f, 0.42f), 0.04f);
            p.FillOutlined(q => S.Poly(q, new[] { new Vector2(-0.24f, 0.32f), new Vector2(0.24f, 0.32f), new Vector2(0.14f, 0.9f) }), new Color(0.35f, 0.18f, 0.48f), 0.04f);
            p.Fill(q => S.Box(q, new Vector2(0f, 0.4f), new Vector2(0.22f, 0.05f), 0f), new Color(1f, 0.75f, 0.3f));
        });

        /// <summary>Icons for clues added in chapters 3-6 (falls back to the paw print).</summary>
        static Sprite ExtraClueIcon(string id)
        {
            switch (id)
            {
                case "c3_tuy": return Feather;
                case "c3_not": return WitchItem("w_mektup");
                case "c4_afis": return WitchItem("w_mektup");
                case "c4_foto": return SideCat(0, 4);
                case "c5_gunluk": return WitchItem("w_kitap");
                case "c5_tasma": return Coin;
                case "c6_oyuncak": return Item(Chains.Ag, 2);
                case "c6_mektup": return WitchItem("w_mektup");
                default: return PawPrint;
            }
        }

        // ---------- alchemy ----------
        static readonly Color[] flaskColors =
        {
            new Color(0.62f, 0.55f, 1f),   // uyku
            new Color(1f, 0.85f, 0.3f),    // ışıltı
            new Color(0.35f, 0.25f, 0.5f), // gölge
            new Color(0.75f, 0.9f, 1f),    // ay
            new Color(0.55f, 1f, 0.85f),   // hayalet
            new Color(1f, 0.5f, 0.75f),    // yıldız
        };

        public static Color PotionColor(int id) => flaskColors[Mathf.Clamp(id - 1, 0, flaskColors.Length - 1)];

        /// <summary>The purple alchemy cauldron that brews potions.</summary>
        static void SimyaPot(Painter p)
        {
            var iron = new Color(0.3f, 0.2f, 0.4f);
            var gold = new Color(1f, 0.78f, 0.3f);
            p.Glow(new Vector2(0f, 0.3f), 0.7f, new Color(0.8f, 0.5f, 1f, 0.45f));
            p.FillOutlined(q => S.U(S.Box(q, new Vector2(-0.48f, -0.76f), new Vector2(0.09f, 0.14f), 0.04f),
                S.Box(q, new Vector2(0.48f, -0.76f), new Vector2(0.09f, 0.14f), 0.04f)), iron, 0.04f);
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, -0.2f), new Vector2(0.84f, 0.6f)), iron, 0.05f);
            p.Fill(q => S.Ring(S.Ellipse(q, new Vector2(0f, -0.22f), new Vector2(0.66f, 0.4f)), 0.035f), new Color(gold.r, gold.g, gold.b, 0.8f));
            p.Fill(q => S.Poly(q - new Vector2(0f, -0.22f), S.Star(5, 0.17f, 0.07f)), gold);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.34f), new Vector2(0.9f, 0.11f), 0.09f), gold, 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.36f), new Vector2(0.74f, 0.07f)), new Color(0.8f, 0.45f, 1f));
            var bub = new Color(0.9f, 0.7f, 1f);
            p.FillOutlined(q => S.Circle(q, new Vector2(-0.22f, 0.6f), 0.12f), bub, 0.035f);
            p.FillOutlined(q => S.Circle(q, new Vector2(0.2f, 0.76f), 0.09f), bub, 0.035f);
            p.Fill(q => S.Poly(q - new Vector2(0.45f, 0.62f), S.Star(4, 0.1f, 0.03f)), new Color(1f, 1f, 0.8f));
        }

        /// <summary>Potion bottles: each recipe has its own colour and flask shape.</summary>
        static void PotionFlask(Painter p, int id)
        {
            var liq = PotionColor(id);
            var glass = new Color(0.85f, 0.92f, 1f, 0.55f);
            var cork = new Color(0.6f, 0.4f, 0.25f);
            int shape = (id - 1) % 3;
            p.Glow(new Vector2(0f, -0.2f), 0.75f, new Color(liq.r, liq.g, liq.b, 0.4f));
            if (shape == 0)
            {
                // round flask
                p.FillOutlined(q => S.U(S.Circle(q, new Vector2(0f, -0.3f), 0.52f), S.Box(q, new Vector2(0f, 0.32f), new Vector2(0.15f, 0.28f), 0.04f)), glass, 0.05f);
                p.Fill(q => S.I(S.Circle(q, new Vector2(0f, -0.3f), 0.45f), q.y + 0.2f), liq);
            }
            else if (shape == 1)
            {
                // tall vial
                p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.3f, 0.66f), 0.22f), glass, 0.05f);
                p.Fill(q => S.I(S.Box(q, new Vector2(0f, -0.08f), new Vector2(0.24f, 0.6f), 0.18f), q.y + 0.05f), liq);
            }
            else
            {
                // triangle (erlenmeyer) flask
                var tri = new[] { new Vector2(-0.6f, -0.78f), new Vector2(0.6f, -0.78f), new Vector2(0.16f, 0.2f), new Vector2(-0.16f, 0.2f) };
                p.FillOutlined(q => S.U(S.Poly(q, tri) - 0.04f, S.Box(q, new Vector2(0f, 0.38f), new Vector2(0.15f, 0.22f), 0.04f)), glass, 0.05f);
                var liqTri = new[] { new Vector2(-0.52f, -0.72f), new Vector2(0.52f, -0.72f), new Vector2(0.28f, -0.2f), new Vector2(-0.28f, -0.2f) };
                p.Fill(q => S.Poly(q, liqTri), liq);
            }
            p.FillOutlined(q => S.Box(q, new Vector2(0f, 0.68f), new Vector2(0.13f, 0.1f), 0.03f), cork, 0.04f);
            p.Fill(q => S.Ellipse(q, new Vector2(-0.18f, -0.25f), new Vector2(0.06f, 0.14f)), new Color(1f, 1f, 1f, 0.6f));
            // stars mark the stronger potions
            int stars = (id + 1) / 2;
            for (int i = 0; i < stars; i++)
            {
                var c = new Vector2(-0.11f * (stars - 1) + i * 0.22f, -0.42f);
                p.Fill(q => S.Poly(q - c, S.Star(5, 0.1f, 0.045f)), new Color(1f, 0.95f, 0.6f));
            }
        }
            /// <summary>Sound toggle icon. mode 0 = all on (two waves), 1 = music off (one wave + note crossed), 2 = muted (cross).</summary>
        public static Sprite Speaker(int mode) => Make("speaker_" + mode, Size, p =>
        {
            var body = new Color(1f, 0.92f, 0.8f);
            var cone = new[] { new Vector2(-0.6f, -0.22f), new Vector2(-0.3f, -0.22f), new Vector2(0.05f, -0.55f), new Vector2(0.05f, 0.55f), new Vector2(-0.3f, 0.22f), new Vector2(-0.6f, 0.22f) };
            p.FillOutlined(q => S.Poly(q, cone), body, 0.06f);
            if (mode == 2)
            {
                var red = new Color(1f, 0.45f, 0.45f);
                p.FillOutlined(q => S.U(S.Seg(q, new Vector2(0.25f, -0.3f), new Vector2(0.75f, 0.3f), 0.07f),
                    S.Seg(q, new Vector2(0.25f, 0.3f), new Vector2(0.75f, -0.3f), 0.07f)), red, 0.05f);
                return;
            }
            p.FillOutlined(q => S.I(S.Ring(S.Circle(q, new Vector2(0.05f, 0f), 0.35f), 0.05f), -(q.x - 0.15f)), body, 0.04f);
            if (mode == 0)
                p.FillOutlined(q => S.I(S.Ring(S.Circle(q, new Vector2(0.05f, 0f), 0.62f), 0.055f), -(q.x - 0.2f)), body, 0.04f);
            else
            {
                // small music note with a slash: music is off
                var nc = new Color(1f, 0.7f, 0.4f);
                p.FillOutlined(q => S.U(S.Ellipse(q, new Vector2(0.55f, -0.35f), new Vector2(0.13f, 0.1f)), S.Box(q, new Vector2(0.66f, 0f), new Vector2(0.03f, 0.35f), 0.01f)), nc, 0.04f);
                p.FillOutlined(q => S.Seg(q, new Vector2(0.35f, -0.55f), new Vector2(0.85f, 0.45f), 0.045f), new Color(1f, 0.45f, 0.45f), 0.03f);
            }
        });
            /// <summary>Paint brush icon for the decoration button.</summary>
        public static Sprite Brush => Make("brush", Size, p =>
        {
            var handle = new Color(0.65f, 0.42f, 0.25f);
            p.FillOutlined(q => S.Seg(q, new Vector2(0.55f, 0.6f), new Vector2(-0.05f, 0f), 0.11f), handle, 0.05f);
            p.FillOutlined(q => S.Seg(q, new Vector2(-0.05f, 0f), new Vector2(-0.18f, -0.13f), 0.14f), new Color(0.75f, 0.75f, 0.82f), 0.05f);
            var tip = new[] { new Vector2(-0.12f, -0.28f), new Vector2(-0.32f, -0.08f), new Vector2(-0.7f, -0.62f) };
            p.FillOutlined(q => S.Poly(q, tip), new Color(1f, 0.55f, 0.25f), 0.05f);
            p.Fill(q => S.Circle(q, new Vector2(0.45f, -0.55f), 0.13f), new Color(0.65f, 0.4f, 0.95f));
            p.Fill(q => S.Circle(q, new Vector2(0.7f, -0.25f), 0.09f), new Color(0.4f, 0.85f, 0.5f));
        });
    }
}
