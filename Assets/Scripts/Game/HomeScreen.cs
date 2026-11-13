using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Main screen. One scene per chapter (the shop, then the cellar). Spend Pati Mührü on story tasks that restore
    /// the scene piece by piece, and look for hidden clues with the eye button.
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        Text chapterTitle, progressText;
        RectTransform progressFill;
        Button prevBtn, nextBtn;
        Image nextLock;
        readonly Image[] gifts = new Image[2];
        readonly Image[] giftChecks = new Image[2];
        readonly Image[] giftGlows = new Image[2];
        const float ProgressW = 600f;
        int view = -1;

        RectTransform sceneArea, sceneClip;
        readonly RectTransform[] scenes = new RectTransform[Story.Chapters.Length];
        readonly Image[] dims = new Image[Story.Chapters.Length];
        readonly RectTransform[] clueLayers = new RectTransform[Story.Chapters.Length];
        readonly ChapterScene[] extra = new ChapterScene[Story.Chapters.Length];
        static readonly string[] sceneNames = { "Shop", "Cellar", "Garden", "Town", "Tower", "Realm" };
        RectTransform ghostLayer;

        // shop (chapter 1)
        GameObject webs, dirt, rug, bottles, counterStuff, bed;
        bool shopArt;   // the shop is built from the drawn pictures in Resources/Rooms (false: the old shapes)

        // shop life (drawn shop only): the cauldron bubbles once its fire is lit, and Kara Pati wanders about
        enum Fx { Bubble, Spark, Steam, Blob, Drop }
        class Bubble { public Image img; public Fx kind; public float age, life, x, size, phase, rise, vx, vy; }
        readonly List<Bubble> bubbles = new List<Bubble>();
        readonly List<Image> flames = new List<Image>();
        RectTransform bubbleLayer;
        float nextSteam, nextBlob, nextSplash;
        float nextBubble;
        enum CatMode { Sit, Walk, Sleep }
        CatMode catMode = CatMode.Sit;
        bool catStarted, shopHasBed, catToBed;
        float catX, catTargetX, catUntil, catWalkClock;
        int catFace = 1;
        const float CatFloorY = -290f, BedX = -330f, BedSleepY = -275f, CatSpeed = 115f;
        Image cauldron, cauldronGlow, lanternA, lanternB, glowA, glowB, plankTop, plankBottom, counterTop, counterBody, karaPati;

        // cellar (chapter 2)
        Image door, doorLight, jarTop, jarBottom, circle, circleGlow, portraitDust, mirror, mirrorGlow, chest, chestGlow, cellarCat, deskTop;
        GameObject jars, messyPapers, tidyPapers, chains, portraitArt, crate;
        readonly List<Image> candles = new List<Image>();
        readonly List<Image> candleGlows = new List<Image>();

        RectTransform taskList;
        Image albumBadge, caseBadge, dailyBadge, chaptersBadge, towerBadge, ustaBadge, ustaIcon;
        Image gameBadge, gameIcon, gameLock, friendBadge, achBadge;
        RectTransform gameNew, friendNew;   // "YENİ" tags until the feature is opened once
        Text caseBadgeText;
        readonly List<RectTransform> floatingGhosts = new List<RectTransform>();
        readonly Dictionary<string, Image> clueSpots = new Dictionary<string, Image>();
        int shownDone = -1;
        float eyeUntil;

        // chapter-specific lines first, then the general ones
        static readonly TrList[] chapterCatLines =
        {
            new TrList(L.N("Önce örümcek ağları. Sonra mama. Sonra yine mama.")),
            new TrList(L.N("Bu mahzen çok soğuk... patilerim dondu."), L.N("Aynada bir şey kıpırdadı, gördün mü?")),
            new TrList(L.N("Ay ışığında tüylerim parlıyor, değil mi?"), L.N("Bu korkuluk bana bakıyor. Bence o da kedi sever.")),
            new TrList(L.N("Hayalet kasabası ama pazarı fena değil!"), L.N("Saat kulesi çalışınca herkes uyanacak.")),
            new TrList(L.N("Bu merdivenler bitmiyor... beni taşır mısın?"), L.N("Teleskopla bakınca Cadı Hanım'ı görebilir miyiz?")),
            new TrList(L.N("Gölge aslında yalnız. Ona bir yumak verelim."), L.N("Eve dönmeye az kaldı, hissediyorum!")),
        };

        static readonly TrList catLines = new TrList
        (
            L.N("Miyav. Mühürleri sen topla, dükkânı ben yönetirim."),
            L.N("Cadı bir yerlerde... bıyıklarımda hissediyorum."),
            L.N("Mırr. Kulağımın arkasını kaşı, sonra işe dön."),
            L.N("Hayaletlerden korkma, çoğu sadece sohbet etmek istiyor."),
            L.N("Göz düğmesine bas, gizli şeyleri birlikte bulalım.")
        );

        // ------------------------------------------------------------------ build
        public void Build(RectTransform root)
        {
            // header with chapter switcher
            var header = UIKit.Section(root, "Header", 140f, 120f);
            chapterTitle = UIKit.Txt("Chapter", header, "", 40, UIKit.Orange);
            chapterTitle.fontStyle = FontStyle.Bold;
            chapterTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -4f), new Vector2(800f, 56f));
            var barBg = UIKit.Img("Bar", header, ProcArt.Rounded, UIKit.PanelDark, true);
            barBg.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(-50f, -74f), new Vector2(ProgressW + 12f, 36f));
            var fill = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, UIKit.Orange, true);
            progressFill = fill.rectTransform;
            progressFill.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(6f, 0f), new Vector2(40f, 24f));
            progressText = UIKit.Txt("Count", header, "0/8", 32, UIKit.Cream);
            progressText.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(350f, -74f), new Vector2(120f, 40f));

            // gift chests sitting on the progress bar (halfway and at the end)
            for (int g = 0; g < gifts.Length; g++)
            {
                int gi = g;
                float x = 6f + ProgressW * ChapterGifts.At[g];
                giftGlows[g] = UIKit.Img("GiftGlow", barBg.transform, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.4f, 0.7f));
                giftGlows[g].rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(x, 4f), new Vector2(130f, 130f));
                gifts[g] = UIKit.Img("Gift", barBg.transform, ProcArt.Classic(Chains.Sandik, 1), Color.white);
                gifts[g].rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(x, 4f), new Vector2(70f, 70f));
                gifts[g].raycastTarget = true;
                var gb = gifts[g].gameObject.AddComponent<Button>();
                gb.transition = Selectable.Transition.None;
                gb.onClick.AddListener(() => OnGiftTapped(gi));
                giftChecks[g] = UIKit.Img("Check", gifts[g].transform, ProcArt.Check, Color.white);
                giftChecks[g].rectTransform.At(new Vector2(20f, -18f), new Vector2(36f, 36f));
            }

            prevBtn = UIKit.Btn("Prev", header, "<", UIKit.PanelDark, () => ShowChapter(view - 1, true), 54);
            ((RectTransform)prevBtn.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(96f, 100f));
            nextBtn = UIKit.Btn("Next", header, ">", UIKit.PanelDark, () => ShowChapter(view + 1, true), 54);
            ((RectTransform)nextBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(96f, 100f));
            nextLock = UIKit.Img("Lock", nextBtn.transform, ProcArt.Lock, Color.white);
            nextLock.rectTransform.At(new Vector2(26f, -30f), new Vector2(46f, 46f));

            // scenes
            sceneArea = UIKit.Section(root, "Scene", 272f, 940f);
            // the rooms are clipped to a rounded card and get a hand-drawn frame on top. The clip is a little smaller
            // than the frame, so its hard (not smoothed) edge falls under the frame's line instead of showing outside it.
            var clip = UIKit.ImgArt("SceneClip", sceneArea, ProcArt.Rounded, Color.white, true);
            clip.pixelsPerUnitMultiplier = 0.5f;
            clip.rectTransform.At(Vector2.zero, new Vector2(1004f, 924f));
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            sceneClip = clip.rectTransform;
            scenes[0] = BuildShop(sceneArea);
            scenes[1] = BuildCellar(sceneArea);
            for (int i = 2; i < scenes.Length; i++)
            {
                extra[i] = ChapterScenes.Build(i, NewScene(i < sceneNames.Length ? sceneNames[i] : "Chapter" + i), MakeCatTappable);
                scenes[i] = extra[i].root;
                dims[i] = extra[i].dim;
            }
            for (int i = 0; i < scenes.Length; i++) BuildClues(i);
            var frame = UIKit.ImgArt("SceneFrame", sceneArea, ProcArt.FrameInk, Color.white, true);
            frame.pixelsPerUnitMultiplier = 0.5f;
            frame.rectTransform.At(Vector2.zero, new Vector2(1020f, 940f));

            // investigation buttons on top of the scene
            var caseBtn = RoundButton(sceneArea, "CaseFile", ProcArt.CaseFile, new Vector2(0f, 1f), new Vector2(100f, -76f), () => GM.OpenCaseFile());
            caseBadge = UIKit.Img("Badge", caseBtn.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            caseBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            caseBadgeText = UIKit.Txt("N", caseBadge.transform, "0", 26, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(caseBadgeText.rectTransform);
            RoundButton(sceneArea, "Eye", ProcArt.Eye, new Vector2(1f, 1f), new Vector2(-100f, -76f), UseEye);
            // settings: music and effects as two separate switches, and the language
            RoundButton(sceneArea, "Settings", ProcArt.Gear, new Vector2(1f, 1f), new Vector2(-100f, -200f), ToggleSettings);
            // Friendship Book: every customer, how good friends you are, the story lines they told you
            var book = RoundButton(sceneArea, "FriendBook", ProcArt.Heart, new Vector2(1f, 1f), new Vector2(-100f, -324f), () => GM.OpenFriendBook());
            friendBadge = UIKit.Img("Badge", book.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            friendBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var fbt = UIKit.Txt("!", friendBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(fbt.rectTransform);
            friendNew = NewTag(book.transform);
            // Achievements: "!" while a reward is waiting
            var trophy = RoundButton(sceneArea, "Achievements", ProcArt.Trophy, new Vector2(1f, 1f), new Vector2(-100f, -448f), () => GM.OpenAchievements());
            achBadge = UIKit.Img("Badge", trophy.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            achBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var abt = UIKit.Txt("!", achBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(abt.rectTransform);
            BuildSettings();
            var daily = RoundButton(sceneArea, "Daily", ProcArt.Calendar, new Vector2(0f, 1f), new Vector2(100f, -200f), () => GM.OpenDaily());
            dailyBadge = UIKit.Img("Badge", daily.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            dailyBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var db = UIKit.Txt("!", dailyBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(db.rectTransform);
            // chapter list (like a map of the story), red dot when a gift chest is waiting
            var chapters = RoundButton(sceneArea, "Chapters", ProcArt.Classic(Chains.Sandik, 1), new Vector2(0f, 1f), new Vector2(100f, -324f), () => GM.OpenChapters());
            chaptersBadge = UIKit.Img("Badge", chapters.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            chaptersBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var cbt = UIKit.Txt("!", chaptersBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(cbt.rectTransform);
            // "Usta Kedim": name and colour of the Master Cat
            var usta = RoundButton(sceneArea, "UstaKedim", ProcArt.SideCat(3, 4), new Vector2(0f, 1f), new Vector2(100f, -448f), () => GM.OpenUstaPanel());
            ustaIcon = usta.transform.Find("Icon").GetComponent<Image>();
            ustaIcon.rectTransform.sizeDelta = new Vector2(92f, 92f);
            ustaBadge = UIKit.Img("Badge", usta.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            ustaBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var ubt = UIKit.Txt("!", ustaBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(ubt.rectTransform);
            // Game Room: mini games (opens at level 5; "!" while a free play is waiting)
            var games = RoundButton(sceneArea, "GameRoom", ProcArt.GameRoomIcon, new Vector2(0f, 1f), new Vector2(100f, -572f), () => GM.OpenGameRoom());
            gameIcon = games.transform.Find("Icon").GetComponent<Image>();
            gameLock = UIKit.Img("Lock", games.transform, ProcArt.Lock, Color.white);
            gameLock.rectTransform.At(new Vector2(30f, -30f), new Vector2(46f, 46f));
            gameBadge = UIKit.Img("Badge", games.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            gameBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var gbt = UIKit.Txt("!", gameBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(gbt.rectTransform);
            gameNew = NewTag(games.transform);
            // the chapter title also opens the list
            chapterTitle.raycastTarget = true;
            var titleBtn = chapterTitle.gameObject.AddComponent<Button>();
            titleBtn.transition = Selectable.Transition.None;
            titleBtn.onClick.AddListener(() => GM.OpenChapters());

            // tasks
            var tasks = UIKit.Section(root, "Tasks", 1228f, 515f);
            var panel = UIKit.Img("Panel", tasks, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 30f, 30f);
            var t = UIKit.Txt("Title", panel.transform, L.T("Görevler"), 36, UIKit.Orange, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -10f), new Vector2(400f, 56f));
            taskList = UIKit.Rect("List", panel.transform);
            UIKit.Stretch(taskList, 20f, 20f, 70f, 10f);

            // bottom buttons
            var bottom = UIKit.Section(root, "Bottom", 1765f, 170f);
            var album = UIKit.Btn("Album", bottom, "", UIKit.Panel, () => GM.OpenAlbum());
            ((RectTransform)album.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(270f, 150f));
            var aIcon = UIKit.Img("Icon", album.transform, ProcArt.AlbumIcon, Color.white);
            aIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(18f, 0f), new Vector2(100f, 100f));
            var aLabel = album.Label();
            aLabel.text = L.T("Albüm");
            aLabel.fontSize = 34;
            aLabel.rectTransform.offsetMin = new Vector2(110f, 4f);
            albumBadge = UIKit.Img("Badge", album.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            albumBadge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, -14f), new Vector2(46f, 46f));
            var bt = UIKit.Txt("!", albumBadge.transform, "!", 32, Color.white);
            UIKit.Stretch(bt.rectTransform);

            // witch's tower: Kara Pati's home (red dot when the bowls run low)
            var towerBtn = UIKit.Btn("Tower", bottom, "", new Color(0.42f, 0.28f, 0.6f), () => GM.GoTower());
            ((RectTransform)towerBtn.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(315f, 0f), new Vector2(240f, 150f));
            var tIcon = UIKit.Img("Icon", towerBtn.transform, ProcArt.TowerIcon, Color.white);
            tIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 4f), new Vector2(100f, 100f));
            var tLabel = towerBtn.Label();
            tLabel.text = L.T("Kule");
            tLabel.fontSize = 34;
            tLabel.rectTransform.offsetMin = new Vector2(100f, 4f);
            towerBadge = UIKit.Img("Badge", towerBtn.transform, ProcArt.RoundInk, new Color(0.9f, 0.3f, 0.3f));
            towerBadge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, -14f), new Vector2(46f, 46f));
            var tbt = UIKit.Txt("!", towerBadge.transform, "!", 32, Color.white);
            UIKit.Stretch(tbt.rectTransform);

            var play = UIKit.Btn("Play", bottom, L.T("Dükkânı Aç"), UIKit.Green, () => GM.GoBoard(), 46);
            ((RectTransform)play.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(460f, 150f));
            play.Label().fontStyle = FontStyle.Bold;
            play.Label().rectTransform.offsetMin = new Vector2(110f, 4f);
            var pIcon = UIKit.Img("Icon", play.transform, ProcArt.Item(Chains.KaraKazan, 1), Color.white);
            pIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 4f), new Vector2(100f, 100f));
        }

        /// <summary>A small orange "YENİ" tag under a round button.</summary>
        RectTransform NewTag(Transform parent)
        {
            var tag = UIKit.Img("New", parent, ProcArt.Rounded, UIKit.Orange, true);
            tag.raycastTarget = false;
            tag.rectTransform.At(new Vector2(0f, -58f), new Vector2(96f, 38f));
            var tt = UIKit.Txt("T", tag.transform, L.T("YENİ"), 24, Color.white, TextAnchor.MiddleCenter, false);
            tt.fontStyle = FontStyle.Bold;
            tt.raycastTarget = false;
            UIKit.Stretch(tt.rectTransform);
            tag.gameObject.SetActive(false);
            return tag.rectTransform;
        }

        Button RoundButton(RectTransform parent, string name, Sprite icon, Vector2 anchor, Vector2 pos, System.Action onClick)
        {
            // a cream hand-drawn disc that sits fully inside the room
            var bg = UIKit.Img(name, parent, ProcArt.RoundInk, new Color(1f, 0.96f, 0.88f));
            bg.raycastTarget = true;
            bg.rectTransform.Anchor(anchor, new Vector2(0.5f, 0.5f)).At(pos, new Vector2(104f, 104f));
            var ic = UIKit.Img("Icon", bg.transform, icon, Color.white);
            ic.rectTransform.sizeDelta = new Vector2(70f, 70f);
            var b = bg.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => { Tween.Punch(bg.transform, 0.12f, 0.25f); Sfx.Play("click"); onClick(); });
            return b;
        }

        static Image Part(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = UIKit.ScenePart(name, parent, s, c, size, sliced);
            img.rectTransform.At(pos, size);
            return img;
        }

        RectTransform NewScene(string name)
        {
            return UIKit.Rect(name, sceneClip).At(Vector2.zero, new Vector2(1020f, 940f));
        }

        // ------------------------------------------------------------------ chapter 1: shop
        /// <summary>The shop from the drawn pictures: one background with the furniture laid on it. The story still
        /// switches the same pieces (dirty window, crooked shelves, rug, lanterns, counter, cat bed).</summary>
        RectTransform BuildShop(RectTransform area)
        {
            if (ProcArt.Room("shop_bg") == null) return BuildShopClassic(area);
            shopArt = true;
            var scene = NewScene("Shop");
            Sprite R(string n) => ProcArt.Room(n);

            Part("Room", scene, R("shop_bg"), Color.white, Vector2.zero, new Vector2(1020f, 940f));

            // window: the dirty glass is a second picture laid exactly over the clean one
            // (kept clear of the round buttons along the left edge)
            Part("Window", scene, R("shop_window"), Color.white, new Vector2(-255f, 190f), new Vector2(240f, 227f));
            dirt = Part("WindowDirty", scene, R("shop_window_dirty"), Color.white, new Vector2(-255f, 190f), new Vector2(240f, 227f)).gameObject;

            // two shelves; the bottles stand on their top edge
            plankTop = Part("ShelfTop", scene, R("shop_shelf"), Color.white, new Vector2(250f, 241f), new Vector2(380f, 107f));
            plankBottom = Part("ShelfBottom", scene, R("shop_shelf"), Color.white, new Vector2(250f, 103f), new Vector2(380f, 107f));
            bottles = UIKit.Rect("Bottles", scene).gameObject;
            for (int i = 0; i < 4; i++)
                Part("Bottle", bottles.transform, ProcArt.Bottle(i), Color.white, new Vector2(125f + i * 82f, 332f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(4), Color.white, new Vector2(140f, 192f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(1), Color.white, new Vector2(220f, 192f), new Vector2(80f, 80f));
            Part("Book", bottles.transform, ProcArt.WitchItem("w_kitap"), Color.white, new Vector2(330f, 196f), new Vector2(84f, 84f));

            // lanterns hang from hooks in the ceiling, between the window and the shelves
            Part("Hook", scene, R("shop_hook"), Color.white, new Vector2(-75f, 420f), new Vector2(41f, 100f));
            Part("Hook", scene, R("shop_hook"), Color.white, new Vector2(15f, 420f), new Vector2(41f, 100f));
            glowA = Part("GlowA", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(-75f, 335f), new Vector2(320f, 320f));
            glowB = Part("GlowB", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(15f, 335f), new Vector2(320f, 320f));
            lanternA = Part("LanternA", scene, ProcArt.Lantern(false), Color.white, new Vector2(-75f, 335f), new Vector2(120f, 135f));
            lanternB = Part("LanternB", scene, ProcArt.Lantern(false), Color.white, new Vector2(15f, 335f), new Vector2(120f, 135f));

            rug = UIKit.Rect("Rug", scene).gameObject;
            Part("Rug", rug.transform, R("shop_rug"), Color.white, new Vector2(0f, -345f), new Vector2(520f, 240f));
            cauldronGlow = Part("CauldronGlow", scene, ProcArt.GlowSprite, new Color(1f, 0.5f, 0.8f, 0.45f), new Vector2(0f, -190f), new Vector2(480f, 480f));
            // a little fire under the pot (shown once it is lit); flames flicker from their base
            flames.Clear();
            float[] fx = { -48f, 0f, 48f }, fw = { 56f, 70f, 56f }, fh = { 82f, 104f, 82f };
            for (int i = 0; i < 3; i++)
            {
                var fl = Part("Flame", scene, ProcArt.Flame, Color.white, Vector2.zero, new Vector2(fw[i], fh[i]));
                fl.rectTransform.pivot = new Vector2(0.5f, 0f);
                fl.rectTransform.anchoredPosition = new Vector2(fx[i], -385f);   // tips lick the pot from behind
                fl.gameObject.SetActive(false);
                flames.Add(fl);
            }
            cauldron = Part("Cauldron", scene, ProcArt.Item(Chains.KaraKazan, 1), Color.white, new Vector2(0f, -230f), new Vector2(300f, 300f));
            bubbleLayer = UIKit.Rect("Bubbles", scene).At(Vector2.zero, new Vector2(1020f, 940f));

            // the counter's top board is its own picture so it can lie crooked before it is repaired
            counterBody = Part("CounterBody", scene, R("shop_counter"), Color.white, new Vector2(340f, -241f), new Vector2(300f, 138f));
            counterTop = Part("CounterTop", scene, R("shop_counter_top"), Color.white, new Vector2(340f, -150f), new Vector2(300f, 52f));
            counterStuff = UIKit.Rect("CounterStuff", scene).gameObject;
            Part("Ball", counterStuff.transform, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(300f, -79f), new Vector2(100f, 100f));
            Part("Letter", counterStuff.transform, ProcArt.WitchItem("w_mektup"), Color.white, new Vector2(400f, -104f), new Vector2(70f, 70f));

            // cobwebs in the two top corners (the drawing is a top-right corner web; the left one is mirrored)
            webs = UIKit.Rect("Webs", scene).gameObject;
            Part("Web", webs.transform, R("shop_web"), Color.white, new Vector2(410f, 374f), new Vector2(200f, 192f));
            Part("Web", webs.transform, R("shop_web"), Color.white, new Vector2(-410f, 374f), new Vector2(200f, 192f)).rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            bed = UIKit.Rect("Bed", scene).gameObject;
            Part("Bed", bed.transform, R("shop_bed"), Color.white, new Vector2(-330f, -355f), new Vector2(250f, 171f));
            karaPati = Part("KaraPati", scene, ProcArt.SideCat(0, 4), Color.white, new Vector2(-330f, -290f), new Vector2(190f, 190f));
            MakeCatTappable(karaPati);

            ghostLayer = UIKit.Rect("Ghosts", scene).At(Vector2.zero, scene.sizeDelta);
            dims[0] = Part("Dust", scene, ProcArt.Rounded, new Color(0.05f, 0.03f, 0.08f, 0.45f), Vector2.zero, new Vector2(1020f, 940f), true);
            return scene;
        }

        /// <summary>The shop from plain shapes, used when the drawn room pictures are missing.</summary>
        RectTransform BuildShopClassic(RectTransform area)
        {
            var scene = NewScene("Shop");
            var wood = new Color(0.45f, 0.3f, 0.2f);

            Part("Wall", scene, ProcArt.Rounded, new Color(0.3f, 0.21f, 0.36f), Vector2.zero, new Vector2(1020f, 940f), true);
            for (int i = 0; i < 9; i++)
                Part("Stripe", scene, null, new Color(1f, 0.95f, 0.85f, 0.05f), new Vector2(-440f + i * 110f, 120f), new Vector2(20f, 660f));
            Part("Floor", scene, ProcArt.Rounded, new Color(0.33f, 0.22f, 0.17f), new Vector2(0f, -330f), new Vector2(1020f, 280f), true);
            for (int i = 0; i < 3; i++)
                Part("Plank", scene, null, new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -250f - i * 75f), new Vector2(1000f, 4f));

            Part("WinFrame", scene, ProcArt.Rounded, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(260f, 290f), true);
            Part("Glass", scene, ProcArt.Rounded, new Color(0.1f, 0.12f, 0.3f), new Vector2(-290f, 190f), new Vector2(214f, 244f), true);
            Part("Moon", scene, ProcArt.Moon, Color.white, new Vector2(-320f, 225f), new Vector2(110f, 110f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-230f, 270f), new Vector2(26f, 26f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-250f, 140f), new Vector2(18f, 18f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-350f, 120f), new Vector2(22f, 22f));
            dirt = UIKit.Rect("Dirt", scene).At(new Vector2(-290f, 190f), new Vector2(214f, 244f)).gameObject;
            Part("Grime", dirt.transform, ProcArt.Rounded, new Color(0.45f, 0.38f, 0.3f, 0.88f), Vector2.zero, new Vector2(214f, 244f), true);
            Part("Smudge", dirt.transform, ProcArt.Circle, new Color(0.3f, 0.25f, 0.2f, 0.5f), new Vector2(-40f, 40f), new Vector2(90f, 70f));
            Part("Smudge", dirt.transform, ProcArt.Circle, new Color(0.3f, 0.25f, 0.2f, 0.4f), new Vector2(50f, -50f), new Vector2(110f, 80f));
            Part("BarV", scene, null, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(12f, 244f));
            Part("BarH", scene, null, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(214f, 12f));

            plankTop = Part("ShelfTop", scene, ProcArt.Rounded, wood, new Vector2(250f, 280f), new Vector2(360f, 28f), true);
            plankBottom = Part("ShelfBottom", scene, ProcArt.Rounded, wood, new Vector2(250f, 140f), new Vector2(360f, 28f), true);
            bottles = UIKit.Rect("Bottles", scene).gameObject;
            for (int i = 0; i < 4; i++)
                Part("Bottle", bottles.transform, ProcArt.Bottle(i), Color.white, new Vector2(125f + i * 82f, 332f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(4), Color.white, new Vector2(140f, 192f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(1), Color.white, new Vector2(220f, 192f), new Vector2(80f, 80f));
            Part("Book", bottles.transform, ProcArt.WitchItem("w_kitap"), Color.white, new Vector2(330f, 196f), new Vector2(84f, 84f));

            Part("String", scene, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(-90f, 425f), new Vector2(4f, 90f));
            Part("String", scene, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(110f, 425f), new Vector2(4f, 90f));
            glowA = Part("GlowA", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(-90f, 345f), new Vector2(320f, 320f));
            glowB = Part("GlowB", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(110f, 345f), new Vector2(320f, 320f));
            lanternA = Part("LanternA", scene, ProcArt.Lantern(false), Color.white, new Vector2(-90f, 345f), new Vector2(100f, 110f));
            lanternB = Part("LanternB", scene, ProcArt.Lantern(false), Color.white, new Vector2(110f, 345f), new Vector2(100f, 110f));

            rug = UIKit.Rect("Rug", scene).gameObject;
            Part("RugOuter", rug.transform, ProcArt.Circle, new Color(0.6f, 0.2f, 0.3f), new Vector2(0f, -365f), new Vector2(620f, 170f));
            Part("RugMid", rug.transform, ProcArt.Circle, new Color(0.8f, 0.4f, 0.42f), new Vector2(0f, -365f), new Vector2(540f, 130f));
            Part("RugIn", rug.transform, ProcArt.Circle, new Color(0.6f, 0.2f, 0.3f), new Vector2(0f, -365f), new Vector2(300f, 70f));
            cauldronGlow = Part("CauldronGlow", scene, ProcArt.GlowSprite, new Color(0.4f, 1f, 0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(480f, 480f));
            cauldron = Part("Cauldron", scene, ProcArt.Classic(Chains.Kazan, 1), Color.white, new Vector2(0f, -230f), new Vector2(300f, 300f));

            counterBody = Part("CounterBody", scene, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.2f), new Vector2(340f, -255f), new Vector2(260f, 180f), true);
            counterTop = Part("CounterTop", scene, ProcArt.Rounded, wood, new Vector2(340f, -155f), new Vector2(310f, 32f), true);
            counterStuff = UIKit.Rect("CounterStuff", scene).gameObject;
            Part("Ball", counterStuff.transform, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(300f, -95f), new Vector2(100f, 100f));
            Part("Letter", counterStuff.transform, ProcArt.WitchItem("w_mektup"), Color.white, new Vector2(400f, -118f), new Vector2(70f, 70f));

            webs = UIKit.Rect("Webs", scene).gameObject;
            Part("Web", webs.transform, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(-430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(-30f, 420f), new Vector2(130f, 130f));
            Part("Web", webs.transform, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(420f, -100f), new Vector2(120f, 120f));

            bed = UIKit.Rect("Bed", scene).gameObject;
            Part("BedBase", bed.transform, ProcArt.Circle, new Color(0.7f, 0.25f, 0.3f), new Vector2(-330f, -350f), new Vector2(280f, 120f));
            Part("Cushion", bed.transform, ProcArt.Circle, new Color(0.95f, 0.65f, 0.6f), new Vector2(-330f, -345f), new Vector2(230f, 80f));
            karaPati = Part("KaraPati", scene, ProcArt.SideCat(0, 4), Color.white, new Vector2(-330f, -290f), new Vector2(190f, 190f));
            MakeCatTappable(karaPati);

            ghostLayer = UIKit.Rect("Ghosts", scene).At(Vector2.zero, scene.sizeDelta);
            dims[0] = Part("Dust", scene, ProcArt.Rounded, new Color(0.05f, 0.03f, 0.08f, 0.45f), Vector2.zero, new Vector2(1020f, 940f), true);
            return scene;
        }

        void MakeCatTappable(Image cat)
        {
            cat.raycastTarget = true;
            var catBtn = cat.gameObject.AddComponent<Button>();
            catBtn.transition = Selectable.Transition.None;
            catBtn.onClick.AddListener(() =>
            {
                Tween.Punch(cat.transform, 0.15f, 0.3f);
                if (shopArt && cat == karaPati && catMode == CatMode.Walk) CatSit(3f);
                string line = catLines[Random.Range(0, catLines.Length)];
                if (view >= 0 && view < chapterCatLines.Length && Random.value < 0.6f)
                    line = chapterCatLines[view][Random.Range(0, chapterCatLines[view].Length)];
                GM.Toast(L.F("Kara Pati: \"{0}\"", line));
            });
        }

        // ------------------------------------------------------------------ chapter 2: cellar
        RectTransform BuildCellar(RectTransform area)
        {
            var scene = NewScene("Cellar");
            var stone = new Color(0.17f, 0.16f, 0.23f);
            Part("Wall", scene, ProcArt.Rounded, stone, Vector2.zero, new Vector2(1020f, 940f), true);

            // stone blocks
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                float x = -440f + c * 112f + (r % 2 == 0 ? 0f : 56f);
                float y = 430f - r * 72f;
                if (x > 445f) continue;
                float v = 0.19f + ((c * 7 + r * 13) % 5) * 0.012f;
                Part("Stone", scene, ProcArt.Rounded, new Color(v, v * 0.95f, v * 1.3f, 0.9f), new Vector2(x, y), new Vector2(100f, 62f), true);
            }
            Part("Floor", scene, ProcArt.Rounded, new Color(0.12f, 0.11f, 0.16f), new Vector2(0f, -330f), new Vector2(1020f, 280f), true);
            for (int i = 0; i < 4; i++)
                Part("Crack", scene, null, new Color(0f, 0f, 0f, 0.25f), new Vector2(-380f + i * 250f, -300f - (i % 2) * 70f), new Vector2(150f, 3f));

            // stairs + door (top-left)
            for (int i = 0; i < 6; i++)
                Part("Step", scene, ProcArt.Rounded, new Color(0.27f, 0.25f, 0.33f), new Vector2(-440f + i * 22f, 230f - i * 78f), new Vector2(170f, 34f), true);
            doorLight = Part("DoorLight", scene, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.5f, 0.6f), new Vector2(-400f, 360f), new Vector2(360f, 360f));
            Part("Doorway", scene, ProcArt.Rounded, new Color(0.05f, 0.04f, 0.07f), new Vector2(-400f, 350f), new Vector2(150f, 180f), true);
            door = Part("Door", scene, ProcArt.Rounded, new Color(0.4f, 0.26f, 0.17f), new Vector2(-400f, 350f), new Vector2(150f, 180f), true);
            Part("Knob", door.transform, ProcArt.Circle, new Color(1f, 0.8f, 0.3f), new Vector2(46f, -10f), new Vector2(22f, 22f));

            // mirror
            mirrorGlow = Part("MirrorGlow", scene, ProcArt.GlowSprite, new Color(0.7f, 0.5f, 1f, 0.6f), new Vector2(-150f, 230f), new Vector2(380f, 420f));
            mirror = Part("Mirror", scene, ProcArt.Mirror(false), Color.white, new Vector2(-150f, 230f), new Vector2(200f, 280f));

            // portrait
            Part("Frame", scene, ProcArt.Rounded, new Color(0.6f, 0.42f, 0.22f), new Vector2(80f, 290f), new Vector2(200f, 240f), true);
            Part("Canvas", scene, ProcArt.Rounded, new Color(0.3f, 0.2f, 0.35f), new Vector2(80f, 290f), new Vector2(170f, 210f), true);
            portraitArt = UIKit.Rect("PortraitArt", scene).gameObject;
            Part("Hat", portraitArt.transform, ProcArt.WitchItem("w_sapka"), Color.white, new Vector2(60f, 330f), new Vector2(110f, 110f));
            Part("Cat", portraitArt.transform, ProcArt.SideCat(0, 4), Color.white, new Vector2(110f, 240f), new Vector2(100f, 100f));
            portraitDust = Part("Dust", scene, ProcArt.Rounded, new Color(0.42f, 0.4f, 0.42f, 0.95f), new Vector2(80f, 290f), new Vector2(170f, 210f), true);

            // jar shelf (right)
            jarTop = Part("JarShelfTop", scene, ProcArt.Rounded, new Color(0.38f, 0.26f, 0.18f), new Vector2(350f, 300f), new Vector2(280f, 26f), true);
            jarBottom = Part("JarShelfBottom", scene, ProcArt.Rounded, new Color(0.38f, 0.26f, 0.18f), new Vector2(350f, 170f), new Vector2(280f, 26f), true);
            jars = UIKit.Rect("Jars", scene).gameObject;
            for (int i = 0; i < 3; i++)
                Part("Jar", jars.transform, ProcArt.Bottle(i + 1), new Color(0.85f, 0.9f, 0.85f), new Vector2(270f + i * 85f, 352f), new Vector2(84f, 84f));
            for (int i = 0; i < 3; i++)
                Part("Jar", jars.transform, ProcArt.Bottle(i + 2), new Color(0.85f, 0.85f, 0.95f), new Vector2(280f + i * 80f, 222f), new Vector2(80f, 80f));

            // magic circle on the floor
            circleGlow = Part("CircleGlow", scene, ProcArt.GlowSprite, new Color(0.7f, 0.4f, 1f, 0.55f), new Vector2(40f, -390f), new Vector2(560f, 200f));
            circle = Part("Circle", scene, ProcArt.MagicCircle, new Color(0.6f, 0.6f, 0.65f, 0.3f), new Vector2(40f, -390f), new Vector2(440f, 130f));

            // desk (right, bottom)
            deskTop = Part("DeskTop", scene, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.2f), new Vector2(330f, -120f), new Vector2(320f, 30f), true);
            Part("LegL", scene, null, new Color(0.33f, 0.22f, 0.16f), new Vector2(200f, -200f), new Vector2(22f, 140f));
            Part("LegR", scene, null, new Color(0.33f, 0.22f, 0.16f), new Vector2(460f, -200f), new Vector2(22f, 140f));
            messyPapers = UIKit.Rect("MessyPapers", scene).gameObject;
            for (int i = 0; i < 6; i++)
            {
                var paper = Part("Paper", messyPapers.transform, null, new Color(0.92f, 0.88f, 0.78f), new Vector2(150f + i * 60f, -300f - (i % 3) * 40f), new Vector2(60f, 44f));
                paper.rectTransform.localEulerAngles = new Vector3(0f, 0f, -30f + i * 17f);
            }
            tidyPapers = UIKit.Rect("TidyPapers", scene).gameObject;
            Part("Stack", tidyPapers.transform, null, new Color(0.92f, 0.88f, 0.78f), new Vector2(260f, -92f), new Vector2(90f, 26f));
            Part("Book", tidyPapers.transform, ProcArt.WitchItem("w_kitap"), Color.white, new Vector2(400f, -80f), new Vector2(80f, 80f));

            // chained chest (centre)
            chestGlow = Part("ChestGlow", scene, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.4f, 0.55f), new Vector2(-110f, -220f), new Vector2(320f, 320f));
            chest = Part("Chest", scene, ProcArt.Classic(Chains.Sandik, 1), Color.white, new Vector2(-110f, -230f), new Vector2(190f, 190f));
            chains = UIKit.Rect("Chains", scene).gameObject;
            var ch1 = Part("ChainA", chains.transform, ProcArt.Chain, Color.white, new Vector2(-110f, -235f), new Vector2(200f, 60f));
            ch1.rectTransform.localEulerAngles = new Vector3(0f, 0f, 35f);
            var ch2 = Part("ChainB", chains.transform, ProcArt.Chain, Color.white, new Vector2(-110f, -235f), new Vector2(200f, 60f));
            ch2.rectTransform.localEulerAngles = new Vector3(0f, 0f, -35f);
            crate = UIKit.Rect("Crate", scene).gameObject;
            Part("Box", crate.transform, ProcArt.Rounded, new Color(0.3f, 0.22f, 0.16f), new Vector2(-330f, -250f), new Vector2(150f, 120f), true);

            // candles
            var candlePos = new[] { new Vector2(-250f, -330f), new Vector2(190f, -410f), new Vector2(330f, -60f), new Vector2(-30f, 130f) };
            foreach (var cp in candlePos)
            {
                candleGlows.Add(Part("CandleGlow", scene, ProcArt.GlowSprite, new Color(1f, 0.75f, 0.35f, 0.5f), cp + new Vector2(0f, 20f), new Vector2(260f, 260f)));
                candles.Add(Part("Candle", scene, ProcArt.Candle(false), Color.white, cp, new Vector2(110f, 124f)));
            }

            // a few permanent cobwebs for the mood
            Part("Web", scene, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.3f), new Vector2(250f, 420f), new Vector2(150f, 150f));
            Part("Web", scene, ProcArt.Classic(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.25f), new Vector2(-250f, 420f), new Vector2(120f, 120f));

            cellarCat = Part("KaraPati", scene, ProcArt.SideCat(0, 4), Color.white, new Vector2(-260f, -150f), new Vector2(150f, 150f));
            MakeCatTappable(cellarCat);

            dims[1] = Part("Dark", scene, ProcArt.Rounded, new Color(0.02f, 0.01f, 0.05f, 0.7f), Vector2.zero, new Vector2(1020f, 940f), true);
            return scene;
        }

        // ------------------------------------------------------------------ clues
        void BuildClues(int ch)
        {
            // clues sit above the darkness overlay so they can be spotted
            clueLayers[ch] = UIKit.Rect("Clues", scenes[ch]).At(Vector2.zero, new Vector2(1020f, 940f));
            foreach (var clue in Story.Clues)
            {
                if (clue.chapter != ch) continue;
                var c = clue;
                var img = Part("Clue_" + c.id, clueLayers[ch], ProcArt.Sparkle, new Color(1f, 0.95f, 0.75f, 0.3f), new Vector2(c.x, c.y), new Vector2(70f, 70f));
                img.raycastTarget = true;
                var b = img.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => OnClueTapped(c, img));
                clueSpots[c.id] = img;
            }
        }

        void OnClueTapped(Clue c, Image img)
        {
            if (GM.HasClue(c.id)) { GM.ShowDialog(c.title, c.text); return; }
            img.sprite = ProcArt.ClueIcon(c.id);
            img.color = Color.white;
            img.rectTransform.sizeDelta = new Vector2(90f, 90f);
            img.rectTransform.localEulerAngles = Vector3.zero;
            Tween.Pop(img.transform, 0.4f);
            FX.Burst(clueLayers[c.chapter], img.rectTransform.anchoredPosition, new Color(1f, 0.9f, 0.5f), 10);
            GM.FindClue(c);
        }

        bool ClueAvailable(Clue c) => GM.TasksDone >= c.afterTasks;

        void RefreshClues()
        {
            foreach (var c in Story.Clues)
            {
                if (!clueSpots.TryGetValue(c.id, out var img)) continue;
                bool found = GM.HasClue(c.id);
                bool avail = ClueAvailable(c);
                img.gameObject.SetActive(found || avail);
                if (found)
                {
                    img.sprite = ProcArt.ClueIcon(c.id);
                    img.color = c.id.EndsWith("_iz") ? new Color(0.6f, 0.9f, 1f, 0.8f) : Color.white;
                    img.rectTransform.sizeDelta = new Vector2(90f, 90f);
                    img.rectTransform.localEulerAngles = Vector3.zero;
                    img.rectTransform.localScale = Vector3.one;
                }
            }
            int found2 = 0;
            foreach (var c in Story.Clues) if (GM.HasClue(c.id)) found2++;
            int fresh = found2 - GM.CluesSeen;   // only clues not looked at yet
            caseBadge.gameObject.SetActive(fresh > 0);
            caseBadgeText.text = fresh.ToString();
        }

        public void RefreshUstaButton()
        {
            if (!ustaIcon || GM == null || GM.Cats == null) return;
            bool has = GM.Cats.HasAnyMaster;
            ustaIcon.sprite = ProcArt.SideCat(3, 4, has ? GM.Cats.MasterColor : KittenColors.Gece);
            ustaIcon.color = has ? Color.white : new Color(1f, 1f, 1f, 0.35f);   // no Master yet: a faded hint
            ustaBadge.gameObject.SetActive(has && GM.UstaNew);
        }

        // ------------------------------------------------------------------ settings menu
        RectTransform settings;
        Button musicBtn, fxBtn;
        readonly Button[] langBtns = new Button[L.Codes.Length];
        readonly Button[] themeBtns = new Button[Theme.Choices.Length];

        void BuildSettings()
        {
            var bg = UIKit.Img("SettingsMenu", sceneArea, ProcArt.Rounded, new Color(0.1f, 0.07f, 0.15f, 0.96f), true);
            bg.raycastTarget = true;
            settings = bg.rectTransform;
            settings.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-170f, -150f), new Vector2(380f, 556f));
            var title = UIKit.Txt("Title", settings, L.T("Ayarlar"), 32, UIKit.Orange, TextAnchor.MiddleCenter, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -8f), new Vector2(340f, 44f));
            musicBtn = UIKit.Btn("Music", settings, "", UIKit.Green, () => { Sfx.SetMusic(!Sfx.MusicOn); RefreshSettings(); }, 28);
            ((RectTransform)musicBtn.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -58f), new Vector2(340f, 66f));
            fxBtn = UIKit.Btn("Effects", settings, "", UIKit.Green, () => { Sfx.SetEffects(!Sfx.EffectsOn); RefreshSettings(); }, 28);
            ((RectTransform)fxBtn.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -134f), new Vector2(340f, 66f));

            var langTitle = UIKit.Txt("Lang", settings, L.T("Dil"), 28, UIKit.Cream, TextAnchor.MiddleCenter, false);
            langTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -212f), new Vector2(340f, 40f));
            for (int i = 0; i < L.Codes.Length; i++)
            {
                string code = L.Codes[i];
                // each language is written in its own language, so a player who can't read the current one still finds theirs
                langBtns[i] = UIKit.Btn("Lang_" + code, settings, L.Names[i], UIKit.Disabled, () => GM.ChangeLanguage(code), 28);
                float w = 340f / L.Codes.Length - 8f;
                ((RectTransform)langBtns[i].transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f))
                    .At(new Vector2(-170f + w * 0.5f + 4f + i * (w + 8f), -254f), new Vector2(w, 66f));
            }

            var themeTitle = UIKit.Txt("Theme", settings, L.T("Tema"), 28, UIKit.Cream, TextAnchor.MiddleCenter, false);
            themeTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -332f), new Vector2(340f, 40f));
            for (int i = 0; i < Theme.Choices.Length; i++)
            {
                var id = Theme.Choices[i];
                themeBtns[i] = UIKit.Btn("Theme_" + id, settings, Theme.Name(id), UIKit.Disabled, () => GM.ChangeTheme(id), 28);
                // two per row: light themes on top, dark ones below
                const float w = 166f;
                int col = i % 2, row = i / 2;
                ((RectTransform)themeBtns[i].transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f))
                    .At(new Vector2(-170f + w * 0.5f + 4f + col * (w + 8f), -374f - row * 74f), new Vector2(w, 66f));
            }
            settings.gameObject.SetActive(false);
        }

        void ToggleSettings()
        {
            bool open = !settings.gameObject.activeSelf;
            settings.gameObject.SetActive(open);
            if (open) { settings.SetAsLastSibling(); RefreshSettings(); Tween.Pop(settings, 0.2f); }
        }

        void RefreshSettings()
        {
            musicBtn.Label().text = L.F("Müzik: {0}", Sfx.MusicOn ? L.T("Açık") : L.T("Kapalı"));
            musicBtn.GetComponent<Image>().color = Theme.Ui(Sfx.MusicOn ? UIKit.Green : UIKit.Disabled);
            fxBtn.Label().text = L.F("Efektler: {0}", Sfx.EffectsOn ? L.T("Açık") : L.T("Kapalı"));
            fxBtn.GetComponent<Image>().color = Theme.Ui(Sfx.EffectsOn ? UIKit.Green : UIKit.Disabled);
            for (int i = 0; i < langBtns.Length; i++)
                langBtns[i].GetComponent<Image>().color = Theme.Ui(L.Codes[i] == L.Lang ? UIKit.Orange : UIKit.Disabled);
            for (int i = 0; i < themeBtns.Length; i++)
                themeBtns[i].GetComponent<Image>().color = Theme.Ui(Theme.Choices[i] == Theme.Current ? UIKit.Orange : UIKit.Disabled);
        }

        void UseEye()
        {
            int waiting = 0;
            foreach (var c in Story.Clues)
                if (c.chapter == view && ClueAvailable(c) && !GM.HasClue(c.id)) waiting++;
            if (waiting == 0)
            {
                bool more = false;
                foreach (var c in Story.Clues) if (c.chapter == view && !ClueAvailable(c)) more = true;
                GM.Toast(more ? L.T("Şimdilik gizli bir şey yok. Görevleri ilerlet, yenileri belirir.") : L.T("Buradaki bütün ipuçlarını buldun!"));
                return;
            }
            eyeUntil = Time.time + 4f;
            GM.Toast(L.F("Parlayan yerlere dokun! ({0} ipucu)", waiting), UIKit.Orange);
        }

        // ------------------------------------------------------------------ state
        public void ShowChapter(int ch, bool fromButton = false)
        {
            int current = Story.CurrentChapter(GM.TasksDone);
            ch = Mathf.Clamp(ch, 0, Story.Chapters.Length - 1);
            if (ch > current)
            {
                if (fromButton) GM.Toast(L.F("Önce {0} bitmeli.", Story.Chapters[current].name));
                return;
            }
            bool changed = ch != view;
            view = ch;
            if (changed && fromButton) Tween.Pop(scenes[ch], 0.3f);
            Refresh(false);
        }

        /// <summary>Curtain + big title when a new chapter opens.</summary>
        public void PlayChapterIntro(int ch)
        {
            if (ch < 0 || ch >= Story.Chapters.Length) return;
            var curtain = UIKit.Img("ChapterCurtain", sceneArea, ProcArt.Rounded, new Color(0.04f, 0.02f, 0.08f, 0f), true);
            curtain.rectTransform.At(Vector2.zero, new Vector2(1020f, 940f));
            curtain.raycastTarget = true;
            var glow = UIKit.Img("Glow", curtain.transform, ProcArt.GlowSprite, new Color(1f, 0.66f, 0.3f, 0f));
            glow.rectTransform.At(new Vector2(0f, 20f), new Vector2(900f, 500f));
            string full = Story.Chapters[ch].name;
            int colon = full.IndexOf(':');
            var small = UIKit.TxtRaw("Num", curtain.transform, colon > 0 ? full.Substring(0, colon) : "", 44, new Color(1f, 0.96f, 0.88f, 0f));
            small.rectTransform.At(new Vector2(0f, 90f), new Vector2(900f, 60f));
            var big = UIKit.TxtRaw("Name", curtain.transform, colon > 0 ? full.Substring(colon + 1).Trim() : full, 76, new Color(1f, 0.66f, 0.3f, 0f));
            big.fontStyle = FontStyle.Bold;
            big.rectTransform.At(new Vector2(0f, 0f), new Vector2(980f, 110f));
            var star = UIKit.Img("Star", curtain.transform, ProcArt.Sparkle, new Color(1f, 0.95f, 0.7f, 0f));
            star.rectTransform.At(new Vector2(0f, -100f), new Vector2(80f, 80f));

            System.Action<float> setA = a =>
            {
                if (!curtain) return;
                curtain.color = new Color(0.04f, 0.02f, 0.08f, 0.85f * a);
                glow.color = new Color(1f, 0.66f, 0.3f, 0.45f * a);
                small.color = new Color(1f, 0.96f, 0.88f, a);
                big.color = new Color(1f, 0.66f, 0.3f, a);
                star.color = new Color(1f, 0.95f, 0.7f, a);
            };
            setA(0f);
            Tween.Run(0.45f, t =>
            {
                setA(t);
                if (big) big.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Tween.OutBack(t));
                if (star) star.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 180f);
            }, () =>
            {
                Tween.Run(1.4f, t => { if (star) star.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f + t * 120f); }, () =>
                {
                    Tween.Run(0.6f, t => setA(1f - t), () => { if (curtain) Destroy(curtain.gameObject); });
                });
            });
        }

        // ------------------------------------------------------------------ chapter gifts
        void RefreshGifts()
        {
            for (int g = 0; g < gifts.Length; g++)
            {
                bool claimed = GM.GiftClaimed(view, g);
                bool ready = GM.GiftReady(view, g);
                gifts[g].color = claimed ? new Color(1f, 1f, 1f, 0.55f) : ready ? Color.white : new Color(0.82f, 0.78f, 0.74f, 1f);
                giftChecks[g].gameObject.SetActive(claimed);
                giftGlows[g].gameObject.SetActive(ready);
                if (!ready) gifts[g].rectTransform.localScale = Vector3.one;
            }
        }

        void OnGiftTapped(int g)
        {
            if (GM.GiftClaimed(view, g)) { GM.Toast(L.T("Bu sandığı zaten açtın.")); return; }
            if (GM.GiftReady(view, g))
            {
                FX.Burst((RectTransform)gifts[g].transform.parent, gifts[g].rectTransform.anchoredPosition, UIKit.Orange, 14);
                GM.ClaimChapterGift(view, g);
                Refresh(false);
                return;
            }
            int left = ChapterGifts.TasksNeeded(view, g) - Mathf.Clamp(GM.TasksDone - Story.ChapterStart(view), 0, 999);
            Tween.Punch(gifts[g].transform, 0.12f, 0.25f);
            GM.Toast(L.F("Bu sandık için {0} görev daha yap. İçinde: {1}", left, ChapterGifts.Text(view, g)));
        }

        public void Refresh(bool animate)
        {
            int done = GM.TasksDone;
            int current = Story.CurrentChapter(done);
            if (view < 0 || view > current) view = current;   // e.g. after a reset the old chapter is locked again

            for (int i = 0; i < scenes.Length; i++) scenes[i].gameObject.SetActive(i == view);

            var chapter = Story.Chapters[view];
            int start = Story.ChapterStart(view);
            int inChapter = Mathf.Clamp(done - start, 0, chapter.tasks.Length);
            chapterTitle.text = chapter.name;
            progressText.text = inChapter + "/" + chapter.tasks.Length;
            float fw = Mathf.Max(24f, ProgressW * inChapter / chapter.tasks.Length);
            if (animate)
            {
                float from = progressFill.sizeDelta.x;
                var pf = progressFill;
                Tween.Run(0.5f, t => { if (pf) pf.sizeDelta = new Vector2(Mathf.Lerp(from, fw, Tween.OutCubic(t)), 24f); });
            }
            else progressFill.sizeDelta = new Vector2(fw, 24f);

            RefreshUstaButton();
            prevBtn.gameObject.SetActive(view > 0);
            bool hasNext = view < Story.Chapters.Length - 1;
            nextBtn.gameObject.SetActive(hasNext);
            nextLock.gameObject.SetActive(hasNext && view + 1 > current);
            RefreshGifts();

            bool justDid = animate && shownDone >= 0 && done > shownDone;
            ApplyShop(done);
            ApplyCellar(done);
            for (int i = 2; i < extra.Length; i++) extra[i]?.Apply(done, i);
            if (justDid) HighlightTask(Story.TaskAt(done - 1).id);
            shownDone = done;

            BuildTasks();
            RefreshGhosts();
            RefreshClues();
            albumBadge.gameObject.SetActive(GM.AnyClaimableSet);
            dailyBadge.gameObject.SetActive(GM.DailyAvailable);
            chaptersBadge.gameObject.SetActive(GM.AnyGiftReady);
            towerBadge.gameObject.SetActive(GM.CatNeedsCare);
            bool room = GM.GameRoomUnlocked;
            gameIcon.color = room ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            gameLock.gameObject.SetActive(!room);
            gameBadge.gameObject.SetActive(GM.AnyMiniFree);
            friendBadge.gameObject.SetActive(GM.AnyFriendNew);
            achBadge.gameObject.SetActive(GM.AnyAchReady);
            GM.CheckIntros();   // may introduce a feature that just opened (and start its "YENİ" tag)
            gameNew.gameObject.SetActive(GM.GameRoomNew);
            friendNew.gameObject.SetActive(GM.FriendBookNew);
        }

        bool Done(string id, int done) => Story.IsDone(id, done);

        void ApplyShop(int done)
        {
            webs.SetActive(!Done("ag", done));
            dirt.SetActive(!Done("pencere", done));

            bool shelves = Done("raf", done);
            plankTop.rectTransform.localEulerAngles = new Vector3(0, 0, shelves ? 0f : -14f);
            plankBottom.rectTransform.localEulerAngles = new Vector3(0, 0, shelves ? 0f : 10f);
            bottles.SetActive(shelves);

            bool fire = Done("kazan", done);
            cauldron.color = fire || shopArt ? Color.white : new Color(0.5f, 0.5f, 0.56f);   // the drawn pot brews even before its fire
            cauldronGlow.gameObject.SetActive(fire);

            rug.SetActive(Done("hali", done));

            bool lit = Done("fener", done);
            lanternA.sprite = ProcArt.Lantern(lit);
            lanternB.sprite = ProcArt.Lantern(lit);
            glowA.gameObject.SetActive(lit);
            glowB.gameObject.SetActive(lit);

            bool counter = Done("tezgah", done);
            counterTop.rectTransform.localEulerAngles = new Vector3(0, 0, counter ? 0f : 12f);
            counterBody.color = shopArt ? (counter ? Color.white : new Color(0.72f, 0.7f, 0.7f))
                : counter ? new Color(0.42f, 0.28f, 0.2f) : new Color(0.3f, 0.2f, 0.16f);
            counterStuff.SetActive(counter);

            bool hasBed = Done("yatak", done);
            bed.SetActive(hasBed);
            if (shopArt)
            {
                // the drawn shop's cat walks about on its own (TickShopCat); here only where it starts
                shopHasBed = hasBed;
                if (!catStarted || (!hasBed && catMode == CatMode.Sleep))
                {
                    catStarted = true;
                    catX = BedX;
                    if (hasBed) CatSleep(); else CatSit(2f);
                }
            }
            else
            {
            karaPati.sprite = ProcArt.SideCat(0, hasBed ? 5 : 4);
            karaPati.rectTransform.anchoredPosition = hasBed ? new Vector2(-330f, -318f) : new Vector2(-330f, -290f);
            }

            int inCh = Mathf.Clamp(done, 0, Story.Chapters[0].tasks.Length);
            dims[0].color = new Color(0.05f, 0.03f, 0.08f, 0.3f * (1f - inCh / (float)Story.Chapters[0].tasks.Length));
        }

        void ApplyCellar(int done)
        {
            bool open = Done("kapi", done);
            door.rectTransform.anchoredPosition = open ? new Vector2(-480f, 350f) : new Vector2(-400f, 350f);
            door.rectTransform.sizeDelta = open ? new Vector2(40f, 180f) : new Vector2(150f, 180f);
            doorLight.gameObject.SetActive(open);

            bool lit = Done("mum", done);
            for (int i = 0; i < candles.Count; i++)
            {
                candles[i].sprite = ProcArt.Candle(lit);
                candleGlows[i].gameObject.SetActive(lit);
            }

            bool jarsOk = Done("kavanoz", done);
            jarTop.rectTransform.localEulerAngles = new Vector3(0, 0, jarsOk ? 0f : 12f);
            jarBottom.rectTransform.localEulerAngles = new Vector3(0, 0, jarsOk ? 0f : -9f);
            jars.SetActive(jarsOk);

            bool desk = Done("masa", done);
            messyPapers.SetActive(!desk);
            tidyPapers.SetActive(desk);

            bool chestOpen = Done("zincir", done);
            chains.SetActive(!chestOpen);
            chestGlow.gameObject.SetActive(chestOpen);

            bool circ = Done("daire", done);
            circle.color = circ ? new Color(0.85f, 0.6f, 1f, 0.95f) : new Color(0.6f, 0.6f, 0.65f, 0.3f);
            circleGlow.gameObject.SetActive(circ);

            portraitDust.gameObject.SetActive(!Done("portre", done));

            bool mirrorOk = Done("ayna", done);
            mirror.sprite = ProcArt.Mirror(mirrorOk);
            mirrorGlow.gameObject.SetActive(mirrorOk);

            int start = Story.ChapterStart(1);
            int inCh = Mathf.Clamp(done - start, 0, Story.Chapters[1].tasks.Length);
            // still a cellar: it never gets fully bright
            dims[1].color = new Color(0.02f, 0.01f, 0.05f, 0.15f + 0.55f * (1f - inCh / (float)Story.Chapters[1].tasks.Length));
        }

        Transform TaskTarget(string id)
        {
            switch (id)
            {
                case "pencere": return scenes[0].Find("Window") ?? scenes[0].Find("Moon");
                case "raf": return plankTop.transform;
                case "kazan": return cauldron.transform;
                case "hali": return rug.transform.GetChild(0);
                case "fener": return lanternA.transform;
                case "tezgah": return counterTop.transform;
                case "yatak": return karaPati.transform;
                case "kapi": return door.transform;
                case "mum": return candles[0].transform;
                case "kavanoz": return jarTop.transform;
                case "masa": return deskTop.transform;
                case "zincir": return chest.transform;
                case "daire": return circle.transform;
                case "portre": return portraitArt.transform.GetChild(0);
                case "ayna": return mirror.transform;
                default:
                    foreach (var e in extra)
                        if (e != null && e.targets.TryGetValue(id, out var tr)) return tr;
                    return null;
            }
        }

        void HighlightTask(string id)
        {
            var target = TaskTarget(id);
            var scene = scenes[view];
            Vector2 local = new Vector2(0f, 380f);
            if (target)
            {
                Tween.Pop(target, 0.4f);
                local = scene.InverseTransformPoint(target.position);
            }
            FX.Burst(scene, local, UIKit.Orange, 12);
        }

        static Sprite Or(Sprite a, Sprite b) => a ? a : b;

        Sprite TaskIcon(string id)
        {
            switch (id)
            {
                case "ag": return Or(ProcArt.Room("shop_web"), ProcArt.Classic(Chains.Ag, 1));
                case "pencere": return Or(ProcArt.Room("shop_window"), ProcArt.Moon);
                case "raf": return ProcArt.Bottle(0);
                case "kazan": return ProcArt.Item(Chains.KaraKazan, 1);
                case "hali": return Or(ProcArt.Room("shop_rug"), ProcArt.Classic(Chains.Ag, 4));
                case "fener": return ProcArt.Lantern(true);
                case "tezgah": return ProcArt.WitchItem("w_kure");
                case "yatak": return ProcArt.SideCat(0, 5);
                case "kapi": return ProcArt.Lock;
                case "mum": return ProcArt.Candle(true);
                case "kavanoz": return ProcArt.Bottle(2);
                case "masa": return ProcArt.WitchItem("w_mektup");
                case "zincir": return ProcArt.Chain;
                case "daire": return ProcArt.MagicCircle;
                case "portre": return ProcArt.WitchItem("w_sapka");
                case "ayna": return ProcArt.Mirror(true);
                default:
                    foreach (var e in extra)
                        if (e != null && e.icons.TryGetValue(id, out var sp)) return sp;
                    return ProcArt.Sparkle;
            }
        }

        void BuildTasks()
        {
            for (int i = taskList.childCount - 1; i >= 0; i--) Destroy(taskList.GetChild(i).gameObject);
            int done = GM.TasksDone;
            var chapter = Story.Chapters[view];
            int start = Story.ChapterStart(view);
            int inChapter = Mathf.Max(0, done - start);

            if (inChapter >= chapter.tasks.Length)
            {
                var row = Row(0);
                bool last = view == Story.Chapters.Length - 1;
                string msg = last ? L.T("Hikâye tamamlandı! Cadı Hanım evinde.\nYeni maceralar yakında...") : L.T("Bölüm tamamlandı!\nSıradaki bölüm için > düğmesine bas.");
                var txt = UIKit.Txt("Done", row, msg, 34, UIKit.Cream);
                UIKit.Stretch(txt.rectTransform, 20f, 20f);
                return;
            }

            for (int k = 0; k < 3 && inChapter + k < chapter.tasks.Length; k++)
            {
                int idx = start + inChapter + k;
                var task = chapter.tasks[inChapter + k];
                bool current = k == 0;
                bool affordable = GM.Stamps >= task.cost;
                var row = Row(k);
                if (!current) row.GetComponent<Image>().color = Theme.Dark ? Theme.Ui(new Color(0.13f, 0.09f, 0.19f, 0.6f)) : Theme.Ui(new Color(0.2f, 0.14f, 0.29f, 0.55f));

                var icon = UIKit.Img("Icon", row, TaskIcon(task.id), current ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 0f), new Vector2(94f, 94f));
                if (task.id == "daire") icon.color = current ? new Color(0.85f, 0.6f, 1f) : new Color(0.85f, 0.6f, 1f, 0.45f);

                var title = UIKit.Txt("Title", row, task.title, 32, current ? UIKit.Cream : new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);
                title.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(124f, 0f), new Vector2(480f, 100f));

                var stamp = UIKit.Img("Stamp", row, ProcArt.Stamp, current ? Color.white : new Color(1f, 1f, 1f, 0.5f));
                stamp.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-268f, 0f), new Vector2(56f, 56f));
                var cost = UIKit.Txt("Cost", row, task.cost.ToString(), 36, current ? UIKit.Cream : new Color(1f, 1f, 1f, 0.5f));
                cost.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-222f, 0f), new Vector2(50f, 50f));

                if (current)
                {
                    var btn = UIKit.Btn("Do", row, L.T("Yap"), affordable ? UIKit.Green : UIKit.Disabled, () => GM.DoTask(idx), 38);
                    ((RectTransform)btn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(180f, 90f));
                }
                else
                {
                    var wait = UIKit.Txt("Next", row, L.T("Sırada"), 28, new Color(1f, 1f, 1f, 0.45f));
                    wait.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(180f, 90f));
                }
            }
        }

        RectTransform Row(int k)
        {
            var img = UIKit.Img("Row", taskList, ProcArt.Rounded, UIKit.PanelDark, true);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -k * 142f);
            rt.sizeDelta = new Vector2(0f, 128f);
            return rt;
        }

        // ghosts you collected as stickers come to visit the shop
        void RefreshGhosts()
        {
            foreach (var g in floatingGhosts) if (g) Destroy(g.gameObject);
            floatingGhosts.Clear();
            for (int i = 0; i < Ghosts.Names.Length; i++)
            {
                if (!GM.HasSticker("g" + i)) continue;
                var img = UIKit.Img("Ghost", ghostLayer, ProcArt.Ghost(i), new Color(1f, 1f, 1f, 0.8f));
                var rt = img.rectTransform;
                rt.At(new Vector2(-420f + (i % 4) * 240f + Random.Range(-30f, 30f), (i < 4 ? 40f : -80f) + Random.Range(-20f, 20f)), new Vector2(70f, 70f));
                floatingGhosts.Add(rt);
            }
        }

        // ------------------------------------------------------------------ shop life
        /// <summary>
        /// The shop cauldron is always brewing: its surface boils with little blobs, bubbles wobble up and pop, and
        /// steam drifts off. Lighting its fire (the story task) turns it up: flames flicker under the pot, it rocks and
        /// heaves, the glow pulses, bubbles come thick and fast with gold stars among them, and drops splash out.
        /// </summary>
        void TickCauldron(float dt, float t)
        {
            bool lit = cauldronGlow && cauldronGlow.gameObject.activeSelf;
            const float Surface = -207f;   // the brew line in the pot picture
            if (cauldron)
            {
                float heave = lit ? 0.022f : 0.01f;
                cauldron.rectTransform.localScale = new Vector3(1f + Mathf.Sin(t * 3.4f) * heave, 1f - Mathf.Sin(t * 3.4f) * heave, 1f);
                cauldron.rectTransform.localEulerAngles = new Vector3(0f, 0f, lit ? Mathf.Sin(t * 2.1f) * 1.6f : 0f);
            }
            if (lit && cauldronGlow)
                cauldronGlow.color = new Color(1f, 0.5f, 0.8f, 0.42f + Mathf.Sin(t * 2.4f) * 0.12f);
            for (int i = 0; i < flames.Count; i++)
            {
                var fl = flames[i];
                if (!fl) continue;
                if (fl.gameObject.activeSelf != lit) fl.gameObject.SetActive(lit);
                if (!lit) continue;
                fl.rectTransform.localScale = new Vector3(1f + Mathf.Sin(t * 13f + i * 1.7f) * 0.1f, 1f + Mathf.Sin(t * 17f + i * 2.3f) * 0.2f, 1f);
                fl.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 9f + i) * 8f);
            }
            if (!bubbleLayer) return;

            // blobs swelling and bursting on the brew surface
            if (t >= nextBlob)
            {
                nextBlob = t + (lit ? Random.Range(0.08f, 0.18f) : Random.Range(0.25f, 0.45f));
                var b = Spawn(Fx.Blob, ProcArt.RoundInk, Random.value < 0.5f ? new Color(1f, 0.55f, 0.82f) : new Color(0.92f, 0.62f, 1f));
                b.life = Random.Range(0.45f, 0.8f);
                b.x = Random.Range(-62f, 62f);
                b.size = Random.Range(18f, 34f);
            }
            // bubbles rising out of the pot (and gold stars once it is lit)
            if (t >= nextBubble)
            {
                nextBubble = t + (lit ? Random.Range(0.1f, 0.22f) : Random.Range(0.3f, 0.55f));
                bool star = lit && Random.value < 0.2f;
                var b = Spawn(star ? Fx.Spark : Fx.Bubble, star ? ProcArt.Sparkle : ProcArt.RoundInk,
                    star ? new Color(1f, 0.9f, 0.5f) : Random.value < 0.5f ? new Color(1f, 0.62f, 0.86f) : new Color(0.86f, 0.7f, 1f));
                b.life = Random.Range(1.4f, 2.3f);
                b.rise = lit ? Random.Range(190f, 260f) : Random.Range(120f, 180f);
                b.x = Random.Range(-58f, 58f);
                b.size = star ? Random.Range(24f, 36f) : Random.Range(16f, lit ? 36f : 28f);
            }
            // steam puffs
            if (t >= nextSteam)
            {
                nextSteam = t + (lit ? Random.Range(0.5f, 0.9f) : Random.Range(1.1f, 1.8f));
                var b = Spawn(Fx.Steam, ProcArt.Circle, new Color(1f, 0.9f, 0.98f, 0.25f));
                b.life = Random.Range(2.2f, 3.2f);
                b.rise = Random.Range(240f, 320f);
                b.x = Random.Range(-45f, 45f);
                b.size = Random.Range(55f, 90f);
            }
            // drops splashing out over the rim
            if (lit && t >= nextSplash)
            {
                nextSplash = t + Random.Range(0.6f, 1.3f);
                int n = Random.Range(2, 4);
                float x0 = Random.Range(-40f, 40f);
                for (int i = 0; i < n; i++)
                {
                    var b = Spawn(Fx.Drop, ProcArt.RoundInk, new Color(1f, 0.55f, 0.82f));
                    b.life = 1.2f;
                    b.x = x0;
                    b.rise = Surface;
                    b.vx = Random.Range(-170f, 170f);
                    b.vy = Random.Range(260f, 380f);
                    b.size = Random.Range(12f, 18f);
                }
            }

            foreach (var b in bubbles)
            {
                if (!b.img.gameObject.activeSelf) continue;
                b.age += dt;
                float k = b.age / b.life;
                var rt = b.img.rectTransform;
                var c = b.img.color;
                switch (b.kind)
                {
                    case Fx.Blob:
                        if (k >= 1f) { b.img.gameObject.SetActive(false); continue; }
                        // a flattened bump that swells up from the surface and bursts
                        float sw = k < 0.75f ? Mathf.Sin(k / 0.75f * Mathf.PI * 0.5f) : Mathf.Lerp(1f, 1.4f, (k - 0.75f) / 0.25f);
                        rt.sizeDelta = new Vector2(b.size * sw, b.size * 0.7f * sw);
                        rt.anchoredPosition = new Vector2(b.x, Surface + b.size * 0.2f * sw);
                        c.a = k > 0.75f ? Mathf.Lerp(1f, 0f, (k - 0.75f) / 0.25f) : 1f;
                        break;
                    case Fx.Drop:
                        b.vy -= 900f * dt;
                        b.x += b.vx * dt;
                        b.rise += b.vy * dt;
                        // gone once it falls back below the rim
                        if (k >= 1f || (b.vy < 0f && b.rise < Surface - 10f)) { b.img.gameObject.SetActive(false); continue; }
                        rt.anchoredPosition = new Vector2(b.x, b.rise);
                        rt.sizeDelta = new Vector2(b.size, b.size * (1f + Mathf.Abs(b.vy) / 900f));
                        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(b.vy, b.vx) * Mathf.Rad2Deg - 90f);
                        c.a = 1f;
                        break;
                    default:
                        if (k >= 1f) { b.img.gameObject.SetActive(false); continue; }
                        bool steam = b.kind == Fx.Steam;
                        rt.anchoredPosition = new Vector2(b.x + Mathf.Sin(b.age * (steam ? 1.6f : 4f) + b.phase) * (steam ? 24f : 14f), Surface + k * b.rise);
                        float s = steam ? Mathf.Lerp(0.5f, 1.7f, k)
                            : k < 0.15f ? Mathf.Lerp(0.3f, 1f, k / 0.15f) : k > 0.85f ? Mathf.Lerp(1f, 1.4f, (k - 0.85f) / 0.15f) : 1f;
                        rt.sizeDelta = Vector2.one * b.size * s;
                        if (b.kind == Fx.Spark) rt.localEulerAngles = new Vector3(0f, 0f, b.age * 120f);
                        c.a = steam ? 0.25f * Mathf.Sin(k * Mathf.PI) : k > 0.75f ? Mathf.Lerp(1f, 0f, (k - 0.75f) / 0.25f) : 1f;
                        break;
                }
                b.img.color = c;
            }
        }

        /// <summary>A free effect image from the pool (or a new one), reset and shown.</summary>
        Bubble Spawn(Fx kind, Sprite sprite, Color color)
        {
            var b = bubbles.Find(x => !x.img.gameObject.activeSelf);
            if (b == null)
            {
                b = new Bubble { img = UIKit.ImgArt("Fx", bubbleLayer, sprite, color) };
                bubbles.Add(b);
            }
            b.kind = kind;
            b.img.sprite = sprite;
            b.img.color = color;
            b.age = 0f;
            b.phase = Random.Range(0f, 6.3f);
            b.img.rectTransform.localEulerAngles = Vector3.zero;
            // steam drifts behind the bubbles
            if (kind == Fx.Steam) b.img.transform.SetAsFirstSibling(); else b.img.transform.SetAsLastSibling();
            b.img.gameObject.SetActive(true);
            return b;
        }

        void CatSit(float seconds)
        {
            catMode = CatMode.Sit;
            catUntil = Time.time + seconds;
            karaPati.sprite = ProcArt.SideCat(0, 4);
            karaPati.rectTransform.anchoredPosition = new Vector2(catX, CatFloorY);
        }

        void CatSleep()
        {
            catMode = CatMode.Sleep;
            catUntil = Time.time + Random.Range(9f, 16f);
            catX = BedX;
            catFace = 1;
            karaPati.sprite = ProcArt.SideCat(0, 5);
            karaPati.rectTransform.anchoredPosition = new Vector2(BedX, BedSleepY);
        }

        /// <summary>Kara Pati sits a while, then trots to another spot on the floor; once she has a bed she
        /// sometimes goes to sleep in it and wakes up again later.</summary>
        void TickShopCat(float dt, float t)
        {
            var rt = karaPati.rectTransform;
            if (catMode == CatMode.Walk)
            {
                catWalkClock += dt;
                float step = CatSpeed * dt;
                if (Mathf.Abs(catTargetX - catX) <= step)
                {
                    catX = catTargetX;
                    if (catToBed) CatSleep(); else CatSit(Random.Range(2.5f, 6f));
                }
                else
                {
                    catX += Mathf.Sign(catTargetX - catX) * step;
                    karaPati.sprite = ProcArt.SideCat(0, (int)(catWalkClock / 0.13f) % 4);
                    rt.anchoredPosition = new Vector2(catX, CatFloorY + Mathf.Abs(Mathf.Sin(catWalkClock * 12f)) * 4f);
                }
            }
            else if (Time.time >= catUntil)
            {
                if (catMode == CatMode.Sleep)
                {
                    // wakes up and sits beside the bed first
                    rt.localScale = Vector3.one;
                    catX = BedX + 40f;
                    CatSit(2f);
                }
                else
                {
                    catToBed = shopHasBed && Random.value < 0.3f;
                    catTargetX = catToBed ? BedX : Random.Range(-400f, 400f);
                    if (!catToBed && Mathf.Abs(catTargetX - catX) < 140f)
                        catTargetX = Mathf.Clamp(catX + (catX < 0f ? 1f : -1f) * Random.Range(160f, 360f), -400f, 400f);
                    catFace = catTargetX >= catX ? 1 : -1;
                    catWalkClock = 0f;
                    catMode = CatMode.Walk;
                }
            }

            // the drawings face right; walking left turns the picture round (scale is left free for the tap bounce)
            rt.localEulerAngles = new Vector3(0f, catMode == CatMode.Sleep || catFace > 0 ? 0f : 180f, 0f);
            // a sleeping cat breathes
            if (catMode == CatMode.Sleep) rt.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < floatingGhosts.Count; i++)
            {
                var g = floatingGhosts[i];
                if (!g) continue;
                g.anchoredPosition += new Vector2(Mathf.Sin(t * 0.7f + i) * 0.4f, Mathf.Sin(t * 2f + i * 1.3f) * 0.35f);
            }
            if (cauldronGlow && cauldronGlow.gameObject.activeSelf)
                cauldronGlow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.05f);
            for (int i = 0; i < candleGlows.Count; i++)
                if (candleGlows[i].gameObject.activeSelf)
                    candleGlows[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 7f + i * 2f) * 0.06f + Mathf.Sin(t * 13f + i) * 0.03f);
            if (circleGlow && circleGlow.gameObject.activeSelf)
                circleGlow.color = new Color(0.7f, 0.4f, 1f, 0.45f + Mathf.Sin(t * 2f) * 0.12f);
            float pulse = 1f + Mathf.Sin(t * 4f) * 0.08f;
            if (gameNew && gameNew.gameObject.activeSelf) gameNew.localScale = Vector3.one * pulse;
            if (friendNew && friendNew.gameObject.activeSelf) friendNew.localScale = Vector3.one * pulse;
            if (!shopArt && karaPati && GM != null && Story.IsDone("yatak", GM.TasksDone))
                karaPati.rectTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);

            if (shopArt && view == 0 && karaPati && karaPati.gameObject.activeInHierarchy)
            {
                TickCauldron(Time.deltaTime, t);
                TickShopCat(Time.deltaTime, t);
            }

            if (view >= 0 && view < extra.Length && extra[view] != null) extra[view].tick?.Invoke(t);

            // ready gifts hop up and down so they get noticed
            for (int g = 0; g < gifts.Length; g++)
                if (gifts[g] && giftGlows[g].gameObject.activeSelf)
                {
                    gifts[g].rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(t * 4f + g)) * 0.18f);
                    giftGlows[g].rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 40f);
                }

            // clue twinkle; the eye makes waiting clues pulse brightly
            bool eye = t < eyeUntil;
            foreach (var kv in clueSpots)
            {
                var img = kv.Value;
                if (!img || !img.gameObject.activeInHierarchy || GM == null || GM.HasClue(kv.Key)) continue;
                float a = eye ? 0.75f + Mathf.Sin(t * 8f) * 0.25f : 0.18f + Mathf.Sin(t * 2.5f + img.rectTransform.anchoredPosition.x) * 0.1f;
                img.color = new Color(1f, 0.95f, 0.7f, a);
                float sc = eye ? 1.3f + Mathf.Sin(t * 8f) * 0.15f : 1f;
                img.rectTransform.localScale = Vector3.one * sc;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 40f);
            }
        }
    }
}
