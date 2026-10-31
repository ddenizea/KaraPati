using UnityEngine;

namespace KaraPati
{
    /// <summary>Cellar (chapter 2) props and investigation icons.</summary>
    public static partial class ProcArt
    {
        public static Sprite Candle(bool lit) => Make("candle_" + (lit ? 1 : 0), Size, p =>
        {
            if (lit) p.Glow(new Vector2(0f, 0.45f), 0.55f, new Color(1f, 0.75f, 0.35f, 0.7f));
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.35f), new Vector2(0.2f, 0.45f), 0.07f), new Color(0.96f, 0.92f, 0.8f), 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(0.05f, 0.05f), new Vector2(0.08f, 0.1f)), new Color(0.88f, 0.84f, 0.72f));
            p.Fill(q => S.Seg(q, new Vector2(0f, 0.1f), new Vector2(0f, 0.22f), 0.02f), new Color(0.15f, 0.1f, 0.1f));
            if (!lit) return;
            p.Fill(q => S.U(S.Circle(q, new Vector2(0f, 0.36f), 0.11f),
                S.Poly(q, new[] { new Vector2(-0.1f, 0.38f), new Vector2(0.1f, 0.38f), new Vector2(0f, 0.68f) })), new Color(1f, 0.6f, 0.2f));
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.38f), new Vector2(0.05f, 0.1f)), new Color(1f, 0.95f, 0.6f));
        });

        public static Sprite Chain => Make("chain", Size, p =>
        {
            var metal = new Color(0.62f, 0.62f, 0.68f);
            for (int i = -3; i <= 3; i++)
            {
                var c = new Vector2(i * 0.26f, 0f);
                bool flat = (i & 1) == 0;
                p.FillOutlined(q => S.Ring(S.Ellipse(q, c, flat ? new Vector2(0.17f, 0.11f) : new Vector2(0.15f, 0.06f)), 0.035f), metal, 0.03f);
            }
        });

        public static Sprite Mirror(bool fixedUp) => Make("mirror_" + (fixedUp ? 1 : 0), Size, p =>
        {
            var gold = new Color(0.85f, 0.65f, 0.3f);
            if (fixedUp) p.Glow(Vector2.zero, 1f, new Color(0.7f, 0.5f, 1f, 0.5f));
            p.FillOutlined(q => S.Ellipse(q, Vector2.zero, new Vector2(0.6f, 0.9f)), gold, 0.05f);
            p.Fill(q => S.Ellipse(q, Vector2.zero, new Vector2(0.48f, 0.78f)),
                fixedUp ? new Color(0.62f, 0.55f, 0.9f) : new Color(0.15f, 0.17f, 0.28f));
            if (fixedUp)
            {
                // a faint figure in a pointy hat behind the glass
                p.Fill(q => S.Circle(q, new Vector2(0f, 0.05f), 0.2f), new Color(1f, 1f, 1f, 0.35f));
                p.Fill(q => S.Poly(q, new[] { new Vector2(-0.28f, 0.2f), new Vector2(0.28f, 0.2f), new Vector2(0.06f, 0.62f) }), new Color(1f, 1f, 1f, 0.35f));
                p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.45f), new Vector2(0.3f, 0.25f)), new Color(1f, 1f, 1f, 0.3f));
                p.Fill(q => S.Ellipse(q, new Vector2(-0.2f, 0.4f), new Vector2(0.06f, 0.2f)), new Color(1f, 1f, 1f, 0.5f));
            }
            else
            {
                var crack = new Color(0.85f, 0.9f, 1f, 0.9f);
                p.Fill(q => S.U(S.U(S.Seg(q, new Vector2(-0.1f, 0.1f), new Vector2(-0.35f, 0.5f), 0.015f),
                        S.Seg(q, new Vector2(-0.1f, 0.1f), new Vector2(0.3f, 0.35f), 0.015f)),
                    S.U(S.Seg(q, new Vector2(-0.1f, 0.1f), new Vector2(0.05f, -0.6f), 0.015f),
                        S.Seg(q, new Vector2(-0.1f, 0.1f), new Vector2(-0.42f, -0.2f), 0.015f))), crack);
            }
        });

        /// <summary>White magic circle; tint it with the Image colour.</summary>
        public static Sprite MagicCircle => Make("magic_circle", Size, p =>
        {
            var star = S.Star(5, 0.66f, 0.26f);
            p.Fill(q => S.U(S.U(S.Ring(S.Circle(q, Vector2.zero, 0.9f), 0.04f), S.Ring(S.Circle(q, Vector2.zero, 0.72f), 0.025f)),
                S.Ring(S.Poly(q, star), 0.022f)), Color.white);
        });

        /// <summary>White paw print; tint it with the Image colour.</summary>
        public static Sprite PawPrint => Make("paw_print", Size, p =>
        {
            p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.25f), new Vector2(0.4f, 0.32f)), Color.white);
            p.Fill(q => S.U(S.U(S.Circle(q, new Vector2(-0.45f, 0.15f), 0.16f), S.Circle(q, new Vector2(-0.16f, 0.45f), 0.16f)),
                S.U(S.Circle(q, new Vector2(0.16f, 0.45f), 0.16f), S.Circle(q, new Vector2(0.45f, 0.15f), 0.16f))), Color.white);
        });

        public static Sprite Eye => Make("ui_eye", Size, p =>
        {
            p.FillOutlined(q => S.I(S.Circle(q, new Vector2(0f, -0.55f), 0.95f), S.Circle(q, new Vector2(0f, 0.55f), 0.95f)), new Color(1f, 0.96f, 0.88f), 0.06f);
            p.Fill(q => S.Circle(q, Vector2.zero, 0.3f), new Color(1f, 0.6f, 0.2f));
            p.Fill(q => S.Ellipse(q, Vector2.zero, new Vector2(0.09f, 0.22f)), Outline);
            p.Fill(q => S.Circle(q, new Vector2(-0.1f, 0.1f), 0.07f), Color.white);
        });

        public static Sprite Lock => Make("ui_lock", Size, p =>
        {
            p.FillOutlined(q => S.I(S.Ring(S.Circle(q, new Vector2(0f, 0.2f), 0.36f), 0.09f), 0.05f - q.y), new Color(0.7f, 0.7f, 0.76f), 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.3f), new Vector2(0.55f, 0.45f), 0.1f), new Color(1f, 0.78f, 0.3f), 0.06f);
            p.Fill(q => S.U(S.Circle(q, new Vector2(0f, -0.22f), 0.1f), S.Box(q, new Vector2(0f, -0.42f), new Vector2(0.04f, 0.14f), 0f)), Outline);
        });

        public static Sprite CaseFile => Make("ui_casefile", Size, p =>
        {
            p.FillOutlined(q => S.Box(q, new Vector2(0.04f, -0.04f), new Vector2(0.62f, 0.78f), 0.08f), new Color(1f, 0.95f, 0.85f), 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(-0.04f, 0.02f), new Vector2(0.62f, 0.78f), 0.08f), new Color(0.78f, 0.55f, 0.3f), 0.05f);
            p.Fill(q => S.Box(q, new Vector2(-0.04f, 0.45f), new Vector2(0.3f, 0.09f), 0.03f), new Color(1f, 0.95f, 0.85f));
            var pc = new Vector2(0.02f, -0.18f);
            p.Fill(S.Xf(q => S.U(S.Ellipse(q, new Vector2(0f, -0.25f), new Vector2(0.4f, 0.32f)),
                S.U(S.U(S.Circle(q, new Vector2(-0.45f, 0.15f), 0.16f), S.Circle(q, new Vector2(-0.16f, 0.45f), 0.16f)),
                    S.U(S.Circle(q, new Vector2(0.16f, 0.45f), 0.16f), S.Circle(q, new Vector2(0.45f, 0.15f), 0.16f)))), pc, 0.4f),
                new Color(0.45f, 0.28f, 0.15f));
        });

        /// <summary>Icon shown for a clue once it is found.</summary>
        public static Sprite ClueIcon(string id)
        {
            switch (id)
            {
                case "c1_not": return WitchItem("w_mektup");
                case "c1_gunluk": return WitchItem("w_kitap");
                case "c2_supurge": return WitchItem("w_supurge");
                case "c2_takvim": return Moon;
                case "c2_tasma": return Coin;
                case "c2_mektup": return WitchItem("w_mektup");
                default: return PawPrint;
            }
        }
    }
}
