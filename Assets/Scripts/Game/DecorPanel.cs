using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Decoration sheet that slides up from the bottom while the scene stays visible above it.
    /// Tap a bought ornament to place it; tap a new one twice to buy it.
    /// </summary>
    public class DecorPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform sheet, rows;
        Text title, goldText;
        int chapter;
        int armedSlot = -1, armedVariant = -1;
        float armedUntil;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            // light dim over the scene, tap it to close
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.15f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Sheet", root, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.98f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            sheet = bg.rectTransform;
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.sizeDelta = new Vector2(-20f, 860f);
            sheet.anchoredPosition = new Vector2(0f, 10f);

            var icon = UIKit.Img("Icon", sheet, ProcArt.Brush, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(26f, -18f), new Vector2(84f, 84f));
            title = UIKit.Txt("Title", sheet, "", 40, UIKit.Orange, TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(124f, -16f), new Vector2(600f, 50f));
            var sub = UIKit.Txt("Sub", sheet, "Alınan süsleri istediğin zaman ücretsiz değiştirebilirsin.", 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft, false);
            sub.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(126f, -66f), new Vector2(640f, 34f));

            var coin = UIKit.Img("Coin", sheet, ProcArt.Coin, Color.white);
            coin.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-300f, -52f), new Vector2(50f, 50f));
            goldText = UIKit.Txt("Gold", sheet, "", 34, UIKit.Cream, TextAnchor.MiddleLeft, false);
            goldText.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-190f, -52f), new Vector2(160f, 50f));

            var close = UIKit.Btn("Close", sheet, "X", new Color(0.8f, 0.3f, 0.35f), Close, 40);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-18f, -18f), new Vector2(84f, 84f));

            rows = UIKit.Rect("Rows", sheet);
            UIKit.Stretch(rows, 20f, 20f, 116f, 16f);
            gameObject.SetActive(false);
        }

        public void Open(int ch)
        {
            Sfx.Play("page");
            chapter = ch;
            armedSlot = -1;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            // slide up
            var s = sheet;
            Tween.Run(0.3f, t => { if (s) s.anchoredPosition = new Vector2(0f, Mathf.Lerp(-860f, 10f, Tween.OutCubic(t))); });
        }

        public void Close() => gameObject.SetActive(false);

        void Update()
        {
            // an unconfirmed purchase expires after a few seconds
            if (armedSlot >= 0 && Time.unscaledTime > armedUntil) { armedSlot = -1; Fill(); }
        }

        void Fill()
        {
            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
            title.text = "Dekorasyon  •  " + (chapter + 1) + ". Bölüm";
            goldText.text = GM.Gold.ToString();

            for (int slot = 0; slot < Decor.Slots; slot++)
            {
                var row = UIKit.Rect("Row", rows);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -slot * 182f);
                row.sizeDelta = new Vector2(0f, 172f);

                var label = UIKit.Txt("Label", row, Decor.SlotNames[slot], 30, UIKit.Cream, TextAnchor.MiddleLeft, false);
                label.fontStyle = FontStyle.Bold;
                label.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(12f, 10f), new Vector2(200f, 60f));

                int selected = GM.DecorSelected(chapter, slot);
                for (int v = 0; v < Decor.Variants; v++)
                    Tile(row, slot, v, 220f + v * 180f, selected == v);
            }
        }

        void Tile(RectTransform row, int slot, int v, float x, bool selected)
        {
            bool owned = v == 0 || GM.DecorOwned(chapter, slot, v);
            int price = Decor.Price(chapter, slot, v);
            bool armed = armedSlot == slot && armedVariant == v;

            var bg = UIKit.Img("Tile", row, ProcArt.Rounded,
                armed ? UIKit.Orange : selected ? new Color(0.45f, 0.32f, 0.22f) : new Color(0.14f, 0.09f, 0.2f, 0.95f), true);
            var rt = bg.rectTransform;
            rt.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(x, 0f), new Vector2(164f, 164f));
            bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            int s0 = slot, v0 = v;
            b.onClick.AddListener(() => OnTile(s0, v0));

            if (selected)
            {
                var fr = UIKit.Img("Frame", rt, ProcArt.Frame, UIKit.Orange, true);
                fr.rectTransform.sizeDelta = new Vector2(176f, 176f);
            }
            var ic = UIKit.Img("Icon", rt, DecorArt.Icon(slot, v), v == 0 ? new Color(1f, 1f, 1f, 0.35f) : DecorArt.IconTint(slot, v));
            ic.rectTransform.At(new Vector2(0f, 16f), new Vector2(v == 0 ? 64f : 100f, v == 0 ? 64f : 100f));
            ic.preserveAspect = true;

            string caption = armed ? "Emin misin?" : selected ? "Yerleşti" : owned ? (v == 0 ? "Yok" : "Senin") : price + " altın";
            var cap = UIKit.Txt("Cap", rt, caption, 24, armed ? Color.white : owned ? UIKit.Cream : (GM.Gold >= price ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.55f, 0.5f)), TextAnchor.MiddleCenter, false);
            cap.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 6f), new Vector2(160f, 34f));
            var name = UIKit.Txt("Name", rt, Decor.VariantNames[slot][v], 20, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, false);
            name.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -4f), new Vector2(160f, 26f));
        }

        void OnTile(int slot, int v)
        {
            bool owned = v == 0 || GM.DecorOwned(chapter, slot, v);
            if (owned)
            {
                armedSlot = -1;
                GM.SelectDecor(chapter, slot, v);
                Fill();
                return;
            }
            int price = Decor.Price(chapter, slot, v);
            if (armedSlot == slot && armedVariant == v && Time.unscaledTime <= armedUntil)
            {
                armedSlot = -1;
                if (GM.BuyDecor(chapter, slot, v)) Sfx.Play("gift");
                Fill();
                return;
            }
            if (GM.Gold < price) { GM.Toast(Decor.VariantNames[slot][v] + " için " + price + " altın gerekiyor."); Sfx.Play("error"); return; }
            armedSlot = slot;
            armedVariant = v;
            armedUntil = Time.unscaledTime + 3f;
            Fill();
        }
    }
}
