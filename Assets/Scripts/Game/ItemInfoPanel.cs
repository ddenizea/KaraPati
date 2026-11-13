using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Item encyclopedia opened with the "i" button: the whole merge chain as a grid of tiles with arrows
    /// (undiscovered levels stay "?", the selected level is framed, a green badge shows how many of each
    /// level are on the table), where the item comes from, and which potions use it.
    /// The grid adapts to the chain length, so a future 14-level chain still fits.
    /// </summary>
    public class ItemInfoPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, body;
        Text title;

        public bool IsOpen => gameObject.activeSelf;

        static readonly Color Paper = new Color(0.93f, 0.86f, 0.72f);
        static readonly Color Card = new Color(1f, 0.96f, 0.88f);
        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);
        static readonly Color Dim = new Color(0.32f, 0.2f, 0.12f, 0.55f);
        static readonly Color TileBg = new Color(0.86f, 0.79f, 0.66f);
        static readonly Color TileUnknown = new Color(0.8f, 0.74f, 0.62f);
        static readonly Color TileCurrent = new Color(0.6f, 0.82f, 0.42f);
        static readonly Color Badge = new Color(0.3f, 0.72f, 0.42f);
        const float Gap = 46f;   // room for the arrow between two tiles

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = Theme.Ui(new Color(0.03f, 0.01f, 0.06f, 0.7f));
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, Paper, true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the panel don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(960f, 1200f));

            // coloured header with the item name, like a recipe card
            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.6f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 130f);
            title = UIKit.Txt("Title", head.transform, "", 56, Color.white, TextAnchor.MiddleCenter, false);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 56;
            UIKit.Stretch(title.rectTransform, 120f, 120f, 10f, 10f);
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.Outline;
            outline.effectDistance = new Vector2(2f, -2f);

            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            body = UIKit.Rect("Body", panel);
            UIKit.Stretch(body, 30f, 30f, 150f, 30f);
            gameObject.SetActive(false);
        }

        public void Open(ItemData d)
        {
            Sfx.Play("page");
            if (d == null) return;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill(d);
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        // ------------------------------------------------------------ layout helpers
        float y;   // how far down the body we have filled

        void Top(RectTransform rt, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(0f, h);
        }

        Text Line(string s, float h, int size = 28, bool bold = false)
        {
            var t = UIKit.Txt("Text", body, s, size, Ink, TextAnchor.MiddleCenter, false);
            if (bold) t.fontStyle = FontStyle.Bold;
            Top(t.rectTransform, h);
            y += h;
            return t;
        }

        /// <summary>A lighter card behind a section.</summary>
        RectTransform CardBox(float h)
        {
            var c = UIKit.Img("Card", body, ProcArt.Rounded, Card, true);
            Top(c.rectTransform, h);
            y += h;
            return c.rectTransform;
        }

        /// <summary>One square tile. count = how many are on the table (green badge), level = small number in the corner.</summary>
        void Tile(RectTransform parent, Vector2 pos, float size, Sprite s, bool known, bool current, int count, int level)
        {
            var bg = UIKit.Img("Tile", parent, ProcArt.Rounded, current ? TileCurrent : known ? TileBg : TileUnknown, true);
            var rt = bg.rectTransform;
            rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(pos, new Vector2(size, size));
            if (current)
            {
                var fr = UIKit.Img("Frame", rt, ProcArt.Frame, new Color(1f, 1f, 1f, 0.95f), true);
                fr.rectTransform.sizeDelta = new Vector2(size + 16f, size + 16f);
            }
            if (known)
            {
                var ic = UIKit.Img("Icon", rt, s, Color.white);
                ic.rectTransform.sizeDelta = new Vector2(size * 0.78f, size * 0.78f);
                ic.preserveAspect = true;
                if (level > 0)
                {
                    var lt = UIKit.Txt("Lv", rt, level.ToString(), Mathf.RoundToInt(size * 0.15f), new Color(0.32f, 0.2f, 0.12f, 0.7f), TextAnchor.LowerRight, false);
                    lt.fontStyle = FontStyle.Bold;
                    UIKit.Stretch(lt.rectTransform, 8f, 10f, 8f, 4f);
                }
            }
            else
            {
                var q = UIKit.Txt("Q", rt, "?", Mathf.RoundToInt(size * 0.5f), new Color(1f, 1f, 1f, 0.85f));
                q.fontStyle = FontStyle.Bold;
                UIKit.Stretch(q.rectTransform);
            }
            if (count > 0)
            {
                // how many of these are on the table right now
                var b = UIKit.Img("Count", rt, ProcArt.Circle, Badge);
                b.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(6f, -6f), new Vector2(48f, 48f));
                var bt = UIKit.Txt("N", b.transform, count.ToString(), 26, Color.white, TextAnchor.MiddleCenter, false);
                bt.fontStyle = FontStyle.Bold;
                UIKit.Stretch(bt.rectTransform);
            }
        }

        void Arrow(RectTransform parent, Vector2 pos)
        {
            var a = UIKit.Txt("Arrow", parent, "›", 64, new Color(0.32f, 0.2f, 0.12f, 0.45f), TextAnchor.MiddleCenter, false);
            a.fontStyle = FontStyle.Bold;
            a.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(pos, new Vector2(Gap, 80f));
        }

        void Sign(RectTransform parent, string s, Vector2 pos)
        {
            var t = UIKit.Txt("Sign", parent, s, 60, Dim, TextAnchor.MiddleCenter, false);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(pos, new Vector2(80f, 80f));
        }

        Text CardTitle(RectTransform card, string s)
        {
            var t = UIKit.Txt("Head", card, s, 38, Dim, TextAnchor.MiddleCenter, false);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -16f), new Vector2(700f, 56f));
            return t;
        }

        int OnTable(string chain, int level)
        {
            int n = 0;
            foreach (var c in GM.Board.AllCells())
            {
                var d = GM.Board.Get(c);
                if (d != null && !d.bubble && d.chain == chain && d.level == level) n++;
            }
            return n;
        }

        /// <summary>
        /// How to lay out a chain of `levels`: short chains sit on one centred row, up to 16 levels use 4 columns,
        /// longer ones 5 smaller columns so even a 20-level chain fits on a phone screen.
        /// </summary>
        static void GridFor(int levels, out int columns, out float tile)
        {
            if (levels <= 4) { columns = levels; tile = 170f; }
            else if (levels <= 16) { columns = 4; tile = 170f; }
            else { columns = 5; tile = 128f; }
        }

        // ------------------------------------------------------------ content
        void Fill(ItemData d)
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            y = 0f;
            title.text = d.Name;

            if (Chains.IsIngredient(d.chain)) FillIngredient(d);
            else if (Chains.IsPotion(d.chain)) FillPotion(d);
            else if (Chains.IsGenerator(d.chain)) FillGenerator(d);
            else Line(L.T("Dükkânın bir parçası."), 60f);
            // the window grows with its content (but never past the screen)
            panel.sizeDelta = new Vector2(960f, Mathf.Clamp(150f + y + 40f, 520f, 1900f));
        }

        void FillIngredient(ItemData d)
        {
            int max = Chains.MaxLevel(d.chain);
            int found = 0;
            for (int l = 1; l <= max; l++) if (GM.IsDiscovered(d.chain, l)) found++;

            // ---- the chain: rows of tiles with an arrow after each one (also at a row's end: the chain continues below)
            GridFor(max, out int columns, out float tile);
            int rows = (max + columns - 1) / columns;
            float rowH = tile + 30f;
            var card = CardBox(100f + rows * rowH + 60f);
            CardTitle(card, L.F("Sv. {0} / {1}", d.level, max));

            float rowW = columns * tile + (columns - 1) * Gap;
            float x0 = -rowW / 2f + tile / 2f;
            for (int l = 1; l <= max; l++)
            {
                int i = l - 1, col = i % columns, row = i / columns;
                var pos = new Vector2(x0 + col * (tile + Gap), -96f - row * rowH - tile / 2f);
                bool known = GM.IsDiscovered(d.chain, l);
                Tile(card, pos, tile, ProcArt.Item(d.chain, l), known, l == d.level, OnTable(d.chain, l), l);
                if (l < max) Arrow(card, pos + new Vector2(tile / 2f + Gap / 2f, 0f));
            }
            var hint = UIKit.Txt("Hint", card,
                found < max ? L.F("Birleştirdikçe gizli seviyeler açılır ({0}/{1}).", found, max) : L.T("Bütün seviyeleri keşfettin!"),
                26, Dim, TextAnchor.MiddleCenter, false);
            hint.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 32f), new Vector2(800f, 44f));
            y += 20f;

            // ---- where it comes from: big picture of the source box
            string src = Chains.SourceOf(d.chain);
            var srow = UIKit.Rect("Source", body);
            Top(srow, 170f);
            y += 170f;
            var st = UIKit.Txt("Label", srow, L.T("Nereden çıkar:"), 40, Ink, TextAnchor.MiddleRight, false);
            st.fontStyle = FontStyle.Bold;
            st.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 18f), new Vector2(420f, 60f));
            var sd = UIKit.Txt("Desc", srow, L.T("dokun: 1 enerji"), 26, Dim, TextAnchor.MiddleRight, false);
            sd.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, -26f), new Vector2(420f, 40f));
            var icon = UIKit.Img("Src", srow, ProcArt.Item(src, 1), Color.white);
            icon.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 0f), new Vector2(150f, 150f));
            icon.preserveAspect = true;
            var sn = UIKit.Txt("Name", srow, Chains.Name(src, 1), 26, Ink, TextAnchor.MiddleLeft, false);
            sn.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(170f, 0f), new Vector2(260f, 40f));

            // ---- which potions use it (only unlocked recipes)
            var uses = new List<Recipe>();
            foreach (var r in Recipes.All)
                if (Recipes.Unlocked(r, GM.TasksDone) && (r.aChain == d.chain || r.bChain == d.chain)) uses.Add(r);
            if (uses.Count == 0) return;
            y += 10f;
            Line(L.T("İksirlerde kullanılır:"), 50f, 32, true);
            var urow = UIKit.Rect("Uses", body);
            Top(urow, 180f);
            y += 180f;
            int n = Mathf.Min(uses.Count, 5);
            for (int i = 0; i < n; i++)
            {
                var r = uses[i];
                int need = r.aChain == d.chain ? r.aLevel : r.bLevel;
                var pos = new Vector2((i - (n - 1) / 2f) * 170f, -64f);
                Tile(urow, pos, 120f, ProcArt.Item(Chains.Iksir, r.id), true, need == d.level, 0, 0);
                var cap = UIKit.Txt("Cap", urow, L.F("Sv. {0} lazım", need), 22, Dim, TextAnchor.UpperCenter, false);
                cap.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(pos + new Vector2(0f, -70f), new Vector2(170f, 34f));
            }
        }

        void FillPotion(ItemData d)
        {
            var r = Recipes.Get(d.level);
            if (r == null) return;
            var card = CardBox(300f);
            CardTitle(card, "Tarif");
            Tile(card, new Vector2(-300f, -170f), 160f, ProcArt.Item(r.aChain, r.aLevel), true, false, OnTable(r.aChain, r.aLevel), r.aLevel);
            Sign(card, "+", new Vector2(-150f, -170f));
            Tile(card, new Vector2(0f, -170f), 160f, ProcArt.Item(r.bChain, r.bLevel), true, false, OnTable(r.bChain, r.bLevel), r.bLevel);
            Sign(card, "=", new Vector2(150f, -170f));
            Tile(card, new Vector2(300f, -170f), 160f, ProcArt.Item(Chains.Iksir, r.id), true, true, 0, 0);
            y += 20f;
            Line(L.F("Simya Kazanı'nda kaynar: {0}.", r.seconds >= 60 ? L.F("{0} dakika", (r.seconds / 60f).ToString("0.#")) : L.F("{0} saniye", r.seconds)), 50f);
            Line(L.F("Siparişte {0} altın + {1} Pati Mührü. Satarsan {2} altın.", r.Gold, Recipes.PotionStamps, BoardController.PotionSellPrice(r.id)), 50f);
        }

        void FillGenerator(ItemData d)
        {
            int gl = GM.GeneratorLevel(d.chain);
            // the two chains at levels 1 and 2, or (Kara Kazan) the magic chain at levels 1 to 3
            bool kara = d.chain == Chains.KaraKazan;
            string a = d.chain == Chains.Kazan ? Chains.Ot : Chains.Kristal;
            string b = d.chain == Chains.Kazan ? Chains.Mantar : Chains.Ag;
            var tiles = kara
                ? new[] { (Chains.Sihir, 1), (Chains.Sihir, 2), (Chains.Sihir, 3) }
                : new[] { (a, 1), (b, 1), (a, 2), (b, 2) };
            var card = CardBox(300f);
            CardTitle(card, L.F("Üretir  •  Sv. {0}/{1}", gl, Generators.MaxLevel));
            for (int i = 0; i < tiles.Length; i++)
            {
                var (tc, tl) = tiles[i];
                float x = (i - (tiles.Length - 1) * 0.5f) * 200f;
                Tile(card, new Vector2(x, -170f), 160f, ProcArt.Item(tc, tl), GM.IsDiscovered(tc, tl), false, OnTable(tc, tl), tl);
            }
            y += 20f;
            Line(L.F("Her dokunuş 1 enerji. {0}.", Generators.ChanceText(gl)), 50f);
            if (kara) Line(L.F("%{0} ihtimalle iki tane birden çıkar.", Mathf.RoundToInt(Generators.KaraKazanDouble * 100f)), 50f);
            Line(gl < Generators.MaxLevel
                ? L.F("Sonraki seviye {0} altın: {1}.", Generators.UpgradeCost(gl), Generators.ChanceText(gl + 1))
                : L.T("En üst seviyede!"), 50f);
        }
    }
}
