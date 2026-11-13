using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// "Dostluk Defteri": every customer on a card (picture, friendship stage, hearts, points to the next stage).
    /// Customers who haven't come yet are a foggy silhouette. Tap a card to read everything that customer has told
    /// you so far; lines from later stages stay locked. A "!" marks customers with a story line you haven't read.
    /// </summary>
    public class FriendBookPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, grid, detail;
        Text count, dName, dStage, dBody;
        Image dPortrait;
        Button dGift;   // with tower visits off: today's visitor gives its gift here

        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);
        static readonly Color Paper = new Color(1f, 0.96f, 0.88f);

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

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.93f, 0.86f, 0.72f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the book don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(980f, 1820f));

            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.62f, 0.28f, 0.42f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 120f);
            var title = UIKit.Txt("Title", head.transform, L.T("Dostluk Defteri"), 52, Color.white, TextAnchor.MiddleCenter, true);
            title.fontStyle = FontStyle.Bold;
            UIKit.Stretch(title.rectTransform, 30f, 120f, 10f, 10f);
            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            count = UIKit.Txt("Count", panel, "", 30, new Color(0.45f, 0.32f, 0.22f), TextAnchor.MiddleCenter, false);
            count.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -150f), new Vector2(900f, 44f));

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -185f), new Vector2(920f, 1610f));
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = new Vector2(0f, -185f);

            BuildDetail();
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ one customer's story
        void BuildDetail()
        {
            detail = UIKit.Rect("Detail", panel);
            detail.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -130f), new Vector2(920f, 1670f));
            detail.pivot = new Vector2(0.5f, 1f);
            detail.anchoredPosition = new Vector2(0f, -130f);

            var back = UIKit.Btn("Back", detail, L.T("< Defter"), new Color(0.62f, 0.28f, 0.42f), ShowGrid, 32);
            ((RectTransform)back.transform).Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(0f, -10f), new Vector2(220f, 80f));

            var disc = UIKit.Img("Disc", detail, ProcArt.Circle, Paper);
            disc.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -10f), new Vector2(280f, 280f));
            dPortrait = UIKit.Img("Portrait", disc.transform, ProcArt.Customer("boo", true), Color.white);
            dPortrait.preserveAspect = true;
            dPortrait.rectTransform.sizeDelta = new Vector2(230f, 230f);

            dName = UIKit.Txt("Name", detail, "", 48, Ink, TextAnchor.MiddleCenter, false);
            dName.fontStyle = FontStyle.Bold;
            dName.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -300f), new Vector2(880f, 64f));
            dStage = UIKit.Txt("Stage", detail, "", 30, new Color(0.62f, 0.28f, 0.42f), TextAnchor.MiddleCenter, false);
            dStage.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -364f), new Vector2(880f, 44f));

            dGift = UIKit.Btn("Gift", detail, L.T("Hediyeni Al"), UIKit.Green, TakeGift, 30);
            ((RectTransform)dGift.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(0f, -10f), new Vector2(250f, 80f));

            var page = UIKit.Img("Page", detail, ProcArt.Rounded, Paper, true);
            page.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -420f), new Vector2(900f, 1230f));
            page.rectTransform.pivot = new Vector2(0.5f, 1f);
            page.rectTransform.anchoredPosition = new Vector2(0f, -420f);
            dBody = UIKit.Txt("Body", page.transform, "", 30, Ink, TextAnchor.UpperLeft, false);
            dBody.lineSpacing = 1.1f;
            UIKit.Stretch(dBody.rectTransform, 36f, 36f, 30f, 30f);
            detail.gameObject.SetActive(false);
        }

        CustomerDef shown;

        void TakeGift()
        {
            if (shown == null || !GM.FriendGiftReady(shown)) return;
            string what = GM.TakeFriendGift(shown);
            dGift.gameObject.SetActive(false);
            GM.ShowDialog(shown.name, Customers.GiftLine(shown.id) + "\n\n" + L.F("Hediye: {0}", what));
        }

        void ShowDetail(CustomerDef c)
        {
            shown = c;
            dGift.gameObject.SetActive(!GM.TowerFriendsOn && GM.IsVisitorToday(c) && GM.FriendGiftReady(c));
            int st = GM.FriendStage(c);
            grid.gameObject.SetActive(false);
            count.gameObject.SetActive(false);
            detail.gameObject.SetActive(true);
            dPortrait.sprite = ProcArt.Customer(c.id, st >= c.FullLookStage);
            dName.text = c.name;
            dStage.text = Customers.StageName(c, st) + "  ·  " + c.difficulty;
            if (c.id == Customers.NightOwl) dStage.text += "  ·  " + L.T("Sadece geceleri gelir (20:00-06:00)");

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < c.MaxStage; i++)
            {
                string stageName = Customers.StageName(c, i + 1);
                if (i < st) sb.Append("<b>").Append(stageName).Append("</b>\n<i>\"").Append(c.lines[Mathf.Min(i, c.lines.Length - 1)]).Append("\"</i>");
                else sb.Append("<color=#A08A78><b>").Append(stageName).Append("</b>\n").Append(L.T("Dostluğunuz ilerledikçe açılır.")).Append("</color>");
                sb.Append("\n\n");
            }
            if (st >= c.MaxStage)
            {
                sb.Append("<color=#B0457A>").Append(L.T("Can Dostun! Siparişleri +1 Pati Mührü verir, kulede seni ziyaret edip her gün hediye bırakır.")).Append("</color>");
                if (!GM.TowerFriendsOn) sb.Append("\n<color=#7A5A48>").Append(L.T("Kule ziyaretleri kapalı: bugün uğrayan can dostlarının hediyesini buradan alırsın.")).Append("</color>");
            }
            else
            {
                int have = GM.FriendPoints(c) - c.PointsFor(st), need = c.steps[st];
                sb.Append("<color=#7A5A48>").Append(L.F("Sonraki seviye: {0}/{1} sipariş puanı. İksir 2 sayar.", have, need)).Append("</color>");
            }
            dBody.text = sb.ToString();
            GM.MarkFriendSeen(c);
            Sfx.Play("page");
            Tween.Pop(detail, 0.25f);
        }

        void ShowGrid()
        {
            detail.gameObject.SetActive(false);
            grid.gameObject.SetActive(true);
            count.gameObject.SetActive(true);
            Fill();
        }

        // ------------------------------------------------------------------ the book
        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowGrid();
            Tween.Pop(panel, 0.3f);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GM.OnFriendBookClosed();
        }

        void Fill()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            var all = Customers.All;
            int met = 0, best = 0;
            const int cols = 2;
            int rows = Mathf.CeilToInt(all.Length / (float)cols);
            float gap = 16f, w = (920f - gap) / cols, h = Mathf.Min(300f, (1610f - gap * (rows - 1)) / rows);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                bool here = GM.Level >= c.unlockLevel;
                if (here) met++;
                if (GM.IsBestFriend(c)) best++;
                var pos = new Vector2(-460f + w * 0.5f + (i % cols) * (w + gap), -h * 0.5f - (i / cols) * (h + gap));
                BuildCard(c, here, pos, new Vector2(w, h));
            }
            count.text = L.F("{0}/{1} müşteriyle tanıştın  ·  {2} Can Dostu", met, all.Length, best);
        }

        void BuildCard(CustomerDef c, bool here, Vector2 pos, Vector2 size)
        {
            int st = GM.FriendStage(c);
            bool bf = GM.IsBestFriend(c);
            var card = UIKit.Img("C_" + c.id, grid, ProcArt.Rounded, bf ? new Color(1f, 0.9f, 0.7f) : Paper, true);
            var rt = card.rectTransform;
            rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(pos, size);

            float pic = Mathf.Min(size.y - 30f, 200f);
            var disc = UIKit.Img("Disc", rt, ProcArt.Circle, here ? new Color(0.93f, 0.86f, 0.95f) : new Color(0.55f, 0.5f, 0.62f));
            disc.raycastTarget = false;
            disc.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 0f), new Vector2(pic, pic));
            var face = UIKit.Img("Face", disc.transform, here ? (st == 0 ? ProcArt.CustomerSilhouette(c.id) : ProcArt.Customer(c.id, st >= c.FullLookStage)) : ProcArt.CustomerSilhouette(c.id),
                here ? Color.white : new Color(0.3f, 0.25f, 0.4f, 0.8f));
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.rectTransform.sizeDelta = new Vector2(pic * 0.85f, pic * 0.85f);
            if (!here)
            {
                var fog = UIKit.Img("Fog", disc.transform, ProcArt.Fog, Color.white);
                fog.raycastTarget = false;
                fog.rectTransform.At(new Vector2(0f, -pic * 0.2f), new Vector2(pic, pic * 0.5f));
            }

            float tx = pic + 30f, tw = size.x - tx - 14f;
            var name = UIKit.Txt("Name", rt, here ? c.name : "???", 32, Ink, TextAnchor.UpperLeft, false);
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            name.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(tx, -16f), new Vector2(tw, 80f));

            var info = UIKit.Txt("Info", rt, "", 26, new Color(0.45f, 0.32f, 0.22f), TextAnchor.LowerLeft, false);
            info.raycastTarget = false;
            info.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0f)).At(new Vector2(tx, 14f), new Vector2(tw, 120f));
            if (!here) info.text = L.F("{0}. seviyede gelir", c.unlockLevel);
            else
            {
                // hearts: one per stage
                string hearts = "";
                for (int s = 0; s < c.MaxStage; s++) hearts += s < st ? "<color=#E0457A>♥</color>" : "<color=#C9B8A8>♥</color>";
                string next = bf ? "<color=#B0457A>" + L.T("Can Dostu!") + "</color>"
                                 : L.F("{0}/{1} puan", GM.FriendPoints(c) - c.PointsFor(st), c.steps[st]);
                info.text = Customers.StageName(c, st) + "\n" + hearts + "\n" + next;
            }

            if (c.id == Customers.NightOwl)
            {
                var moon = UIKit.Img("Night", rt, ProcArt.Moon, Color.white);
                moon.raycastTarget = false;
                moon.rectTransform.Anchor(new Vector2(1f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(-30f, 30f), new Vector2(44f, 44f));
            }

            if (here && GM.FriendHasNew(c))
            {
                var badge = UIKit.Img("Badge", rt, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
                badge.raycastTarget = false;
                badge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-24f, -24f), new Vector2(44f, 44f));
                var bt = UIKit.Txt("!", badge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
                UIKit.Stretch(bt.rectTransform);
            }

            card.raycastTarget = true;
            var b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() =>
            {
                if (here) ShowDetail(c);
                else { Sfx.Play("error"); Tween.Punch(rt, 0.06f, 0.2f); }
            });
        }
    }
}
