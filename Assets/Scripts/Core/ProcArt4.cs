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
    }
}
