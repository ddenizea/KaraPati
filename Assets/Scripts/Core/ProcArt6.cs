using UnityEngine;

namespace KaraPati
{
    /// <summary>Art for the Game Room and its mini games (Fare Avı: barrels and mice).</summary>
    public static partial class ProcArt
    {
        /// <summary>Home screen button of the Game Room: a yarn ball with a little sparkle.</summary>
        public static Sprite GameRoomIcon => Make("ui_gameroom", Size, p =>
        {
            Yarn(p, 0.78f, new Color(0.95f, 0.5f, 0.62f));
            p.FillOutlined(q => S.Poly(q - new Vector2(0.55f, 0.58f), S.Star(4, 0.32f, 0.1f)), new Color(1f, 0.92f, 0.5f), 0.04f);
        });

        /// <summary>Gold trophy cup with a star (home screen achievements button).</summary>
        public static Sprite Trophy => Make("ui_trophy", Size, p =>
        {
            var gold = new Color(1f, 0.8f, 0.3f);
            var dark = new Color(0.85f, 0.58f, 0.18f);
            // handles first, the cup covers their inner half
            p.FillOutlined(q => S.U(S.Ring(S.Ellipse(q, new Vector2(-0.55f, 0.35f), new Vector2(0.24f, 0.22f)), 0.06f),
                                    S.Ring(S.Ellipse(q, new Vector2(0.55f, 0.35f), new Vector2(0.24f, 0.22f)), 0.06f)), gold, 0.04f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.78f), new Vector2(0.42f, 0.1f), 0.05f), dark, 0.05f);
            p.FillOutlined(q => S.Box(q, new Vector2(0f, -0.56f), new Vector2(0.1f, 0.16f), 0.02f), gold, 0.05f);
            p.FillOutlined(q => S.I(S.Ellipse(q, new Vector2(0f, 0.28f), new Vector2(0.6f, 0.72f)), S.Box(q, new Vector2(0f, 0.05f), new Vector2(0.7f, 0.62f), 0f)), gold, 0.05f);
            p.Fill(q => S.I(S.Ellipse(q, new Vector2(0.18f, 0.2f), new Vector2(0.38f, 0.55f)), S.Box(q, new Vector2(0.35f, 0.05f), new Vector2(0.3f, 0.6f), 0f)), dark);   // shade on the right
            p.FillOutlined(q => S.Poly(q - new Vector2(0f, 0.25f), S.Star(5, 0.26f, 0.11f)), new Color(1f, 0.96f, 0.75f), 0.03f);
        });

        static readonly Color[] MedalColors =
        {
            new Color(0.72f, 0.68f, 0.76f), new Color(0.86f, 0.53f, 0.28f), new Color(0.84f, 0.87f, 0.92f), new Color(1f, 0.82f, 0.3f),
        };

        /// <summary>Achievement medal: 0 = nothing yet (grey), 1 bronze, 2 silver, 3 gold.</summary>
        public static Sprite Medal(int step) => Make("ui_medal_" + step, Size, p =>
        {
            var c = MedalColors[Mathf.Clamp(step, 0, 3)];
            var ribbon = step > 0 ? new Color(0.85f, 0.32f, 0.38f) : new Color(0.55f, 0.5f, 0.6f);
            p.FillOutlined(q => S.U(S.Seg(q, new Vector2(-0.36f, 0.88f), new Vector2(-0.12f, 0.2f), 0.13f),
                                    S.Seg(q, new Vector2(0.36f, 0.88f), new Vector2(0.12f, 0.2f), 0.13f)), ribbon, 0.04f);
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, -0.22f), new Vector2(0.66f, 0.66f)), c, 0.05f);
            p.Fill(q => S.Ring(S.Ellipse(q, new Vector2(0f, -0.22f), new Vector2(0.5f, 0.5f)), 0.03f), Color.Lerp(c, Color.black, 0.2f));
            p.FillOutlined(q => S.Poly(q - new Vector2(0f, -0.22f), S.Star(5, 0.3f, 0.13f)), Color.Lerp(c, Color.white, 0.45f), 0.03f);
        });

        /// <summary>Wooden cellar barrel seen from the front, its dark mouth at the top.</summary>
        public static Sprite Barrel => Make("mg_barrel", Size, p =>
        {
            var wood = new Color(0.6f, 0.38f, 0.22f);
            var woodDark = new Color(0.45f, 0.27f, 0.15f);
            var band = new Color(0.42f, 0.42f, 0.5f);
            // body: slightly bulging sides
            Sdf body = q => S.I(S.Ellipse(q, new Vector2(0f, -0.15f), new Vector2(0.92f, 1.05f)), S.Box(q, new Vector2(0f, -0.2f), new Vector2(0.9f, 0.7f), 0.05f));
            p.FillOutlined(body, wood, 0.05f);
            // staves
            p.Fill(q => S.I(body(q), S.U(S.Seg(q, new Vector2(-0.45f, 0.5f), new Vector2(-0.48f, -0.9f), 0.02f),
                S.U(S.Seg(q, new Vector2(0f, 0.5f), new Vector2(0f, -0.9f), 0.02f), S.Seg(q, new Vector2(0.45f, 0.5f), new Vector2(0.48f, -0.9f), 0.02f)))), woodDark);
            // metal bands
            p.Fill(q => S.I(body(q), S.U(S.Box(q, new Vector2(0f, 0.22f), new Vector2(1f, 0.06f), 0f), S.Box(q, new Vector2(0f, -0.62f), new Vector2(1f, 0.06f), 0f))), band);
            // mouth
            p.FillOutlined(q => S.Ellipse(q, new Vector2(0f, 0.5f), new Vector2(0.82f, 0.2f)), woodDark, 0.05f);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, 0.48f), new Vector2(0.68f, 0.13f)), new Color(0.12f, 0.07f, 0.1f));
        });

        /// <summary>Front of the barrel only (the rim and body below the mouth), drawn over the mouse so it pops out of the barrel.</summary>
        public static Sprite BarrelFront => Make("mg_barrel_front", Size, p =>
        {
            var wood = new Color(0.6f, 0.38f, 0.22f);
            var woodDark = new Color(0.45f, 0.27f, 0.15f);
            var band = new Color(0.42f, 0.42f, 0.5f);
            // the body minus the open mouth: what stands in front of a mouse climbing out
            Sdf front = q => S.Sub(S.I(S.Ellipse(q, new Vector2(0f, -0.15f), new Vector2(0.92f, 1.05f)), S.Box(q, new Vector2(0f, -0.2f), new Vector2(0.9f, 0.7f), 0.05f)),
                S.Ellipse(q, new Vector2(0f, 0.5f), new Vector2(0.68f, 0.13f)));
            p.FillOutlined(front, wood, 0.05f);
            p.Fill(q => S.I(front(q), S.U(S.Seg(q, new Vector2(-0.45f, 0.42f), new Vector2(-0.48f, -0.9f), 0.02f),
                S.U(S.Seg(q, new Vector2(0f, 0.3f), new Vector2(0f, -0.9f), 0.02f), S.Seg(q, new Vector2(0.45f, 0.42f), new Vector2(0.48f, -0.9f), 0.02f)))), woodDark);
            p.Fill(q => S.I(front(q), S.U(S.Box(q, new Vector2(0f, 0.22f), new Vector2(1f, 0.06f), 0f), S.Box(q, new Vector2(0f, -0.62f), new Vector2(1f, 0.06f), 0f))), band);
            // the lip of the mouth (front half of the rim)
            p.Fill(q => S.I(S.Ring(S.Ellipse(q, new Vector2(0f, 0.5f), new Vector2(0.75f, 0.165f)), 0.05f), q.y - 0.5f), woodDark);
        });

        /// <summary>Settings button: a cream cog.</summary>
        public static Sprite Gear => Make("ui_gear", Size, p =>
        {
            var teeth = new Vector2[32];
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                float r = (i / 2) % 2 == 0 ? 0.86f : 0.66f;   // pairs of points: tooth top, then the gap
                teeth[i] = new Vector2(Mathf.Cos(a + 0.2f), Mathf.Sin(a + 0.2f)) * r;
            }
            p.FillOutlined(q => S.Sub(S.U(S.Poly(q, teeth), S.Circle(q, Vector2.zero, 0.7f)), S.Circle(q, Vector2.zero, 0.26f)), new Color(1f, 0.93f, 0.8f), 0.06f);
        });

        /// <summary>Golden hourglass: the bonus card in İksir Hafızası (+5 seconds).</summary>
        public static Sprite Hourglass => Make("mg_hourglass", Size, p =>
        {
            var gold = new Color(1f, 0.78f, 0.28f);
            var glass = new Color(0.85f, 0.93f, 1f, 0.85f);
            var sand = new Color(1f, 0.86f, 0.45f);
            p.Glow(Vector2.zero, 1f, new Color(1f, 0.85f, 0.4f, 0.35f));
            var top = new[] { new Vector2(-0.45f, 0.62f), new Vector2(0.45f, 0.62f), new Vector2(0.06f, 0.02f), new Vector2(-0.06f, 0.02f) };
            var bot = new[] { new Vector2(-0.06f, -0.02f), new Vector2(0.06f, -0.02f), new Vector2(0.45f, -0.62f), new Vector2(-0.45f, -0.62f) };
            p.FillOutlined(q => S.U(S.Poly(q, top), S.Poly(q, bot)), glass, 0.05f);
            p.Fill(q => S.I(S.Poly(q, top), q.y - 0.3f), sand);                                // a little sand left above the neck
            p.Fill(q => S.I(S.Poly(q, bot), q.y + 0.3f), sand);                                // a pile at the bottom
            p.Fill(q => S.Seg(q, new Vector2(0f, -0.02f), new Vector2(0f, -0.3f), 0.025f), sand);   // the falling stream
            p.FillOutlined(q => S.U(S.Box(q, new Vector2(0f, 0.7f), new Vector2(0.6f, 0.09f), 0.04f), S.Box(q, new Vector2(0f, -0.7f), new Vector2(0.6f, 0.09f), 0.04f)), gold, 0.05f);
        });

        /// <summary>Pink yarn ball thrown in Yumak Atma.</summary>
        public static Sprite YarnBall => Make("mg_yarn", Size, p => Yarn(p, 0.95f, new Color(0.95f, 0.5f, 0.62f)));

        /// <summary>White ring (tint it): the spot where the jumping cat can catch the yarn.</summary>
        public static Sprite TargetRing => Make("mg_ring", Size, p => p.Fill(q => S.Ring(S.Circle(q, Vector2.zero, 0.84f), 0.08f), Color.white));

        /// <summary>A cellar mouse peeking up; gold = the rare golden mouse.</summary>
        public static Sprite Mouse(bool gold) => Make(gold ? "mg_mouse_gold" : "mg_mouse", Size, p =>
        {
            var fur = gold ? new Color(1f, 0.8f, 0.3f) : new Color(0.66f, 0.64f, 0.72f);
            var inner = gold ? new Color(1f, 0.62f, 0.4f) : new Color(1f, 0.66f, 0.74f);
            var pink = new Color(1f, 0.5f, 0.6f);
            if (gold) p.Glow(new Vector2(0f, 0.1f), 1f, new Color(1f, 0.9f, 0.4f, 0.45f));
            // ears
            p.FillOutlined(q => S.U(S.Circle(q, new Vector2(-0.5f, 0.45f), 0.3f), S.Circle(q, new Vector2(0.5f, 0.45f), 0.3f)), fur, 0.05f);
            p.Fill(q => S.U(S.Circle(q, new Vector2(-0.5f, 0.45f), 0.19f), S.Circle(q, new Vector2(0.5f, 0.45f), 0.19f)), inner);
            // head and body
            p.FillOutlined(q => S.U(S.Ellipse(q, new Vector2(0f, 0.02f), new Vector2(0.52f, 0.46f)), S.Ellipse(q, new Vector2(0f, -0.7f), new Vector2(0.5f, 0.45f))), fur, 0.05f);
            // paws on the barrel rim
            p.FillOutlined(q => S.U(S.Ellipse(q, new Vector2(-0.28f, -0.48f), new Vector2(0.13f, 0.09f)), S.Ellipse(q, new Vector2(0.28f, -0.48f), new Vector2(0.13f, 0.09f))), inner, 0.04f);
            // face
            p.Fill(q => S.U(S.Circle(q, new Vector2(-0.19f, 0.08f), 0.075f), S.Circle(q, new Vector2(0.19f, 0.08f), 0.075f)), Outline);
            p.Fill(q => S.U(S.Circle(q, new Vector2(-0.17f, 0.11f), 0.025f), S.Circle(q, new Vector2(0.21f, 0.11f), 0.025f)), Color.white);
            p.Fill(q => S.Ellipse(q, new Vector2(0f, -0.1f), new Vector2(0.08f, 0.06f)), pink);
            p.Fill(q => S.U(S.U(S.Seg(q, new Vector2(-0.12f, -0.12f), new Vector2(-0.5f, -0.04f), 0.012f), S.Seg(q, new Vector2(-0.12f, -0.15f), new Vector2(-0.48f, -0.22f), 0.012f)),
                S.U(S.Seg(q, new Vector2(0.12f, -0.12f), new Vector2(0.5f, -0.04f), 0.012f), S.Seg(q, new Vector2(0.12f, -0.15f), new Vector2(0.48f, -0.22f), 0.012f))), Outline);
            if (gold) p.Fill(q => S.Poly(q - new Vector2(0.45f, 0.82f), S.Star(4, 0.16f, 0.05f)), Color.white);
        });
    }
}
