using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Tower decoration sheet that slides up from the bottom. Two tabs:
    /// "Envanter" (ornaments you own - tap to place on the floor in view) and "Dükkân" (buy ornaments, tap twice).
    /// </summary>
    public class DecorPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform sheet, rows;
        Text title, goldText;
        Button tabInv, tabShop, prevFloor, nextFloor;
        bool shopTab;
        int floor;
        int armedSlot = -1, armedVariant = -1;
        float armedUntil;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.2f);
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
            sheet.sizeDelta = new Vector2(-20f, 980f);
            sheet.anchoredPosition = new Vector2(0f, 10f);

            var icon = UIKit.Img("Icon", sheet, ProcArt.Brush, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(26f, -18f), new Vector2(84f, 84f));
            // floor picker: < 1. Kat > decides where a placed ornament goes
            prevFloor = UIKit.Btn("PrevFloor", sheet, "<", UIKit.PanelDark, () => ChangeFloor(-1), 36);
            ((RectTransform)prevFloor.transform).Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(122f, -14f), new Vector2(60f, 56f));
            title = UIKit.Txt("Title", sheet, "", 34, UIKit.Orange, TextAnchor.MiddleCenter, false);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;   // long floor names shrink instead of wrapping
            title.resizeTextMinSize = 20;
            title.resizeTextMaxSize = 34;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(190f, -16f), new Vector2(440f, 50f));
            nextFloor = UIKit.Btn("NextFloor", sheet, ">", UIKit.PanelDark, () => ChangeFloor(1), 36);
            ((RectTransform)nextFloor.transform).Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(638f, -14f), new Vector2(60f, 56f));
            var sub = UIKit.Txt("Sub", sheet, "Oklarla katı seç, envanterden bir süse dokun.", 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft, false);
            sub.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(126f, -66f), new Vector2(640f, 34f));

            var coin = UIKit.Img("Coin", sheet, ProcArt.Coin, Color.white);
            coin.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-300f, -52f), new Vector2(50f, 50f));
            goldText = UIKit.Txt("Gold", sheet, "", 34, UIKit.Cream, TextAnchor.MiddleLeft, false);
            goldText.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-190f, -52f), new Vector2(160f, 50f));

            var close = UIKit.Btn("Close", sheet, "X", new Color(0.8f, 0.3f, 0.35f), Close, 40);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-18f, -18f), new Vector2(84f, 84f));

            tabInv = UIKit.Btn("TabInv", sheet, "Envanter", UIKit.PanelDark, () => SetTab(false), 32);
            ((RectTransform)tabInv.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(-180f, -116f), new Vector2(340f, 76f));
            tabShop = UIKit.Btn("TabShop", sheet, "Dükkân", UIKit.PanelDark, () => SetTab(true), 32);
            ((RectTransform)tabShop.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(180f, -116f), new Vector2(340f, 76f));

            rows = UIKit.Rect("Rows", sheet);
            UIKit.Stretch(rows, 20f, 20f, 210f, 16f);
            gameObject.SetActive(false);
        }

        public void Open(int targetFloor)
        {
            Sfx.Play("page");
            floor = targetFloor;
            armedSlot = -1;
            // start on the shop if there is nothing to place yet
            shopTab = !HasInventory();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            var s = sheet;
            Tween.Run(0.3f, t => { if (s) s.anchoredPosition = new Vector2(0f, Mathf.Lerp(-980f, 10f, Tween.OutCubic(t))); });
        }

        public void Close() => gameObject.SetActive(false);

        bool HasInventory()
        {
            for (int s = 0; s < Decor.Slots; s++)
                for (int v = 1; v < Decor.Variants; v++)
                    if (GM.InventoryCount(Decor.ItemId(s, v)) > 0) return true;
            return false;
        }

        void ChangeFloor(int dir)
        {
            int open = GM.TowerFloorsOpen;
            if (open <= 1) { GM.Toast("Başka kat açmak için kulede kilitli kata dokun."); return; }
            floor = (floor + dir + open) % open;   // wraps around: after the top floor comes floor 1
            Sfx.Play("page");
            Fill();
        }

        void SetTab(bool shop)
        {
            shopTab = shop;
            armedSlot = -1;
            Fill();
        }

        void Update()
        {
            if (armedSlot >= 0 && Time.unscaledTime > armedUntil) { armedSlot = -1; Fill(); }
        }

        void Fill()
        {
            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
            title.text = (floor + 1) + ". Kat  •  " + Tower.FloorNames[floor];
            bool many = GM.TowerFloorsOpen > 1;
            prevFloor.gameObject.SetActive(many);
            nextFloor.gameObject.SetActive(many);
            goldText.text = GM.Gold.ToString();
            tabInv.GetComponent<Image>().color = shopTab ? UIKit.PanelDark : UIKit.Orange;
            tabShop.GetComponent<Image>().color = shopTab ? UIKit.Orange : UIKit.PanelDark;

            for (int slot = 0; slot < Decor.Slots; slot++)
            {
                var row = UIKit.Rect("Row", rows);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -slot * 186f);
                row.sizeDelta = new Vector2(0f, 176f);

                var label = UIKit.Txt("Label", row, Decor.SlotNames[slot], 30, UIKit.Cream, TextAnchor.MiddleLeft, false);
                label.fontStyle = FontStyle.Bold;
                label.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(12f, 10f), new Vector2(200f, 60f));

                for (int v = 1; v < Decor.Variants; v++)
                    Tile(row, slot, v, 230f + (v - 1) * 230f);
            }
        }

        void Tile(RectTransform row, int slot, int v, float x)
        {
            string id = Decor.ItemId(slot, v);
            int have = GM.InventoryCount(id);
            int price = Decor.TowerPrice(slot, v);
            bool armed = armedSlot == slot && armedVariant == v;
            bool dim = !shopTab && have == 0;

            var bg = UIKit.Img("Tile", row, ProcArt.Rounded, armed ? UIKit.Orange : new Color(0.14f, 0.09f, 0.2f, 0.95f), true);
            var rt = bg.rectTransform;
            rt.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(x, 0f), new Vector2(210f, 168f));
            bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            int s0 = slot, v0 = v;
            b.onClick.AddListener(() => OnTile(s0, v0));

            var tint = DecorArt.IconTint(slot, v);
            if (dim) tint = new Color(tint.r, tint.g, tint.b, 0.3f);
            var ic = UIKit.Img("Icon", rt, DecorArt.Icon(slot, v), tint);
            ic.rectTransform.At(new Vector2(0f, 14f), new Vector2(96f, 96f));
            ic.preserveAspect = true;

            var name = UIKit.Txt("Name", rt, Decor.VariantNames[slot][v], 22, new Color(1f, 1f, 1f, dim ? 0.35f : 0.75f), TextAnchor.MiddleCenter, false);
            name.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -4f), new Vector2(200f, 28f));

            string caption;
            Color capColor;
            if (shopTab)
            {
                caption = armed ? "Emin misin?" : price + " altın";
                capColor = armed ? Color.white : GM.Gold >= price ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.55f, 0.5f);
            }
            else
            {
                caption = have > 0 ? "x" + have + "  Yerleştir" : "Yok";
                capColor = have > 0 ? UIKit.Cream : new Color(1f, 1f, 1f, 0.35f);
            }
            var cap = UIKit.Txt("Cap", rt, caption, 24, capColor, TextAnchor.MiddleCenter, false);
            cap.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 6f), new Vector2(200f, 34f));

            // in the shop, show how many you already have in a corner badge
            if (shopTab && have > 0)
            {
                var badge = UIKit.Img("Have", rt, ProcArt.Circle, UIKit.Green);
                badge.rectTransform.At(new Vector2(80f, 50f), new Vector2(44f, 44f));
                var bt = UIKit.Txt("N", badge.transform, have.ToString(), 24, Color.white, TextAnchor.MiddleCenter, false);
                UIKit.Stretch(bt.rectTransform);
            }
        }

        void OnTile(int slot, int v)
        {
            string id = Decor.ItemId(slot, v);
            if (!shopTab)
            {
                if (GM.InventoryCount(id) <= 0) { GM.Toast("Bu süsten yok. Dükkân sekmesinden alabilirsin."); return; }
                Close();
                GM.TowerUI.PlaceFromInventory(id, floor);
                return;
            }
            int price = Decor.TowerPrice(slot, v);
            if (armedSlot == slot && armedVariant == v && Time.unscaledTime <= armedUntil)
            {
                armedSlot = -1;
                if (GM.BuyTowerDecor(slot, v)) Sfx.Play("gift");
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
