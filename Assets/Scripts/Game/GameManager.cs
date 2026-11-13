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

        public const float EnergyRegenSec = 120f;   // one energy every 2 minutes (economy pass: the story lasts about 2-3 weeks)
        public const int EnergyPackCost = 20, EnergyPackAmount = 10;
        const string SaveKey = "karapati_save_v1";
        const string BackupKey = "karapati_save_v1_backup";   // last save that loaded fine
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
                if (left.TotalDays >= 1) return L.F("{0}g {1}sa", (int)left.TotalDays, left.Hours);
                if (left.TotalHours >= 1) return L.F("{0}sa {1}dk", (int)left.TotalHours, left.Minutes);
                return L.F("{0}dk {1}sn", left.Minutes, left.Seconds);
            }
        }
        public int TasksDone { get; private set; }
        public bool KittenHintShown { get; set; }
        bool introSeen;
        readonly HashSet<string> stickers = new HashSet<string>();
        readonly HashSet<string> claimedSets = new HashSet<string>();
        readonly List<int> friends = new List<int>();   // friendship points per customer (Customers.All order)
        readonly List<int> friendSeen = new List<int>();
        int friendGiftDay;
        readonly List<string> friendGifts = new List<string>();
        FriendBookPanel friendBook;
        AchievementsPanel achPanel;
        readonly HashSet<string> clues = new HashSet<string>();

        public RectTransform Root { get; private set; }
        public BoardController Board { get; private set; }
        public OrderPanel Orders { get; private set; }
        public Tutorial Tut { get; private set; }
        public KittenChaos Cats { get; private set; }
        public bool OnBoard { get; private set; }
        public bool OnTower { get; private set; }
        public TowerScreen TowerUI => tower;
        public bool ModalOpen => (album && album.IsOpen) || (caseFile && caseFile.IsOpen) || (storagePanel && storagePanel.IsOpen) || (eventPanel && eventPanel.IsOpen) || (dailyPanel && dailyPanel.IsOpen) || (recipePanel && recipePanel.IsOpen) || (itemInfo && itemInfo.IsOpen) || (chapterPanel && chapterPanel.IsOpen) || (decorPanel && decorPanel.IsOpen) || (expeditionPanel && expeditionPanel.IsOpen) || (housePanel && housePanel.IsOpen) || (ustaColorPanel && ustaColorPanel.IsOpen) || (gameRoom && gameRoom.IsOpen) || (friendBook && friendBook.IsOpen) || (achPanel && achPanel.IsOpen) || (dialogRoot && dialogRoot.activeSelf);

        HomeScreen home;
        AlbumPanel album;
        CaseFilePanel caseFile;
        RecipePanel recipePanel;
        ItemInfoPanel itemInfo;
        ChapterPanel chapterPanel;
        ExpeditionPanel expeditionPanel;
        HousePanel housePanel;
        UstaColorPanel ustaColorPanel;
        GameRoomPanel gameRoom;
        int miniDay;
        readonly int[] miniUsed = new int[MiniGames.Count];
        readonly int[] miniBest = new int[MiniGames.Count];
        public string UstaName { get; private set; } = "";
        public int UstaXp { get; private set; }
        public int UstaLevel => UstaLevels.LevelOf(UstaXp);
        readonly HashSet<string> hintsSeen = new HashSet<string>();

        /// <summary>True only the first time it is asked for this id (one-off guide hints).</summary>
        public bool FirstTime(string id)
        {
            if (!hintsSeen.Add(id)) return false;
            MarkDirty();
            return true;
        }

        public bool Seen(string id) => hintsSeen.Contains(id);

        // ------------------------------------------------------------------ new-feature intros
        // The first time a feature opens, Kara Pati explains it in a dialog; its home button wears a "YENİ" tag until opened.
        public bool GameRoomNew => GameRoomUnlocked && !Seen("open_gameroom");
        public bool FriendBookNew => Seen("intro_friendbook") && !Seen("open_friendbook");

        /// <summary>Called when the home screen refreshes: introduce features that just became available.</summary>
        public void CheckIntros()
        {
            if (OnBoard || OnTower || (Tut != null && Tut.Active)) return;
            if (GameRoomUnlocked && FirstTime("intro_gameroom"))
                ShowDialog(L.T("Kara Pati"), L.T("Oyun Odası açıldı! Fare Avı, Yumak Atma ve İksir Hafızası seni bekliyor. Her oyunu günde 2 kez bedava oynarsın, sonrası enerjiyle. Soldaki yeni düğmeye dokun!"));
            if (AnyFriendNew && FirstTime("intro_friendbook"))
                ShowDialog(L.T("Kara Pati"), L.T("Müşterilerinle dost oluyorsun! Sağdaki kalp düğmesi Dostluk Defteri'ni açar, herkesin hikâyesi orada. Bir müşteri Can Dostun olunca her gün kuleye uğrar ve hediye bırakır."));
        }

        // ------------------------------------------------------------------ achievements
        readonly Dictionary<string, int> stats = new Dictionary<string, int>();
        readonly List<string> achClaimed = new List<string>();   // "id#step" for every reward taken

        public int Stat(string key) => stats.TryGetValue(key, out var v) ? v : 0;

        /// <summary>Counts something for the achievements; a toast when it just reached a step.</summary>
        public void AddStat(string key, int amount = 1)
        {
            int before = Stat(key);
            stats[key] = before + amount;
            MarkDirty();
            var d = Achievements.Get(key);
            if (d == null) return;
            for (int k = 0; k < d.steps.Length; k++)
                if (before < d.steps[k] && before + amount >= d.steps[k])
                {
                    Toast(L.F("Başarım: {0} ({1})!", d.Name, Achievements.StepName(k)), UIKit.Orange, 3.5f);
                    Sfx.Play("gift", 1.2f);
                    break;
                }
        }

        /// <summary>How far the player is on an achievement (some are worked out from the save, so old saves count too).</summary>
        public int AchProgress(Achievements.Def d)
        {
            int n = 0;
            switch (d.id)
            {
                case "order": return Served;
                case "friend":
                    foreach (var c in Customers.All) if (IsBestFriend(c)) n++;
                    return n;
                case "chapter":
                    for (int c = 0; c < Story.Chapters.Length; c++)
                        if (TasksDone >= Story.ChapterStart(c) + Story.Chapters[c].tasks.Length) n++;
                    return n;
                case "sticker":
                    foreach (var s in Stickers.Sets) if (StickerCount(s) >= s.ids.Length) n++;
                    return n;
                case "decor":
                    n = placed.Count;
                    foreach (var kv in inventory) if (Decor.ParseItem(kv.Key, out _, out _)) n += kv.Value;
                    return Mathf.Max(n, Stat("decor"));
                default: return Stat(d.id);
            }
        }

        /// <summary>How many steps' rewards were taken (0..3).</summary>
        public int AchClaimed(Achievements.Def d)
        {
            int n = 0;
            while (n < d.steps.Length && achClaimed.Contains(d.id + "#" + n)) n++;
            return n;
        }

        /// <summary>The next step is reached and its reward is waiting.</summary>
        public bool AchReady(Achievements.Def d)
        {
            int k = AchClaimed(d);
            return k < d.steps.Length && AchProgress(d) >= d.steps[k];
        }

        public bool AnyAchReady
        {
            get { foreach (var d in Achievements.All) if (AchReady(d)) return true; return false; }
        }

        public void ClaimAchievement(Achievements.Def d)
        {
            if (!AchReady(d)) return;
            int k = AchClaimed(d);
            achClaimed.Add(d.id + "#" + k);
            AddGold(Achievements.Gold[k]);
            AddEnergy(Achievements.Energy[k]);
            Sfx.Play("gift");
            Toast(L.F("{0} ({1}): {2}", d.Name, Achievements.StepName(k), Achievements.RewardText(k)), UIKit.Orange, 3.5f);
            Save();
        }

        public void OpenAchievements() => achPanel.Open();

        public void OnAchievementsClosed()
        {
            if (!OnBoard && !OnTower) home.Refresh(false);
            Save();
        }

        // ------------------------------------------------------------------ best friends in the tower (can be turned off)
        public bool TowerFriendsOn { get; private set; } = true;

        public void SetTowerFriends(bool on)
        {
            if (TowerFriendsOn == on) return;
            TowerFriendsOn = on;
            MarkDirty();
            if (tower) tower.Refresh();
            Toast(on ? L.T("Can dostların yine kuleye uğrayacak!") : L.T("Can dostların artık kuleye gelmeyecek. Hediyelerini Dostluk Defteri'nden alabilirsin."));
        }

        /// <summary>One of today's visitors (the tower shows them; with visits off, the book gives their gift).</summary>
        public bool IsVisitorToday(CustomerDef c) => BestFriendsToday(TowerFriends.MaxVisitors).Contains(c);
        public bool UstaNew { get; private set; }
        public long UstaPlayAt { get; set; }
        Button infoBtn, popBtn;
        readonly HashSet<string> discovered = new HashSet<string>();
        readonly HashSet<string> chapterGifts = new HashSet<string>();
        int kazanLevel = 1, sandikLevel = 1;
        readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
        readonly List<PlacedSave> placed = new List<PlacedSave>();
        DecorPanel decorPanel;
        CanvasScaler scaler;
        CanvasGroup homeCg, boardCg, towerCg;
        TowerScreen tower;
        int towerFloors = 1;
        long lastFed, lastWatered;
        Button homeBtn;
        RectTransform storageBtn, levelBadge;
        RectTransform tripBtn, infoPanelRt;   // Master's expedition button next to the storage box
        Image tripIcon;
        Text tripText;
        int tripIconColor = -1;
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

        static readonly TrList tips = new TrList
        (
            L.N("Aynı iki eşyayı üst üste sürükle, birleşsinler."),
            L.N("Bahçe Sepeti'ne ve Kiler Dolabı'na dokunarak malzeme üret (1 enerji)."),
            L.N("Hayaletlerin istediklerini masada topla, sonra 'Ver'e bas."),
            L.N("Mor Simya Kazanı'na iki malzeme at: iksir kaynar, hayaletler iksire çok para verir!"),
            L.N("İki yavruyu üst üste sürükle: 2 saatlik bir Çırak Kedi olur!"),
            L.N("Siparişler Pati Mührü verir; mühürlerle dükkânı onarırsın.")
        );

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            Sfx.Init();
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
                int left = Mathf.CeilToInt(EnergyRegenSec - energyTimer);
                energyTimerText.text = Energy < MaxEnergy ? (left / 60) + ":" + (left % 60).ToString("00") : L.T("dolu");   // m:ss
            }
            else
            {
                energyTimer = 0f;
                energyTimerText.text = L.T("dolu");
            }


            eventTick -= Time.unscaledDeltaTime;
            if (eventTick <= 0f)
            {
                eventTick = 1f;
                RefreshEventButton();
                RefreshTripButton();
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
            var boardArea = UIKit.Section(boardRt, "Board", 556f, 1290f);   // right under the order cards; messages show at the bottom
            Board = boardArea.gameObject.AddComponent<BoardController>();
            Board.Build(boardArea);
            BuildInfoBar(UIKit.Section(boardRt, "InfoBar", 1916f, 134f));

            // --- witch's tower
            var towerRt = UIKit.Fill(Root, "TowerScreen");
            towerCg = towerRt.gameObject.AddComponent<CanvasGroup>();
            tower = towerRt.gameObject.AddComponent<TowerScreen>();
            tower.Build(towerRt);

            // --- shared overlays
            BuildTopBar(UIKit.Section(Root, "TopBar", 12f, 115f));

            var toastArea = UIKit.Section(Root, "Toast", 545f, 70f);
            toastRt = toastArea;
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

            var recipeRt = UIKit.Rect("RecipePanel", canvasRt);
            recipePanel = recipeRt.gameObject.AddComponent<RecipePanel>();
            recipePanel.Build();

            var infoRt = UIKit.Rect("ItemInfo", canvasRt);
            itemInfo = infoRt.gameObject.AddComponent<ItemInfoPanel>();
            itemInfo.Build();

            var chaptersRt = UIKit.Rect("ChapterPanel", canvasRt);
            chapterPanel = chaptersRt.gameObject.AddComponent<ChapterPanel>();
            chapterPanel.Build();

            var houseRt = UIKit.Rect("HousePanel", canvasRt);
            housePanel = houseRt.gameObject.AddComponent<HousePanel>();
            housePanel.Build();

            var colorRt = UIKit.Rect("UstaColorPanel", canvasRt);
            ustaColorPanel = colorRt.gameObject.AddComponent<UstaColorPanel>();
            ustaColorPanel.Build();

            var bookRt = UIKit.Rect("FriendBook", canvasRt);
            friendBook = bookRt.gameObject.AddComponent<FriendBookPanel>();
            friendBook.Build();

            var achRt = UIKit.Rect("Achievements", canvasRt);
            achPanel = achRt.gameObject.AddComponent<AchievementsPanel>();
            achPanel.Build();

            var roomRt = UIKit.Rect("GameRoom", canvasRt);
            gameRoom = roomRt.gameObject.AddComponent<GameRoomPanel>();
            gameRoom.Build();

            var tripRt = UIKit.Rect("ExpeditionPanel", canvasRt);
            expeditionPanel = tripRt.gameObject.AddComponent<ExpeditionPanel>();
            expeditionPanel.Build();

            var decorRt = UIKit.Rect("DecorPanel", canvasRt);
            decorPanel = decorRt.gameObject.AddComponent<DecorPanel>();
            decorPanel.Build();

            BuildDialog(canvasRt);

            Cats = gameObject.AddComponent<KittenChaos>();
            Cats.Init(Board.CatsLayer);

            Tut = gameObject.AddComponent<Tutorial>();
            Tut.Build(Root);

            // start on the shop screen
            OnBoard = false;
            boardCg.gameObject.SetActive(false);
            towerCg.gameObject.SetActive(false);
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
                Toast(L.F("Seviye {0}: {1}/{2} XP. XP hikâyeden gelir: görevler, bölüm sonları ve gizli ipuçları.", Level, Xp, Levels.XpToNext(Level)));
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

            // the Master's expedition: shown next to the storage box only while it is away
            var trip = UIKit.Btn("Trip", area, "", new Color(0.42f, 0.28f, 0.6f), () => Cats.TripButton(), 22);
            tripBtn = (RectTransform)trip.transform;
            tripBtn.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(182f, 0f), new Vector2(120f, 134f));
            tripIcon = UIKit.Img("Icon", tripBtn, ProcArt.SideCat(3, 4), Color.white);
            tripIcon.rectTransform.At(new Vector2(0f, 16f), new Vector2(92f, 92f));
            tripIcon.raycastTarget = false;
            tripText = trip.Label();
            tripText.alignment = TextAnchor.LowerCenter;
            tripText.fontStyle = FontStyle.Bold;
            tripBtn.gameObject.SetActive(false);

            homeBtn = UIKit.Btn("Home", area, "", new Color(0.3f, 0.55f, 0.6f), GoHome);
            ((RectTransform)homeBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-22f, 0f), new Vector2(150f, 134f));
            var hIcon = UIKit.Img("Icon", homeBtn.transform, ProcArt.House, Color.white);
            hIcon.rectTransform.sizeDelta = new Vector2(90f, 90f);

            var panel = UIKit.Img("Panel", area, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 186f, 186f, 0f, 0f);
            infoPanelRt = panel.rectTransform;
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
            // long texts shrink to fit instead of running over each other (the bar gets narrower when the trip button shows)
            infoTitle.resizeTextForBestFit = true;
            infoTitle.resizeTextMinSize = 20;
            infoTitle.resizeTextMaxSize = 34;
            infoTitle.verticalOverflow = VerticalWrapMode.Truncate;
            infoDesc.resizeTextForBestFit = true;
            infoDesc.resizeTextMinSize = 16;
            infoDesc.resizeTextMaxSize = 25;
            infoDesc.verticalOverflow = VerticalWrapMode.Truncate;
            actionBtn = UIKit.Btn("Action", panel.transform, "", UIKit.Orange, OnAction, 28);
            ((RectTransform)actionBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(176f, 96f));
            // "i" button: opens the merge chain / source info for the selected item
            infoBtn = UIKit.Btn("Info", panel.transform, "i", new Color(0.35f, 0.55f, 0.85f), OpenSelectedInfo, 40);
            ((RectTransform)infoBtn.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-200f, -8f), new Vector2(62f, 62f));
            infoBtn.Label().fontStyle = FontStyle.Bold;
            infoBtn.gameObject.SetActive(false);
            // L.T("Patlat"): a ghost bubble can be popped right away to free its cell
            popBtn = UIKit.Btn("Pop", panel.transform, L.T("Patlat"), new Color(0.85f, 0.3f, 0.35f), () => Board.PopBubble(Board.Selected), 24);
            ((RectTransform)popBtn.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-200f, -10f), new Vector2(110f, 48f));
            popBtn.Label().fontStyle = FontStyle.Bold;
            popBtn.gameObject.SetActive(false);
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

            var next = UIKit.Btn("Next", panel.transform, L.T("Devam"), UIKit.Orange, CloseDialog, 36);
            ((RectTransform)next.transform).Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f)).At(new Vector2(-30f, 26f), new Vector2(230f, 86f));
            dialogRoot.SetActive(false);
        }

        // ------------------------------------------------------------------ screens
        public void GoBoard()
        {
            if (OnBoard) return;
            OnBoard = true;
            Sfx.Play("whoosh");
            Cross(homeCg, boardCg);
            Orders.Refresh();
            ShowInfo(null);
            Tut.Begin();
        }

        public void GoHome()
        {
            if (!OnBoard) return;
            OnBoard = false;
            Sfx.Play("whoosh", 0.85f);
            Board.HideSelector();
            Cats.Deselect();
            infoKitten = null;
            home.Refresh(false);
            Cross(boardCg, homeCg);
            Save();
        }

        public void GoTower()
        {
            if (OnBoard || OnTower) return;
            OnTower = true;
            Sfx.Play("whoosh", 1.1f);
            tower.Refresh();
            Cross(homeCg, towerCg);
            tower.ScrollToFloor(0, true);
        }

        public void LeaveTower()
        {
            if (!OnTower) return;
            OnTower = false;
            tower.Deselect();
            Sfx.Play("whoosh", 0.85f);
            home.Refresh(false);
            Cross(towerCg, homeCg);
            Save();
        }

        // ------------------------------------------------------------------ tower & cat care
        public int TowerFloorsOpen => towerFloors;
        public float FoodLevel => Tower.Level(lastFed);
        public float WaterLevel => Tower.Level(lastWatered);
        public bool CatHappy => FoodLevel > Tower.HappyAbove && WaterLevel > Tower.HappyAbove;
        public bool CatNeedsCare => !CatHappy;

        public bool UnlockTowerFloor(int i)
        {
            if (i != towerFloors || i >= Tower.Floors) return false;
            if (!TrySpendGold(Tower.FloorPrice[i])) return false;
            towerFloors++;
            Sfx.Play("levelup");
            Toast(L.F("{0}. kat açıldı: {1}! Burayı süsleyebilirsin.", i + 1, Tower.FloorNames[i]), UIKit.Orange, 4f);
            Save();
            return true;
        }

        public bool FillFood()
        {
            if (FoodLevel > 0.95f) { Toast(L.T("Mama kabı zaten dolu.")); return false; }
            bool wasHappy = CatHappy;
            lastFed = DateTime.UtcNow.Ticks;
            Sfx.Play("spawn", 0.9f);
            Sfx.Play("paw", 1.15f, 0.8f);
            AnnounceCare(wasHappy);
            return true;
        }

        public bool FillWater()
        {
            if (WaterLevel > 0.95f) { Toast(L.T("Su kabı zaten dolu.")); return false; }
            bool wasHappy = CatHappy;
            lastWatered = DateTime.UtcNow.Ticks;
            Sfx.Play("bubble", 0.9f);
            Sfx.Play("paw", 1.15f, 0.8f);
            AnnounceCare(wasHappy);
            return true;
        }

        void AnnounceCare(bool wasHappy)
        {
            if (!wasHappy && CatHappy) Toast(L.F("Kara Pati mutlu! Siparişlerden +%{0} altın kazanırsın.", Mathf.RoundToInt(Tower.HappyBonus * 100f)), UIKit.Orange, 4f);
            else if (!CatHappy) Toast(FoodLevel <= Tower.HappyAbove ? L.T("Şimdi biraz mama...") : L.T("Şimdi biraz su..."));
            else Toast(L.T("Afiyet olsun Kara Pati!"));
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
            Toast(L.F("Günlük ödül: {0}", DailyRewards.Text(m)), UIKit.Orange);
            Sfx.Play("gift");
            lastDailyDay = DailyRewards.Today;
            DailyIndex = (DailyIndex + 1) % DailyRewards.Days.Length;
            Save();
            return true;
        }

        /// <summary>Editor test: jump to the next chapter's start (on the last chapter: one task before the finale).</summary>
        public void DebugSkipChapter()
        {
            int ch = Story.CurrentChapter(TasksDone);
            if (ch < Story.Chapters.Length - 1) TasksDone = Story.ChapterStart(ch + 1);
            else TasksDone = Mathf.Max(TasksDone, Story.TotalTasks - 1);
            Save();
            if (!OnBoard) { home.ShowChapter(Story.CurrentChapter(TasksDone)); home.PlayChapterIntro(Story.CurrentChapter(TasksDone)); }
            home.Refresh(false);
            Toast(Story.Chapters[Story.CurrentChapter(TasksDone)].name, UIKit.Orange);
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
        /// <summary>How many found clues the player has already seen in the case file.</summary>
        public int CluesSeen { get; private set; }

        public void OpenCaseFile()
        {
            caseFile.Open();
            if (CluesSeen != clues.Count) { CluesSeen = clues.Count; MarkDirty(); }   // the red badge goes away
            home.Refresh(false);
        }
        public void OpenRecipes() => recipePanel.Open();
        public void OpenChapters() => chapterPanel.Open();
        public void ShowHomeChapter(int ch) => home.ShowChapter(ch, true);
        public void RefreshHome() => home.Refresh(false);

        // ------------------------------------------------------------------ tower decoration (gold sink)
        public IReadOnlyList<PlacedSave> Placed => placed;
        public int InventoryCount(string id) => inventory.TryGetValue(id, out var n) ? n : 0;

        List<string> InventoryList()
        {
            var list = new List<string>();
            foreach (var kv in inventory) if (kv.Value > 0) list.Add(kv.Key + ":" + kv.Value);
            return list;
        }
        public void OpenTowerDecor(int floor) => decorPanel.Open(floor);

        public bool BuyTowerDecor(int slot, int variant)
        {
            if (!TrySpendGold(Decor.TowerPrice(slot, variant))) return false;
            string id = Decor.ItemId(slot, variant);
            inventory[id] = InventoryCount(id) + 1;
            AddStat("decor");
            Toast(L.F("{0} envantere eklendi!", Decor.ItemName(id)), UIKit.Orange);
            Save();
            return true;
        }

        /// <summary>A free ornament (e.g. found by the Master cat) goes straight into the tower inventory.</summary>
        public void GiftTowerDecor(string id)
        {
            inventory[id] = InventoryCount(id) + 1;
            MarkDirty();
        }

        /// <summary>Takes one ornament out of the inventory and puts it on a floor.</summary>
        public PlacedSave PlaceTowerDecor(string id, int floor, Vector2 pos)
        {
            if (InventoryCount(id) <= 0 || floor < 0 || floor >= towerFloors) return null;
            inventory[id] = InventoryCount(id) - 1;
            if (inventory[id] <= 0) inventory.Remove(id);
            var p = new PlacedSave { id = id, floor = floor, x = pos.x, y = pos.y };
            placed.Add(p);
            MarkDirty();
            return p;
        }

        /// <summary>Puts a placed ornament back into the inventory.</summary>
        public void StoreTowerDecor(PlacedSave p)
        {
            if (!placed.Remove(p)) return;
            inventory[p.id] = InventoryCount(p.id) + 1;
            MarkDirty();
        }

        /// <summary>Moves an ornament to the end of the list, so it is drawn in front of the others.</summary>
        public void BringTowerDecorToFront(PlacedSave p)
        {
            if (!placed.Remove(p)) return;
            placed.Add(p);
            MarkDirty();
        }

        /// <summary>Old saves bought ornaments for the story scenes; those now live in the tower inventory.</summary>
        void MigrateSceneDecor(List<string> oldOwned)
        {
            if (oldOwned == null || oldOwned.Count == 0) return;
            int moved = 0;
            foreach (var k in oldOwned)
            {
                var parts = k.Split('_');
                if (parts.Length != 3 || !int.TryParse(parts[1], out var s) || !int.TryParse(parts[2], out var v)) continue;
                string id = Decor.ItemId(s, v);
                if (!Decor.ParseItem(id, out _, out _)) continue;
                inventory[id] = InventoryCount(id) + 1;
                moved++;
            }
            if (moved > 0) StartCoroutine(After(1.5f, () => Toast(L.F("{0} eski süsün Cadı Kulesi envanterine taşındı. Kuleye gidip yerleştir!", moved), UIKit.Orange, 5f)));
        }

        // ------------------------------------------------------------------ generator upgrades (gold sink)
        public int GeneratorLevel(string chain) => chain == Chains.Kazan ? kazanLevel : chain == Chains.Sandik ? sandikLevel : 1;

        public void UpgradeGenerator(string chain)
        {
            int lvl = GeneratorLevel(chain);
            if (lvl >= Generators.MaxLevel) { Toast(L.F("{0} zaten en üst seviyede!", Chains.Name(chain, 1))); return; }
            int price = Generators.UpgradeCost(lvl);
            if (!TrySpendGold(price)) return;
            if (chain == Chains.Kazan) kazanLevel++; else sandikLevel++;
            lvl++;
            Toast(L.F("{0} {1}. seviyeye çıktı! {2}", Chains.Name(chain, 1), lvl, Generators.ChanceText(lvl)), UIKit.Orange, 4f);
            Sfx.Play("levelup", 1.1f);
            foreach (var c in Board.AllCells())
            {
                var d = Board.Get(c);
                if (d != null && d.chain == chain)
                {
                    Board.RefreshView(c);
                    var v = Board.ViewAt(c);
                    if (v) Tween.Pop(v.rt, 0.4f);
                    FX.Burst(Board.FxLayer, BoardController.CellPos(c), UIKit.Orange, 14);
                }
            }
            Board.RefreshSelection();
            Save();
        }

        // ------------------------------------------------------------------ chapter gifts
        public bool GiftClaimed(int chapter, int gift) => chapterGifts.Contains(ChapterGifts.Id(chapter, gift));

        public bool GiftReady(int chapter, int gift)
        {
            if (GiftClaimed(chapter, gift)) return false;
            int done = TasksDone - Story.ChapterStart(chapter);
            return done >= ChapterGifts.TasksNeeded(chapter, gift);
        }

        public bool AnyGiftReady
        {
            get
            {
                for (int c = 0; c < Story.Chapters.Length; c++)
                    for (int g = 0; g < ChapterGifts.At.Length; g++)
                        if (GiftReady(c, g)) return true;
                return false;
            }
        }

        public void ClaimChapterGift(int chapter, int gift)
        {
            if (!GiftReady(chapter, gift)) return;
            chapterGifts.Add(ChapterGifts.Id(chapter, gift));
            AddGold(ChapterGifts.Gold(chapter, gift));
            if (ChapterGifts.Energy(chapter, gift) > 0) { Energy += ChapterGifts.Energy(chapter, gift); RefreshHud(); }
            if (ChapterGifts.Stamps(chapter, gift) > 0) AddStamps(ChapterGifts.Stamps(chapter, gift));
            if (ChapterGifts.Sticker(chapter, gift) && !GiveRandomMissingAny()) AddGold(50);
            Toast(L.F("Sandık açıldı: {0}", ChapterGifts.Text(chapter, gift)), UIKit.Orange, 4f);
            Sfx.Play("gift");
            Save();
        }

        // ------------------------------------------------------------------ item encyclopedia
        public bool IsDiscovered(string chain, int level) => discovered.Contains(Recipes.Key(chain, level));

        public void Discover(string chain, int level)
        {
            if (string.IsNullOrEmpty(chain)) return;
            bool added = false;
            // reaching a level means the lower ones of the same chain were seen on the way
            int from = Chains.IsIngredient(chain) ? 1 : level;
            for (int l = from; l <= level; l++) added |= discovered.Add(Recipes.Key(chain, l));
            if (added) MarkDirty();
        }

        /// <summary>Item encyclopedia for any item, e.g. one a customer asks for.</summary>
        public void OpenItemInfo(ItemData d) { if (d != null) itemInfo.Open(d); }

        public void OpenHouse(Vector2Int hc) => housePanel.Open(hc);
        public void OpenUstaPanel() => ustaColorPanel.Open();

        /// <summary>Switch the language: save, then build every screen again in the new language.</summary>
        public void ChangeLanguage(string code)
        {
            if (code == L.Lang) return;
            Save();
            L.SetLanguage(code);
            Sfx.Play("page");
            I = null;
            Destroy(gameObject);
            // a fresh GameManager next frame, once this one (and its canvas) is gone
            Tween.Run(0.05f, _ => { }, () => { if (I == null) new GameObject("KaraPati Game").AddComponent<GameManager>(); });
        }

        // ------------------------------------------------------------------ game room (mini games)
        public bool GameRoomUnlocked => Level >= MiniGames.UnlockLevel;

        public void OpenGameRoom()
        {
            if (!GameRoomUnlocked)
            {
                Toast(L.F("Oyun Odası {0}. seviyede açılır. Hikâyede ilerledikçe XP kazanırsın!", MiniGames.UnlockLevel));
                Sfx.Play("error");
                return;
            }
            if (!Seen("open_gameroom")) { hintsSeen.Add("open_gameroom"); MarkDirty(); }
            gameRoom.Open();
        }

        public void OnGameRoomClosed()
        {
            if (!OnBoard && !OnTower) home.Refresh(false);
            Save();
        }

        /// <summary>Free plays come back every day at midnight.</summary>
        void RollMiniDay()
        {
            int today = DailyRewards.Today;
            if (miniDay == today) return;
            miniDay = today;
            for (int i = 0; i < miniUsed.Length; i++) miniUsed[i] = 0;
            MarkDirty();
        }

        public int MiniFreeLeft(int g)
        {
            RollMiniDay();
            return Mathf.Max(0, MiniGames.FreePerDay - miniUsed[g]);
        }

        /// <summary>For the "!" on the home button: a free play is waiting in a game that exists.</summary>
        public bool AnyMiniFree
        {
            get
            {
                if (!GameRoomUnlocked) return false;
                for (int g = 0; g < MiniGames.Count; g++)
                    if (MiniGames.Ready(g) && MiniFreeLeft(g) > 0) return true;
                return false;
            }
        }

        public int MiniBest(int g) => miniBest[g];

        /// <summary>Pay for one play: a free one if any is left today, otherwise energy. paid = energy was spent.</summary>
        public bool PayMini(int g, out bool paid)
        {
            paid = false;
            if (MiniFreeLeft(g) > 0)
            {
                miniUsed[g]++;
                MarkDirty();
                return true;
            }
            if (Energy < MiniGames.EnergyCost) return false;
            Energy -= MiniGames.EnergyCost;
            paid = true;
            RefreshHud();
            MarkDirty();
            return true;
        }

        /// <summary>Remember the score; true when it beats the best so far.</summary>
        public bool MiniRecord(int g, int score)
        {
            if (score <= miniBest[g]) return false;
            miniBest[g] = score;
            MarkDirty();
            return true;
        }

        /// <summary>Mini game prize: `count` ingredients (level 2-3) on the table, else in the storage box, else sold.</summary>
        public string GiveMiniChest(int count = 3)
        {
            var items = new List<ItemData>();
            for (int i = 0; i < count; i++) items.Add(new ItemData(Chains.RandomIngredient(), Random.Range(2, 4)));
            return GiveItems(items);
        }

        /// <summary>Items given away from another screen: on the table, else in the storage box, else sold. Says where they went.</summary>
        public string GiveItems(List<ItemData> items)
        {
            int onBoard = 0, stored = 0, sold = 0;
            foreach (var item in items)
            {
                if (Board.NearestEmpty(new Vector2Int(3, 5), out var e)) { Board.Set(e, item, true); onBoard++; }
                else if (Storage.Count < StorageSlots) { Storage.Add(item); Discover(item.chain, item.level); stored++; }
                else { AddGold(Chains.Value(item.level)); sold++; }
            }
            OnBoardChanged();
            RefreshHud();
            var parts = new List<string>();
            if (onBoard > 0) parts.Add(L.F("{0} malzeme masaya", onBoard));
            if (stored > 0) parts.Add(L.F("{0} malzeme depoya", stored));
            if (sold > 0) parts.Add(L.F("{0} malzeme yer olmadığı için altına", sold));
            return L.F("{0} gitti.", string.Join(", ", parts));
        }

        // ------------------------------------------------------------------ friendship book and best friends in the tower
        public void OpenFriendBook()
        {
            if (!Seen("open_friendbook")) { hintsSeen.Add("open_friendbook"); MarkDirty(); }
            friendBook.Open();
        }

        public void OnFriendBookClosed()
        {
            if (!OnBoard && !OnTower) home.Refresh(false);
            Save();
        }

        public int FriendSeen(CustomerDef c)
        {
            int i = Customers.IndexOf(c.id);
            return i >= 0 && i < friendSeen.Count ? friendSeen[i] : 0;
        }

        /// <summary>The player read this customer's story up to its current stage.</summary>
        public void MarkFriendSeen(CustomerDef c)
        {
            int i = Customers.IndexOf(c.id);
            if (i < 0) return;
            while (friendSeen.Count <= i) friendSeen.Add(0);
            friendSeen[i] = FriendStage(c);
            MarkDirty();
        }

        public bool FriendHasNew(CustomerDef c) => FriendStage(c) > FriendSeen(c);
        public bool AnyFriendNew { get { foreach (var c in Customers.All) if (FriendHasNew(c)) return true; return false; } }

        /// <summary>Best friends visiting the tower today: at most `max`, a different mix every day.</summary>
        public List<CustomerDef> BestFriendsToday(int max)
        {
            var list = new List<CustomerDef>();
            foreach (var c in Customers.All) if (IsBestFriend(c)) list.Add(c);
            var rng = new System.Random(DailyRewards.Today);
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        void RollFriendGiftDay()
        {
            if (friendGiftDay == DailyRewards.Today) return;
            friendGiftDay = DailyRewards.Today;
            friendGifts.Clear();
        }

        public bool FriendGiftReady(CustomerDef c)
        {
            RollFriendGiftDay();
            return IsBestFriend(c) && !friendGifts.Contains(c.id);
        }

        /// <summary>Take today's gift from a best friend; returns what it was. Each friend gives something in character.</summary>
        public string TakeFriendGift(CustomerDef c)
        {
            if (!FriendGiftReady(c)) return "";
            friendGifts.Add(c.id);
            string what;
            switch (c.id)
            {
                case "kurbaga": AddGold(15); what = L.F("+{0} altın", 15); break;
                case "boo": AddEnergy(5); what = L.F("+{0} enerji", 5); break;
                case "bocek": AddStamps(1); what = L.F("+{0} Pati Mührü", 1); break;
                case "sis": what = GiveItems(new List<ItemData> { new ItemData(Chains.Ag, 3) }); break;
                case "tirtil": AddGold(25); what = L.F("+{0} altın", 25); break;
                case "pus": AddEnergy(8); what = L.F("+{0} enerji", 8); break;
                case "kartal": what = GiveMiniChest(2); break;
                case "baykus": AddStamps(2); what = L.F("+{0} Pati Mührü", 2); break;
                case "ugultu": AddStamps(2); what = L.F("+{0} Pati Mührü", 2); break;
                case "ejderha": AddEnergy(15); what = L.F("+{0} enerji", 15); break;
                case "kontes": AddStamps(3); what = L.F("+{0} Pati Mührü", 3); break;
                default: AddGold(10); what = L.F("+{0} altın", 10); break;
            }
            Sfx.Play("gift");
            RefreshHud();
            MarkDirty();
            return what;
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            Energy += amount;
            RefreshHud();
            MarkDirty();
        }

        public void DebugGameRoom()
        {
            for (int i = 0; i < miniUsed.Length; i++) miniUsed[i] = 0;
            miniDay = DailyRewards.Today;
            gameRoom.Open();
        }

        /// <summary>The Master's name: the one the player gave, or L.T("Usta Kedi") / "Gri Usta".</summary>
        public string UstaDisplayName(int col) =>
            !string.IsNullOrEmpty(UstaName) ? UstaName : col == KittenColors.Gece ? L.T("Usta Kedi") : L.F("{0} Usta", UstaColorPanel.ColorName(col));

        public void SetUstaName(string n)
        {
            n = (n ?? "").Trim();
            if (n.Length > UstaColorPanel.NameLimit) n = n.Substring(0, UstaColorPanel.NameLimit);
            if (n == UstaName) return;
            UstaName = n;
            if (n.Length > 0) Toast("Usta Kedi'nin yeni adı: " + n + "!", UIKit.Orange);
            SeenUsta();
            if (infoKitten && infoKitten.level == 3) ShowKittenInfo(infoKitten);
            RefreshUstaViews();
            MarkDirty();
        }

        /// <summary>The Master and Kara Pati played: the happiness bar fills a little (both bowls 5% fuller).</summary>
        public void UstaPlayed(float cooldownHours)
        {
            UstaPlayAt = DateTime.UtcNow.Ticks + TimeSpan.FromHours(cooldownHours).Ticks;
            long boost = TimeSpan.FromHours(Tower.BowlHours * 0.05f).Ticks, now = DateTime.UtcNow.Ticks;
            if (lastFed > 0) lastFed = Math.Min(now, lastFed + boost);
            if (lastWatered > 0) lastWatered = Math.Min(now, lastWatered + boost);
            MarkDirty();
        }

        /// <summary>The Master did some work. A new level brings a toast with its perk.</summary>
        public void AddUstaXp(int amount)
        {
            if (amount <= 0) return;
            int before = UstaLevel;
            UstaXp += amount;
            int after = UstaLevel;
            if (after > before)
            {
                Toast(L.F("{0} {1}. seviyeye çıktı! {2}", UstaDisplayName(Cats != null ? Cats.MasterColor : KittenColors.Gece), after, UstaLevels.Perk(after)), UIKit.Orange, 5f);
                Sfx.Play("levelup", 1.2f);
            }
            MarkDirty();
        }

        public void UstaPromoted()
        {
            UstaNew = true;
            RefreshUstaViews();
            MarkDirty();
        }

        public void SeenUsta()
        {
            if (!UstaNew) return;
            UstaNew = false;
            RefreshUstaViews();
            MarkDirty();
        }

        /// <summary>Redraw everything that shows the Master: home button, tower cushion.</summary>
        public void RefreshUstaViews()
        {
            if (home) home.RefreshUstaButton();
            if (tower) tower.RefreshUsta();
        }
        public KittenAgent InfoKitten => infoKitten;

        void OpenSelectedInfo()
        {
            var d = Board.Get(Board.Selected);
            if (d == null) return;
            if (Chains.IsStation(d.chain)) { OpenRecipes(); return; }
            itemInfo.Open(d);
        }

        // ------------------------------------------------------------------ clues
        public bool HasClue(string id) => clues.Contains(id);

        public void FindClue(Clue c)
        {
            if (clues.Contains(c.id)) return;
            clues.Add(c.id);
            Sfx.Play("ready", 1.15f);
            AddStamps(1);
            AddXp(Levels.ClueXp);
            Save();
            ShowDialog(c.title, c.text, () =>
            {
                Toast(L.F("İpucu Cadı Dosyası'na eklendi: +1 Pati Mührü, +{0} XP", Levels.ClueXp), UIKit.Orange);
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
            if (speaker == L.T("Cadı Hanım")) return ProcArt.Witch;   // speakers arrive already translated
            if (speaker == L.T("Gölge")) return ProcArt.SideCat(0, 4);
            if (speaker == L.T("Usta Kedi")) return ProcArt.SideCat(3, 4);
            if (I != null && I.Cats != null && I.Cats.HasAnyMaster && speaker == I.UstaDisplayName(I.Cats.MasterColor)) return ProcArt.SideCat(3, 4, I.Cats.MasterColor);
            var cust = Customers.ByName(speaker);
            if (cust != null) return ProcArt.Customer(cust.id, I != null && I.FriendStage(cust) >= cust.FullLookStage);
            int gi = Ghosts.Names.IndexOf(speaker);
            return gi >= 0 ? ProcArt.Ghost(gi) : ProcArt.KaraPatiHead;
        }

        // ------------------------------------------------------------------ story
        public void DoTask(int idx)
        {
            if (idx != TasksDone || idx >= Story.TotalTasks) return;
            var task = Story.TaskAt(idx);
            if (Stamps < task.cost)
            {
                Toast(L.F("{0} Pati Mührü gerekiyor. 'Dükkânı Aç'a bas, sipariş hazırla!", task.cost), new Color(1f, 0.6f, 0.5f));
                Tween.Punch(stampIcon, 0.3f, 0.3f);
                return;
            }
            int chapter = Story.CurrentChapter(idx);
            Stamps -= task.cost;
            TasksDone++;
            Sfx.Play("stamp");
            Sfx.Play("ready", 1f, 0.6f);
            AddXp(Levels.TaskXp);
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
                            AddXp(Levels.ChapterXp);
                            Toast(L.F("{0} bitti: +{1} XP!", Story.Chapters[chapter].name, Levels.ChapterXp), UIKit.Orange, 3.5f);
                            if (chapter + 1 < Story.Chapters.Length)
                            {
                                home.ShowChapter(chapter + 1);
                                home.PlayChapterIntro(chapter + 1);
                            }
                            else PlayFinale(0);
                        });
                    }
                    else if (Stamps < Story.TaskAt(TasksDone).cost && TasksDone <= 2) Toast(Story.FirstBoardHint, UIKit.Orange, 4.5f);
                    else if (HasNewClue()) Toast(L.T("Göz düğmesine bas: yeni bir ipucu belirdi!"), UIKit.Orange, 4f);
                });
            }));
        }

        // the whole story is done: a short epilogue, then a big reward
        void PlayFinale(int i)
        {
            if (i < Story.Finale.Length)
            {
                ShowDialog(Story.Finale[i][0], Story.Finale[i][1], () => PlayFinale(i + 1));
                return;
            }
            AddGold(Story.FinaleGold);
            AddStamps(Story.FinaleStamps);
            Save();
            FX.Burst(Root, new Vector2(0f, 200f), UIKit.Orange, 30);
            Toast(L.F("TEBRİKLER! Hikâyeyi bitirdin: +{0} altın, +{1} Pati Mührü", Story.FinaleGold, Story.FinaleStamps), UIKit.Orange, 6f);
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
            Toast(L.F("Büyük ödül! +{0} altın, +{1} mühür, +{2} enerji", Stickers.GrandGold, Stickers.GrandStamps, Stickers.GrandEnergy), UIKit.Orange, 4.5f);
            Save();
            Sfx.Play("gift");
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
                    Toast(L.F("Kopya etiket ({0}): +5 altın", Stickers.Name(id)));
                }
                return;
            }
            stickers.Add(id);
            ShowStickerPopup(id);
            var set = Stickers.SetOf(id);
            if (set != null && IsSetClaimable(set))
                StartCoroutine(After(2.6f, () => Toast(L.F("\"{0}\" seti tamamlandı! Albümden ödülünü al.", set.name), UIKit.Orange, 4f)));
            MarkDirty();
        }

        void GiveRandomMissing(StickerSet set)
        {
            var missing = new List<string>();
            foreach (var id in set.ids) if (!stickers.Contains(id)) missing.Add(id);
            if (missing.Count > 0) GiveSticker(missing[Random.Range(0, missing.Count)], false);
        }

        // ------------------------------------------------------------------ customer friendships
        public int FriendPoints(CustomerDef c)
        {
            int i = Customers.IndexOf(c.id);
            return i >= 0 && i < friends.Count ? friends[i] : 0;
        }

        public int FriendStage(CustomerDef c) => c.StageOf(FriendPoints(c));
        public bool IsBestFriend(CustomerDef c) => FriendStage(c) >= c.MaxStage;

        /// <summary>A delivered order makes a customer a bit more of a friend; new stages bring a line and a reward.</summary>
        public void AddFriendship(string id, int points)
        {
            var c = Customers.Get(id);
            if (c == null || points <= 0) return;
            int i = Customers.IndexOf(id);
            while (friends.Count <= i) friends.Add(0);
            int before = c.StageOf(friends[i]);
            friends[i] = Mathf.Min(friends[i] + points, c.PointsFor(c.MaxStage));
            int after = c.StageOf(friends[i]);
            for (int st = before + 1; st <= after; st++)
            {
                int en = Customers.StageEnergy(st), sp = Customers.StageStamps(c, st);
                Energy += en;
                AddStamps(sp);
                string title = Customers.StageName(c, st);
                string extra = st >= c.MaxStage ? " " + L.T("Siparişleri artık +1 Pati Mührü verir!") : "";
                ShowDialog(c.name, c.lines[Mathf.Clamp(st - 1, 0, c.lines.Length - 1)]);
                Toast(L.F("{0} ile {1} oldunuz! +{2} enerji, +{3} mühür.", c.name, title, en, sp) + extra, UIKit.Orange, 5f);
                Sfx.Play(st >= c.MaxStage ? "gift" : "levelup", 1.1f);
            }
            RefreshHud();
            MarkDirty();
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
            Toast(L.F("\"{0}\" ödülü: +{1} altın, +{2} mühür!", s.name, Stickers.SetRewardGold, Stickers.SetRewardStamps), UIKit.Orange);
            Save();
            Sfx.Play("gift");
            return true;
        }

        void ShowStickerPopup(string id)
        {
            Sfx.Play("ready", 1.25f, 0.7f);
            stickerPopIcon.sprite = ProcArt.StickerIcon(id);
            var set = Stickers.SetOf(id);
            stickerPopText.text = L.T("<b>Yeni etiket!</b>") + "\n" + Stickers.Name(id) + (set != null ? "  <size=24>(" + set.name + ")</size>" : "");
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
            if (infoBtn) infoBtn.gameObject.SetActive(d != null && !d.bubble && !Chains.IsHouse(d.chain));
            if (popBtn) popBtn.gameObject.SetActive(d != null && d.bubble);
            infoTitle.rectTransform.offsetMax = new Vector2(d != null && d.bubble ? -320f : -196f, -14f);   // room for L.T("Patlat")

            if (d == null)
            {
                infoTitle.text = L.T("İpucu");
                infoDesc.text = tips[tipIndex % tips.Length];
                actionBtn.gameObject.SetActive(false);   // nothing selected: just the tip, no button
                return;
            }

            infoTitle.text = d.Name + (Chains.IsSpecial(d.chain) ? "" : "  <size=26>(" + L.F("Sv. {0}", d.level) + ")</size>");
            if (d.reserved)
            {
                infoDesc.text = L.T("Minik Çırak bunu kazana taşıyacak. Vazgeçmek için Minik Çırak'a dokun.");
                actionBtn.gameObject.SetActive(false);
                return;
            }
            actionBtn.gameObject.SetActive(false);
            btnImg.color = UIKit.Orange;

            if (d.nap)
            {
                infoDesc.text = L.T("Üstünde bir yavru uyuyor! Dokunup okşarsan kalkar.");
                return;
            }
            if (d.bubble)
            {
                int left = Mathf.Max(0, Mathf.CeilToInt((float)TimeSpan.FromTicks(d.bubbleUntil - DateTime.UtcNow.Ticks).TotalSeconds));
                infoTitle.text = d.Name + "  <size=26>(Baloncuk)</size>";
                infoDesc.text = L.F("Bedava kopya! {0} sn içinde açmazsan uçup gider.", left);
                int price = Bubbles.Price(d.level);
                actionBtn.gameObject.SetActive(true);
                actionBtn.Label().text = L.F("Aç\n{0} altın", price);
                btnImg.color = Gold >= price ? UIKit.Green : UIKit.Disabled;
                return;
            }
            if (Chains.IsStation(d.chain))
            {
                actionBtn.gameObject.SetActive(true);
                if (d.BrewReady)
                {
                    infoDesc.text = L.F("{0} hazır! Kazana dokun ya da al.", Chains.Name(Chains.Iksir, d.brew));
                    actionBtn.Label().text = L.T("Al!");
                    btnImg.color = UIKit.Green;
                }
                else if (d.Brewing)
                {
                    long left = d.brewUntil - DateTime.UtcNow.Ticks;
                    int sec = Mathf.CeilToInt((float)TimeSpan.FromTicks(left).TotalSeconds);
                    int price = Recipes.SkipPrice(left);
                    infoDesc.text = L.F("{0} kaynıyor... {1} sn kaldı.", Chains.Name(Chains.Iksir, d.brew), sec);
                    actionBtn.Label().text = L.F("Hızlandır\n{0} altın", price);
                    btnImg.color = Gold >= price ? UIKit.Orange : UIKit.Disabled;
                }
                else if (!string.IsNullOrEmpty(d.mix) && Recipes.ParseKey(d.mix, out var mc, out var ml))
                {
                    infoDesc.text = L.F("İçinde: {0}. Eşini sürükle, iksir kaynasın!", Chains.Name(mc, ml));
                    actionBtn.Label().text = L.T("Geri Al");
                }
                else
                {
                    infoDesc.text = L.F("İki malzemeyi kazana sürükle, iksir kaynasın. {0}/{1} tarif açık.", Recipes.UnlockedCount(TasksDone), Recipes.All.Length);
                    actionBtn.Label().text = L.T("Tarifler");
                }
                var stirrer = Cats.StirrerAt(Board.Selected);
                if (stirrer)
                    infoDesc.text += "\n" + L.F("{0} Çırak karıştırıyor: {1} kat hızlı.", KittenColors.Name(stirrer.color), stirrer.color == KittenColors.Gri ? L.T("2,5") : "2");
                return;
            }
            if (Chains.IsHouse(d.chain))
            {
                bool full = d.cats >= KittenChaos.HouseSlots;
                infoDesc.text = full
                    ? (Cats.HasApprentice ? L.T("İki yavru içeride. Masadaki Çırak gidince yenisini çıkarabilirsin.") : L.T("İki yavru içeride! Dokun: Çırak Kedi çıksın."))
                    : L.F("İçeride {0}/{1} yavru. Yavruyu üstüne sürükle: Usta ve Çırak onu kovalamaz.", d.cats, KittenChaos.HouseSlots);
                if (full && !Cats.HasApprentice)
                {
                    actionBtn.gameObject.SetActive(true);
                    actionBtn.Label().text = L.T("Çırak Yap");
                    btnImg.color = UIKit.Green;
                }
                return;
            }
            if (Chains.IsPotion(d.chain))
            {
                var r = Recipes.Get(d.level);
                infoDesc.text = L.F("Hayaletler bu iksiri sipariş eder: {0} altın + {1} mühür!", r != null ? r.Gold : 0, Recipes.PotionStamps);
                actionBtn.gameObject.SetActive(true);
                actionBtn.Label().text = L.F("Sat  +{0}", BoardController.PotionSellPrice(d.level));
                return;
            }
            if (Chains.IsGenerator(d.chain))
            {
                int gl = GeneratorLevel(d.chain);
                infoTitle.text = d.Name + "  <size=26>(" + L.F("Sv. {0}/{1}", gl, Generators.MaxLevel) + ")</size>";
                infoDesc.text = (d.chain == Chains.Kazan ? L.T("Kedi otu ve balık çıkarır.") : L.T("Süt ve yün çıkarır.")) + " " + L.T("Dokun: 1 enerji.") + "\n" + Generators.ChanceText(gl);
                actionBtn.gameObject.SetActive(true);
                if (gl < Generators.MaxLevel)
                {
                    int price = Generators.UpgradeCost(gl);
                    actionBtn.Label().text = L.F("Yükselt\n{0} altın", price);
                    btnImg.color = Gold >= price ? UIKit.Green : UIKit.Disabled;
                }
                else
                {
                    actionBtn.Label().text = L.T("En üst\nseviye");
                    btnImg.color = UIKit.Disabled;
                }
                return;
            }
            int max = Chains.MaxLevel(d.chain);
            string src = Chains.Name(Chains.SourceOf(d.chain), 1);
            infoDesc.text = (d.level < max
                ? L.F("İki tanesini birleştir: {0}", IsDiscovered(d.chain, d.level + 1) ? Chains.Name(d.chain, d.level + 1) : "???")
                : L.T("En üst seviye! Hayaletler buna bayılır.")) + "\n" + L.F("Kaynak: {0}  (i: seviye listesi)", src);
            actionBtn.gameObject.SetActive(true);
            actionBtn.Label().text = L.F("Sat  +{0}", d.level);
        }

        public void ShowKittenInfo(KittenAgent a)
        {
            if (infoTitle == null) return;
            if (!a) { ShowInfo(null); return; }
            infoKitten = a;
            kittenInfoTimer = 1f;
            if (infoBtn) infoBtn.gameObject.SetActive(false);
            if (popBtn) popBtn.gameObject.SetActive(false);
            infoTitle.rectTransform.offsetMax = new Vector2(-196f, -14f);

            var btnImg = actionBtn.GetComponent<Image>();
            btnImg.color = UIKit.Orange;
            actionBtn.gameObject.SetActive(false);
            string colName = KittenColors.Name(a.color);
            switch (a.level)
            {
                case 1:
                    if (a.Training)
                    {
                        var tl = TimeSpan.FromTicks(Math.Max(0L, a.trainUntil - DateTime.UtcNow.Ticks));
                        infoTitle.text = L.T("Eğitimdeki Yavru") + "  <size=24>(" + colName + ")</size>";
                        infoDesc.text = L.F("Usta'dan ders alıyor. {0} dk {1} sn sonra Minik Çırak olacak.", tl.Minutes, tl.Seconds);
                        break;
                    }
                    if (a.star)
                    {
                        var sl = TimeSpan.FromTicks(Math.Max(0L, a.starUntil - DateTime.UtcNow.Ticks));
                        infoTitle.text = "<color=#FFD24D>" + L.T("Yıldızlı Yavru") + "</color>  <size=24>(" + colName + ")</size>";
                        infoDesc.text = L.T("Usta'nın üstüne bırak: Yıldızlı Minik Çırak olur. Beklerken topun yanına sürükle, orada oynasın.")
                            + "\n" + L.F("{0} sonra gidecek.", sl.TotalHours >= 1.0 ? L.F("{0} sa {1} dk", (int)sl.TotalHours, sl.Minutes) : L.F("{0} dk {1} sn", sl.Minutes, sl.Seconds));
                        break;   // no "Sahiplendir": a star kitten is never given away by mistake
                    }
                    infoTitle.text = L.T("Yaramaz Yavru") + "  <size=24>(" + colName + ")</size>";
                    if (a.state == KittenAgent.State.Napping) infoDesc.text = L.T("Mışıl mışıl uyuyor. Dokun, kalksın.");
                    else if (Cats.TrainBlock() == null) infoDesc.text = L.T("Usta'nın üstüne bırak: Minik Çırak olur. Kedi Evi'nde de güvende kalır.");
                    else if (Cats.FindHouse(out _)) infoDesc.text = L.T("Kedi Evi'ne sürükle: orada kovalanmaz, iki yavru Çırak olur.");
                    else infoDesc.text = L.T("Başka bir yavrunun üstüne sürükle: Çırak Kedi olurlar.");
                    actionBtn.gameObject.SetActive(true);
                    actionBtn.Label().text = L.T("Sahiplendir +3");
                    break;
                case 2:
                {
                    infoTitle.text = L.F("{0} Çırak", colName);
                    var left = TimeSpan.FromTicks(Math.Max(0L, a.expires - DateTime.UtcNow.Ticks));
                    string time = left.TotalHours >= 1.0 ? L.F("{0} sa {1} dk", (int)left.TotalHours, left.Minutes) : L.F("{0} dk {1} sn", left.Minutes, left.Seconds);
                    infoDesc.text = (a.Stirring
                        ? (Cats.PotBrewing(a.stirCell) ? L.T("Kazanı karıştırıyor, iksir hızlı pişiyor.") : L.T("Kazanın başında iksir bekliyor."))
                        : KittenColors.Talents[Mathf.Clamp(a.color, 0, KittenColors.Count - 1)]) + "\n" + L.F("Kalan: {0}", time);
                    if (!Cats.HasMaster && !Cats.UstaAway)
                    {
                        actionBtn.gameObject.SetActive(true);
                        actionBtn.Label().text = L.F("Usta Yap\n{0} altın", KittenChaos.PromoteCost);
                        btnImg.color = Gold >= KittenChaos.PromoteCost ? UIKit.Green : UIKit.Disabled;
                    }
                    break;
                }
                case 4:
                {
                    infoTitle.text = a.star ? "<color=#FFD24D>" + L.T("Yıldızlı Minik Çırak") + "</color>  <size=24>(" + colName + ")</size>" : L.F("{0} Minik Çırak", colName);
                    var left = TimeSpan.FromTicks(Math.Max(0L, a.expires - DateTime.UtcNow.Ticks));
                    infoDesc.text = (Cats.MiniBusy ? Cats.MiniStatus
                        : a.star ? L.T("Siparişlerde 'Hazırla'ya bas: iksiri ve malzemeleriyle siparişi baştan sona kendisi hazırlar.")
                        : L.T("Tarif Defteri'nde 'Yaptır' de, malzemeleri kazana taşısın."))
                        + "\n" + L.F("Kalan: {0}", left.TotalHours >= 1.0 ? L.F("{0} sa {1} dk", (int)left.TotalHours, left.Minutes) : L.F("{0} dk {1} sn", left.Minutes, left.Seconds));
                    actionBtn.gameObject.SetActive(true);
                    // while it runs for a recipe: L.T("Vazgeç") drops the job and frees the locked items
                    actionBtn.Label().text = Cats.MiniBusy ? L.T("Vazgeç") : L.T("Tarifler");
                    btnImg.color = Cats.MiniBusy ? new Color(0.85f, 0.3f, 0.3f) : UIKit.Green;
                    break;
                }
                default:
                    infoTitle.text = UstaDisplayName(a.color);
                    if (a.state == KittenAgent.State.Sitting)
                    {
                        infoDesc.text = L.T("Bir yavruya ders veriyor. Eğitim bitince yine malzeme getirir.");
                        break;
                    }
                    infoDesc.text = a.homed ? L.T("Yastığından malzeme getirir, yavrulara tıslar. Yavru bırakırsan eğitir, keşfe de çıkar.")
                                            : L.T("Onu tezgahın sağındaki mor yastığa sürükle: oradan malzeme getirir, yavrulara tıslar.");
                    actionBtn.gameObject.SetActive(true);
                    actionBtn.Label().text = L.T("Keşfe\nGönder");
                    btnImg.color = UIKit.Green;
                    break;
            }
        }

        void OnAction()
        {
            if (infoKitten)
            {
                if (infoKitten.level == 1) Cats.Adopt(infoKitten);
                else if (infoKitten.level == 2) Cats.Promote(infoKitten);
                else if (infoKitten.level == 4) { if (Cats.MiniBusy) { Cats.CancelMiniJob(); ShowKittenInfo(infoKitten); } else OpenRecipes(); }
                else if (infoKitten.state != KittenAgent.State.Sitting) expeditionPanel.Open();
                return;
            }
            var d = Board.Get(Board.Selected);
            if (d != null && d.bubble) { Board.OpenBubble(Board.Selected); return; }
            if (d != null && Chains.IsStation(d.chain)) { Board.PotAction(); return; }
            if (d != null && Chains.IsHouse(d.chain)) { Cats.MakeApprenticeFromHouse(Board.Selected); ShowInfo(Board.Get(Board.Selected)); return; }
            if (d != null && Chains.IsGenerator(d.chain)) { UpgradeGenerator(d.chain); return; }
            if (d != null) { Board.SellSelected(); return; }
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
                Sfx.Play("levelup");
                Stamps += 1;
                Energy = Mathf.Max(Energy, MaxEnergy);
                ShowDialog(L.F("Seviye {0}!", Level),
                    L.F("Tebrikler! Ödül: +{0} altın, +1 Pati Mührü ve enerjin doldu. Maksimum enerji artık {1}, depoda {2} yer var.", g, MaxEnergy, StorageSlots) +
                    (Level == MiniGames.UnlockLevel ? "\n\n" + L.T("Yeni: Oyun Odası açıldı! Ana ekrandaki yumak düğmesine dokun.") : ""));
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
                Toast(L.T("Depoya sadece malzemeler girer."));
                return false;
            }
            if (Storage.Count >= StorageSlots)
            {
                Toast(L.F("Depo dolu! ({0} yer) Seviye atladıkça büyür.", StorageSlots));
                return false;
            }
            Storage.Add(new ItemData(d.chain, d.level));
            Board.Remove(cell);
            Board.HideSelector();
            ShowInfo(null);
            Tween.Punch(storageBtn, 0.2f, 0.3f);
            Toast(L.F("{0} depoya kondu.", d.Name));
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
                Toast(L.T("Masa dolu! Önce yer aç."));
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
            eventTimerText.text = EventActive ? EventTimeLeftText : L.T("bitti");
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
            Toast(L.F("{0} ödülü: {1}", PumpkinEvent.Name, PumpkinEvent.RewardText(m)), UIKit.Orange);
            RefreshEventButton();
            Save();
            Sfx.Play("gift");
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
                Toast(L.F("{0} altın gerekiyor.", amount), new Color(1f, 0.6f, 0.5f));
                Sfx.Play("error");
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

        /// <summary>Show/hide the expedition button next to the storage box and update its countdown.</summary>
        public void RefreshTripButton()
        {
            if (!tripBtn || Cats == null) return;
            bool away = Cats.UstaAway;
            if (tripBtn.gameObject.activeSelf != away)
            {
                tripBtn.gameObject.SetActive(away);
                // make room: the info panel starts after the trip button while it is visible
                infoPanelRt.offsetMin = new Vector2(away ? 312f : 186f, infoPanelRt.offsetMin.y);
            }
            if (!away) return;
            bool ready = Cats.TripReady;
            int want = ready ? -2 : Cats.TripColor;
            if (want != tripIconColor)
            {
                tripIconColor = want;
                tripIcon.sprite = ready ? ProcArt.Classic(Chains.Sandik, 1) : ProcArt.SideCat(3, 4, Cats.TripColor);
            }
            if (ready)
            {
                tripText.text = L.T("Döndü!");
                tripText.color = new Color(1f, 0.85f, 0.4f);
                Tween.Punch(tripBtn, 0.06f, 0.25f);
            }
            else
            {
                var left = TimeSpan.FromTicks(Math.Max(0L, Cats.TripUntil - DateTime.UtcNow.Ticks));
                tripText.text = (int)left.TotalHours + ":" + left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00");
                tripText.color = UIKit.Cream;
            }
        }

        public bool TrySpendEnergy(int amount)
        {
            if (Energy < amount)
            {
                Toast(L.T("Enerjin bitti! Biraz bekle ya da altınla al (+)."), new Color(1f, 0.6f, 0.5f));
                Sfx.Play("error");
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
                Toast(L.F("{0} enerji için {1} altın gerekiyor.", EnergyPackAmount, EnergyPackCost), new Color(1f, 0.6f, 0.5f));
                return;
            }
            Gold -= EnergyPackCost;
            Energy += EnergyPackAmount;
            RefreshHud();
            Toast(L.F("+{0} enerji! Kazan fokurduyor.", EnergyPackAmount));
            MarkDirty();
        }

        public void RegisterServed()
        {
            Served++;
            tipIndex++;
            MarkDirty();
        }

        public void RefreshHud()
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
        RectTransform toastRt;

        public void Toast(string msg, Color? color = null, float duration = 3f)
        {
            if (!toastText) return;
            // on the table the messages show at the bottom, just above the info bar, so they don't cover
            // the counter where the cats say their words; elsewhere they stay under the top bar
            if (toastRt) toastRt.anchoredPosition = new Vector2(0f, OnBoard ? -1848f : -545f);
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
            Tut.Step = 0;
            discovered.Clear();
            chapterGifts.Clear();
            kazanLevel = sandikLevel = 1;
            towerFloors = 1;
            lastFed = lastWatered = 0;
            inventory.Clear();
            placed.Clear();
            tipIndex = 0;
            stickers.Clear();
            claimedSets.Clear();
            clues.Clear();
            CluesSeen = 0;
            friends.Clear();
            friendSeen.Clear();
            friendGifts.Clear();
            friendGiftDay = 0;
            TowerFriendsOn = true;
            stats.Clear();
            achClaimed.Clear();
            UstaName = "";
            hintsSeen.Clear();
            UstaNew = false;
            UstaPlayAt = 0;
            UstaXp = 0;
            miniDay = 0;
            System.Array.Clear(miniUsed, 0, miniUsed.Length);
            System.Array.Clear(miniBest, 0, miniBest.Length);

            Cats.ClearAll();
            Board.ClearAll();
            Board.Set(new Vector2Int(1, 4), new ItemData(Chains.Kazan, 1), true);
            Board.Set(new Vector2Int(5, 4), new ItemData(Chains.Sandik, 1), true);
            Board.Set(BoardController.PotCell, new ItemData(Chains.Simya, 1), true);   // on the counter
            Board.Set(BoardController.HouseCell, new ItemData(Chains.KediEvi, 1), true);
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
            if (OnTower) LeaveTower();
            home.ShowChapter(0);
            home.Refresh(false);
            tower.RebuildDecor();
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
                tutorialStep = Tut.Step,
                discovered = new List<string>(discovered),
                chapterGifts = new List<string>(chapterGifts),
                kazanLevel = kazanLevel,
                sandikLevel = sandikLevel,
                towerFloors = towerFloors,
                lastFed = lastFed,
                lastWatered = lastWatered,
                inventory = InventoryList(),
                placed = new List<PlacedSave>(placed),
                stickers = new List<string>(stickers),
                clues = new List<string>(clues),
                cluesSeen = CluesSeen,
                level = Level,
                xp = Xp,
                eventStart = eventStart,
                pumpkins = Pumpkins,
                eventClaimed = EventClaimed,
                dailyIndex = DailyIndex,
                lastDailyDay = lastDailyDay,
                claimedSets = new List<string>(claimedSets),
                friends = new List<int>(friends),
                friendSeen = new List<int>(friendSeen),
                friendGiftDay = friendGiftDay,
                friendGifts = new List<string>(friendGifts),
                towerFriendsOff = !TowerFriendsOn,
                statKeys = new List<string>(stats.Keys),
                statVals = new List<int>(stats.Values),
                achClaimed = new List<string>(achClaimed)
            };
            Board.WriteSave(s.cells);
            // a real game always has machines on the table; an empty board means the game state was wiped
            // (e.g. Unity reloaded scripts mid-play), so never overwrite a good save with it
            if (s.cells.Count == 0) { Debug.LogWarning("KaraPati: masa boş görünüyor, kayıt atlandı."); return; }
            Orders.WriteSave(s.orders);
            foreach (var it in Storage) s.storage.Add(new ReqSave { chain = it.chain, level = it.level });
            Cats.WriteSave(s.kittens);
            s.ustaTrip = Cats.Trip + 1;
            s.ustaTripUntil = Cats.TripUntil;
            s.ustaTripColor = Cats.TripColor + 1;
            s.ustaName = UstaName;
            s.ustaXp = UstaXp;
            s.hints = new List<string>(hintsSeen);
            s.ustaNew = UstaNew;
            s.ustaPlayAt = UstaPlayAt;
            s.miniDay = miniDay;
            s.miniUsed = new List<int>(miniUsed);
            s.miniBest = new List<int>(miniBest);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        static bool IsValidSave(string json, out SaveData s)
        {
            s = null;
            if (string.IsNullOrEmpty(json)) return false;
            try { s = JsonUtility.FromJson<SaveData>(json); }
            catch { return false; }
            return s != null && s.cells != null && s.cells.Count > 0;
        }

        void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            // a broken or empty main save falls back to the backup copy before starting over
            if (!IsValidSave(json, out var check))
            {
                string backup = PlayerPrefs.GetString(BackupKey, "");
                if (IsValidSave(backup, out _))
                {
                    Debug.LogWarning("KaraPati: ana kayıt bozuk, yedek kayıttan yükleniyor.");
                    json = backup;
                }
            }
            if (string.IsNullOrEmpty(json)) { NewGame(); return; }
            try
            {
                var s = JsonUtility.FromJson<SaveData>(json);
                if (s == null || s.cells == null || s.cells.Count == 0) { NewGame(); return; }
                // this one is good: keep a copy
                PlayerPrefs.SetString(BackupKey, json);
                PlayerPrefs.Save();
                Gold = s.gold;
                Served = s.served;
                KittenHintShown = s.kittenHint;
                Stamps = s.stamps;
                TasksDone = Mathf.Clamp(s.tasksDone, 0, Story.TotalTasks);
                clues.Clear();
                if (s.clues != null) foreach (var id in s.clues) clues.Add(id);
                CluesSeen = Mathf.Min(s.cluesSeen, clues.Count);
                introSeen = s.introSeen;
                Tut.Step = s.tutorialStep;
                discovered.Clear();
                if (s.discovered != null) foreach (var k in s.discovered) discovered.Add(k);
                chapterGifts.Clear();
                if (s.chapterGifts != null) foreach (var k in s.chapterGifts) chapterGifts.Add(k);
                kazanLevel = Mathf.Clamp(s.kazanLevel, 1, Generators.MaxLevel);
                sandikLevel = Mathf.Clamp(s.sandikLevel, 1, Generators.MaxLevel);
                towerFloors = Mathf.Clamp(s.towerFloors, 1, Tower.Floors);
                lastFed = s.lastFed;
                lastWatered = s.lastWatered;
                inventory.Clear();
                if (s.inventory != null)
                    foreach (var k in s.inventory)
                    {
                        int i = k.IndexOf(':');
                        if (i > 0 && int.TryParse(k.Substring(i + 1), out var n) && n > 0) inventory[k.Substring(0, i)] = n;
                    }
                placed.Clear();
                if (s.placed != null)
                    foreach (var p in s.placed)
                        if (p != null && Decor.ParseItem(p.id, out _, out _) && p.floor >= 0 && p.floor < towerFloors) placed.Add(p);
                MigrateSceneDecor(s.decorOwned);
                // players from before the guide existed already know the basics
                if (Tut.Step == 0 && (s.served > 0 || s.tasksDone > 0)) Tut.Step = Tutorial.Done;
                stickers.Clear();
                if (s.stickers != null) foreach (var id in s.stickers) stickers.Add(id);
                claimedSets.Clear();
                if (s.claimedSets != null) foreach (var id in s.claimedSets) claimedSets.Add(id);
                friends.Clear();
                if (s.friends != null) friends.AddRange(s.friends);
                friendSeen.Clear();
                if (s.friendSeen != null) friendSeen.AddRange(s.friendSeen);
                friendGiftDay = s.friendGiftDay;
                friendGifts.Clear();
                if (s.friendGifts != null) friendGifts.AddRange(s.friendGifts);
                TowerFriendsOn = !s.towerFriendsOff;
                if (s.statKeys != null && s.statVals != null)
                    for (int k = 0; k < s.statKeys.Count && k < s.statVals.Count; k++) stats[s.statKeys[k]] = s.statVals[k];
                if (s.achClaimed != null) achClaimed.AddRange(s.achClaimed);

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
                Board.EnsurePot();
                Board.ArrangeCounter();   // older saves: pot and house move onto the counter
                Cats.ReadSave(s.kittens);
                Cats.SetTrip(s.ustaTrip - 1, s.ustaTripUntil, s.ustaTripColor > 0 ? s.ustaTripColor - 1 : KittenColors.Gece);
                UstaName = s.ustaName ?? "";
                UstaXp = s.ustaXp;
                hintsSeen.Clear();
                if (s.hints != null) foreach (var h in s.hints) hintsSeen.Add(h);
                UstaNew = s.ustaNew;
                UstaPlayAt = s.ustaPlayAt;
                miniDay = s.miniDay;
                for (int i = 0; i < MiniGames.Count; i++)
                {
                    miniUsed[i] = s.miniUsed != null && i < s.miniUsed.Count ? s.miniUsed[i] : 0;
                    miniBest[i] = s.miniBest != null && i < s.miniBest.Count ? s.miniBest[i] : 0;
                }
                Orders.ReadSave(s.orders);
                RefreshHud();
                ShowInfo(null);
                home.Refresh(false);
                tower.RebuildDecor();
                if (!introSeen) ShowIntro();
                else
                {
                    Toast(L.T("Tekrar hoş geldin, Kara Pati!"));
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
