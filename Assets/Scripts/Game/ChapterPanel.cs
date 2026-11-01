using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>"Bölümler": every chapter as a card with its character portrait, progress, status and gift chests.</summary>
    public class ChapterPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, list;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>The character that belongs to each chapter.</summary>
        public static Sprite Portrait(int ch, out Color tint)
        {
            tint = Color.white;
            switch (ch)
            {
                case 0: return ProcArt.KaraPatiHead;
                case 1: return ProcArt.Ghost(0);
                case 2: return ProcArt.Scarecrow;
                case 3: return ProcArt.Ghost(5);
                case 4: return ProcArt.GhostCat;
                default: tint = new Color(0.62f, 0.5f, 0.82f); return ProcArt.SideCat(0, 4);
            }
        }

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
            panel.At(Vector2.zero, new Vector2(1000f, 1600f));

            var title = UIKit.Txt("Title", panel, "Bölümler", 56, new Color(0.35f, 0.2f, 0.12f), TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(40f, -34f), new Vector2(600f, 70f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.75f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            list = UIKit.Rect("List", panel);
            UIKit.Stretch(list, 30f, 30f, 130f, 30f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        void Fill()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            int done = GM.TasksDone;
            int current = Story.CurrentChapter(done);
            var ink = new Color(0.3f, 0.18f, 0.1f);

            for (int ch = 0; ch < Story.Chapters.Length; ch++)
            {
                int c0 = ch;
                var chapter = Story.Chapters[ch];
                int inCh = Mathf.Clamp(done - Story.ChapterStart(ch), 0, chapter.tasks.Length);
                bool locked = ch > current;
                bool finished = inCh >= chapter.tasks.Length;
                float pct = inCh / (float)chapter.tasks.Length;

                var card = UIKit.Img("Card", list, ProcArt.Rounded,
                    locked ? new Color(0.72f, 0.66f, 0.58f) : ch == current && !finished ? new Color(1f, 0.95f, 0.85f) : new Color(0.97f, 0.92f, 0.82f), true);
                var rt = card.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -ch * 238f);
                rt.sizeDelta = new Vector2(0f, 222f);
                card.raycastTarget = true;
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnCard(c0));

                // portrait
                var ring = UIKit.Img("Ring", rt, ProcArt.Circle, finished ? UIKit.Green : ch == current ? UIKit.Orange : new Color(0.5f, 0.45f, 0.4f));
                ring.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(18f, 0f), new Vector2(176f, 176f));
                var face = UIKit.Img("FaceBg", ring.transform, ProcArt.Circle, new Color(0.2f, 0.13f, 0.28f));
                face.rectTransform.sizeDelta = new Vector2(156f, 156f);
                var p = UIKit.Img("Portrait", face.transform, Portrait(ch, out var tint), locked ? new Color(0.05f, 0.03f, 0.08f, 0.95f) : tint);
                p.rectTransform.sizeDelta = new Vector2(136f, 136f);
                p.preserveAspect = true;
                if (locked)
                {
                    var lk = UIKit.Img("Lock", face.transform, ProcArt.Lock, new Color(1f, 1f, 1f, 0.85f));
                    lk.rectTransform.sizeDelta = new Vector2(70f, 70f);
                }

                // title + status
                var name = UIKit.Txt("Name", rt, locked ? (ch + 1) + ". Bölüm: ???" : chapter.name, 36, ink, TextAnchor.MiddleLeft, false);
                name.fontStyle = FontStyle.Bold;
                name.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(214f, -22f), new Vector2(560f, 50f));
                name.rectTransform.pivot = new Vector2(0f, 1f);
                name.rectTransform.anchoredPosition = new Vector2(214f, -22f);

                string status = locked ? "Kilitli" : finished ? "Tamamlandı" : "Keşfediliyor";
                var badge = UIKit.Img("Status", rt, ProcArt.Rounded, locked ? new Color(0.5f, 0.45f, 0.42f) : finished ? UIKit.Green : UIKit.Orange, true);
                badge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-18f, -20f), new Vector2(200f, 52f));
                var st = UIKit.Txt("T", badge.transform, status, 26, Color.white, TextAnchor.MiddleCenter, false);
                st.fontStyle = FontStyle.Bold;
                UIKit.Stretch(st.rectTransform);

                if (locked)
                {
                    var hint = UIKit.Txt("Hint", rt, "Önceki bölümü bitirince açılır.", 26, new Color(0.35f, 0.25f, 0.2f), TextAnchor.MiddleLeft, false);
                    hint.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0f)).At(new Vector2(214f, 50f), new Vector2(600f, 50f));
                    continue;
                }

                // progress bar with the two gift chests
                const float BarW = 560f;
                var bar = UIKit.Img("Bar", rt, ProcArt.Rounded, new Color(0.35f, 0.25f, 0.2f, 0.35f), true);
                bar.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0.5f)).At(new Vector2(214f, 66f), new Vector2(BarW, 30f));
                var fill = UIKit.Img("Fill", bar.transform, ProcArt.Rounded, finished ? UIKit.Green : UIKit.Orange, true);
                fill.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(0f, 0f), new Vector2(Mathf.Max(24f, BarW * pct), 30f));
                for (int g = 0; g < ChapterGifts.At.Length; g++)
                {
                    bool claimed = GM.GiftClaimed(ch, g), ready = GM.GiftReady(ch, g);
                    var gi = UIKit.Img("Gift", bar.transform, ProcArt.Item(Chains.Sandik, 1),
                        claimed ? new Color(1f, 1f, 1f, 0.5f) : ready ? Color.white : new Color(0.45f, 0.42f, 0.5f));
                    gi.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(BarW * ChapterGifts.At[g], 4f), new Vector2(ready ? 66f : 54f, ready ? 66f : 54f));
                    if (claimed)
                    {
                        var ok = UIKit.Img("Ok", gi.transform, ProcArt.Check, Color.white);
                        ok.rectTransform.At(new Vector2(16f, -14f), new Vector2(30f, 30f));
                    }
                    if (ready)
                    {
                        // a waiting chest can be opened right here
                        int cg = ch, gg = g;
                        gi.raycastTarget = true;
                        var gb = gi.gameObject.AddComponent<Button>();
                        gb.transition = Selectable.Transition.None;
                        gb.onClick.AddListener(() => { GM.ClaimChapterGift(cg, gg); GM.RefreshHome(); Fill(); });
                        var glow = UIKit.Img("Glow", bar.transform, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.4f, 0.6f));
                        glow.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(BarW * ChapterGifts.At[g], 4f), new Vector2(120f, 120f));
                        glow.transform.SetSiblingIndex(gi.transform.GetSiblingIndex());
                    }
                }
                var pctText = UIKit.Txt("Pct", rt, Mathf.RoundToInt(pct * 100f) + "%", 44, ink, TextAnchor.MiddleRight, false);
                pctText.fontStyle = FontStyle.Bold;
                pctText.rectTransform.Anchor(new Vector2(1f, 0f), new Vector2(1f, 0.5f)).At(new Vector2(-22f, 66f), new Vector2(150f, 60f));
            }
        }

        void OnCard(int ch)
        {
            if (ch > Story.CurrentChapter(GM.TasksDone)) { GM.Toast("Bu bölüm henüz kilitli. Önceki bölümü bitir!"); return; }
            Close();
            GM.ShowHomeChapter(ch);
        }
    }
}
