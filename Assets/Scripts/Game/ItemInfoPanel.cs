using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Item encyclopedia opened with the "i" button: the whole merge chain (undiscovered levels stay "?"),
    /// where the item comes from, and which potions use it.
    /// </summary>
    public class ItemInfoPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, body;
        Image headIcon;
        Text title, sub;

        public bool IsOpen => gameObject.activeSelf;

        static readonly Color Ink = new Color(1f, 0.96f, 0.88f);
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.6f);

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.7f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1180f));

            var iconBg = UIKit.Img("IconBg", panel, ProcArt.Circle, new Color(0.14f, 0.09f, 0.2f));
            iconBg.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -24f), new Vector2(150f, 150f));
            headIcon = UIKit.Img("Icon", iconBg.transform, null, Color.white);
            headIcon.rectTransform.sizeDelta = new Vector2(124f, 124f);
            headIcon.preserveAspect = true;

            title = UIKit.Txt("Title", panel, "", 48, UIKit.Orange, TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(200f, -40f), new Vector2(660f, 64f));
            sub = UIKit.Txt("Sub", panel, "", 28, Dim, TextAnchor.MiddleLeft, false);
            sub.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(202f, -108f), new Vector2(660f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            body = UIKit.Rect("Body", panel);
            UIKit.Stretch(body, 30f, 30f, 200f, 30f);
            gameObject.SetActive(false);
        }

        public void Open(ItemData d)
        {
            if (d == null) return;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill(d);
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        // ------------------------------------------------------------ layout helpers
        float y;

        Text Heading(string s)
        {
            var t = UIKit.Txt("Heading", body, s, 32, UIKit.Orange, TextAnchor.MiddleLeft, false);
            t.fontStyle = FontStyle.Bold;
            Top(t.rectTransform, 46f);
            y += 54f;
            return t;
        }

        void Top(RectTransform rt, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(0f, h);
        }

        RectTransform Tile(RectTransform parent, float x, Sprite s, bool known, bool current, string label, int level)
        {
            var bg = UIKit.Img("Tile", parent, ProcArt.Rounded, current ? new Color(0.45f, 0.3f, 0.2f) : new Color(0.14f, 0.09f, 0.2f, 0.95f), true);
            var rt = bg.rectTransform;
            rt.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(x, 0f), new Vector2(150f, 150f));
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            if (current)
            {
                var fr = UIKit.Img("Frame", rt, ProcArt.Frame, UIKit.Orange, true);
                fr.rectTransform.sizeDelta = new Vector2(162f, 162f);
            }
            var ic = UIKit.Img("Icon", rt, s, known ? Color.white : new Color(0.08f, 0.04f, 0.12f, 0.9f));
            ic.rectTransform.sizeDelta = new Vector2(124f, 124f);
            ic.preserveAspect = true;
            if (!known)
            {
                var q = UIKit.Txt("Q", rt, "?", 64, new Color(1f, 1f, 1f, 0.8f));
                q.fontStyle = FontStyle.Bold;
                UIKit.Stretch(q.rectTransform);
            }
            if (level > 0)
            {
                var lv = UIKit.Img("Lv", rt, ProcArt.Circle, new Color(0.1f, 0.06f, 0.15f, 0.95f));
                lv.rectTransform.At(new Vector2(52f, -52f), new Vector2(42f, 42f));
                var lt = UIKit.Txt("N", lv.transform, level.ToString(), 24, Ink, TextAnchor.MiddleCenter, false);
                UIKit.Stretch(lt.rectTransform);
            }
            if (label != null)
            {
                var name = UIKit.Txt("Name", rt, known ? label : "???", 22, known ? Ink : Dim, TextAnchor.UpperCenter, false);
                name.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -6f), new Vector2(170f, 60f));
            }
            return rt;
        }

        RectTransform Row(float h)
        {
            var r = UIKit.Rect("Row", body);
            Top(r, h);
            y += h;
            return r;
        }

        Text Para(string s, float h = 44f)
        {
            var t = UIKit.Txt("Text", body, s, 27, Ink, TextAnchor.UpperLeft, false);
            Top(t.rectTransform, h);
            y += h;
            return t;
        }

        // ------------------------------------------------------------ content
        void Fill(ItemData d)
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            y = 0f;
            headIcon.sprite = ProcArt.Item(d.chain, d.level);
            title.text = d.Name;

            if (Chains.IsIngredient(d.chain)) FillIngredient(d);
            else if (Chains.IsPotion(d.chain)) FillPotion(d);
            else if (Chains.IsGenerator(d.chain)) FillGenerator(d);
            else { sub.text = Chains.ChainTitle(d.chain); Para("Dükkânın bir parçası."); }
            // shrink the window to its content
            panel.sizeDelta = new Vector2(1000f, Mathf.Clamp(200f + y + 50f, 560f, 1180f));
        }

        void FillIngredient(ItemData d)
        {
            int max = Chains.MaxLevel(d.chain);
            sub.text = "Seviye " + d.level + "/" + max + "  •  " + Chains.ChainTitle(d.chain);

            Heading("Birleştirme zinciri");
            var row = Row(250f);
            float step = 940f / max;
            for (int l = 1; l <= max; l++)
            {
                bool known = GM.IsDiscovered(d.chain, l);
                float x = (l - 1) * step + (step - 150f) / 2f;
                Tile(row, x, ProcArt.Item(d.chain, l), known, l == d.level, Chains.Name(d.chain, l), l);
                if (l < max)
                {
                    var arrow = UIKit.Txt("Arrow", row, "›", 48, UIKit.Orange, TextAnchor.MiddleCenter, false);
                    arrow.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(x + 150f + (step - 150f) / 2f, -75f), new Vector2(40f, 60f));
                }
            }
            int found = 0;
            for (int l = 1; l <= max; l++) if (GM.IsDiscovered(d.chain, l)) found++;
            Para(found < max ? "Birleştirdikçe gizli seviyeler açılır. (" + found + "/" + max + " keşfedildi)" : "Bütün seviyeleri keşfettin!", 50f);
            y += 10f;

            Heading("Nereden gelir?");
            string src = Chains.SourceOf(d.chain);
            var srow = Row(170f);
            Tile(srow, 0f, ProcArt.Item(src, 1), true, false, null, 0);
            var st = UIKit.Txt("Src", srow, Chains.Name(src, 1) + ": dokun, 1 enerji harca.\nBazen 2. seviye çıkar. Kaynak kutu masada parlıyor.",
                26, Ink, TextAnchor.MiddleLeft, false);
            st.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(176f, -75f), new Vector2(760f, 150f));
            st.rectTransform.pivot = new Vector2(0f, 0.5f);
            st.rectTransform.anchoredPosition = new Vector2(176f, -75f);
            y += 10f;

            Heading("Nerede kullanılır?");
            var uses = new List<Recipe>();
            foreach (var r in Recipes.All)
                if (Recipes.Unlocked(r, GM.TasksDone) && (r.aChain == d.chain || r.bChain == d.chain)) uses.Add(r);
            if (uses.Count == 0) { Para("Hayalet siparişlerinde. İksir tarifleri ilerledikçe açılır."); return; }
            var urow = Row(230f);
            for (int i = 0; i < uses.Count && i < 5; i++)
            {
                var r = uses[i];
                int need = r.aChain == d.chain ? r.aLevel : r.bLevel;
                Tile(urow, i * 188f, ProcArt.Item(Chains.Iksir, r.id), true, need == d.level, r.Name + "\n(Sv. " + need + ")", 0);
            }
        }

        void FillPotion(ItemData d)
        {
            var r = Recipes.Get(d.level);
            sub.text = Chains.ChainTitle(d.chain);
            if (r == null) return;
            Heading("Tarif");
            var row = Row(250f);
            Tile(row, 60f, ProcArt.Item(r.aChain, r.aLevel), true, false, Chains.Name(r.aChain, r.aLevel), r.aLevel);
            var plus = UIKit.Txt("Plus", row, "+", 64, UIKit.Orange, TextAnchor.MiddleCenter, false);
            plus.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(270f, -75f), new Vector2(60f, 80f));
            Tile(row, 330f, ProcArt.Item(r.bChain, r.bLevel), true, false, Chains.Name(r.bChain, r.bLevel), r.bLevel);
            var eq = UIKit.Txt("Eq", row, "=", 64, UIKit.Orange, TextAnchor.MiddleCenter, false);
            eq.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(540f, -75f), new Vector2(60f, 80f));
            Tile(row, 600f, ProcArt.Item(Chains.Iksir, r.id), true, true, r.Name, 0);

            Heading("Nereden gelir?");
            Para("Simya Kazanı'nda kaynar: " + (r.seconds >= 60 ? (r.seconds / 60f).ToString("0.#") + " dakika" : r.seconds + " saniye") + ".", 50f);
            Heading("Değeri");
            Para("Hayalet siparişinde " + r.Gold + " altın + " + Recipes.PotionStamps + " Pati Mührü. Satarsan " + BoardController.PotionSellPrice(r.id) + " altın.", 80f);
        }

        void FillGenerator(ItemData d)
        {
            sub.text = "Üretici";
            Heading("Ne üretir?");
            var row = Row(250f);
            string a = d.chain == Chains.Kazan ? Chains.Ot : Chains.Kristal;
            string b = d.chain == Chains.Kazan ? Chains.Mantar : Chains.Ag;
            Tile(row, 0f, ProcArt.Item(a, 1), GM.IsDiscovered(a, 1), false, Chains.Name(a, 1), 1);
            Tile(row, 188f, ProcArt.Item(b, 1), GM.IsDiscovered(b, 1), false, Chains.Name(b, 1), 1);
            Tile(row, 376f, ProcArt.Item(a, 2), GM.IsDiscovered(a, 2), false, Chains.Name(a, 2), 2);
            Tile(row, 564f, ProcArt.Item(b, 2), GM.IsDiscovered(b, 2), false, Chains.Name(b, 2), 2);
            Para("Her dokunuş 1 enerji. Çoğunlukla 1. seviye, bazen 2. seviye çıkar.", 50f);
        }
    }
}
