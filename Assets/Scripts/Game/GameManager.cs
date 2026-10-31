using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// Boots the whole game from code: builds both screens (shop + merge table), owns the economy
    /// (gold, energy, Pati Mührü), stickers, story progress, dialogs, toasts and saving.
    /// It spawns itself in any scene, so pressing Play is enough.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("KaraPati Game").AddComponent<GameManager>();
        }

        public const float EnergyRegenSec = 8f;
        public const int EnergyPackCost = 20, EnergyPackAmount = 10;
        const string SaveKey = "karapati_save_v1";
        const float RefW = 1080f, RefH = 2060f;

        public int Gold { get; private set; }
        public int Stamps { get; private set; }
        public int Energy { get; private set; }
        public int Served { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int MaxEnergy => Levels.MaxEnergy(Level);
        public int StorageSlots => Levels.StorageSlots(Level);
        public readonly List<ItemData> Storage = new List<ItemData>();
        public int Pumpkins { get; private set; }
        public int EventClaimed { get; private set; }
        long eventStart;
        public int DailyIndex { get; private set; }
        int lastDailyDay = -1;
        public bool DailyAvailable => lastDailyDay != DailyRewards.Today;
        public bool EventActive => DateTime.UtcNow.Ticks < EventEnd;
        long EventEnd => eventStart + TimeSpan.FromDays(PumpkinEvent.Days).Ticks;
        public string EventTimeLeftText
        {
            get
            {
                var left = TimeSpan.FromTicks(Math.Max(0L, EventEnd - DateTime.UtcNow.Ticks));
                if (left.TotalDays >= 1) return (int)left.TotalDays + "g " + left.Hours + "sa";
                if (left.TotalHours >= 1) return (int)left.TotalHours + "sa " + left.Minutes + "dk";
                return left.Minutes + "dk " + left.Seconds + "sn";
            }
        }
        public int TasksDone { get; private set; }
        public bool KittenHintShown { get; set; }
        bool introSeen;
        readonly HashSet<string> stickers = new HashSet<string>();
        readonly HashSet<string> claimedSets = new HashSet<string>();
        readonly HashSet<string> clues = new HashSet<string>();

        public RectTransform Root { get; private set; }
        public BoardController Board { get; private set; }
        public OrderPanel Orders { get; private set; }
        public KittenChaos Cats { get; private set; }
        public bool OnBoard { get; private set; }
        public bool ModalOpen => (album && album.IsOpen) || (caseFile && caseFile.IsOpen) || (storagePanel && storagePanel.IsOpen) || (eventPanel && eventPanel.IsOpen) || (dailyPanel && dailyPanel.IsOpen) || (dialogRoot && dialogRoot.activeSelf);

        HomeScreen home;
        AlbumPanel album;
        CaseFilePanel caseFile;
        CanvasScaler scaler;
        CanvasGroup homeCg, boardCg;
        Button homeBtn;
        RectTransform storageBtn, levelBadge;
        Text storageText, levelText;
        Image levelRing;
        StoragePanel storagePanel;
        EventPanel eventPanel;
        DailyPanel dailyPanel;
        RectTransform eventBtn;
        Text eventTimerText;
        GameObject eventBadge;
        float eventTick;
        Text goldText, stampText, energyText, energyTimerText, toastText, infoTitle, infoDesc;
        RectTransform goldIcon, stampIcon;
        CanvasGroup toastGroup;
        Button actionBtn;
        float energyTimer;
        bool dirty;
        float saveTimer;
        Coroutine toastCo;
        float resetArmedUntil;
        int tipIndex;
        KittenAgent infoKitten;
        float kittenInfoTimer;

        // dialog
        GameObject dialogRoot;
        Image dialogPortrait;
        Text dialogName, dialogText;
        Action dialogDone;
        readonly Queue<(string speaker, string line, Action done)> dialogQueue = new Queue<(string, string, Action)>();

        // sticker popup
        RectTransform stickerPop;
        Image stickerPopIcon;
        Text stickerPopText;
        CanvasGroup stickerPopCg;
        Coroutine stickerCo;

        static readonly string[] tips =
        {
            "Aynı iki eşyayı üst üste sürükle, birleşsinler.",
            "Kazana ve sandığa dokunarak malzeme üret (1 enerji).",
            "Hayaletlerin istediklerini masada topla, sonra 'Ver'e bas.",
            "İki yavruyu üst üste sürükle: 2 saatlik bir Çırak Kedi olur!",
            "Siparişler Pati Mührü verir; mühürlerle dükkânı onarırsın."
        };

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            SetupCamera();
            EnsureEventSystem();
            BuildUI();
            Load();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void Update()
        {
            UpdateScalerMatch();

            if (Energy < MaxEnergy)
            {
                energyTimer += Time.deltaTime;
                if (energyTimer >= EnergyRegenSec)
                {
                    energyTimer -= EnergyRegenSec;
                    Energy++;
                    RefreshHud();
                    MarkDirty();
                }
                energyTimerText.text = Energy < MaxEnergy ? Mathf.CeilToInt(EnergyRegenSec - energyTimer) + " sn" : "dolu";
            }
            else
            {
                energyTimer = 0f;
                energyTimerText.text = "dolu";
            }

            if (resetArmedUntil > 0f && Time.unscaledTime > resetArmedUntil)
            {
                resetArmedUntil = 0f;
                if (infoKitten == null && Board.Get(Board.Selected) == null) ShowInfo(null);
            }

            eventTick -= Time.unscaledDeltaTime;
            if (eventTick <= 0f)
            {
                eventTick = 1f;
                RefreshEventButton();
            }

            if (infoKitten)
            {
                kittenInfoTimer -= Time.unscaledDeltaTime;
                if (kittenInfoTimer <= 0f) ShowKittenInfo(infoKitten);
            }

            if (dirty)
            {
                saveTimer -= Time.unscaledDeltaTime;
                if (saveTimer <= 0f) Save();
            }
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();

        // ------------------------------------------------------------------ setup
        void SetupCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.06f, 0.14f);
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void UpdateScalerMatch()
        {
            if (!scaler) return;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : RefW / RefH;
            scaler.matchWidthOrHeight = aspect > RefW / RefH ? 1f : 0f;
        }

        void BuildUI()
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateScalerMatch();
            var canvasRt = (RectTransform)cgo.transform;

            var bg = UIKit.Img("Background", canvasRt, ProcArt.Gradient, Color.white);
            UIKit.Stretch(bg.rectTransform);
            for (int i = 0; i < 14; i++)
            {
                var s = UIKit.Img("Star", canvasRt, ProcArt.Sparkle, new Color(1f, 0.95f, 0.8f, Random.Range(0.15f, 0.45f)));
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(Random.value, Random.value);
                float sz = Random.Range(12f, 30f);
                s.rectTransform.sizeDelta = new Vector2(sz, sz);
            }

            Root = UIKit.Rect("Root", canvasRt);
            Root.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(Vector2.zero, new Vector2(RefW, RefH));

            // --- home screen
            var homeRt = UIKit.Fill(Root, "HomeScreen");
            homeCg = homeRt.gameObject.AddComponent<CanvasGroup>();
            home = homeRt.gameObject.AddComponent<HomeScreen>();
            home.Build(homeRt);

            // --- board screen
            var boardRt = UIKit.Fill(Root, "BoardScreen");
            boardCg = boardRt.gameObject.AddComponent<CanvasGroup>();
            var ordersArea = UIKit.Section(boardRt, "Orders", 138f, 400f);
            Orders = ordersArea.gameObject.AddComponent<OrderPanel>();
            Orders.Build(ordersArea);
            var boardArea = UIKit.Section(boardRt, "Board", 618f, 1290f);
            Board = boardArea.gameObject.AddComponent<BoardController>();
            Board.Build(boardArea);
            BuildInfoBar(UIKit.Section(boardRt, "InfoBar", 1916f, 134f));

            // --- shared overlays
            BuildTopBar(UIKit.Section(Root, "TopBar", 12f, 115f));

            var toastArea = UIKit.Section(Root, "Toast", 545f, 70f);
            toastGroup = toastArea.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            var toastBg = UIKit.Img("Bg", toastArea, ProcArt.Rounded, new Color(0.08f, 0.05f, 0.12f, 0.82f), true);
            UIKit.Stretch(toastBg.rectTransform, 30f, 30f, -6f, -6f);
            toastText = UIKit.Txt("Text", toastArea, "", 29, UIKit.Cream);
            UIKit.Stretch(toastText.rectTransform, 50f, 50f);

            BuildStickerPopup();

            var albumRt = UIKit.Rect("Album", canvasRt);
            album = albumRt.gameObject.AddComponent<AlbumPanel>();
            album.Build();

            var caseRt = UIKit.Rect("CaseFile", canvasRt);
            caseFile = caseRt.gameObject.AddComponent<CaseFilePanel>();
            caseFile.Build();

            var storeRt = UIKit.Rect("StoragePanel", canvasRt);
            storagePanel = storeRt.gameObject.AddComponent<StoragePanel>();
            storagePanel.Build();

            var eventRt = UIKit.Rect("EventPanel", canvasRt);
            eventPanel = eventRt.gameObject.AddComponent<EventPanel>();
            eventPanel.Build();

            var dailyRt = UIKit.Rect("DailyPanel", canvasRt);
            dailyPanel = dailyRt.gameObject.AddComponent<DailyPanel>();
            dailyPanel.Build();

            BuildDialog(canvasRt);

            Cats = gameObject.AddComponent<KittenChaos>();
            Cats.Init(Board.CatsLayer);

            // start on the shop screen
            OnBoard = false;
            boardCg.gameObject.SetActive(false);
        }

        Image Pill(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float width)
        {
            var p = UIKit.Img(name, parent, ProcArt.Rounded, UIKit.PanelDark, true);
            p.rectTransform.Anchor(anchor, pivot).At(pos, new Vector2(width, 92f));
            return p;
        }

        void BuildTopBar(RectTransform bar)
        {
            // player level ring
            var lvBg = UIKit.Img("Level", bar, ProcArt.Circle, new Color(0.1f, 0.07f, 0.15f));
            lvBg.raycastTarget = true;
            levelBadge = lvBg.rectTransform;
            levelBadge.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(100f, 100f));
            levelRing = UIKit.Img("Ring", lvBg.transform, ProcArt.Circle, new Color(0.35f, 0.75f, 1f));
            levelRing.rectTransform.sizeDelta = new Vector2(100f, 100f);
            levelRing.type = Image.Type.Filled;
            levelRing.fillMethod = Image.FillMethod.Radial360;
            levelRing.fillOrigin = (int)Image.Origin360.Top;
            levelRing.fillClockwise = true;
            var lvIn = UIKit.Img("Inner", lvBg.transform, ProcArt.Circle, new Color(0.16f, 0.12f, 0.24f));
            lvIn.rectTransform.sizeDelta = new Vector2(78f, 78f);
            levelText = UIKit.Txt("N", lvBg.transform, "1", 40, UIKit.Cream);
            levelText.fontStyle = FontStyle.Bold;
            UIKit.Stretch(levelText.rectTransform);
            var lvBtn = lvBg.gameObject.AddComponent<Button>();
            lvBtn.transition = Selectable.Transition.None;
            lvBtn.onClick.AddListener(() =>
            {
                Tween.Punch(levelBadge, 0.15f, 0.25f);
                Toast("Seviye " + Level + ": " + Xp + "/" + Levels.XpToNext(Level) + " XP. Sipariş, görev ve ipuçları XP verir.");
            });

            var gold = Pill(bar, "Gold", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, 0f), 230f);
            var coin = UIKit.Img("Coin", gold.transform, ProcArt.Coin, Color.white);
            goldIcon = coin.rectTransform;
            goldIcon.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 0f), new Vector2(70f, 70f));
            goldText = UIKit.Txt("Value", gold.transform, "0", 40, UIKit.Cream, TextAnchor.MiddleLeft);
            UIKit.Stretch(goldText.rectTransform, 88f, 8f);

            var st = Pill(bar, "Stamps", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(384f, 0f), 200f);
            var stImg = UIKit.Img("Stamp", st.transform, ProcArt.Stamp, Color.white);
            stampIcon = stImg.rectTransform;
            stampIcon.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 0f), new Vector2(70f, 70f));
            stampText = UIKit.Txt("Value", st.transform, "0", 40, UIKit.Cream, TextAnchor.MiddleLeft);
            UIKit.Stretch(stampText.rectTransform, 88f, 8f);

            // time-limited event
            var ev = UIKit.Img("Event", bar, ProcArt.Rounded, new Color(0.45f, 0.2f, 0.45f), true);
            ev.raycastTarget = true;
            eventBtn = ev.rectTransform;
            eventBtn.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(598f, 0f), new Vector2(112f, 104f));
            var evIcon = UIKit.Img("Icon", ev.transform, ProcArt.Pumpkin, Color.white);
            evIcon.rectTransform.At(new Vector2(0f, 12f), new Vector2(74f, 74f));
            eventTimerText = UIKit.Txt("Timer", ev.transform, "", 20, new Color(1f, 0.9f, 0.6f));
            eventTimerText.rectTransform.At(new Vector2(0f, -36f), new Vector2(120f, 30f));
            var evB = UIKit.Img("Badge", ev.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            evB.rectTransform.At(new Vector2(44f, 40f), new Vector2(38f, 38f));
            var evBt = UIKit.Txt("!", evB.transform, "!", 28, Color.white);
            UIKit.Stretch(evBt.rectTransform);
            eventBadge = evB.gameObject;
            var evBtn = ev.gameObject.AddComponent<Button>();
            evBtn.onClick.AddListener(() => eventPanel.Open());

            var en = Pill(bar, "Energy", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), 330f);
            var bolt = UIKit.Img("Bolt", en.transform, ProcArt.Bolt, Color.white);
            bolt.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f, 0f), new Vector2(70f, 70f));
            energyText = UIKit.Txt("Value", en.transform, "30/30", 38, UIKit.Cream, TextAnchor.MiddleLeft);
            energyText.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(82f, 10f), new Vector2(150f, 50f));
            energyTimerText = UIKit.Txt("Timer", en.transform, "", 22, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleLeft);
            energyTimerText.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(84f, -24f), new Vector2(150f, 30f));
            var plus = UIKit.Btn("Buy", en.transform, "+", UIKit.Green, BuyEnergy, 48);
            ((RectTransform)plus.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-10f, 0f), new Vector2(76f, 76f));
        }

        void BuildInfoBar(RectTransform area)
        {
            // storage box (bottom-left) and back-to-shop (bottom-right), like a classic merge layout
            var store = UIKit.Btn("Storage", area, "", new Color(0.3f, 0.55f, 0.6f), () => storagePanel.Open(), 26);
            storageBtn = (RectTransform)store.transform;
            storageBtn.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(22f, 0f), new Vector2(150f, 134f));
            var sIcon = UIKit.Img("Icon", storageBtn, ProcArt.Crate, Color.white);
            sIcon.rectTransform.At(new Vector2(0f, 14f), new Vector2(92f, 92f));
            storageText = store.Label();
            storageText.alignment = TextAnchor.LowerCenter;
            storageText.text = "0/4";

            homeBtn = UIKit.Btn("Home", area, "", new Color(0.3f, 0.55f, 0.6f), GoHome);
            ((RectTransform)homeBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-22f, 0f), new Vector2(150f, 134f));
            var hIcon = UIKit.Img("Icon", homeBtn.transform, ProcArt.House, Color.white);
            hIcon.rectTransform.sizeDelta = new Vector2(90f, 90f);

            var panel = UIKit.Img("Panel", area, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 186f, 186f, 0f, 0f);
            infoTitle = UIKit.Txt("Title", panel.transform, "", 34, UIKit.Orange, TextAnchor.UpperLeft);
            infoTitle.fontStyle = FontStyle.Bold;
            infoTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            infoTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            infoTitle.rectTransform.pivot = new Vector2(0f, 1f);
            infoTitle.rectTransform.offsetMin = new Vector2(24f, -60f);
            infoTitle.rectTransform.offsetMax = new Vector2(-196f, -14f);
            infoDesc = UIKit.Txt("Desc", panel.transform, "", 25, UIKit.Cream, TextAnchor.UpperLeft);
            infoDesc.rectTransform.anchorMin = new Vector2(0f, 0f);
            infoDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            infoDesc.rectTransform.offsetMin = new Vector2(24f, 6f);
            infoDesc.rectTransform.offsetMax = new Vector2(-196f, -58f);
            actionBtn = UIKit.Btn("Action", panel.transform, "", UIKit.Orange, OnAction, 28);
            ((RectTransform)actionBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(176f, 96f));
        }

        void BuildStickerPopup()
        {
            stickerPop = UIKit.Section(Root, "StickerPopup", 140f, 150f);
            stickerPopCg = stickerPop.gameObject.AddComponent<CanvasGroup>();
            stickerPopCg.blocksRaycasts = false;
            stickerPopCg.alpha = 0f;
            var bg = UIKit.Img("Bg", stickerPop, ProcArt.Rounded, new Color(0.98f, 0.94f, 0.86f), true);
            bg.rectTransform.At(Vector2.zero, new Vector2(640f, 140f));
            var paper = UIKit.Img("Paper", bg.transform, ProcArt.Circle, new Color(0.15f, 0.1f, 0.2f));
            paper.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 0f), new Vector2(118f, 118f));
            stickerPopIcon = UIKit.Img("Icon", paper.transform, null, Color.white);
            stickerPopIcon.rectTransform.sizeDelta = new Vector2(96f, 96f);
            stickerPopText = UIKit.Txt("Text", bg.transform, "", 30, new Color(0.25f, 0.15f, 0.3f), TextAnchor.MiddleLeft, false);
            UIKit.Stretch(stickerPopText.rectTransform, 150f, 20f);
        }

        void BuildDialog(RectTransform canvasRt)
        {
            var rt = UIKit.Rect("Dialog", canvasRt);
            UIKit.Stretch(rt);
            dialogRoot = rt.gameObject;
            var dim = rt.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.01f, 0.06f, 0.55f);
            var dimBtn = rt.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseDialog);

            var panel = UIKit.Img("Panel", rt, ProcArt.Rounded, new Color(0.24f, 0.16f, 0.33f, 0.99f), true);
            panel.raycastTarget = true;
            panel.rectTransform.At(new Vector2(0f, -330f), new Vector2(1000f, 430f));

            var frame = UIKit.Img("PortraitBg", panel.transform, ProcArt.Circle, new Color(0.13f, 0.09f, 0.19f));
            frame.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -30f), new Vector2(190f, 190f));
            dialogPortrait = UIKit.Img("Portrait", frame.transform, null, Color.white);
            dialogPortrait.rectTransform.sizeDelta = new Vector2(160f, 160f);

            dialogName = UIKit.Txt("Name", panel.transform, "", 40, UIKit.Orange, TextAnchor.MiddleLeft);
            dialogName.fontStyle = FontStyle.Bold;
            dialogName.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(250f, -30f), new Vector2(700f, 56f));
            dialogText = UIKit.Txt("Text", panel.transform, "", 34, UIKit.Cream, TextAnchor.UpperLeft);
            dialogText.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(250f, -95f), new Vector2(710f, 220f));
            dialogText.rectTransform.pivot = new Vector2(0f, 1f);
            dialogName.rectTransform.pivot = new Vector2(0f, 1f);

            var next = UIKit.Btn("Next", panel.transform, "Devam", UIKit.Orange, CloseDialog, 36);
            ((RectTransform)next.transform).Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f)).At(new Vector2(-30f, 26f), new Vector2(230f, 86f));
            dialogRoot.SetActive(false);
        }

        // ------------------------------------------------------------------ screens
        public void GoBoard()
        {
            if (OnBoard) return;
            OnBoard = true;
            Cross(homeCg, boardCg);
            Orders.Refresh();
            ShowInfo(null);
        }

        public void GoHome()
        {
            if (!OnBoard) return;
            OnBoard = false;
            Board.HideSelector();
            Cats.Deselect();
            infoKitten = null;
            home.Refresh(false);
            Cross(boardCg, homeCg);
            Save();
        }

        void Cross(CanvasGroup from, CanvasGroup to)
        {
            to.gameObject.SetActive(true);
            to.alpha = 0f;
            from.blocksRaycasts = false;
            to.blocksRaycasts = false;
            Tween.Run(0.25f, t =>
            {
                if (from) from.alpha = 1f - t;
                if (to) to.alpha = t;
            }, () =>
            {
                if (from) { from.gameObject.SetActive(false); from.alpha = 1f; from.blocksRaycasts = true; }
                if (to) to.blocksRaycasts = true;
            });
        }

        public void OpenAlbum() => album.Open();
        public void OpenDaily() => dailyPanel.Open();
        public void OnDailyClosed() { if (!OnBoard) home.Refresh(false); }

        public bool ClaimDaily()
        {
            if (!DailyAvailable) return false;
            var m = DailyRewards.Days[DailyIndex % DailyRewards.Days.Length];
            switch (m.type)
            {
                case "gold": AddGold(m.amount); break;
                case "energy": Energy += m.amount; RefreshHud(); break;
                case "stamps": AddStamps(m.amount); break;
                default:
                    AddGold(m.amount);
                    if (!GiveRandomMissingAny()) AddGold(50);
                    break;
            }
            Toast("Günlük ödül: " + DailyRewards.Text(m), UIKit.Orange);
            lastDailyDay = DailyRewards.Today;
            DailyIndex = (DailyIndex + 1) % DailyRewards.Days.Length;
            Save();
            return true;
        }

        public void DebugResetDaily()
        {
            lastDailyDay = -1;
            if (!OnBoard) home.Refresh(false);
            dailyPanel.Open();
        }

        void MaybeShowDaily()
        {
            if (DailyAvailable && !OnBoard) StartCoroutine(After(0.6f, () => { if (!ModalOpen && DailyAvailable) dailyPanel.Open(); }));
        }
        public void OpenCaseFile() => caseFile.Open();

        // ------------------------------------------------------------------ clues
        public bool HasClue(string id) => clues.Contains(id);

        public void FindClue(Clue c)
        {
            if (clues.Contains(c.id)) return;
            clues.Add(c.id);
            AddStamps(1);
            AddXp(5);
            Save();
            ShowDialog(c.title, c.text, () =>
            {
                Toast("İpucu Cadı Dosyası'na eklendi: +1 Pati Mührü", UIKit.Orange);
                home.Refresh(false);
            });
        }
        public void OnAlbumClosed() { if (!OnBoard) home.Refresh(false); }

        // ------------------------------------------------------------------ dialog
        public void ShowDialog(string speaker, string line, Action done = null)
        {
            if (dialogRoot.activeSelf) { dialogQueue.Enqueue((speaker, line, done)); return; }
            dialogName.text = speaker;
            dialogText.text = line;
            dialogPortrait.sprite = Portrait(speaker);
            dialogDone = done;
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
            Tween.Pop(dialogRoot.transform.GetChild(0), 0.3f);
        }

        void CloseDialog()
        {
            dialogRoot.SetActive(false);
            var d = dialogDone;
            dialogDone = null;
            d?.Invoke();
            if (!dialogRoot.activeSelf && dialogQueue.Count > 0)
            {
                var next = dialogQueue.Dequeue();
                ShowDialog(next.speaker, next.line, next.done);
            }
        }

        static Sprite Portrait(string speaker)
        {
            int gi = Array.IndexOf(Ghosts.Names, speaker);
            return gi >= 0 ? ProcArt.Ghost(gi) : ProcArt.KaraPatiHead;
        }

        // ------------------------------------------------------------------ story
        public void DoTask(int idx)
        {
            if (idx != TasksDone || idx >= Story.TotalTasks) return;
            var task = Story.TaskAt(idx);
            if (Stamps < task.cost)
            {
                Toast(task.cost + " Pati Mührü gerekiyor. 'Dükkânı Aç'a bas, sipariş hazırla!", new Color(1f, 0.6f, 0.5f));
                Tween.Punch(stampIcon, 0.3f, 0.3f);
                return;
            }
            int chapter = Story.CurrentChapter(idx);
            Stamps -= task.cost;
            TasksDone++;
            AddXp(10);
            AddPumpkins(2);
            RefreshHud();
            home.Refresh(true);
            Save();
            bool chapterDone = TasksDone == Story.ChapterStart(chapter) + Story.Chapters[chapter].tasks.Length;
            StartCoroutine(After(0.7f, () =>
            {
                ShowDialog(task.speaker, task.line, () =>
                {
                    GiveRandomMissing(Stickers.Sets[2]);
                    if (task.id == "yatak") GiveSticker("c_karapati", false);
                    home.Refresh(false);
                    if (chapterDone)
                    {
                        ShowDialog("Kara Pati", Story.Chapters[chapter].endLine, () =>
                        {
                            if (chapter + 1 < Story.Chapters.Length)
                            {
                                home.ShowChapter(chapter + 1);
                                Toast(Story.Chapters[chapter + 1].name + " açıldı!", UIKit.Orange, 4f);
                            }
                        });
                    }
                    else if (Stamps < Story.TaskAt(TasksDone).cost && TasksDone <= 2) Toast(Story.FirstBoardHint, UIKit.Orange, 4.5f);
                    else if (HasNewClue()) Toast("Göz düğmesine bas: yeni bir ipucu belirdi!", UIKit.Orange, 4f);
                });
            }));
        }

        bool HasNewClue()
        {
            foreach (var c in Story.Clues)
                if (c.afterTasks == TasksDone && !clues.Contains(c.id)) return true;
            return false;
        }

        static IEnumerator After(float s, Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }

        // ------------------------------------------------------------------ stickers
        public bool HasSticker(string id) => stickers.Contains(id);
        public bool IsSetClaimed(StickerSet s) => claimedSets.Contains(s.id);
        public bool IsSetClaimable(StickerSet s) => !IsSetClaimed(s) && StickerCount(s) >= s.ids.Length;

        public bool AnyClaimableSet
        {
            get
            {
                foreach (var s in Stickers.Sets) if (IsSetClaimable(s)) return true;
                return AllStickersCollected && !IsGrandClaimed;
            }
        }

        public bool IsGrandClaimed => claimedSets.Contains(Stickers.GrandId);

        public bool AllStickersCollected
        {
            get
            {
                foreach (var s in Stickers.Sets) if (StickerCount(s) < s.ids.Length) return false;
                return true;
            }
        }

        public bool ClaimGrand()
        {
            if (!AllStickersCollected || IsGrandClaimed) return false;
            claimedSets.Add(Stickers.GrandId);
            Gold += Stickers.GrandGold;
            Energy += Stickers.GrandEnergy;
            AddStamps(Stickers.GrandStamps);
            Toast("Büyük ödül! +" + Stickers.GrandGold + " altın, +" + Stickers.GrandStamps + " mühür, +" + Stickers.GrandEnergy + " enerji", UIKit.Orange, 4.5f);
            Save();
            return true;
        }

        public int StickerCount(StickerSet s)
        {
            int n = 0;
            foreach (var id in s.ids) if (stickers.Contains(id)) n++;
            return n;
        }

        /// <summary>Award a sticker. Duplicates from random drops turn into a little gold.</summary>
        public void GiveSticker(string id, bool duplicateGold = true)
        {
            if (stickers.Contains(id))
            {
                if (duplicateGold)
                {
                    AddGold(5);
                    Toast("Kopya etiket (" + Stickers.Name(id) + "): +5 altın");
                }
                return;
            }
            stickers.Add(id);
            ShowStickerPopup(id);
            var set = Stickers.SetOf(id);
            if (set != null && IsSetClaimable(set))
                StartCoroutine(After(2.6f, () => Toast("\"" + set.name + "\" seti tamamlandı! Albümden ödülünü al.", UIKit.Orange, 4f)));
            MarkDirty();
        }

        void GiveRandomMissing(StickerSet set)
        {
            var missing = new List<string>();
            foreach (var id in set.ids) if (!stickers.Contains(id)) missing.Add(id);
            if (missing.Count > 0) GiveSticker(missing[Random.Range(0, missing.Count)], false);
        }

        public void OnOrderDelivered(int ghostIndex)
        {
            float r = Random.value;
            if (r < 0.4f) GiveSticker("g" + ghostIndex);
            else if (r < 0.55f)
            {
                var w = Stickers.Sets[2].ids;
                GiveSticker(w[Random.Range(0, w.Length)]);
            }
            if (Random.value < 0.05f) GiveSticker("c_hayaletkedi", false);
        }

        public bool ClaimSet(StickerSet s)
        {
            if (!IsSetClaimable(s)) return false;
            claimedSets.Add(s.id);
            AddGold(Stickers.SetRewardGold);
            AddStamps(Stickers.SetRewardStamps);
            Toast("\"" + s.name + "\" ödülü: +" + Stickers.SetRewardGold + " altın, +" + Stickers.SetRewardStamps + " mühür!", UIKit.Orange);
            Save();
            return true;
        }

        void ShowStickerPopup(string id)
        {
            stickerPopIcon.sprite = ProcArt.StickerIcon(id);
            var set = Stickers.SetOf(id);
            stickerPopText.text = "<b>Yeni etiket!</b>\n" + Stickers.Name(id) + (set != null ? "  <size=24>(" + set.name + ")</size>" : "");
            stickerPop.SetAsLastSibling();
            if (stickerCo != null) StopCoroutine(stickerCo);
            stickerCo = StartCoroutine(StickerPopCo());
        }

        IEnumerator StickerPopCo()
        {
            stickerPopCg.alpha = 1f;
            Tween.Pop(stickerPop, 0.35f);
            yield return new WaitForSecondsRealtime(2.4f);
            float t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; stickerPopCg.alpha = 1f - t / 0.35f; yield return null; }
            stickerPopCg.alpha = 0f;
        }

        // ------------------------------------------------------------------ info bar
        public void ShowInfo(ItemData d)
        {
            if (infoTitle == null) return;
            infoKitten = null;
            Cats?.Deselect();
            var btnImg = actionBtn.GetComponent<Image>();
            if (d == null)
            {
                infoTitle.text = "İpucu";
                infoDesc.text = tips[tipIndex % tips.Length];
                bool armed = resetArmedUntil > Time.unscaledTime;
                actionBtn.gameObject.SetActive(true);
                actionBtn.Label().text = armed ? "Emin misin?" : "Yeni Oyun";
                btnImg.color = armed ? new Color(0.85f, 0.3f, 0.3f) : UIKit.Disabled;
                return;
            }

            infoTitle.text = d.Name + (Chains.IsGenerator(d.chain) ? "" : "  <size=26>(Sv. " + d.level + ")</size>");
            actionBtn.gameObject.SetActive(false);
            btnImg.color = UIKit.Orange;

            if (d.nap)
            {
                infoDesc.text = "Üstünde bir yavru uyuyor! Dokunup okşarsan kalkar.";
                return;
            }
            if (d.bubble)
            {
                int left = Mathf.Max(0, Mathf.CeilToInt((float)TimeSpan.FromTicks(d.bubbleUntil - DateTime.UtcNow.Ticks).TotalSeconds));
                infoTitle.text = d.Name + "  <size=26>(Baloncuk)</size>";
                infoDesc.text = "Bedava kopya! " + left + " sn içinde açmazsan uçup gider.";
                int price = Bubbles.Price(d.level);
                actionBtn.gameObject.SetActive(true);
                actionBtn.Label().text = "Aç\n" + price + " altın";
                btnImg.color = Gold >= price ? UIKit.Green : UIKit.Disabled;
                return;
            }
            if (Chains.IsGenerator(d.chain))
            {
                infoDesc.text = (d.chain == Chains.Kazan ? "Ot ve mantar üretir." : "Kristal ve örümcek ağı üretir.") + " Dokun: 1 enerji.";
                return;
            }
            int max = Chains.MaxLevel(d.chain);
            infoDesc.text = d.level < max
                ? "İki tanesini birleştir: " + Chains.Name(d.chain, d.level + 1)
                : "En üst seviye! Hayaletler buna bayılır.";
            actionBtn.gameObject.SetActive(true);
            actionBtn.Label().text = "Sat  +" + d.level;
        }

        public void ShowKittenInfo(KittenAgent a)
        {
            if (infoTitle == null) return;
            if (!a) { ShowInfo(null); return; }
            infoKitten = a;
            kittenInfoTimer = 1f;
            var btnImg = actionBtn.GetComponent<Image>();
            btnImg.color = UIKit.Orange;
            actionBtn.gameObject.SetActive(false);
            switch (a.level)
            {
                case 1:
                    infoTitle.text = "Yaramaz Yavru";
                    infoDesc.text = a.state == KittenAgent.State.Napping
                        ? "Mışıl mışıl uyuyor... Dokunup okşa, kalksın."
                        : "Eşyaları iter, üstlerine yatar! Başka bir yavrunun üstüne sürükle: Çırak Kedi.";
                    actionBtn.gameObject.SetActive(true);
                    actionBtn.Label().text = "Sahiplendir +3";
                    break;
                case 2:
                {
                    infoTitle.text = "Çırak Kedi";
                    var left = TimeSpan.FromTicks(Math.Max(0L, a.expires - DateTime.UtcNow.Ticks));
                    string time = left.TotalHours >= 1.0 ? (int)left.TotalHours + " sa " + left.Minutes + " dk" : left.Minutes + " dk " + left.Seconds + " sn";
                    infoDesc.text = "Dokun: bedava malzeme. Buradayken yavru gelmez. Kalan: " + time;
                    if (Cats.HasMaster) infoDesc.text += " (Dükkânda zaten Usta var.)";
                    else
                    {
                        actionBtn.gameObject.SetActive(true);
                        actionBtn.Label().text = "Usta Yap\n" + KittenChaos.PromoteCost + " altın";
                        btnImg.color = Gold >= KittenChaos.PromoteCost ? UIKit.Green : UIKit.Disabled;
                    }
                    break;
                }
                default:
                    infoTitle.text = "Usta Kedi";
                    infoDesc.text = "Kalıcı yardımcı! Her " + (int)KittenChaos.MasterInterval + " sn'de kendiliğinden malzeme getirir.";
                    break;
            }
        }

        void OnAction()
        {
            if (infoKitten)
            {
                if (infoKitten.level == 1) Cats.Adopt(infoKitten);
                else if (infoKitten.level == 2) Cats.Promote(infoKitten);
                return;
            }
            var d = Board.Get(Board.Selected);
            if (d != null && d.bubble) { Board.OpenBubble(Board.Selected); return; }
            if (d != null) { Board.SellSelected(); return; }
            if (resetArmedUntil > Time.unscaledTime)
            {
                resetArmedUntil = 0f;
                NewGame();
                return;
            }
            resetArmedUntil = Time.unscaledTime + 3f;
            ShowInfo(null);
        }

        // ------------------------------------------------------------------ level / xp
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            int ups = 0;
            while (Xp >= Levels.XpToNext(Level))
            {
                Xp -= Levels.XpToNext(Level);
                Level++;
                ups++;
                int g = Levels.GoldReward(Level);
                Gold += g;
                Stamps += 1;
                Energy = Mathf.Max(Energy, MaxEnergy);
                ShowDialog("Seviye " + Level + "!",
                    "Tebrikler! Ödül: +" + g + " altın, +1 Pati Mührü ve enerjin doldu. Maksimum enerji artık " + MaxEnergy +
                    ", depoda " + StorageSlots + " yer var.");
            }
            RefreshHud();
            if (levelBadge) Tween.Punch(levelBadge, ups > 0 ? 0.35f : 0.12f, 0.3f);
            if (ups > 0 && !OnBoard && home) home.Refresh(false);
            MarkDirty();
        }

        // ------------------------------------------------------------------ storage box
        public bool IsOverStorage(Vector2 screen) =>
            OnBoard && storageBtn && RectTransformUtility.RectangleContainsScreenPoint(storageBtn, screen, null);

        /// <summary>Called when an item is dropped on the storage button. Returns true if it was handled.</summary>
        public bool TryStore(Vector2Int cell)
        {
            var d = Board.Get(cell);
            if (d == null) return false;
            if (!Chains.IsIngredient(d.chain))
            {
                Toast("Depoya sadece malzemeler girer.");
                return false;
            }
            if (Storage.Count >= StorageSlots)
            {
                Toast("Depo dolu! (" + StorageSlots + " yer) Seviye atladıkça büyür.");
                return false;
            }
            Storage.Add(new ItemData(d.chain, d.level));
            Board.Remove(cell);
            Board.HideSelector();
            ShowInfo(null);
            Tween.Punch(storageBtn, 0.2f, 0.3f);
            Toast(d.Name + " depoya kondu.");
            RefreshHud();
            OnBoardChanged();
            return true;
        }

        public bool TakeFromStorage(int index)
        {
            if (index < 0 || index >= Storage.Count) return false;
            var center = new Vector2Int(BoardController.Cols / 2, BoardController.Rows - 1);
            if (!Board.NearestEmpty(center, out var e))
            {
                Toast("Masa dolu! Önce yer aç.");
                return false;
            }
            var d = Storage[index];
            Storage.RemoveAt(index);
            Board.Set(e, d, true);
            RefreshHud();
            OnBoardChanged();
            return true;
        }

        // ------------------------------------------------------------------ pumpkin event
        void RefreshEventButton()
        {
            if (!eventTimerText) return;
            eventTimerText.text = EventActive ? EventTimeLeftText : "bitti";
            bool claimable = EventClaimed < PumpkinEvent.Path.Length && Pumpkins >= PumpkinEvent.Path[EventClaimed].need;
            eventBadge.SetActive(claimable);
        }

        public void AddPumpkins(int n)
        {
            if (n <= 0 || !EventActive) return;
            Pumpkins += n;
            if (eventBtn)
            {
                Tween.Punch(eventBtn, 0.25f, 0.35f);
                FloatText(eventBtn, "+" + n);
            }
            RefreshEventButton();
            MarkDirty();
        }

        void FloatText(RectTransform anchor, string text)
        {
            var t = UIKit.Txt("Float", Root, text, 40, UIKit.Orange);
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(160f, 60f);
            rt.position = anchor.position;
            Vector3 from = rt.position;
            float lift = 90f * Root.lossyScale.y;
            Tween.Run(0.9f, k =>
            {
                if (!rt) return;
                rt.position = from + Vector3.down * lift * Tween.OutCubic(k);
                t.color = new Color(1f, 0.66f, 0.3f, 1f - k * k);
            }, () => { if (rt) Destroy(rt.gameObject); });
        }

        public bool ClaimEventStep(int idx)
        {
            var path = PumpkinEvent.Path;
            if (idx != EventClaimed || idx >= path.Length || Pumpkins < path[idx].need) return false;
            var m = path[idx];
            switch (m.type)
            {
                case "gold": AddGold(m.amount); break;
                case "energy": Energy += m.amount; RefreshHud(); break;
                case "stamps": AddStamps(m.amount); break;
                default:
                    if (!GiveRandomMissingAny()) AddGold(50);
                    break;
            }
            EventClaimed++;
            Toast(PumpkinEvent.Name + " ödülü: " + PumpkinEvent.RewardText(m), UIKit.Orange);
            RefreshEventButton();
            Save();
            return true;
        }

        bool GiveRandomMissingAny()
        {
            var missing = new List<string>();
            foreach (var set in Stickers.Sets)
                foreach (var id in set.ids)
                    if (!stickers.Contains(id)) missing.Add(id);
            if (missing.Count == 0) return false;
            GiveSticker(missing[Random.Range(0, missing.Count)], false);
            return true;
        }

        // ------------------------------------------------------------------ economy
        public void AddGold(int amount)
        {
            Gold += amount;
            RefreshHud();
            if (goldIcon) Tween.Punch(goldIcon, 0.25f, 0.3f);
            MarkDirty();
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount)
            {
                Toast(amount + " altın gerekiyor.", new Color(1f, 0.6f, 0.5f));
                return false;
            }
            Gold -= amount;
            RefreshHud();
            MarkDirty();
            return true;
        }

        public void AddStamps(int amount)
        {
            Stamps += amount;
            RefreshHud();
            if (stampIcon) Tween.Punch(stampIcon, 0.3f, 0.35f);
            MarkDirty();
            if (!OnBoard && home) home.Refresh(false);
        }

        public bool TrySpendEnergy(int amount)
        {
            if (Energy < amount)
            {
                Toast("Enerjin bitti! Biraz bekle ya da altınla al (+).", new Color(1f, 0.6f, 0.5f));
                return false;
            }
            Energy -= amount;
            RefreshHud();
            MarkDirty();
            return true;
        }

        void BuyEnergy()
        {
            if (Gold < EnergyPackCost)
            {
                Toast(EnergyPackAmount + " enerji için " + EnergyPackCost + " altın gerekiyor.", new Color(1f, 0.6f, 0.5f));
                return;
            }
            Gold -= EnergyPackCost;
            Energy += EnergyPackAmount;
            RefreshHud();
            Toast("+" + EnergyPackAmount + " enerji! Kazan fokurduyor.");
            MarkDirty();
        }

        public void RegisterServed()
        {
            Served++;
            tipIndex++;
            MarkDirty();
        }

        void RefreshHud()
        {
            goldText.text = Gold.ToString();
            stampText.text = Stamps.ToString();
            energyText.text = Energy + "/" + MaxEnergy;
            levelText.text = Level.ToString();
            levelRing.fillAmount = Mathf.Clamp01(Xp / (float)Levels.XpToNext(Level));
            storageText.text = Storage.Count + "/" + StorageSlots;
        }

        public void OnBoardChanged()
        {
            Orders.Refresh();
            MarkDirty();
        }

        public void MarkDirty()
        {
            if (!dirty) saveTimer = 1.5f;
            dirty = true;
        }

        // ------------------------------------------------------------------ toast
        public void Toast(string msg, Color? color = null, float duration = 3f)
        {
            if (!toastText) return;
            toastText.text = msg;
            toastText.color = color ?? UIKit.Cream;
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo(duration));
        }

        IEnumerator ToastCo(float duration)
        {
            float t = 0f;
            toastGroup.alpha = 1f;
            Tween.Pop(toastText.transform, 0.25f);
            while (t < duration) { t += Time.unscaledDeltaTime; yield return null; }
            t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / 0.4f; yield return null; }
            toastGroup.alpha = 0f;
        }

        // ------------------------------------------------------------------ save / load
        void NewGame()
        {
            Gold = 20;
            Stamps = 1;
            Level = 1;
            Xp = 0;
            Storage.Clear();
            Pumpkins = 0;
            EventClaimed = 0;
            DailyIndex = 0;
            lastDailyDay = -1;
            eventStart = DateTime.UtcNow.Ticks;
            Energy = MaxEnergy;
            energyTimer = 0f;
            Served = 0;
            TasksDone = 0;
            KittenHintShown = false;
            introSeen = false;
            tipIndex = 0;
            stickers.Clear();
            claimedSets.Clear();
            clues.Clear();

            Cats.ClearAll();
            Board.ClearAll();
            Board.Set(new Vector2Int(1, 4), new ItemData(Chains.Kazan, 1), true);
            Board.Set(new Vector2Int(5, 4), new ItemData(Chains.Sandik, 1), true);
            Board.Set(new Vector2Int(2, 6), new ItemData(Chains.Ot, 1), true);
            Board.Set(new Vector2Int(3, 6), new ItemData(Chains.Ot, 1), true);
            Board.Set(new Vector2Int(4, 6), new ItemData(Chains.Mantar, 1), true);
            Board.Set(new Vector2Int(3, 7), new ItemData(Chains.Kristal, 1), true);
            Board.Set(new Vector2Int(2, 7), new ItemData(Chains.Ag, 1), true);

            Orders.ClearAll();
            Orders.FillEmpty();
            RefreshHud();
            ShowInfo(null);
            if (OnBoard) GoHome();
            home.ShowChapter(0);
            home.Refresh(false);
            Save();
            ShowIntro();
        }

        void ShowIntro()
        {
            if (introSeen) return;
            ShowDialog("Kara Pati", Story.IntroLine, () => { introSeen = true; MarkDirty(); MaybeShowDaily(); });
        }

        void Save()
        {
            dirty = false;
            if (Board == null || Orders == null || Cats == null) return;
            var s = new SaveData
            {
                gold = Gold,
                energy = Energy,
                energyTimer = energyTimer,
                stamp = DateTime.UtcNow.Ticks,
                served = Served,
                kittenHint = KittenHintShown,
                stamps = Stamps,
                tasksDone = TasksDone,
                introSeen = introSeen,
                stickers = new List<string>(stickers),
                clues = new List<string>(clues),
                level = Level,
                xp = Xp,
                eventStart = eventStart,
                pumpkins = Pumpkins,
                eventClaimed = EventClaimed,
                dailyIndex = DailyIndex,
                lastDailyDay = lastDailyDay,
                claimedSets = new List<string>(claimedSets)
            };
            Board.WriteSave(s.cells);
            Orders.WriteSave(s.orders);
            foreach (var it in Storage) s.storage.Add(new ReqSave { chain = it.chain, level = it.level });
            Cats.WriteSave(s.kittens);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) { NewGame(); return; }
            try
            {
                var s = JsonUtility.FromJson<SaveData>(json);
                if (s == null || s.cells == null || s.cells.Count == 0) { NewGame(); return; }
                Gold = s.gold;
                Served = s.served;
                KittenHintShown = s.kittenHint;
                Stamps = s.stamps;
                TasksDone = Mathf.Clamp(s.tasksDone, 0, Story.TotalTasks);
                clues.Clear();
                if (s.clues != null) foreach (var id in s.clues) clues.Add(id);
                introSeen = s.introSeen;
                stickers.Clear();
                if (s.stickers != null) foreach (var id in s.stickers) stickers.Add(id);
                claimedSets.Clear();
                if (s.claimedSets != null) foreach (var id in s.claimedSets) claimedSets.Add(id);

                eventStart = s.eventStart > 0 ? s.eventStart : DateTime.UtcNow.Ticks;
                Pumpkins = s.pumpkins;
                EventClaimed = s.eventClaimed;
                DailyIndex = Mathf.Clamp(s.dailyIndex, 0, DailyRewards.Days.Length - 1);
                lastDailyDay = s.lastDailyDay;
                Level = Mathf.Max(1, s.level);
                Xp = Mathf.Max(0, s.xp);
                Storage.Clear();
                if (s.storage != null)
                    foreach (var it in s.storage)
                        if (!string.IsNullOrEmpty(it.chain)) Storage.Add(new ItemData(it.chain, it.level));
                Energy = s.energy;
                energyTimer = s.energyTimer;
                if (Energy < MaxEnergy && s.stamp > 0)
                {
                    float elapsed = (float)TimeSpan.FromTicks(Math.Max(0, DateTime.UtcNow.Ticks - s.stamp)).TotalSeconds + energyTimer;
                    int gain = Mathf.FloorToInt(elapsed / EnergyRegenSec);
                    Energy = Mathf.Min(MaxEnergy, Energy + gain);
                    energyTimer = Energy < MaxEnergy ? elapsed % EnergyRegenSec : 0f;
                }
                Board.ReadSave(s.cells);
                Cats.ReadSave(s.kittens);
                Orders.ReadSave(s.orders);
                RefreshHud();
                ShowInfo(null);
                home.Refresh(false);
                if (!introSeen) ShowIntro();
                else
                {
                    Toast("Tekrar hoş geldin, Kara Pati!");
                    MaybeShowDaily();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("KaraPati: kayıt okunamadı, yeni oyun başlıyor. " + e.Message);
                NewGame();
            }
        }
    }
}
