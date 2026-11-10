using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>L.T("Cadı Dosyası"): the notebook where every clue found in the scenes is kept.</summary>
    public class CaseFilePanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, list, tabs;
        Text count;
        int tab = -1;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.7f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.93f, 0.86f, 0.72f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1640f));

            var icon = UIKit.Img("Icon", panel, ProcArt.CaseFile, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -24f), new Vector2(100f, 100f));
            var title = UIKit.Txt("Title", panel, L.T("Cadı Dosyası"), 52, new Color(0.35f, 0.2f, 0.12f), TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(150f, -30f), new Vector2(600f, 70f));
            count = UIKit.Txt("Count", panel, "", 30, new Color(0.45f, 0.3f, 0.2f), TextAnchor.MiddleLeft, false);
            count.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(152f, -92f), new Vector2(600f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.75f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            tabs = UIKit.Rect("Tabs", panel);
            tabs.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -150f), new Vector2(940f, 90f));
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -150f);

            list = UIKit.Rect("List", panel);
            UIKit.Stretch(list, 30f, 30f, 262f, 30f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            tab = Story.CurrentChapter(GM.TasksDone);
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void Fill()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            for (int i = tabs.childCount - 1; i >= 0; i--) Destroy(tabs.GetChild(i).gameObject);
            int current = Story.CurrentChapter(GM.TasksDone);
            int n = Story.Chapters.Length;
            float tw = 940f / n;
            var ink = new Color(0.3f, 0.18f, 0.1f);

            // chapter tabs with a found/total count each
            for (int ch = 0; ch < n; ch++)
            {
                int c0 = ch, total = 0, got = 0;
                foreach (var c in Story.Clues) if (c.chapter == ch) { total++; if (GM.HasClue(c.id)) got++; }
                bool open = ch <= current;
                bool sel = ch == tab;
                var col = sel ? new Color(0.65f, 0.3f, 0.15f) : open ? new Color(0.82f, 0.68f, 0.5f) : new Color(0.75f, 0.7f, 0.62f);
                var b = UIKit.Btn("Tab" + ch, tabs, open ? L.F("{0}. Bölüm\n{1}/{2}", ch + 1, got, total) : "", col, () => SelectTab(c0), 24);
                var brt = (RectTransform)b.transform;
                brt.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(ch * tw + 3f, 0f), new Vector2(tw - 6f, 90f));
                brt.pivot = new Vector2(0f, 1f);
                brt.anchoredPosition = new Vector2(ch * tw + 3f, 0f);
                var lbl = b.Label();
                if (lbl) lbl.color = sel ? Color.white : ink;
                if (!open)
                {
                    var lk = UIKit.Img("Lock", brt, ProcArt.Lock, new Color(1f, 1f, 1f, 0.8f));
                    lk.rectTransform.At(Vector2.zero, new Vector2(54f, 54f));
                }
            }

            int found = 0;
            foreach (var c in Story.Clues) if (GM.HasClue(c.id)) found++;
            count.text = L.F("{0}/{1} ipucu bulundu", found, Story.Clues.Length);

            var head = UIKit.Txt("Chapter", list, Story.Chapters[tab].name, 34, new Color(0.65f, 0.3f, 0.15f), TextAnchor.MiddleLeft, false);
            head.fontStyle = FontStyle.Bold;
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(-20f, 50f);

            int row = 0;
            foreach (var c in Story.Clues)
            {
                if (c.chapter != tab) continue;
                bool has = GM.HasClue(c.id);
                var bg = UIKit.Img("Clue", list, ProcArt.Rounded, has ? new Color(1f, 0.97f, 0.9f) : new Color(0.85f, 0.78f, 0.64f), true);
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(64f + row * 200f));
                rt.sizeDelta = new Vector2(0f, 186f);

                var ic = UIKit.Img("Icon", bg.transform, has ? ProcArt.ClueIcon(c.id) : ProcArt.Sparkle,
                    has ? (c.id.EndsWith("_iz") ? new Color(0.45f, 0.65f, 0.85f) : Color.white) : new Color(0.55f, 0.45f, 0.35f, 0.5f));
                ic.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(20f, 0f), new Vector2(110f, 110f));
                var t = UIKit.Txt("Title", bg.transform, has ? c.title : "???", 34, ink, TextAnchor.UpperLeft, false);
                t.fontStyle = FontStyle.Bold;
                t.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(150f, -16f), new Vector2(760f, 44f));
                t.rectTransform.pivot = new Vector2(0f, 1f);
                var d = UIKit.Txt("Text", bg.transform, has ? c.text : L.T("Henüz bulunmadı. Görevleri ilerlet ve göz düğmesini kullan."), 26,
                    has ? ink : new Color(0.45f, 0.35f, 0.25f), TextAnchor.UpperLeft, false);
                d.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(150f, -62f), new Vector2(760f, 120f));
                d.rectTransform.pivot = new Vector2(0f, 1f);
                row++;
            }
            if (row == 0)
            {
                var none = UIKit.Txt("None", list, L.T("Bu bölümde gizli ipucu yok."), 30, new Color(0.45f, 0.35f, 0.25f), TextAnchor.MiddleCenter, false);
                none.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -140f), new Vector2(800f, 60f));
            }
        }

        void SelectTab(int ch)
        {
            if (ch > Story.CurrentChapter(GM.TasksDone)) { GM.Toast(L.T("Bu bölüm henüz kilitli.")); return; }
            tab = ch;
            Fill();
        }

    }
}
