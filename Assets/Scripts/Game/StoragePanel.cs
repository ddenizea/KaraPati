using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>The storage box: items parked off the table. Tap one to put it back on the table.</summary>
    public class StoragePanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, grid;
        Text title;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.6f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.22f, 0.36f, 0.4f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(new Vector2(0f, -420f), new Vector2(1000f, 560f));

            var icon = UIKit.Img("Icon", panel, ProcArt.Crate, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -20f), new Vector2(90f, 90f));
            title = UIKit.Txt("Title", panel, "Depo", 46, UIKit.Cream, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(135f, -32f), new Vector2(600f, 64f));
            var hint = UIKit.Txt("Hint", panel, "Eşyayı masadan depo kutusunun üstüne sürükle. Geri almak için dokun.", 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft);
            hint.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(137f, -88f), new Vector2(800f, 36f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-20f, -20f), new Vector2(86f, 86f));

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -150f), new Vector2(930f, 380f));
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.25f);
        }

        public void Close() => gameObject.SetActive(false);

        void Fill()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            int slots = GM.StorageSlots;
            title.text = "Depo  " + GM.Storage.Count + "/" + slots;
            for (int i = 0; i < Levels.MaxStorage; i++)
            {
                int idx = i;
                bool open = i < slots;
                var cell = UIKit.Img("Slot", grid, ProcArt.Rounded, open ? new Color(0.14f, 0.24f, 0.28f) : new Color(0.1f, 0.15f, 0.18f, 0.6f), true);
                cell.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(5f + (i % 6) * 155f, -(i / 6) * 175f), new Vector2(145f, 160f));
                if (!open)
                {
                    var lk = UIKit.Img("Lock", cell.transform, ProcArt.Lock, new Color(1f, 1f, 1f, 0.5f));
                    lk.rectTransform.At(new Vector2(0f, 16f), new Vector2(64f, 64f));
                    int needLevel = i - 2; // slots = 3 + level
                    var t = UIKit.Txt("Lv", cell.transform, "Sv. " + needLevel, 24, new Color(1f, 1f, 1f, 0.5f));
                    t.rectTransform.At(new Vector2(0f, -50f), new Vector2(140f, 34f));
                    continue;
                }
                if (i >= GM.Storage.Count) continue;
                var d = GM.Storage[i];
                var ic = UIKit.Img("Item", cell.transform, ProcArt.Item(d.chain, d.level), Color.white);
                ic.rectTransform.At(new Vector2(0f, 10f), new Vector2(118f, 118f));
                var lv = UIKit.Txt("Lv", cell.transform, d.Name, 20, UIKit.Cream);
                lv.rectTransform.At(new Vector2(0f, -62f), new Vector2(145f, 30f));
                cell.raycastTarget = true;
                var b = cell.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() =>
                {
                    if (GM.TakeFromStorage(idx))
                    {
                        if (GM.Storage.Count == 0) Close();
                        else Fill();
                    }
                });
            }
        }
    }
}
