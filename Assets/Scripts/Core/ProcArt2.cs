using UnityEngine;

namespace KaraPati
{
    /// <summary>Second half of the placeholder art: walking cats, shop props, stickers, icons.</summary>
    public static partial class ProcArt
    {
        /// <summary>Side-view cat. level 0 = Kara Pati, 1 = kitten, 2 = apprentice, 3 = master.
        /// frame 0-3 walk cycle, 4 sit, 5 sleep. color = KittenColors index (-1: the old look for that level).</summary>
        public static Sprite SideCat(int level, int frame, int color = -1)
        {
            if (level <= 0) color = KittenColors.Gece;
            else if (color < 0 || color > KittenColors.Gece) color = KittenColors.Legacy(level);
            int c = color;
            string key = "sidecat_" + level + "_" + frame + "_" + c;
            int hat = level <= 1 ? 0 : level == 2 ? 1 : level == 3 ? 2 : 3;   // 4 = Mini Apprentice: a little red cap

            // Cats have drawn pictures (Resources/Cats); the code drawing below is only the fallback.
            // Kara Pati: karapati_<pose>. Other coats: cat<colour>_<pose>_<hat>. A night-black master is Kara Pati's twin.
            if (cache.TryGetValue(key, out var have) && have) return have;
            string pose = CatPose(frame);
            Sprite art = null;
            if (level <= 0 || (c == KittenColors.Gece && hat == 2)) art = LoadArt("Cats/karapati_" + pose, key);
            else if (c != KittenColors.Gece) art = LoadArt("Cats/cat" + c + "_" + pose + "_" + hat, key);
            if (art) return art;

            return Make(key, Size, p => DrawSideCat(p, catFur[c], catEye[c], catPattern[c], hat, frame));
        }

        /// <summary>Which drawn picture a cat frame uses: the walk cycle alternates a step and a standing pose.</summary>
        static string CatPose(int frame)
        {
            switch (frame)
            {
                case 0: return "walk1";
                case 2: return "walk2";
                case 4: return "sit";
                case 5: return "sleep";
                default: return "stand";
            }
        }

        // ---------- portraits ----------
        /// <summary>App icon: Kara Pati in a witch hat under the moon. layer 0 = full, 1 = background only, 2 = cat only (adaptive foreground).</summary>
        public static Texture2D AppIcon(int size, int layer = 0)
        {
            var p = new Painter(size, size);
            if (layer != 2)
            {
                // flat night sky, a big pale moon behind the cat, two little stars
                var sky = new Color(0.3f, 0.26f, 0.5f);
                p.VerticalGradient(sky, sky);
                p.Fill(q => S.Circle(q, new Vector2(0f, -0.08f), 0.72f), new Color(1f, 0.93f, 0.7f));
                p.Fill(q => S.Poly(q - new Vector2(-0.72f, 0.74f), S.Star(4, 0.11f, 0.04f)), new Color(1f, 0.95f, 0.78f));
                p.Fill(q => S.Poly(q - new Vector2(0.76f, 0.62f), S.Star(4, 0.08f, 0.03f)), new Color(1f, 0.95f, 0.78f));
            }
            p.Cozy = true; p.WobbleSeed = 1.3f;
            if (layer == 0) CatHead(p, new Vector2(0f, -0.2f), 0.72f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 2);
            // adaptive icons are cropped to the centre ~66%, so the cat is drawn smaller there
            else if (layer == 2) CatHead(p, new Vector2(0f, -0.14f), 0.5f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 2);
            var tex = p.ToTexture(true);
            tex.name = "app_icon_" + layer;
            return tex;
        }

        public static Sprite KaraPatiHead
        {
            get
            {
                if (cache.TryGetValue("karapati_head", out var have) && have) return have;
                var art = LoadArt("Cats/karapati_head", "karapati_head");
                return art ? art : Make("karapati_head", Size, p =>
                    CatHead(p, new Vector2(0f, -0.05f), 0.95f, catFur[KittenColors.Gece], catEye[KittenColors.Gece], false, false, 0));
            }
        }

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
            p.FillOutlined(q => S.Sub(S.Circle(q, Vector2.zero, 0.8f), S.Circle(q, new Vector2(0.38f, 0.26f), 0.68f)), new Color(1f, 0.95f, 0.7f), 0.05f));

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
                case "w_kazan": return Item(Chains.KaraKazan, 1);
                default: return WitchItem(id);
            }
        }
    }
}
