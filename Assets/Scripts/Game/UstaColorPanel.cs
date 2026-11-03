using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// "Usta'nın Rengini Seç": opens when a Master Cat is made, and again from the Master's info bar.
    /// Every colour can be picked, any time, for free; it only changes how the Master looks.
    /// </summary>
    public class UstaColorPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, grid;
        Text sub;
        KittenAgent usta;

        public bool IsOpen => gameObject.activeSelf;

        // black first: the Kara Pati family colour
        static readonly int[] Order = { KittenColors.Gece, KittenColors.Turuncu, KittenColors.Gri, KittenColors.Beyaz, KittenColors.SiyahBeyaz, KittenColors.UcRenkli };
        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);

        public static string ColorName(int col) => col == KittenColors.Gece ? "Siyah" : KittenColors.Name(col);

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
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the panel don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(940f, 1000f));

            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.6f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 130f);
            var title = UIKit.Txt("Title", head.transform, "Usta'nın Rengini Seç", 50, Color.white, TextAnchor.MiddleCenter, true);
            title.fontStyle = FontStyle.Bold;
            UIKit.Stretch(title.rectTransform, 30f, 120f, 10f, 10f);
            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            sub = UIKit.Txt("Sub", panel, "Rengi istediğin zaman ücretsiz değiştirebilirsin.", 28, new Color(0.4f, 0.28f, 0.2f), TextAnchor.MiddleCenter, false);
            sub.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -170f), new Vector2(860f, 50f));

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -210f), new Vector2(880f, 760f));
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = new Vector2(0f, -210f);
            gameObject.SetActive(false);
        }

        public void Open(KittenAgent master)
        {
            if (!master) return;
            usta = master;
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        void Fill()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            for (int i = 0; i < Order.Length; i++)
            {
                int col = Order[i];
                bool current = usta && usta.color == col;
                int cx = i % 3, cy = i / 3;
                var card = UIKit.Img("C" + col, grid, ProcArt.Rounded, current ? new Color(0.78f, 0.93f, 0.7f) : new Color(1f, 0.96f, 0.88f), true);
                var rt = card.rectTransform;
                rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((cx - 1) * 290f, -cy * 370f), new Vector2(270f, 350f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2((cx - 1) * 290f, -cy * 370f);
                var cat = UIKit.Img("Cat", rt, ProcArt.SideCat(3, 4, col), Color.white);
                cat.raycastTarget = false;
                cat.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -135f), new Vector2(220f, 220f));
                var name = UIKit.Txt("Name", rt, ColorName(col), 34, Ink, TextAnchor.MiddleCenter, false);
                name.fontStyle = FontStyle.Bold;
                name.raycastTarget = false;
                name.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, 70f), new Vector2(260f, 50f));
                if (current)
                {
                    var tick = UIKit.Img("Tick", rt, ProcArt.Check, Color.white);
                    tick.raycastTarget = false;
                    tick.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-34f, -34f), new Vector2(54f, 54f));
                }
                card.raycastTarget = true;
                var b = card.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Pick(col));
            }
        }

        void Pick(int col)
        {
            if (!usta) { Close(); return; }
            Close();
            if (usta.color == col) return;
            GM.Cats.SetMasterColor(usta, col);
        }
    }
}
