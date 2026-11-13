using System;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// L.T("Usta Kedim"): opened from the round button on the home screen. Name the Master Cat and pick its colour
    /// (free, any time; it only changes the look). Also shows where the Master is right now.
    /// </summary>
    public class UstaColorPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, grid;
        Image preview;
        Text status;
        InputField nameField;

        public bool IsOpen => gameObject.activeSelf;

        // black first: the Kara Pati family colour
        static readonly int[] Order = { KittenColors.Gece, KittenColors.Turuncu, KittenColors.Gri, KittenColors.Beyaz, KittenColors.SiyahBeyaz, KittenColors.UcRenkli };
        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);
        public const int NameLimit = 12;

        public static string ColorName(int col) => col == KittenColors.Gece ? L.T("Siyah") : KittenColors.Name(col);

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = Theme.Ui(new Color(0.03f, 0.01f, 0.06f, 0.7f));
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.93f, 0.86f, 0.72f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the panel don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(940f, 1330f));

            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.6f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 120f);
            var title = UIKit.Txt("Title", head.transform, L.T("Usta Kedim"), 52, Color.white, TextAnchor.MiddleCenter, true);
            title.fontStyle = FontStyle.Bold;
            UIKit.Stretch(title.rectTransform, 30f, 120f, 10f, 10f);
            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            // big picture of the Master
            var stage = UIKit.Img("Stage", panel, ProcArt.Circle, new Color(1f, 0.96f, 0.88f));
            stage.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(200f, -290f), new Vector2(300f, 300f));
            preview = UIKit.Img("Usta", stage.transform, ProcArt.SideCat(3, 4), Color.white);
            preview.rectTransform.sizeDelta = new Vector2(270f, 270f);

            // name
            var nameLbl = UIKit.Txt("NameLbl", panel, L.T("Adı"), 30, new Color(0.45f, 0.32f, 0.22f), TextAnchor.MiddleLeft, false);
            nameLbl.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 0.5f)).At(new Vector2(390f, -180f), new Vector2(480f, 40f));
            nameField = BuildInput(panel);
            ((RectTransform)nameField.transform).Anchor(new Vector2(0f, 1f), new Vector2(0f, 0.5f)).At(new Vector2(390f, -240f), new Vector2(500f, 84f));
            nameField.onEndEdit.AddListener(v => GM.SetUstaName(v));

            status = UIKit.Txt("Status", panel, "", 26, new Color(0.4f, 0.28f, 0.2f), TextAnchor.UpperLeft, false);
            status.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(390f, -300f), new Vector2(520f, 250f));
            status.rectTransform.pivot = new Vector2(0f, 1f);
            status.rectTransform.anchoredPosition = new Vector2(390f, -300f);

            var colLbl = UIKit.Txt("ColLbl", panel, L.T("Rengi  <size=24>(istediğin zaman ücretsiz değiştirebilirsin)</size>"), 30, new Color(0.45f, 0.32f, 0.22f), TextAnchor.MiddleLeft, false);
            colLbl.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 0.5f)).At(new Vector2(40f, -575f), new Vector2(860f, 46f));

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -615f), new Vector2(880f, 690f));
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = new Vector2(0f, -615f);
            gameObject.SetActive(false);
        }

        static InputField BuildInput(Transform parent)
        {
            var bg = UIKit.Img("Name", parent, ProcArt.Rounded, Color.white, true);
            bg.raycastTarget = true;
            var field = bg.gameObject.AddComponent<InputField>();
            var txt = UIKit.Txt("Text", bg.transform, "", 38, Ink, TextAnchor.MiddleLeft, false);
            txt.supportRichText = false;
            UIKit.Stretch(txt.rectTransform, 22f, 22f, 6f, 6f);
            var ph = UIKit.Txt("Placeholder", bg.transform, L.T("Bir isim yaz..."), 34, new Color(0.32f, 0.2f, 0.12f, 0.4f), TextAnchor.MiddleLeft, false);
            ph.fontStyle = FontStyle.Italic;
            UIKit.Stretch(ph.rectTransform, 22f, 22f, 6f, 6f);
            field.textComponent = txt;
            field.placeholder = ph;
            field.characterLimit = NameLimit;
            field.lineType = InputField.LineType.SingleLine;
            field.targetGraphic = bg;
            return field;
        }

        public void Open()
        {
            if (!GM.Cats.HasAnyMaster)
            {
                GM.Toast(L.T("Bir Çırak'ı Usta yapınca burada seni bekleyecek."));
                return;
            }
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            nameField.SetTextWithoutNotify(GM.UstaName ?? "");
            Fill();
            Tween.Pop(panel, 0.3f);
            GM.SeenUsta();
        }

        public void Close()
        {
            if (nameField && nameField.isFocused) GM.SetUstaName(nameField.text);
            gameObject.SetActive(false);
        }

        void Update()
        {
            // the trip timer ticks while the window is open
            if (IsOpen && Time.frameCount % 30 == 0) RefreshStatus();
        }

        void RefreshStatus()
        {
            var cats = GM.Cats;
            int col = cats.MasterColor;
            string who = GM.UstaDisplayName(col);
            int lv = GM.UstaLevel;
            string stars = "<color=#E8A33A>" + new string('★', lv) + "</color><color=#C9B8A8>" + new string('★', UstaLevels.Max - lv) + "</color>";
            string levelLine = L.F("Seviye {0}  {1}", lv, stars) + "\n" + (lv < UstaLevels.Max
                ? L.F("Deneyim {0}/{1} · Sonraki: {2}", GM.UstaXp - UstaLevels.XpFor(lv), UstaLevels.XpFor(lv + 1) - UstaLevels.XpFor(lv), UstaLevels.Perk(lv + 1))
                : L.T("En üst seviye! Usta'n her şeyi biliyor."));
            if (cats.UstaAway)
            {
                if (cats.TripReady) status.text = levelLine + "\n\n" + L.F("{0} keşiften döndü!\nKedi Evi'nde sandığıyla seni bekliyor.", who);
                else
                {
                    var left = TimeSpan.FromTicks(Math.Max(0L, cats.TripUntil - DateTime.UtcNow.Ticks));
                    string t = left.TotalHours >= 1.0 ? L.F("{0} sa {1} dk", (int)left.TotalHours, left.Minutes) : L.F("{0} dk {1} sn", left.Minutes, left.Seconds);
                    status.text = levelLine + "\n\n" + L.F("{0} keşifte.\nDönüşüne {1} var.", who, t);
                }
            }
            else status.text = levelLine + "\n\n" + L.F("{0} dükkânda çalışıyor.\nAkşamları kulede minderinde uyur.", who);
        }

        void Fill()
        {
            int curCol = GM.Cats.MasterColor;
            preview.sprite = ProcArt.SideCat(3, 4, curCol);
            RefreshStatus();
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            for (int i = 0; i < Order.Length; i++)
            {
                int col = Order[i];
                bool current = curCol == col;
                int cx = i % 3, cy = i / 3;
                var card = UIKit.Img("C" + col, grid, ProcArt.Rounded, current ? new Color(0.78f, 0.93f, 0.7f) : new Color(1f, 0.96f, 0.88f), true);
                var rt = card.rectTransform;
                rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((cx - 1) * 290f, -cy * 340f), new Vector2(270f, 320f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2((cx - 1) * 290f, -cy * 340f);
                var cat = UIKit.Img("Cat", rt, ProcArt.SideCat(3, 4, col), Color.white);
                cat.raycastTarget = false;
                cat.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -125f), new Vector2(200f, 200f));
                var name = UIKit.Txt("Name", rt, ColorName(col), 32, Ink, TextAnchor.MiddleCenter, false);
                name.fontStyle = FontStyle.Bold;
                name.raycastTarget = false;
                name.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, 56f), new Vector2(260f, 50f));
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
            if (GM.Cats.MasterColor == col) return;
            GM.Cats.SetMasterColor(col);
            Tween.Pop(preview.rectTransform, 0.3f);
            Fill();
        }
    }
}
