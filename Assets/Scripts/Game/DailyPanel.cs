using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>7-day login calendar popup.</summary>
    public class DailyPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, grid;
        Text sub;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = Theme.Ui(new Color(0.03f, 0.01f, 0.06f, 0.7f));
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 860f));

            var icon = UIKit.Img("Icon", panel, ProcArt.Calendar, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(100f, -90f), new Vector2(120f, 120f));
            var title = UIKit.Txt("Title", panel, L.T("Günlük Ödül"), 54, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -40f), new Vector2(600f, 70f));
            sub = UIKit.Txt("Sub", panel, "", 28, new Color(1f, 1f, 1f, 0.75f));
            sub.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -112f), new Vector2(700f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -190f), new Vector2(920f, 860f));
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

        public void Close()
        {
            gameObject.SetActive(false);
            GM.OnDailyClosed();
        }

        void Fill()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            bool can = GM.DailyAvailable;
            int today = GM.DailyIndex;
            // after claiming, show the claimed day as done; a full loop restarts from day 1
            if (!can && today == 0) today = DailyRewards.Days.Length;
            sub.text = can ? L.T("Bugünün ödülü hazır! Her gün gel, 7. gün büyük ödül.") : L.T("Bugünkü ödülü aldın. Yarın yine bekleriz!");

            var days = DailyRewards.Days;
            for (int i = 0; i < days.Length; i++)
            {
                var m = days[i];
                bool big = i == days.Length - 1;
                bool past = i < today;
                bool current = i == today;
                // 4 cards on top, 3 below; day 7 is wider
                Vector2 pos, size;
                if (i < 4) { pos = new Vector2(-345f + i * 230f, 0f); size = new Vector2(214f, 300f); }
                else if (!big) { pos = new Vector2(-345f + (i - 4) * 230f, -330f); size = new Vector2(214f, 300f); }
                else { pos = new Vector2(230f, -330f); size = new Vector2(444f, 300f); }

                var card = UIKit.Img("Day" + i, grid, ProcArt.Rounded,
                    current ? (can ? UIKit.Orange : new Color(0.5f, 0.38f, 0.3f)) : past ? new Color(0.2f, 0.14f, 0.28f, 0.7f) : new Color(0.32f, 0.22f, 0.44f), true);
                var rt = card.rectTransform;
                rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(pos, size);

                var head = UIKit.Txt("Day", rt, L.F("{0}. gün", i + 1), 30, current ? Color.white : UIKit.Cream);
                head.fontStyle = FontStyle.Bold;
                head.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -14f), new Vector2(size.x, 44f));

                var ic = UIKit.Img("Icon", rt, Icon(m), past ? new Color(1f, 1f, 1f, 0.4f) : Color.white);
                ic.rectTransform.At(new Vector2(big ? -90f : 0f, 10f), new Vector2(big ? 130f : 110f, big ? 130f : 110f));
                if (big)
                {
                    var st = UIKit.Img("Sticker", rt, ProcArt.AlbumIcon, Color.white);
                    st.rectTransform.At(new Vector2(70f, 10f), new Vector2(120f, 120f));
                }

                var t = UIKit.Txt("Text", rt, DailyRewards.Text(m), 26, past ? new Color(1f, 1f, 1f, 0.5f) : Color.white);
                t.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 64f), new Vector2(size.x - 16f, 40f));

                if (past)
                {
                    var ok = UIKit.Img("Done", rt, ProcArt.Check, Color.white);
                    ok.rectTransform.At(new Vector2(0f, 10f), new Vector2(80f, 80f));
                }
                else if (current && can)
                {
                    var b = UIKit.Btn("Claim", rt, L.T("Al!"), UIKit.Green, Claim, 34);
                    ((RectTransform)b.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 10f), new Vector2(Mathf.Min(180f, size.x - 30f), 56f));
                    Tween.Punch(rt, 0.06f, 0.5f);
                }
            }
        }

        void Claim()
        {
            if (GM.ClaimDaily()) Fill();
        }

        static Sprite Icon(EventMilestone m)
        {
            switch (m.type)
            {
                case "energy": return ProcArt.Bolt;
                case "stamps": return ProcArt.Stamp;
                default: return ProcArt.Coin;
            }
        }
    }
}
