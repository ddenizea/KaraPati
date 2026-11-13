using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// The witch's tower: floors stacked bottom to top in a vertical scroll view.
    /// Floor 1 is Kara Pati's room with the food and water bowls; higher floors are bought with gold.
    /// </summary>
    public class TowerScreen : MonoBehaviour
    {
        public const float FloorW = 1020f, FloorH = 760f, RoofH = 420f, GroundH = 90f;

        GameManager GM => GameManager.I;
        ScrollRect scroll;
        RectTransform content;
        public RectTransform Content => content;
        readonly RectTransform[] floors = new RectTransform[Tower.Floors];
        readonly GameObject[] lockedCovers = new GameObject[Tower.Floors];
        readonly Text[] lockTexts = new Text[Tower.Floors];
        readonly Button[] unlockBtns = new Button[Tower.Floors];
        int armedFloor = -1;
        float armedUntil;

        // free-placement decoration
        readonly RectTransform[] decorLayers = new RectTransform[Tower.Floors];
        readonly RectTransform[] friendLayers = new RectTransform[Tower.Floors];   // best friends visiting (TowerFriends)
        TowerFriends friendsVisit;
        readonly Dictionary<PlacedSave, DecorPiece> pieces = new Dictionary<PlacedSave, DecorPiece>();
        DecorPiece selected;
        RectTransform toolbar;

        // floor 1: Kara Pati and the bowls
        Image cat, foodBowlFill, waterBowlFill, moodFill, needBubble;
        RectTransform foodBowl, waterBowl;
        Text moodText, foodText, waterText;
        Vector2 catHome = new Vector2(-170f, -205f);
        float careTick, catIdle;

        public RectTransform Floor(int i) => i >= 0 && i < floors.Length ? floors[i] : null;
        public RectTransform FriendLayer(int i) => i >= 0 && i < friendLayers.Length ? friendLayers[i] : null;

        // ------------------------------------------------------------------ build
        public void Build(RectTransform root)
        {
            var header = UIKit.Section(root, "TowerHeader", 140f, 110f);
            var back = UIKit.Btn("Back", header, "<", UIKit.PanelDark, () => GM.LeaveTower(), 54);
            ((RectTransform)back.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(96f, 96f));
            var title = UIKit.Txt("Title", header, L.T("Cadı Kulesi"), 50, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(Vector2.zero, new Vector2(700f, 70f));

            // scroll view: a masked window onto a tall column of floors
            var view = UIKit.Section(root, "TowerView", 258f, 1782f);
            var viewImg = view.gameObject.AddComponent<Image>();
            viewImg.color = new Color(0f, 0f, 0f, 0.001f);   // catches drags anywhere in the view
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 30f;

            content = UIKit.Rect("Content", view);
            content.anchorMin = new Vector2(0.5f, 0f);
            content.anchorMax = new Vector2(0.5f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(FloorW, GroundH + Tower.Floors * FloorH + RoofH);
            var ground = UIKit.Img("Ground", content, ProcArt.Rounded, new Color(0.14f, 0.22f, 0.16f), true);
            ground.rectTransform.anchorMin = ground.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            ground.rectTransform.pivot = new Vector2(0.5f, 0f);
            ground.rectTransform.sizeDelta = new Vector2(FloorW + 40f, GroundH + 30f);
            ground.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            ground.raycastTarget = false;
            content.anchoredPosition = Vector2.zero;
            scroll.content = content;
            scroll.viewport = view;

            for (int i = 0; i < Tower.Floors; i++) floors[i] = BuildFloor(i);
            BuildRoof();
            BuildCatRoom(floors[0]);
            BuildToolbar();
            friendsVisit = gameObject.AddComponent<TowerFriends>();
            friendsVisit.Init(this);

            // L.T("Süsle") button: opens the ornament shop / inventory for the floor in view
            var btn = UIKit.Img("Decorate", root, ProcArt.Circle, new Color(0.1f, 0.07f, 0.15f, 0.9f));
            btn.raycastTarget = true;
            btn.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(110f, 120f), new Vector2(170f, 170f));
            var ring = UIKit.Img("Ring", btn.transform, ProcArt.Circle, new Color(1f, 0.66f, 0.3f, 0.7f));
            ring.rectTransform.sizeDelta = new Vector2(180f, 180f);
            ring.transform.SetAsFirstSibling();
            var ic = UIKit.Img("Icon", btn.transform, ProcArt.Brush, Color.white);
            ic.rectTransform.At(new Vector2(0f, 14f), new Vector2(100f, 100f));
            var lbl = UIKit.Txt("Label", btn.transform, L.T("Süsle"), 30, UIKit.Cream);
            lbl.rectTransform.At(new Vector2(0f, -54f), new Vector2(170f, 40f));
            var b = btn.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => { Sfx.Play("click"); Deselect(); GM.OpenTowerDecor(FloorInView()); });
        }

        RectTransform BuildFloor(int i)
        {
            var f = UIKit.Rect("Floor" + (i + 1), content);
            f.anchorMin = f.anchorMax = new Vector2(0.5f, 0f);
            f.pivot = new Vector2(0.5f, 0f);
            f.sizeDelta = new Vector2(FloorW, FloorH);
            f.anchoredPosition = new Vector2(0f, GroundH + i * FloorH);

            var wall = Tower.Wall[i];
            // outer stone, inner wall, floorboards and a ceiling beam
            P("Stone", f, ProcArt.Rounded, new Color(0.16f, 0.13f, 0.2f), Vector2.zero, new Vector2(FloorW, FloorH), true);
            P("Wall", f, ProcArt.Rounded, wall, new Vector2(0f, 20f), new Vector2(FloorW - 60f, FloorH - 60f), true);
            for (int s = 0; s < 6; s++)
                P("Stripe", f, null, new Color(1f, 1f, 1f, 0.035f), new Vector2(-400f + s * 160f, 40f), new Vector2(24f, FloorH - 120f));
            P("Boards", f, ProcArt.Rounded, new Color(wall.r * 0.9f + 0.12f, wall.g * 0.7f + 0.08f, wall.b * 0.5f + 0.04f), new Vector2(0f, -FloorH / 2f + 95f), new Vector2(FloorW - 60f, 150f), true);
            for (int s = 0; s < 3; s++)
                P("Plank", f, null, new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -FloorH / 2f + 60f + s * 40f), new Vector2(FloorW - 70f, 3f));
            P("Beam", f, ProcArt.Rounded, new Color(0.3f, 0.2f, 0.15f), new Vector2(0f, FloorH / 2f - 22f), new Vector2(FloorW, 40f), true);

            // window with the night sky (the terrace is open to the sky instead)
            if (i != 4)
            {
                var wx = i % 2 == 0 ? 300f : -300f;
                P("WinFrame", f, ProcArt.Rounded, new Color(0.35f, 0.24f, 0.18f), new Vector2(wx, 110f), new Vector2(200f, 240f), true);
                P("Glass", f, ProcArt.Rounded, new Color(0.08f, 0.1f, 0.25f), new Vector2(wx, 110f), new Vector2(160f, 200f), true);
                P("Moon", f, ProcArt.Moon, Color.white, new Vector2(wx - 20f, 140f), new Vector2(70f, 70f));
                P("Bar", f, null, new Color(0.35f, 0.24f, 0.18f), new Vector2(wx, 110f), new Vector2(10f, 200f));
            }
            else
            {
                for (int s = 0; s < 10; s++)
                    P("Star", f, ProcArt.Sparkle, new Color(1f, 1f, 0.85f, 0.6f), new Vector2(-420f + s * 95f, 60f + (s * 53) % 200), new Vector2(22f, 22f));
                for (int s = 0; s < 9; s++)
                    P("Rail", f, null, new Color(0.45f, 0.35f, 0.3f), new Vector2(-400f + s * 100f, -170f), new Vector2(12f, 120f));
                P("RailTop", f, ProcArt.Rounded, new Color(0.5f, 0.38f, 0.3f), new Vector2(0f, -110f), new Vector2(FloorW - 80f, 18f), true);
            }
            FloorFurniture(f, i);
            // ornaments the player places live here: above the furniture, below the locked cover (and below the cat)
            decorLayers[i] = UIKit.Rect("Decor", f).At(Vector2.zero, new Vector2(FloorW, FloorH));
            friendLayers[i] = UIKit.Rect("Friends", f).At(Vector2.zero, new Vector2(FloorW, FloorH));

            // name plaque
            var plaque = P("Plaque", f, ProcArt.Rounded, new Color(0.15f, 0.1f, 0.2f, 0.85f), new Vector2(i % 2 == 0 ? -300f : 300f, FloorH / 2f - 80f), new Vector2(360f, 64f), true);
            var name = UIKit.TxtRaw("Name", plaque.transform, L.F("{0}. Kat  •  {1}", i + 1, Tower.FloorNames[i]), 28, UIKit.Cream, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(name.rectTransform);

            // locked cover
            var cover = UIKit.Rect("Locked", f).At(Vector2.zero, new Vector2(FloorW, FloorH));
            var dark = P("Dark", cover, ProcArt.Rounded, new Color(0.04f, 0.02f, 0.07f, 0.9f), Vector2.zero, new Vector2(FloorW, FloorH), true);
            dark.raycastTarget = true;
            P("Web", cover, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.35f), new Vector2(-420f, 300f), new Vector2(170f, 170f));
            P("Web", cover, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.25f), new Vector2(420f, 300f), new Vector2(150f, 150f));
            P("Lock", cover, ProcArt.Lock, Color.white, new Vector2(0f, 120f), new Vector2(120f, 120f));
            var lt = UIKit.TxtRaw("Name", cover, L.F("{0}. Kat: {1}", i + 1, Tower.FloorNames[i]), 40, UIKit.Cream);
            lt.fontStyle = FontStyle.Bold;
            lt.rectTransform.At(new Vector2(0f, 20f), new Vector2(900f, 60f));
            lockTexts[i] = UIKit.TxtRaw("Hint", cover, "", 28, new Color(1f, 1f, 1f, 0.7f));
            lockTexts[i].rectTransform.At(new Vector2(0f, -36f), new Vector2(900f, 44f));
            int fi = i;
            unlockBtns[i] = UIKit.Btn("Unlock", cover, "", UIKit.Green, () => OnUnlock(fi), 36);
            ((RectTransform)unlockBtns[i].transform).At(new Vector2(0f, -130f), new Vector2(420f, 110f));
            lockedCovers[i] = cover.gameObject;
            return f;
        }

        void FloorFurniture(RectTransform f, int i)
        {
            float floorY = -FloorH / 2f + 160f;
            switch (i)
            {
                case 1: // kitchen
                    P("Cauldron", f, ProcArt.Classic(Chains.Kazan, 1), Color.white, new Vector2(-330f, floorY + 30f), new Vector2(220f, 220f));
                    P("Shelf", f, ProcArt.Rounded, new Color(0.4f, 0.27f, 0.18f), new Vector2(150f, 150f), new Vector2(300f, 22f), true);
                    for (int b = 0; b < 4; b++) P("Jar", f, ProcArt.Bottle(b), Color.white, new Vector2(40f + b * 75f, 200f), new Vector2(70f, 70f));
                    break;
                case 2: // library
                    P("Case", f, ProcArt.Rounded, new Color(0.25f, 0.16f, 0.11f), new Vector2(-330f, 30f), new Vector2(240f, 420f), true);
                    var bc = new[] { new Color(0.75f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.75f), new Color(0.4f, 0.65f, 0.4f), new Color(0.75f, 0.6f, 0.3f) };
                    for (int r = 0; r < 4; r++)
                        for (int b = 0; b < 6; b++)
                            P("Book", f, null, bc[(b + r) % 4], new Vector2(-410f + b * 32f, -120f + r * 95f), new Vector2(24f, 70f));
                    break;
                case 3: // bedroom
                    P("Bed", f, ProcArt.Rounded, new Color(0.5f, 0.3f, 0.45f), new Vector2(-260f, floorY), new Vector2(380f, 130f), true);
                    P("Pillow", f, ProcArt.Circle, new Color(0.95f, 0.9f, 0.95f), new Vector2(-400f, floorY + 50f), new Vector2(120f, 60f));
                    P("Blanket", f, ProcArt.Rounded, new Color(0.4f, 0.35f, 0.7f), new Vector2(-200f, floorY + 20f), new Vector2(260f, 100f), true);
                    break;
                case 4: // terrace
                    P("Moon", f, ProcArt.FullMoon, Color.white, new Vector2(300f, 220f), new Vector2(200f, 200f));
                    break;
                case 5: // observatory
                    P("Scope", f, ProcArt.Telescope, Color.white, new Vector2(-260f, floorY + 60f), new Vector2(320f, 320f));
                    P("Chart", f, ProcArt.MagicCircle, new Color(0.8f, 0.7f, 1f, 0.6f), new Vector2(150f, 160f), new Vector2(260f, 260f));
                    break;
            }
        }

        void BuildRoof()
        {
            var r = UIKit.Rect("Roof", content);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(FloorW, RoofH);
            r.anchoredPosition = new Vector2(0f, GroundH + Tower.Floors * FloorH);
            // children are centred in the roof box, so y = 0 is half way up it
            var cone = P("Cone", r, ProcArt.Pennant, new Color(0.42f, 0.24f, 0.6f), new Vector2(0f, -20f), new Vector2(1100f, 380f));
            cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            P("Eave", r, ProcArt.Rounded, new Color(0.32f, 0.18f, 0.46f), new Vector2(0f, -195f), new Vector2(1100f, 30f), true);
            P("Star", r, ProcArt.Sparkle, new Color(1f, 0.88f, 0.4f), new Vector2(0f, 175f), new Vector2(70f, 70f));
            P("Flag", r, ProcArt.Pennant, UIKit.Orange, new Vector2(40f, 165f), new Vector2(60f, 50f));
        }

        // ------------------------------------------------------------------ floor 1: Kara Pati
        void BuildCatRoom(RectTransform f)
        {
            float floorY = -FloorH / 2f + 150f;
            // cat bed
            P("BedShadow", f, ProcArt.Circle, new Color(0f, 0f, 0f, 0.25f), new Vector2(-170f, floorY - 50f), new Vector2(300f, 60f));
            P("Bed", f, ProcArt.Circle, new Color(0.7f, 0.25f, 0.35f), new Vector2(-170f, floorY - 30f), new Vector2(290f, 110f));
            P("Cushion", f, ProcArt.Circle, new Color(0.95f, 0.6f, 0.65f), new Vector2(-170f, floorY - 22f), new Vector2(230f, 70f));

            cat = P("KaraPati", f, ProcArt.SideCat(0, 4), Color.white, catHome, new Vector2(230f, 230f));
            cat.raycastTarget = true;
            var cb = cat.gameObject.AddComponent<Button>();
            cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(PetCat);
            needBubble = P("Need", f, ProcArt.Circle, Color.white, catHome + new Vector2(70f, 140f), new Vector2(80f, 80f));
            var q = UIKit.Txt("!", needBubble.transform, "!", 52, new Color(0.9f, 0.3f, 0.3f), TextAnchor.MiddleCenter, false);
            q.fontStyle = FontStyle.Bold;
            UIKit.Stretch(q.rectTransform);

            foodBowl = MakeBowl(f, "Food", new Vector2(150f, floorY - 30f), new Color(1f, 0.6f, 0.3f), ProcArt.Kibble, out foodBowlFill, out foodText, FillFood);
            waterBowl = MakeBowl(f, "Water", new Vector2(330f, floorY - 30f), new Color(0.45f, 0.7f, 1f), ProcArt.Water, out waterBowlFill, out waterText, FillWater);

            // happiness bar under the plaque
            var panel = P("Mood", f, ProcArt.Rounded, new Color(0.12f, 0.08f, 0.18f, 0.85f), new Vector2(-250f, 175f), new Vector2(420f, 120f), true);
            P("Heart", panel.transform, ProcArt.Heart, Color.white, new Vector2(-160f, 14f), new Vector2(60f, 60f));
            var barBg = P("Bar", panel.transform, ProcArt.Rounded, new Color(0f, 0f, 0f, 0.4f), new Vector2(40f, 14f), new Vector2(300f, 30f), true);
            moodFill = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, new Color(1f, 0.45f, 0.6f), true);
            moodFill.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(Vector2.zero, new Vector2(300f, 30f));
            moodText = UIKit.TxtRaw("Text", panel.transform, "", 24, UIKit.Cream, TextAnchor.MiddleCenter, false);
            moodText.rectTransform.At(new Vector2(0f, -32f), new Vector2(400f, 36f));
            BuildUstaShelf(f);
        }

        // ------------------------------------------------------------------ the Master's shelf
        // The Master Cat works in the shop during the day; at home it naps on a cushion on a wall shelf.
        // Tap it and it plays with Kara Pati (every 5 minutes; the happiness bar fills a little).
        RectTransform ustaShelf;
        Image ustaImg, yarn;
        Text ustaLabel;
        Vector2 ustaHome = new Vector2(250f, 40f);
        bool playing;
        const float PlayHours = 5f / 60f;   // they can play again after 5 minutes

        void BuildUstaShelf(RectTransform f)
        {
            ustaShelf = UIKit.Rect("UstaShelf", f).At(Vector2.zero, new Vector2(FloorW, FloorH));
            P("Bracket", ustaShelf, ProcArt.Rounded, new Color(0.3f, 0.19f, 0.13f), ustaHome + new Vector2(-90f, -40f), new Vector2(18f, 50f), true);
            P("Bracket", ustaShelf, ProcArt.Rounded, new Color(0.3f, 0.19f, 0.13f), ustaHome + new Vector2(90f, -40f), new Vector2(18f, 50f), true);
            P("Plank", ustaShelf, ProcArt.Rounded, new Color(0.45f, 0.3f, 0.2f), ustaHome + new Vector2(0f, -18f), new Vector2(290f, 26f), true);
            P("Cushion", ustaShelf, ProcArt.Circle, new Color(0.55f, 0.35f, 0.78f), ustaHome + new Vector2(0f, 2f), new Vector2(230f, 52f));
            ustaImg = P("Usta", ustaShelf, ProcArt.SideCat(3, 5), Color.white, ustaHome + new Vector2(0f, 62f), new Vector2(190f, 190f));
            ustaImg.raycastTarget = true;
            var b = ustaImg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(TapUsta);
            ustaLabel = UIKit.TxtRaw("Name", ustaShelf, "", 26, UIKit.Cream, TextAnchor.MiddleCenter, false);
            ustaLabel.fontStyle = FontStyle.Bold;
            ustaLabel.rectTransform.At(ustaHome + new Vector2(0f, -56f), new Vector2(300f, 34f));
            ustaLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            yarn = P("Yarn", f, ProcArt.Item(Chains.Ag, 2), Color.white, Vector2.zero, new Vector2(70f, 70f));
            yarn.gameObject.SetActive(false);
            RefreshUsta();
        }

        public void RefreshUsta()
        {
            if (!ustaShelf || !GM || GM.Cats == null) return;
            bool has = GM.Cats.HasAnyMaster;
            ustaShelf.gameObject.SetActive(has);   // the shelf goes up with the first Master
            if (!has || playing) return;
            int col = GM.Cats.MasterColor;
            bool away = GM.Cats.UstaAway;
            ustaImg.gameObject.SetActive(!away);
            ustaImg.sprite = ProcArt.SideCat(3, 5, col);
            ustaImg.rectTransform.anchoredPosition = ustaHome + new Vector2(0f, 62f);
            ustaImg.rectTransform.localScale = Vector3.one;
            ustaLabel.text = away ? L.F("{0} keşifte", GM.UstaDisplayName(col)) : GM.UstaDisplayName(col);
        }

        void TapUsta()
        {
            if (playing || GM.Cats.UstaAway) return;
            long now = System.DateTime.UtcNow.Ticks;
            string who = GM.UstaDisplayName(GM.Cats.MasterColor);
            if (now < GM.UstaPlayAt)
            {
                var left = System.TimeSpan.FromTicks(GM.UstaPlayAt - now);
                Tween.Punch(ustaImg.transform, 0.08f, 0.3f);
                Sfx.Play("paw", 0.8f);
                GM.Toast(L.F("{0} esniyor: \"Biraz sonra oynarız...\" ({1})", who, left.Minutes > 0 ? L.F("{0} dk {1} sn", left.Minutes, left.Seconds) : L.F("{0} sn", left.Seconds)));
                return;
            }
            StartCoroutine(PlayYarn(who));
        }

        System.Collections.IEnumerator PlayYarn(string who)
        {
            playing = true;
            int col = GM.Cats.MasterColor;
            float floorY = -FloorH / 2f + 150f;
            var u = ustaImg.rectTransform;
            var k = cat.rectTransform;
            Vector2 uSpot = new Vector2(120f, floorY + 70f), kSpot = new Vector2(-170f, floorY + 70f);
            // the Master wakes up and hops down; Kara Pati comes over
            ustaImg.sprite = ProcArt.SideCat(3, 4, col);
            Sfx.Play("paw", 1.2f);
            Tween.Move(u, uSpot, 0.5f);
            Tween.Move(k, kSpot, 0.5f);
            u.localScale = new Vector3(-1f, 1f, 1f);   // face Kara Pati
            k.localScale = Vector3.one;
            yield return new WaitForSeconds(0.55f);
            // the yarn rolls back and forth between them
            yarn.gameObject.SetActive(true);
            yarn.transform.SetAsLastSibling();
            Vector2 a = kSpot + new Vector2(80f, -50f), b = uSpot + new Vector2(-80f, -50f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 from = i % 2 == 0 ? a : b, to = i % 2 == 0 ? b : a;
                Sfx.Play("pop", 1.1f + i * 0.05f, 0.6f);
                Tween.Punch(i % 2 == 0 ? k : u, 0.12f, 0.25f);
                float t = 0f;
                while (t < 0.8f)
                {
                    t += Time.deltaTime;
                    float e = Mathf.Clamp01(t / 0.8f);
                    yarn.rectTransform.anchoredPosition = Vector2.Lerp(from, to, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * 60f);
                    yarn.rectTransform.localEulerAngles = new Vector3(0f, 0f, (i % 2 == 0 ? -1f : 1f) * e * 360f);
                    yield return null;
                }
            }
            yarn.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++)
            {
                var h = P("Heart", floors[0], ProcArt.Heart, Color.white, Vector2.Lerp(kSpot, uSpot, i / 3f) + new Vector2(0f, 90f), new Vector2(46f, 46f));
                var hr = h.rectTransform;
                Vector2 hf = hr.anchoredPosition;
                Tween.Run(1.1f, e => { if (!hr) return; hr.anchoredPosition = hf + new Vector2(0f, e * 120f); h.color = new Color(1f, 1f, 1f, 1f - e); }, () => { if (hr) Destroy(hr.gameObject); });
            }
            GM.UstaPlayed(PlayHours);
            GM.Toast(L.F("{0} ve Kara Pati yumakla oynadı! Mutluluk arttı.", who), UIKit.Orange, 4f);
            Sfx.Play("gift", 1.1f);
            RefreshCare();
            yield return new WaitForSeconds(0.6f);
            Tween.Move(u, ustaHome + new Vector2(0f, 62f), 0.5f);
            Tween.Move(k, catHome, 0.5f);
            yield return new WaitForSeconds(0.55f);
            u.localScale = Vector3.one;
            playing = false;
            RefreshUsta();
        }

        RectTransform MakeBowl(RectTransform f, string name, Vector2 pos, Color tint, Sprite content, out Image fill, out Text label, System.Action onTap)
        {
            var root = UIKit.Rect(name, f).At(pos, new Vector2(170f, 150f));
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.001f);
            var b = root.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Tween.Punch(root, 0.15f, 0.25f); onTap(); });
            // bowl first, then what is inside it on top (water sits inside the rim, kibble heaps above it)
            P("Bowl", root, ProcArt.Bowl, tint, new Vector2(0f, -4f), new Vector2(160f, 110f));
            bool water = content == ProcArt.Water;
            fill = P("Fill", root, content, Color.white, water ? new Vector2(0f, 3f) : new Vector2(0f, 22f), water ? new Vector2(112f, 26f) : new Vector2(120f, 70f));
            label = UIKit.TxtRaw("Label", root, "", 24, UIKit.Cream, TextAnchor.MiddleCenter, true);
            label.rectTransform.At(new Vector2(0f, -78f), new Vector2(200f, 34f));
            return root;
        }

        void FillFood()
        {
            if (GM.FillFood()) CatGoTo(foodBowl.anchoredPosition);
            RefreshCare();
        }

        void FillWater()
        {
            if (GM.FillWater()) CatGoTo(waterBowl.anchoredPosition);
            RefreshCare();
        }

        void CatGoTo(Vector2 bowl)
        {
            // trot over to the bowl, nibble, trot back
            var rt = cat.rectTransform;
            Vector2 near = bowl + new Vector2(-120f, 70f);
            rt.localScale = new Vector3(-1f, 1f, 1f);
            Tween.Move(rt, near, 0.45f);
            Tween.Run(1.4f, t => { if (rt && t > 0.35f) rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 40f) * 4f); }, () =>
            {
                if (!rt) return;
                rt.localEulerAngles = Vector3.zero;
                rt.localScale = Vector3.one;
                Tween.Move(rt, catHome, 0.5f);
            });
        }

        void PetCat()
        {
            Sfx.Play("paw", 1.1f);
            Tween.Punch(cat.transform, 0.15f, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                var h = P("Heart", floors[0], ProcArt.Heart, Color.white, cat.rectTransform.anchoredPosition + new Vector2(Random.Range(-50f, 50f), 90f), new Vector2(46f, 46f));
                var hr = h.rectTransform;
                Vector2 from = hr.anchoredPosition;
                float delay = i * 0.12f;
                Tween.Run(1.1f + delay, t =>
                {
                    if (!hr) return;
                    float k = Mathf.Clamp01((t * (1.1f + delay) - delay) / 1.1f);
                    hr.anchoredPosition = from + new Vector2(Mathf.Sin(k * 6f) * 14f, k * 140f);
                    h.color = new Color(1f, 1f, 1f, 1f - k);
                }, () => { if (hr) Destroy(hr.gameObject); });
            }
            GM.Toast(GM.CatHappy ? L.T("Kara Pati mırlıyor: \"Karnım tok, keyfim yerinde!\"")
                                 : L.T("Kara Pati: \"Miyav... kaplarım boş. Biraz mama ve su?\""));
        }

        // ------------------------------------------------------------------ state
        public void Refresh()
        {
            int open = GM.TowerFloorsOpen;
            for (int i = 0; i < Tower.Floors; i++)
            {
                bool locked = i >= open;
                lockedCovers[i].SetActive(locked);
                if (!locked) continue;
                bool next = i == open;
                int price = Tower.FloorPrice[i];
                bool armed = armedFloor == i && Time.unscaledTime < armedUntil;
                unlockBtns[i].gameObject.SetActive(next);
                lockTexts[i].text = next ? L.T("Bu katı tamir edip açmak için altın gerekiyor.") : L.T("Önce alttaki katı aç.");
                if (next)
                {
                    unlockBtns[i].Label().text = armed ? L.T("Emin misin?") : L.F("Aç: {0} altın", price);
                    unlockBtns[i].GetComponent<Image>().color = Theme.Ui(armed ? UIKit.Orange : GM.Gold >= price ? UIKit.Green : UIKit.Disabled);
                }
            }
            RefreshCare();
            RefreshUsta();
            if (friendsVisit) friendsVisit.Refresh();
        }

        void OnUnlock(int i)
        {
            if (armedFloor == i && Time.unscaledTime < armedUntil)
            {
                armedFloor = -1;
                if (GM.UnlockTowerFloor(i))
                {
                    var f = floors[i];
                    FX.Burst(f, Vector2.zero, UIKit.Orange, 24);
                    Tween.Pop(f, 0.35f);
                }
                Refresh();
                return;
            }
            if (GM.Gold < Tower.FloorPrice[i]) { GM.Toast(L.F("{0} altın gerekiyor.", Tower.FloorPrice[i])); Sfx.Play("error"); return; }
            armedFloor = i;
            armedUntil = Time.unscaledTime + 3f;
            Refresh();
        }

        void RefreshCare()
        {
            if (!cat || !GM) return;   // GM is already gone while Play mode is shutting down
            float food = GM.FoodLevel, water = GM.WaterLevel;
            foodBowlFill.rectTransform.localScale = new Vector3(Mathf.Lerp(0.55f, 1f, food), food, 1f);
            foodBowlFill.gameObject.SetActive(food > 0.02f);
            waterBowlFill.rectTransform.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, water), Mathf.Max(0.3f, water), 1f);
            waterBowlFill.gameObject.SetActive(water > 0.02f);
            foodText.text = food <= 0.02f ? L.T("Mama bitti!") : L.F("Mama %{0}", Mathf.RoundToInt(food * 100f));
            waterText.text = water <= 0.02f ? L.T("Su bitti!") : L.F("Su %{0}", Mathf.RoundToInt(water * 100f));
            foodText.SetInk(food < Tower.HappyAbove ? new Color(1f, 0.55f, 0.5f) : UIKit.Cream);
            waterText.SetInk(water < Tower.HappyAbove ? new Color(1f, 0.55f, 0.5f) : UIKit.Cream);

            float mood = (food + water) / 2f;
            moodFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(24f, 300f * mood), 30f);
            bool happy = GM.CatHappy;
            moodText.text = happy ? L.F("Mutlu! Siparişlerden +%{0} altın", Mathf.RoundToInt(Tower.HappyBonus * 100f)) : L.T("Aç ya da susuz: bonus yok");
            needBubble.gameObject.SetActive(!happy);
            cat.color = happy ? Color.white : new Color(0.75f, 0.75f, 0.85f);
        }

        void Update()
        {
            if (!cat || !GM) return;
            float t = Time.time;
            careTick -= Time.unscaledDeltaTime;
            if (careTick <= 0f) { careTick = 1f; RefreshCare(); if (armedFloor >= 0 && Time.unscaledTime >= armedUntil) { armedFloor = -1; Refresh(); } }
            // gentle breathing; the "!" bubble bobs when she needs something
            cat.rectTransform.localScale = new Vector3(cat.rectTransform.localScale.x < 0 ? -1f : 1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);
            if (needBubble.gameObject.activeSelf)
                needBubble.rectTransform.anchoredPosition = cat.rectTransform.anchoredPosition + new Vector2(70f, 140f + Mathf.Sin(t * 4f) * 8f);
            catIdle += Time.deltaTime;
        }

        // ------------------------------------------------------------------ decoration
        public static Vector2 ClampOnFloor(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, -FloorW / 2f + 90f, FloorW / 2f - 90f), Mathf.Clamp(p.y, -FloorH / 2f + 110f, FloorH / 2f - 110f));

        /// <summary>
        /// The open floor that takes up most of the screen. The screen is taller than one floor, so
        /// looking only at its middle picked floor 2 even when floor 1 was fully visible at the bottom.
        /// On a tie the lower floor wins.
        /// </summary>
        public int FloorInView()
        {
            float viewH = ((RectTransform)scroll.transform).rect.height;
            float bottom = -content.anchoredPosition.y, top = bottom + viewH;
            int best = 0;
            float bestShown = -1f;
            for (int i = 0; i < GM.TowerFloorsOpen; i++)
            {
                float f0 = GroundH + i * FloorH, f1 = f0 + FloorH;
                float shown = Mathf.Min(top, f1) - Mathf.Max(bottom, f0);   // visible height of this floor
                if (shown > bestShown + 1f) { bestShown = shown; best = i; }
            }
            return best;
        }

        /// <summary>Rebuild every placed ornament from the save data (after loading or a new game).</summary>
        public void RebuildDecor()
        {
            Deselect();
            foreach (var kv in pieces) if (kv.Value) Destroy(kv.Value.gameObject);
            pieces.Clear();
            foreach (var p in GM.Placed) BuildPiece(p);
        }

        DecorPiece BuildPiece(PlacedSave p)
        {
            if (!Decor.ParseItem(p.id, out var slot, out var v) || p.floor < 0 || p.floor >= decorLayers.Length) return null;
            var rt = DecorArt.Build(decorLayers[p.floor], slot, v, new Vector3(p.x, p.y, 420f));
            if (!rt) return null;
            var hit = UIKit.Img("Hit", rt, null, new Color(1f, 1f, 1f, 0.001f));
            hit.rectTransform.At(DecorArt.HitOffset(slot), DecorArt.HitSize(slot));
            hit.raycastTarget = true;
            rt.localScale = new Vector3(p.flip ? -1f : 1f, 1f, 1f);
            var piece = rt.gameObject.AddComponent<DecorPiece>();
            piece.Init(this, p);
            pieces[p] = piece;
            return piece;
        }

        /// <summary>Called by the decoration sheet: put one ornament from the inventory on the floor in view.</summary>
        public void PlaceFromInventory(string id, int floor)
        {
            floor = Mathf.Clamp(floor, 0, GM.TowerFloorsOpen - 1);
            if (floor != FloorInView()) ScrollToFloor(floor, false);   // show where it landed
            var pos = new Vector2(Random.Range(-60f, 120f), -FloorH / 2f + 200f);   // standing on the floorboards
            if (Decor.ParseItem(id, out var slot, out _) && slot == 0) pos = new Vector2(0f, FloorH / 2f - 120f);
            var p = GM.PlaceTowerDecor(id, floor, pos);
            if (p == null) return;
            var piece = BuildPiece(p);
            if (!piece) return;
            Tween.Pop(piece.transform, 0.4f);
            FX.Burst(decorLayers[floor], pos, UIKit.Orange, 12);
            Sfx.Play("pop", 1.2f);
            Select(piece);
            GM.Toast(L.T("Süsü parmağınla sürükle. Üstündeki düğmelerle çevir, öne al ya da kaldır."));
        }

        void BuildToolbar()
        {
            toolbar = UIKit.Rect("Toolbar", content).At(Vector2.zero, new Vector2(520f, 86f));
            toolbar.anchorMin = toolbar.anchorMax = new Vector2(0.5f, 0f);
            var bg = UIKit.Img("Bg", toolbar, ProcArt.Rounded, new Color(0.1f, 0.07f, 0.15f, 0.92f), true);
            UIKit.Stretch(bg.rectTransform);
            string[] labels = { L.T("Çevir"), L.T("Öne"), L.T("Kaldır"), L.T("Tamam") };
            Color[] colors = { UIKit.PanelDark, UIKit.PanelDark, new Color(0.75f, 0.32f, 0.35f), UIKit.Green };
            for (int i = 0; i < labels.Length; i++)
            {
                int k = i;
                var b = UIKit.Btn(labels[i], toolbar, labels[i], colors[i], () => OnTool(k), 26);
                ((RectTransform)b.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f + i * 128f, 0f), new Vector2(120f, 70f));
            }
            toolbar.gameObject.SetActive(false);
        }

        public void Select(DecorPiece piece)
        {
            if (selected == piece) { FollowSelection(); return; }
            selected = piece;
            toolbar.gameObject.SetActive(piece);
            toolbar.SetAsLastSibling();
            FollowSelection();
        }

        public void Deselect()
        {
            selected = null;
            if (toolbar) toolbar.gameObject.SetActive(false);
        }

        /// <summary>Keep the tool bar just above the selected ornament.</summary>
        public void FollowSelection()
        {
            if (!selected || !toolbar) return;
            var p = selected.data;
            float y = GroundH + p.floor * FloorH + FloorH / 2f + p.y + (p.y > FloorH / 2f - 220f ? -150f : 150f);
            toolbar.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -FloorW / 2f + 270f, FloorW / 2f - 270f), y);
        }

        void OnTool(int k)
        {
            if (!selected) return;
            var p = selected.data;
            var rt = (RectTransform)selected.transform;
            switch (k)
            {
                case 0: // flip
                    p.flip = !p.flip;
                    rt.localScale = new Vector3(p.flip ? -1f : 1f, 1f, 1f);
                    GM.MarkDirty();
                    break;
                case 1: // bring to front
                    GM.BringTowerDecorToFront(p);
                    rt.SetAsLastSibling();
                    Tween.Punch(rt, 0.1f, 0.2f);
                    break;
                case 2: // back to the inventory
                    GM.StoreTowerDecor(p);
                    pieces.Remove(p);
                    Destroy(selected.gameObject);
                    Deselect();
                    GM.Toast(L.F("{0} envantere kaldırıldı.", Decor.ItemName(p.id)));
                    break;
                default:
                    Deselect();
                    break;
            }
        }

        /// <summary>Scroll so floor i is in view.</summary>
        public void ScrollToFloor(int i, bool instant)
        {
            float viewH = ((RectTransform)scroll.transform).rect.height;
            float total = content.sizeDelta.y - viewH;
            float target = total <= 0f ? 0f : Mathf.Clamp01((GroundH + i * FloorH - (viewH - FloorH) / 2f) / total);
            if (instant) { scroll.verticalNormalizedPosition = target; return; }
            float from = scroll.verticalNormalizedPosition;
            Tween.Run(0.4f, k => { if (scroll) scroll.verticalNormalizedPosition = Mathf.Lerp(from, target, Tween.OutCubic(k)); });
        }

        static Image P(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = UIKit.ImgArt(name, parent, s, c, sliced);
            img.rectTransform.At(pos, size);
            img.raycastTarget = false;
            return img;
        }
    }
}
