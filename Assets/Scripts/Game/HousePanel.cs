using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// The cat house window: the two places inside, each kitten with "Sahiplendir" (send it to a family, so a
    /// different colour can come) and "Eğit" (to the Master, becomes a Mini Apprentice), and L.T("Çırak Yap") at the bottom.
    /// </summary>
    public class HousePanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, slots;
        Button makeBtn;
        Text hint;
        Vector2Int house;

        public bool IsOpen => gameObject.activeSelf;

        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);
        static readonly Color Dim = new Color(0.32f, 0.2f, 0.12f, 0.6f);

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
            panel.At(Vector2.zero, new Vector2(940f, 900f));

            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.62f, 0.3f, 0.36f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 130f);
            var icon = UIKit.Img("Icon", head.transform, ProcArt.Item(Chains.KediEvi, 1), Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(24f, 0f), new Vector2(110f, 110f));
            var title = UIKit.Txt("Title", head.transform, L.T("Kedi Evi"), 56, Color.white, TextAnchor.MiddleCenter, true);
            title.fontStyle = FontStyle.Bold;
            UIKit.Stretch(title.rectTransform, 140f, 140f, 10f, 10f);
            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            slots = UIKit.Rect("Slots", panel);
            slots.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -150f), new Vector2(880f, 520f));
            slots.pivot = new Vector2(0.5f, 1f);
            slots.anchoredPosition = new Vector2(0f, -150f);

            hint = UIKit.Txt("Hint", panel, "", 28, Dim, TextAnchor.MiddleCenter, false);
            hint.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 150f), new Vector2(860f, 70f));

            makeBtn = UIKit.Btn("Make", panel, L.T("Çırak Yap"), UIKit.Green, () =>
            {
                GM.Cats.MakeApprenticeFromHouse(house);
                Close();
            }, 40);
            ((RectTransform)makeBtn.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 30f), new Vector2(420f, 100f));
            gameObject.SetActive(false);
        }

        public void Open(Vector2Int hc)
        {
            house = hc;
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        void Fill()
        {
            for (int i = slots.childCount - 1; i >= 0; i--) Destroy(slots.GetChild(i).gameObject);
            var h = GM.Board.Get(house);
            if (h == null || !Chains.IsHouse(h.chain)) { Close(); return; }
            string cols = h.catCols ?? "";
            string trainWhy = GM.Cats.TrainBlock();

            for (int i = 0; i < KittenChaos.HouseSlots; i++)
            {
                int idx = i;
                var card = UIKit.Img("Slot" + i, slots, ProcArt.Rounded, i < cols.Length ? new Color(1f, 0.96f, 0.88f) : new Color(0.86f, 0.79f, 0.66f), true);
                var rt = card.rectTransform;
                rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((i - 0.5f) * 440f, 0f), new Vector2(410f, 510f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2((i - 0.5f) * 440f, 0f);

                if (i >= cols.Length)
                {
                    var empty = UIKit.Txt("Empty", rt, L.T("Boş\n\n<size=26>Masadaki bir yavruyu\nKedi Evi'ne sürükle.</size>"), 40, Dim, TextAnchor.MiddleCenter, false);
                    empty.fontStyle = FontStyle.Bold;
                    UIKit.Stretch(empty.rectTransform, 20f, 20f, 20f, 20f);
                    continue;
                }

                int col = Mathf.Clamp(cols[i] - '0', 0, KittenColors.Count - 1);
                var cat = UIKit.Img("Cat", rt, ProcArt.SideCat(1, 4, col), Color.white);
                cat.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -120f), new Vector2(190f, 190f));
                var name = UIKit.Txt("Name", rt, KittenColors.Name(col) + " Yavru", 32, Ink, TextAnchor.MiddleCenter, false);
                name.fontStyle = FontStyle.Bold;
                name.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -245f), new Vector2(390f, 46f));

                var adopt = UIKit.Btn("Adopt", rt, L.T("Sahiplendir  +3"), UIKit.Orange, () =>
                {
                    GM.Cats.AdoptFromHouse(house, idx);
                    Fill();
                }, 30);
                ((RectTransform)adopt.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 120f), new Vector2(350f, 84f));

                var train = UIKit.Btn("Train", rt, L.T("Usta'ya Eğit"), trainWhy == null ? new Color(0.42f, 0.28f, 0.6f) : UIKit.Disabled, () =>
                {
                    var why = GM.Cats.TrainBlock();
                    if (why != null) { GM.Toast(why); return; }
                    GM.Cats.TrainFromHouse(house, idx);
                    Close();
                }, 30);
                ((RectTransform)train.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 24f), new Vector2(350f, 84f));
            }

            bool full = cols.Length >= KittenChaos.HouseSlots;
            bool canMake = full && !GM.Cats.HasApprentice;
            makeBtn.GetComponent<Image>().color = canMake ? UIKit.Green : UIKit.Disabled;
            makeBtn.interactable = canMake;
            hint.text = !full ? L.T("İki yavru toplanınca birleşip Çırak Kedi olurlar.")
                : GM.Cats.HasApprentice ? L.T("Masada zaten bir Çırak var. O gidince buradan yenisini çıkarırsın.")
                : L.T("İki yavru hazır! Çırak'ın rengi ikisinden biri olur.");
        }
    }
}
