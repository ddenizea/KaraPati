using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Sticker album. Overview: a grand prize for completing every set plus one card per set with its progress.
    /// Tap a card to open the set's stickers.
    /// </summary>
    public class AlbumPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        RectTransform panel, overview, detail, grid;
        Text detailTitle, progress, grandText;
        RectTransform grandFill;
        Button claim, grandClaim;
        Image claimImg, grandImg;
        int setIndex;
        const float GrandBarW = 620f;

        static readonly Color[] setColors =
        {
            new Color(0.45f, 0.62f, 0.95f), new Color(0.95f, 0.6f, 0.3f), new Color(0.65f, 0.4f, 0.9f), new Color(0.35f, 0.75f, 0.55f)
        };

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = Theme.Ui(new Color(0.03f, 0.01f, 0.06f, 0.75f));
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1600f));

            var moon = UIKit.Img("Moon", panel, ProcArt.Moon, new Color(1f, 1f, 1f, 0.25f));
            moon.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-170f, -90f), new Vector2(170f, 170f));

            var title = UIKit.Txt("Title", panel, L.T("Etiket Albümü"), 54, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -30f), new Vector2(700f, 74f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            BuildOverview();
            BuildDetail();
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ overview
        void BuildOverview()
        {
            overview = UIKit.Rect("Overview", panel);
            UIKit.Stretch(overview, 0f, 0f, 120f, 0f);

            // grand prize scroll
            var scroll = UIKit.Img("Scroll", overview, ProcArt.Rounded, new Color(0.95f, 0.9f, 0.78f), true);
            scroll.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -10f), new Vector2(900f, 440f));
            var ink = new Color(0.45f, 0.25f, 0.15f);
            var head = UIKit.Txt("Head", scroll.transform, L.T("Tüm setleri tamamla, büyük ödülü kap:"), 34, ink, TextAnchor.MiddleCenter, false);
            head.fontStyle = FontStyle.Bold;
            head.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -24f), new Vector2(860f, 50f));

            Reward(scroll.transform, ProcArt.Coin, Stickers.GrandGold.ToString(), new Vector2(-250f, -150f));
            Reward(scroll.transform, ProcArt.Stamp, Stickers.GrandStamps.ToString(), new Vector2(0f, -150f));
            Reward(scroll.transform, ProcArt.Bolt, Stickers.GrandEnergy.ToString(), new Vector2(250f, -150f));

            var barBg = UIKit.Img("Bar", scroll.transform, ProcArt.Rounded, new Color(0.35f, 0.28f, 0.42f), true);
            barBg.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -262f), new Vector2(GrandBarW + 12f, 50f));
            var fill = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, UIKit.Green, true);
            grandFill = fill.rectTransform;
            grandFill.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(6f, 0f), new Vector2(40f, 38f));
            grandText = UIKit.Txt("Count", barBg.transform, "0/20", 32, Color.white);
            UIKit.Stretch(grandText.rectTransform);
            var album = UIKit.Img("AlbumIcon", barBg.transform, ProcArt.AlbumIcon, Color.white);
            album.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(-10f, 0f), new Vector2(84f, 84f));

            grandClaim = UIKit.Btn("GrandClaim", scroll.transform, "", UIKit.Disabled, ClaimGrand, 32);
            grandImg = grandClaim.GetComponent<Image>();
            ((RectTransform)grandClaim.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 22f), new Vector2(520f, 80f));
        }

        static void Reward(Transform parent, Sprite icon, string amount, Vector2 pos)
        {
            var img = UIKit.Img("Reward", parent, icon, Color.white);
            img.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(pos, new Vector2(110f, 110f));
            var t = UIKit.Txt("N", img.transform, amount, 36, Color.white);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.At(new Vector2(30f, -40f), new Vector2(120f, 46f));
        }

        void FillOverview()
        {
            for (int i = overview.childCount - 1; i >= 1; i--) Destroy(overview.GetChild(i).gameObject);

            int have = 0, total = 0;
            foreach (var s in Stickers.Sets) { have += GM.StickerCount(s); total += s.ids.Length; }
            grandText.text = have + "/" + total;
            grandFill.sizeDelta = new Vector2(Mathf.Max(40f, GrandBarW * have / total), 38f);
            if (GM.IsGrandClaimed)
            {
                grandClaim.Label().text = L.T("Büyük ödül alındı");
                grandImg.color = Theme.Ui(UIKit.Disabled);
                grandClaim.interactable = false;
            }
            else
            {
                bool ok = have >= total;
                grandClaim.Label().text = ok ? L.T("Büyük Ödülü Al!") : L.T("Bütün etiketleri topla");
                grandImg.color = Theme.Ui(ok ? UIKit.Orange : UIKit.Disabled);
                grandClaim.interactable = ok;
            }

            // cards side by side; narrower when there are more sets (4 sets: 220 wide instead of 280)
            int setCount = Stickers.Sets.Length;
            float cardW = setCount > 3 ? 220f : 280f, inner = cardW - 24f;
            for (int i = 0; i < setCount; i++)
            {
                int idx = i;
                var set = Stickers.Sets[i];
                var card = UIKit.Img("Set" + i, overview, ProcArt.Rounded, new Color(1f, 0.93f, 0.85f), true);
                card.raycastTarget = true;
                var rt = card.rectTransform;
                rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((i - (setCount - 1) / 2f) * (cardW + 18f), -490f), new Vector2(cardW, 420f));
                var btn = card.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => ShowSet(idx));

                var strip = UIKit.Img("Strip", rt, ProcArt.Rounded, setColors[i], true);
                strip.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -12f), new Vector2(inner, 90f));
                var name = UIKit.Txt("Name", strip.transform, set.name, 30, Color.white);
                name.fontStyle = FontStyle.Bold;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 20;
                name.resizeTextMaxSize = 30;
                UIKit.Stretch(name.rectTransform, 8f, 8f);

                var cover = UIKit.Img("Cover", rt, ProcArt.Rounded, new Color(0.18f, 0.12f, 0.26f), true);
                cover.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -112f), new Vector2(inner, 220f));
                var glow = UIKit.Img("Glow", cover.transform, ProcArt.GlowSprite, new Color(setColors[i].r, setColors[i].g, setColors[i].b, 0.5f));
                glow.rectTransform.sizeDelta = new Vector2(inner - 16f, inner - 16f);
                var icon = UIKit.Img("Icon", cover.transform, CoverIcon(set.id), Color.white);
                icon.rectTransform.sizeDelta = new Vector2(Mathf.Min(170f, inner - 40f), Mathf.Min(170f, inner - 40f));

                int n = GM.StickerCount(set);
                var bar = UIKit.Img("Bar", rt, ProcArt.Rounded, new Color(0.3f, 0.24f, 0.38f), true);
                float barW = cardW - 40f;
                bar.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 26f), new Vector2(barW, 48f));
                var bfill = UIKit.Img("Fill", bar.transform, ProcArt.Rounded, UIKit.Green, true);
                bfill.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(4f, 0f), new Vector2(Mathf.Max(30f, (barW - 8f) * n / set.ids.Length), 40f));
                bfill.gameObject.SetActive(n > 0);
                var cnt = UIKit.Txt("Count", bar.transform, n + "/" + set.ids.Length, 30, Color.white);
                UIKit.Stretch(cnt.rectTransform);

                if (GM.IsSetClaimable(set))
                {
                    var badge = UIKit.Img("Badge", rt, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
                    badge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-10f, -10f), new Vector2(56f, 56f));
                    var bt = UIKit.Txt("!", badge.transform, "!", 38, Color.white);
                    UIKit.Stretch(bt.rectTransform);
                }
                else if (GM.IsSetClaimed(set))
                {
                    var ok = UIKit.Img("Done", rt, ProcArt.Check, Color.white);
                    ok.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-10f, -10f), new Vector2(60f, 60f));
                }
            }

            var hint = UIKit.Txt("Hint", overview, L.T("Etiketler siparişlerden, görevlerden ve kedilerden gelir. Bir sete dokun, içine bak."), 26,
                new Color(1f, 1f, 1f, 0.6f));
            hint.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -940f), new Vector2(880f, 80f));
        }

        static Sprite CoverIcon(string setId)
        {
            switch (setId)
            {
                case "ghosts": return ProcArt.Ghost(2);
                case "cats": return ProcArt.KaraPatiHead;
                case "renkli": return ProcArt.SideCat(2, 4, KittenColors.UcRenkli);
                default: return ProcArt.WitchItem("w_sapka");
            }
        }

        // ------------------------------------------------------------------ set detail
        void BuildDetail()
        {
            detail = UIKit.Rect("Detail", panel);
            UIKit.Stretch(detail, 0f, 0f, 120f, 0f);

            var back = UIKit.Btn("Back", detail, "<", UIKit.PanelDark, () => ShowOverview(), 50);
            ((RectTransform)back.transform).Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -6f), new Vector2(90f, 90f));
            detailTitle = UIKit.Txt("SetTitle", detail, "", 40, UIKit.Cream);
            detailTitle.fontStyle = FontStyle.Bold;
            detailTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -10f), new Vector2(700f, 80f));

            grid = UIKit.Rect("Grid", detail);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -120f), new Vector2(900f, 1040f));

            progress = UIKit.Txt("Progress", detail, "", 34, UIKit.Cream);
            progress.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 150f), new Vector2(900f, 50f));

            claim = UIKit.Btn("Claim", detail, "", UIKit.Disabled, Claim, 32);
            claimImg = claim.GetComponent<Image>();
            ((RectTransform)claim.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 40f), new Vector2(760f, 100f));
        }

        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowOverview();
            Tween.Pop(panel, 0.3f);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GM.OnAlbumClosed();
        }

        void ShowOverview()
        {
            overview.gameObject.SetActive(true);
            detail.gameObject.SetActive(false);
            FillOverview();
        }

        void ShowSet(int i)
        {
            setIndex = i;
            overview.gameObject.SetActive(false);
            detail.gameObject.SetActive(true);
            var set = Stickers.Sets[i];
            detailTitle.text = set.name;
            detailTitle.color = setColors[i];

            for (int c = grid.childCount - 1; c >= 0; c--) Destroy(grid.GetChild(c).gameObject);
            for (int n = 0; n < set.ids.Length; n++)
            {
                string id = set.ids[n];
                bool owned = GM.HasSticker(id);
                var slot = UIKit.Rect("Slot", grid);
                slot.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(((n % 3) - 1) * 300f, -(n / 3) * 345f), new Vector2(280f, 330f));

                var edge = UIKit.Img("Edge", slot, ProcArt.Circle, owned ? new Color(0.12f, 0.07f, 0.18f) : new Color(0.1f, 0.06f, 0.14f, 0.6f));
                edge.rectTransform.At(new Vector2(0f, 40f), new Vector2(236f, 236f));
                var paper = UIKit.Img("Paper", slot, ProcArt.Circle, owned ? new Color(1f, 0.97f, 0.92f) : new Color(0.17f, 0.11f, 0.24f));
                paper.rectTransform.At(new Vector2(0f, 40f), new Vector2(220f, 220f));

                if (owned)
                {
                    var icon = UIKit.Img("Icon", slot, ProcArt.StickerIcon(id), Color.white);
                    icon.rectTransform.At(new Vector2(0f, 40f), new Vector2(165f, 165f));
                    icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, ((n * 37) % 11) - 5f);
                }
                else
                {
                    var q = UIKit.Txt("?", slot, "?", 110, new Color(1f, 1f, 1f, 0.25f), TextAnchor.MiddleCenter, false);
                    q.rectTransform.At(new Vector2(0f, 40f), new Vector2(200f, 200f));
                }

                var name = UIKit.Txt("Name", slot, owned ? Stickers.Name(id) : "???", 28, owned ? UIKit.Cream : new Color(1f, 1f, 1f, 0.4f));
                name.rectTransform.At(new Vector2(0f, -120f), new Vector2(290f, 40f));
            }

            int have = GM.StickerCount(set);
            progress.text = have + "/" + set.ids.Length + " etiket";
            string reward = L.F("{0} altın + {1} mühür", Stickers.SetRewardGold, Stickers.SetRewardStamps);
            if (GM.IsSetClaimed(set))
            {
                claim.Label().text = L.T("Ödül alındı");
                claimImg.color = Theme.Ui(UIKit.Disabled);
                claim.interactable = false;
            }
            else if (have >= set.ids.Length)
            {
                claim.Label().text = L.F("Ödülü Al: {0}", reward);
                claimImg.color = Theme.Ui(UIKit.Orange);
                claim.interactable = true;
            }
            else
            {
                claim.Label().text = L.F("Seti tamamla: {0}", reward);
                claimImg.color = Theme.Ui(UIKit.Disabled);
                claim.interactable = false;
            }
        }

        void Claim()
        {
            if (GM.ClaimSet(Stickers.Sets[setIndex]))
            {
                ShowSet(setIndex);
                Tween.Punch(claim.transform, 0.12f, 0.3f);
            }
        }

        void ClaimGrand()
        {
            if (GM.ClaimGrand())
            {
                FillOverview();
                Tween.Punch(grandClaim.transform, 0.15f, 0.35f);
            }
        }
    }
}
