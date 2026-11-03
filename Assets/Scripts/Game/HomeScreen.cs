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

        RectTransform sceneArea;
        readonly RectTransform[] scenes = new RectTransform[Story.Chapters.Length];
        readonly Image[] dims = new Image[Story.Chapters.Length];
        readonly RectTransform[] clueLayers = new RectTransform[Story.Chapters.Length];
        readonly ChapterScene[] extra = new ChapterScene[Story.Chapters.Length];
        static readonly string[] sceneNames = { "Shop", "Cellar", "Garden", "Town", "Tower", "Realm" };
        RectTransform ghostLayer;

        // shop (chapter 1)
        GameObject webs, dirt, rug, bottles, counterStuff, bed;
        Image cauldron, cauldronGlow, lanternA, lanternB, glowA, glowB, plankTop, plankBottom, counterTop, counterBody, karaPati;

        // cellar (chapter 2)
        Image door, doorLight, jarTop, jarBottom, circle, circleGlow, portraitDust, mirror, mirrorGlow, chest, chestGlow, cellarCat, deskTop;
        GameObject jars, messyPapers, tidyPapers, chains, portraitArt, crate;
        readonly List<Image> candles = new List<Image>();
        readonly List<Image> candleGlows = new List<Image>();

        RectTransform taskList;
        Image albumBadge, caseBadge, dailyBadge, chaptersBadge, towerBadge, ustaBadge, ustaIcon;
        Text caseBadgeText;
        readonly List<RectTransform> floatingGhosts = new List<RectTransform>();
        readonly Dictionary<string, Image> clueSpots = new Dictionary<string, Image>();
        int shownDone = -1;
        float eyeUntil;

        // chapter-specific lines first, then the general ones
        static readonly string[][] chapterCatLines =
        {
            new[] { "Önce örümcek ağları. Sonra mama. Sonra yine mama." },
            new[] { "Bu mahzen çok soğuk... patilerim dondu.", "Aynada bir şey kıpırdadı, gördün mü?" },
            new[] { "Ay ışığında tüylerim parlıyor, değil mi?", "Bu korkuluk bana bakıyor. Bence o da kedi sever." },
            new[] { "Hayalet kasabası ama pazarı fena değil!", "Saat kulesi çalışınca herkes uyanacak." },
            new[] { "Bu merdivenler bitmiyor... beni taşır mısın?", "Teleskopla bakınca Cadı Hanım'ı görebilir miyiz?" },
            new[] { "Gölge aslında yalnız. Ona bir yumak verelim.", "Eve dönmeye az kaldı, hissediyorum!" },
        };

        static readonly string[] catLines =
        {
            "Miyav. Mühürleri sen topla, dükkânı ben yönetirim.",
            "Cadı bir yerlerde... bıyıklarımda hissediyorum.",
            "Mırr. Kulağımın arkasını kaşı, sonra işe dön.",
            "Hayaletlerden korkma, çoğu sadece sohbet etmek istiyor.",
            "Göz düğmesine bas, gizli şeyleri birlikte bulalım.",
        };

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
                gifts[g] = UIKit.Img("Gift", barBg.transform, ProcArt.Item(Chains.Sandik, 1), Color.white);
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
            scenes[0] = BuildShop(sceneArea);
            scenes[1] = BuildCellar(sceneArea);
            for (int i = 2; i < scenes.Length; i++)
            {
                extra[i] = ChapterScenes.Build(i, NewScene(i < sceneNames.Length ? sceneNames[i] : "Chapter" + i), MakeCatTappable);
                scenes[i] = extra[i].root;
                dims[i] = extra[i].dim;
            }
            for (int i = 0; i < scenes.Length; i++) BuildClues(i);

            // investigation buttons on top of the scene
            var caseBtn = RoundButton(sceneArea, "CaseFile", ProcArt.CaseFile, new Vector2(0f, 1f), new Vector2(66f, -60f), () => GM.OpenCaseFile());
            caseBadge = UIKit.Img("Badge", caseBtn.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            caseBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            caseBadgeText = UIKit.Txt("N", caseBadge.transform, "0", 26, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(caseBadgeText.rectTransform);
            RoundButton(sceneArea, "Eye", ProcArt.Eye, new Vector2(1f, 1f), new Vector2(-66f, -60f), UseEye);
            // sound: a small menu with music and effects as two separate switches
            var soundBtn = RoundButton(sceneArea, "Sound", ProcArt.Speaker(Sfx.Mode), new Vector2(1f, 1f), new Vector2(-66f, -190f), ToggleSoundMenu);
            soundIcon = soundBtn.transform.Find("Icon").GetComponent<Image>();
            BuildSoundMenu();
            var daily = RoundButton(sceneArea, "Daily", ProcArt.Calendar, new Vector2(0f, 1f), new Vector2(66f, -190f), () => GM.OpenDaily());
            dailyBadge = UIKit.Img("Badge", daily.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            dailyBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var db = UIKit.Txt("!", dailyBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(db.rectTransform);
            // chapter list (like a map of the story), red dot when a gift chest is waiting
            var chapters = RoundButton(sceneArea, "Chapters", ProcArt.Item(Chains.Sandik, 1), new Vector2(0f, 1f), new Vector2(66f, -320f), () => GM.OpenChapters());
            chaptersBadge = UIKit.Img("Badge", chapters.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            chaptersBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var cbt = UIKit.Txt("!", chaptersBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(cbt.rectTransform);
            // "Usta Kedim": name and colour of the Master Cat
            var usta = RoundButton(sceneArea, "UstaKedim", ProcArt.SideCat(3, 4), new Vector2(0f, 1f), new Vector2(66f, -450f), () => GM.OpenUstaPanel());
            ustaIcon = usta.transform.Find("Icon").GetComponent<Image>();
            ustaIcon.rectTransform.sizeDelta = new Vector2(92f, 92f);
            ustaBadge = UIKit.Img("Badge", usta.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            ustaBadge.rectTransform.At(new Vector2(36f, 36f), new Vector2(44f, 44f));
            var ubt = UIKit.Txt("!", ustaBadge.transform, "!", 30, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(ubt.rectTransform);
            // the chapter title also opens the list
            chapterTitle.raycastTarget = true;
            var titleBtn = chapterTitle.gameObject.AddComponent<Button>();
            titleBtn.transition = Selectable.Transition.None;
            titleBtn.onClick.AddListener(() => GM.OpenChapters());

            // tasks
            var tasks = UIKit.Section(root, "Tasks", 1228f, 515f);
            var panel = UIKit.Img("Panel", tasks, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 30f, 30f);
            var t = UIKit.Txt("Title", panel.transform, "Görevler", 36, UIKit.Orange, TextAnchor.MiddleLeft);
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
            aLabel.text = "Albüm";
            aLabel.fontSize = 34;
            aLabel.rectTransform.offsetMin = new Vector2(110f, 4f);
            albumBadge = UIKit.Img("Badge", album.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            albumBadge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, -14f), new Vector2(46f, 46f));
            var bt = UIKit.Txt("!", albumBadge.transform, "!", 32, Color.white);
            UIKit.Stretch(bt.rectTransform);

            // witch's tower: Kara Pati's home (red dot when the bowls run low)
            var towerBtn = UIKit.Btn("Tower", bottom, "", new Color(0.42f, 0.28f, 0.6f), () => GM.GoTower());
            ((RectTransform)towerBtn.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(315f, 0f), new Vector2(240f, 150f));
            var tIcon = UIKit.Img("Icon", towerBtn.transform, ProcArt.TowerIcon, Color.white);
            tIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 4f), new Vector2(100f, 100f));
            var tLabel = towerBtn.Label();
            tLabel.text = "Kule";
            tLabel.fontSize = 34;
            tLabel.rectTransform.offsetMin = new Vector2(100f, 4f);
            towerBadge = UIKit.Img("Badge", towerBtn.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            towerBadge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, -14f), new Vector2(46f, 46f));
            var tbt = UIKit.Txt("!", towerBadge.transform, "!", 32, Color.white);
            UIKit.Stretch(tbt.rectTransform);

            var play = UIKit.Btn("Play", bottom, "Dükkânı Aç", UIKit.Green, () => GM.GoBoard(), 46);
            ((RectTransform)play.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(460f, 150f));
            play.Label().fontStyle = FontStyle.Bold;
            play.Label().rectTransform.offsetMin = new Vector2(110f, 4f);
            var pIcon = UIKit.Img("Icon", play.transform, ProcArt.Item(Chains.Kazan, 1), Color.white);
            pIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 4f), new Vector2(100f, 100f));
        }

        Button RoundButton(RectTransform parent, string name, Sprite icon, Vector2 anchor, Vector2 pos, System.Action onClick)
        {
            var bg = UIKit.Img(name, parent, ProcArt.Circle, new Color(0.1f, 0.07f, 0.15f, 0.85f));
            bg.raycastTarget = true;
            bg.rectTransform.Anchor(anchor, new Vector2(0.5f, 0.5f)).At(pos, new Vector2(110f, 110f));
            var ring = UIKit.Img("Ring", bg.transform, ProcArt.Circle, new Color(1f, 0.66f, 0.3f, 0.5f));
            ring.rectTransform.sizeDelta = new Vector2(118f, 118f);
            ring.transform.SetAsFirstSibling();
            var ic = UIKit.Img("Icon", bg.transform, icon, Color.white);
            ic.rectTransform.sizeDelta = new Vector2(74f, 74f);
            var b = bg.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => { Tween.Punch(bg.transform, 0.12f, 0.25f); Sfx.Play("click"); onClick(); });
            return b;
        }

        static Image Part(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = UIKit.Img(name, parent, s, c, sliced);
            img.rectTransform.At(pos, size);
            return img;
        }

        RectTransform NewScene(string name)
        {
            return UIKit.Rect(name, sceneArea).At(Vector2.zero, new Vector2(1020f, 940f));
        }

        // ------------------------------------------------------------------ chapter 1: shop
        RectTransform BuildShop(RectTransform area)
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
            cauldron = Part("Cauldron", scene, ProcArt.Item(Chains.Kazan, 1), Color.white, new Vector2(0f, -230f), new Vector2(300f, 300f));

            counterBody = Part("CounterBody", scene, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.2f), new Vector2(340f, -255f), new Vector2(260f, 180f), true);
            counterTop = Part("CounterTop", scene, ProcArt.Rounded, wood, new Vector2(340f, -155f), new Vector2(310f, 32f), true);
            counterStuff = UIKit.Rect("CounterStuff", scene).gameObject;
            Part("Ball", counterStuff.transform, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(300f, -95f), new Vector2(100f, 100f));
            Part("Letter", counterStuff.transform, ProcArt.WitchItem("w_mektup"), Color.white, new Vector2(400f, -118f), new Vector2(70f, 70f));

            webs = UIKit.Rect("Webs", scene).gameObject;
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(-430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(-30f, 420f), new Vector2(130f, 130f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(420f, -100f), new Vector2(120f, 120f));

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
                string line = catLines[Random.Range(0, catLines.Length)];
                if (view >= 0 && view < chapterCatLines.Length && Random.value < 0.6f)
                    line = chapterCatLines[view][Random.Range(0, chapterCatLines[view].Length)];
                GM.Toast("Kara Pati: \"" + line + "\"");
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
            chest = Part("Chest", scene, ProcArt.Item(Chains.Sandik, 1), Color.white, new Vector2(-110f, -230f), new Vector2(190f, 190f));
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
            Part("Web", scene, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.3f), new Vector2(250f, 420f), new Vector2(150f, 150f));
            Part("Web", scene, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.25f), new Vector2(-250f, 420f), new Vector2(120f, 120f));

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
            caseBadge.gameObject.SetActive(found2 > 0);
            caseBadgeText.text = found2.ToString();
        }

        public void RefreshUstaButton()
        {
            if (!ustaIcon || GM == null || GM.Cats == null) return;
            bool has = GM.Cats.HasAnyMaster;
            ustaIcon.sprite = ProcArt.SideCat(3, 4, has ? GM.Cats.MasterColor : KittenColors.Gece);
            ustaIcon.color = has ? Color.white : new Color(1f, 1f, 1f, 0.35f);   // no Master yet: a faded hint
            ustaBadge.gameObject.SetActive(has && GM.UstaNew);
        }

        // ------------------------------------------------------------------ sound menu
        Image soundIcon;
        RectTransform soundMenu;
        Button musicBtn, fxBtn;

        void BuildSoundMenu()
        {
            var bg = UIKit.Img("SoundMenu", sceneArea, ProcArt.Rounded, new Color(0.1f, 0.07f, 0.15f, 0.95f), true);
            bg.raycastTarget = true;
            soundMenu = bg.rectTransform;
            soundMenu.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-130f, -150f), new Vector2(340f, 210f));
            var title = UIKit.Txt("Title", soundMenu, "Ses", 30, UIKit.Orange, TextAnchor.MiddleCenter, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -8f), new Vector2(300f, 40f));
            musicBtn = UIKit.Btn("Music", soundMenu, "", UIKit.Green, () => { Sfx.SetMusic(!Sfx.MusicOn); RefreshSoundMenu(); }, 28);
            ((RectTransform)musicBtn.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -54f), new Vector2(300f, 66f));
            fxBtn = UIKit.Btn("Effects", soundMenu, "", UIKit.Green, () => { Sfx.SetEffects(!Sfx.EffectsOn); RefreshSoundMenu(); }, 28);
            ((RectTransform)fxBtn.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -130f), new Vector2(300f, 66f));
            soundMenu.gameObject.SetActive(false);
        }

        void ToggleSoundMenu()
        {
            bool open = !soundMenu.gameObject.activeSelf;
            soundMenu.gameObject.SetActive(open);
            if (open) { soundMenu.SetAsLastSibling(); RefreshSoundMenu(); Tween.Pop(soundMenu, 0.2f); }
        }

        void RefreshSoundMenu()
        {
            musicBtn.Label().text = "Müzik: " + (Sfx.MusicOn ? "Açık" : "Kapalı");
            musicBtn.GetComponent<Image>().color = Sfx.MusicOn ? UIKit.Green : UIKit.Disabled;
            fxBtn.Label().text = "Efektler: " + (Sfx.EffectsOn ? "Açık" : "Kapalı");
            fxBtn.GetComponent<Image>().color = Sfx.EffectsOn ? UIKit.Green : UIKit.Disabled;
            if (soundIcon) soundIcon.sprite = ProcArt.Speaker(Sfx.Mode);
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
                GM.Toast(more ? "Şimdilik gizli bir şey yok. Görevleri ilerlet, yenileri belirir." : "Buradaki bütün ipuçlarını buldun!");
                return;
            }
            eyeUntil = Time.time + 4f;
            GM.Toast("Parlayan yerlere dokun! (" + waiting + " ipucu)", UIKit.Orange);
        }

        // ------------------------------------------------------------------ state
        public void ShowChapter(int ch, bool fromButton = false)
        {
            int current = Story.CurrentChapter(GM.TasksDone);
            ch = Mathf.Clamp(ch, 0, Story.Chapters.Length - 1);
            if (ch > current)
            {
                if (fromButton) GM.Toast("Önce " + Story.Chapters[current].name + " bitmeli.");
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
            var small = UIKit.Txt("Num", curtain.transform, colon > 0 ? full.Substring(0, colon) : "", 44, new Color(1f, 0.96f, 0.88f, 0f));
            small.rectTransform.At(new Vector2(0f, 90f), new Vector2(900f, 60f));
            var big = UIKit.Txt("Name", curtain.transform, colon > 0 ? full.Substring(colon + 1).Trim() : full, 76, new Color(1f, 0.66f, 0.3f, 0f));
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
                gifts[g].color = claimed ? new Color(1f, 1f, 1f, 0.55f) : ready ? Color.white : new Color(0.45f, 0.42f, 0.5f, 0.9f);
                giftChecks[g].gameObject.SetActive(claimed);
                giftGlows[g].gameObject.SetActive(ready);
                if (!ready) gifts[g].rectTransform.localScale = Vector3.one;
            }
        }

        void OnGiftTapped(int g)
        {
            if (GM.GiftClaimed(view, g)) { GM.Toast("Bu sandığı zaten açtın."); return; }
            if (GM.GiftReady(view, g))
            {
                FX.Burst((RectTransform)gifts[g].transform.parent, gifts[g].rectTransform.anchoredPosition, UIKit.Orange, 14);
                GM.ClaimChapterGift(view, g);
                Refresh(false);
                return;
            }
            int left = ChapterGifts.TasksNeeded(view, g) - Mathf.Clamp(GM.TasksDone - Story.ChapterStart(view), 0, 999);
            Tween.Punch(gifts[g].transform, 0.12f, 0.25f);
            GM.Toast("Bu sandık için " + left + " görev daha yap. İçinde: " + ChapterGifts.Text(view, g));
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
            cauldron.color = fire ? Color.white : new Color(0.5f, 0.5f, 0.56f);
            cauldronGlow.gameObject.SetActive(fire);

            rug.SetActive(Done("hali", done));

            bool lit = Done("fener", done);
            lanternA.sprite = ProcArt.Lantern(lit);
            lanternB.sprite = ProcArt.Lantern(lit);
            glowA.gameObject.SetActive(lit);
            glowB.gameObject.SetActive(lit);

            bool counter = Done("tezgah", done);
            counterTop.rectTransform.localEulerAngles = new Vector3(0, 0, counter ? 0f : 12f);
            counterBody.color = counter ? new Color(0.42f, 0.28f, 0.2f) : new Color(0.3f, 0.2f, 0.16f);
            counterStuff.SetActive(counter);

            bool hasBed = Done("yatak", done);
            bed.SetActive(hasBed);
            karaPati.sprite = ProcArt.SideCat(0, hasBed ? 5 : 4);
            karaPati.rectTransform.anchoredPosition = hasBed ? new Vector2(-330f, -318f) : new Vector2(-330f, -290f);

            int inCh = Mathf.Clamp(done, 0, Story.Chapters[0].tasks.Length);
            dims[0].color = new Color(0.05f, 0.03f, 0.08f, 0.45f * (1f - inCh / (float)Story.Chapters[0].tasks.Length));
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
                case "pencere": return scenes[0].Find("Moon");
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

        Sprite TaskIcon(string id)
        {
            switch (id)
            {
                case "ag": return ProcArt.Item(Chains.Ag, 1);
                case "pencere": return ProcArt.Moon;
                case "raf": return ProcArt.Bottle(0);
                case "kazan": return ProcArt.Item(Chains.Kazan, 1);
                case "hali": return ProcArt.Item(Chains.Ag, 4);
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
                string msg = last ? "Hikâye tamamlandı! Cadı Hanım evinde.\nYeni maceralar yakında..." : "Bölüm tamamlandı!\nSıradaki bölüm için > düğmesine bas.";
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
                if (!current) row.GetComponent<Image>().color = new Color(0.13f, 0.09f, 0.19f, 0.6f);

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
                    var btn = UIKit.Btn("Do", row, "Yap", affordable ? UIKit.Green : UIKit.Disabled, () => GM.DoTask(idx), 38);
                    ((RectTransform)btn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(180f, 90f));
                }
                else
                {
                    var wait = UIKit.Txt("Next", row, "Sırada", 28, new Color(1f, 1f, 1f, 0.45f));
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
            if (karaPati && GM != null && Story.IsDone("yatak", GM.TasksDone))
                karaPati.rectTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);

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
