using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    public static class Chains
    {
        public const string Ot = "ot", Mantar = "mantar", Kristal = "kristal", Ag = "ag";
        public const string Kedi = "kedi", Kazan = "kazan", Sandik = "sandik";
        public const string Iksir = "iksir", Simya = "simya";
        public const string KediEvi = "kedievi";   // the kittens' little house on the table

        public static readonly string[] Ingredients = { Ot, Mantar, Kristal, Ag };

        static readonly Dictionary<string, string[]> names = new Dictionary<string, string[]>
        {
            { Ot, new[] { L.N("Yaprak"), L.N("Ot Demeti"), L.N("Adaçayı Destesi"), L.N("Büyülü Ot"), L.N("Ay Çiçeği") } },
            { Mantar, new[] { L.N("Mantar"), L.N("Benekli Mantar"), L.N("Mantar Kümesi"), L.N("Parlayan Mantar"), L.N("Peri Mantarı") } },
            { Kristal, new[] { L.N("Kum Tanesi"), L.N("Çakıl Taşı"), L.N("Kristal"), L.N("Ay Taşı"), L.N("Yıldız Taşı") } },
            { Ag, new[] { L.N("Örümcek Ağı"), L.N("İplik Yumağı"), L.N("İpek Yumak"), L.N("Gölge Kumaşı"), L.N("Hayalet Şal") } },
            { Kedi, new[] { L.N("Yaramaz Yavru"), L.N("Çırak Kedi"), L.N("Usta Kedi") } },
            { Kazan, new[] { L.N("Kazan") } },
            { Sandik, new[] { L.N("Eski Sandık") } },
            { Iksir, new[] { L.N("Uyku İksiri"), L.N("Işıltı İksiri"), L.N("Gölge İksiri"), L.N("Ay İksiri"), L.N("Hayalet İksiri"), L.N("Yıldız İksiri") } },
            { Simya, new[] { L.N("Simya Kazanı") } },
            { KediEvi, new[] { L.N("Kedi Evi") } },
        };

        public static int MaxLevel(string chain) => names[chain].Length;
        public static string Name(string chain, int level) => L.T(names[chain][Mathf.Clamp(level - 1, 0, names[chain].Length - 1)]);
        public static bool IsIngredient(string c) => c == Ot || c == Mantar || c == Kristal || c == Ag;
        public static bool IsGenerator(string c) => c == Kazan || c == Sandik;
        public static bool IsPotion(string c) => c == Iksir;
        public static bool IsStation(string c) => c == Simya;
        public static bool IsHouse(string c) => c == KediEvi;
        /// <summary>Items that show no level badge (machines, potions, the cat house).</summary>
        public static bool IsSpecial(string c) => IsGenerator(c) || IsPotion(c) || IsStation(c) || IsHouse(c);
        public static int Value(int level) => 5 * (1 << (level - 1));

        /// <summary>The board machine an item comes from (empty for machines themselves).</summary>
        public static string SourceOf(string c)
        {
            if (c == Ot || c == Mantar) return Kazan;
            if (c == Kristal || c == Ag) return Sandik;
            if (c == Iksir) return Simya;
            return "";
        }

        public static string ChainTitle(string c)
        {
            switch (c)
            {
                case Ot: return L.T("Otlar");
                case Mantar: return L.T("Mantarlar");
                case Kristal: return L.T("Taşlar ve Kristaller");
                case Ag: return L.T("Ağlar ve Kumaşlar");
                case Iksir: return L.T("İksirler");
                default: return L.T("Dükkân Eşyası");
            }
        }
        public static string RandomIngredient() => Ingredients[UnityEngine.Random.Range(0, Ingredients.Length)];
    }

    [Serializable]
    public class ItemData
    {
        public string chain;
        public int level = 1;
        public bool nap;   // a kitten is sleeping on top of it
        public float cd;   // cooldown for helper cats
        public bool bubble;        // free copy floating in a ghost bubble; must be bought before use
        public long bubbleUntil;   // UTC ticks when the bubble pops
        // alchemy cauldron state
        public string mix = "";    // first ingredient waiting in the pot ("chain:level")
        public int brew;           // recipe id being brewed / ready (0 = none)
        public long brewUntil;     // UTC ticks when the potion is ready
        public int cats;           // cat house: kittens waiting inside (0-2)
        public string catCols = "";  // cat house: their colours, one digit each (e.g. "03")
        [NonSerialized] public bool readyNotified;

        public bool Brewing => brew > 0 && DateTime.UtcNow.Ticks < brewUntil;
        public bool BrewReady => brew > 0 && DateTime.UtcNow.Ticks >= brewUntil;

        public ItemData(string chain, int level)
        {
            this.chain = chain;
            this.level = level;
        }

        public bool IsKitten => chain == Chains.Kedi;
        public bool IsWildKitten => chain == Chains.Kedi && level == 1;
        public string Name => Chains.Name(chain, level);
        /// <summary>Cannot be dragged, merged, sold, stored or delivered right now.</summary>
        [NonSerialized] public bool reserved;   // a Mini Apprentice is about to carry this to the pot (not saved)
        public bool Locked => nap || bubble || reserved;
    }

    public class Recipe
    {
        public int id;
        public string aChain, bChain;
        public int aLevel, bLevel;
        public int seconds;
        public int chapter;   // chapter index that unlocks it
        public string Name => Chains.Name(Chains.Iksir, id);
        public int Gold => 2 * (Chains.Value(aLevel) + Chains.Value(bLevel));
        public bool Uses(string chain, int level) => (chain == aChain && level == aLevel) || (chain == bChain && level == bLevel);
    }

    /// <summary>Alchemy: drop two ingredients into the Simya Kazanı, wait, get a potion ghosts love.</summary>
    public static class Recipes
    {
        public static readonly Recipe[] All =
        {
            new Recipe { id = 1, aChain = Chains.Ot, aLevel = 2, bChain = Chains.Mantar, bLevel = 2, seconds = 30, chapter = 0 },
            new Recipe { id = 2, aChain = Chains.Kristal, aLevel = 2, bChain = Chains.Ot, bLevel = 3, seconds = 45, chapter = 0 },
            new Recipe { id = 3, aChain = Chains.Ag, aLevel = 3, bChain = Chains.Mantar, bLevel = 3, seconds = 60, chapter = 1 },
            new Recipe { id = 4, aChain = Chains.Kristal, aLevel = 3, bChain = Chains.Ot, bLevel = 4, seconds = 90, chapter = 2 },
            new Recipe { id = 5, aChain = Chains.Ag, aLevel = 4, bChain = Chains.Kristal, bLevel = 4, seconds = 120, chapter = 3 },
            new Recipe { id = 6, aChain = Chains.Kristal, aLevel = 5, bChain = Chains.Mantar, bLevel = 4, seconds = 180, chapter = 4 },
        };

        public const int PotionStamps = 3;

        public static Recipe Get(int id) => id >= 1 && id <= All.Length ? All[id - 1] : null;
        public static bool Unlocked(Recipe r, int tasksDone) => Story.CurrentChapter(tasksDone) >= r.chapter;

        public static string Key(string chain, int level) => chain + ":" + level;

        public static bool ParseKey(string key, out string chain, out int level)
        {
            chain = null; level = 0;
            if (string.IsNullOrEmpty(key)) return false;
            int i = key.IndexOf(':');
            if (i <= 0) return false;
            chain = key.Substring(0, i);
            return int.TryParse(key.Substring(i + 1), out level);
        }

        /// <summary>Can this ingredient go into an empty pot?</summary>
        public static bool AnyUses(string chain, int level, int tasksDone)
        {
            foreach (var r in All) if (Unlocked(r, tasksDone) && r.Uses(chain, level)) return true;
            return false;
        }

        public static Recipe Match(string c1, int l1, string c2, int l2, int tasksDone)
        {
            foreach (var r in All)
            {
                if (!Unlocked(r, tasksDone)) continue;
                if ((r.aChain == c1 && r.aLevel == l1 && r.bChain == c2 && r.bLevel == l2) ||
                    (r.aChain == c2 && r.aLevel == l2 && r.bChain == c1 && r.bLevel == l1)) return r;
            }
            return null;
        }

        /// <summary>Partners for an ingredient already waiting in the pot.</summary>
        public static List<Recipe> PartnersOf(string chain, int level, int tasksDone)
        {
            var list = new List<Recipe>();
            foreach (var r in All) if (Unlocked(r, tasksDone) && r.Uses(chain, level)) list.Add(r);
            return list;
        }

        public static int UnlockedCount(int tasksDone)
        {
            int n = 0;
            foreach (var r in All) if (Unlocked(r, tasksDone)) n++;
            return n;
        }

        public static int SkipPrice(long ticksLeft) => Mathf.Max(1, Mathf.CeilToInt((float)TimeSpan.FromTicks(ticksLeft).TotalSeconds / 5f));
    }

    public static class Bubbles
    {
        public const float Seconds = 60f;
        public static float Chance(int level) => level >= 4 ? 0.14f : 0.1f;
        public static int Price(int level) => 6 * level + 4 * (level - 1) * (level - 1);
    }

    [Serializable] public class CellSave { public int x, y; public string chain; public int level; public bool nap; public float cd; public bool bubble; public long bubbleUntil; public string mix; public int brew; public long brewUntil; public int cats; public string catCols; }
    [Serializable] public class ReqSave { public string chain; public int level; }
    [Serializable] public class PlacedSave { public string id; public int floor; public float x, y; public bool flip; }

    [Serializable]
    public class OrderSave
    {
        public string cust;    // customer id (Customers.All)
        public string ghost;   // display name (old saves only had this)
        public int tint;
        public int line;
        public List<ReqSave> reqs = new List<ReqSave>();
    }

    [Serializable]
    public class KittenSave
    {
        public int level;
        public float x, y;
        public bool napping;
        public int napX, napY;
        public long expires;
        public float cd;
        public bool stir;          // apprentice sitting at the alchemy pot, stirring
        public int stirX, stirY;
        public int colorPlus;      // fur colour + 1 (0 in old saves = the old look for that level)
        public long trainUntil;    // kitten being trained by the Master: becomes a Mini Apprentice at this UTC tick
        public string carryChain;  // Mini Apprentice: ingredient it was carrying to the pot
        public int carryLevel;
        public int homeState;      // Master: 0 = save from before the cushion, 1 = not on its cushion yet, 2 = on its cushion
        public bool star;          // a rare star kitten (or the star Mini Apprentice it became)
        public long starUntil;     // star kitten: leaves at this UTC tick unless the Master trains it
    }

    [Serializable]
    public class SaveData
    {
        public int version = 3;
        public int gold;
        public int energy;
        public float energyTimer;
        public long stamp;      // save time (ticks)
        public int served;
        public bool kittenHint;
        public int stamps;      // Pati Mührü
        public int tasksDone;
        public bool introSeen;
        public int tutorialStep;
        public List<string> discovered = new List<string>();
        public List<string> chapterGifts = new List<string>();
        public int kazanLevel, sandikLevel;   // generator upgrades (0 in old saves = level 1)
        public List<string> decorOwned = new List<string>();      // "chapter_slot_variant"
        public List<string> decorSelected = new List<string>();   // "chapter_slot:variant" 
        public int towerFloors;                 // how many tower floors are open (0 in old saves = 1)
        public long lastFed, lastWatered;       // UTC ticks when the bowls were last filled
        public List<string> inventory = new List<string>();      // tower ornaments not placed yet, "s0v1:2"
        public List<PlacedSave> placed = new List<PlacedSave>();
        public int ustaTrip;                    // Master cat's expedition: 0 = at home, otherwise trip index + 1
        public long ustaTripUntil;              // UTC ticks when the Master comes back
        public int ustaTripColor;               // colour + 1 of the Master who is away
        public string ustaName;                 // name the player gave the Master Cat ("" = none)
        public int ustaXp;                      // the Master's experience (UstaLevels)
        public List<string> hints = new List<string>();   // one-off guide hints already shown
        public bool ustaNew;                    // a new Master is waiting to be named ("!" on the Usta Kedim button)
        public long ustaPlayAt;                 // UTC ticks when the Master and Kara Pati can play with the yarn again
        public List<int> friends = new List<int>();   // friendship points per customer (Customers.All order)
        public List<int> friendSeen = new List<int>();   // friendship stage whose story the player already read in the Friendship Book
        public int friendGiftDay;                     // day the list below belongs to (DailyRewards.Today)
        public List<string> friendGifts = new List<string>();   // best friends whose gift in the tower was taken today // ornaments placed on tower floors   // claimed progress-bar gifts, e.g. "2_1" 
        public List<string> clues = new List<string>();
        public int cluesSeen;                   // clues already looked at in the witch's case file (the red badge shows the rest)
        public int miniDay;                     // day the mini game plays below were counted on (DailyRewards.Today)
        public List<int> miniUsed = new List<int>();   // free plays used today, per mini game (MiniGames order)
        public List<int> miniBest = new List<int>();   // best score per mini game
        public int level = 1;
        public int xp;
        public List<ReqSave> storage = new List<ReqSave>();
        public long eventStart;
        public int pumpkins;
        public int eventClaimed;
        public int dailyIndex;
        public int lastDailyDay = -1;
        public List<CellSave> cells = new List<CellSave>();
        public List<OrderSave> orders = new List<OrderSave>();
        public List<KittenSave> kittens = new List<KittenSave>();
        public List<string> stickers = new List<string>();
        public List<string> claimedSets = new List<string>();
    }

    public class StickerSet
    {
        string name_;
        public string id;
        public string name { get => L.T(name_); set => name_ = value; }   // texts translate themselves when read
        public string[] ids;
    }

    public static class Stickers
    {
        public const int SetRewardGold = 100, SetRewardStamps = 3;
        public const int GrandGold = 300, GrandStamps = 10, GrandEnergy = 30;
        public const string GrandId = "all";

        public static readonly StickerSet[] Sets =
        {
            new StickerSet { id = "ghosts", name = L.N("Hayalet Dostlar"), ids = new[] { "g0", "g1", "g2", "g3", "g4", "g5", "g6", "g7" } },
            new StickerSet { id = "cats", name = L.N("Dükkân Kedileri"), ids = new[] { "c_yavru", "c_uykucu", "c_cirak", "c_usta", "c_karapati", "c_hayaletkedi" } },
            new StickerSet { id = "witch", name = L.N("Cadının Eşyaları"), ids = new[] { "w_sapka", "w_supurge", "w_kitap", "w_kure", "w_kazan", "w_mektup" } },
            new StickerSet { id = "renkli", name = L.N("Renkli Çıraklar"), ids = new[] { "r0", "r1", "r2", "r3", "r4" } },
        };

        static readonly Dictionary<string, string> names = new Dictionary<string, string>
        {
            { "c_yavru", L.N("Yaramaz Yavru") }, { "c_uykucu", L.N("Uykucu Yavru") }, { "c_cirak", L.N("Çırak Kedi") },
            { "c_usta", L.N("Usta Kedi") }, { "c_karapati", L.N("Kara Pati") }, { "c_hayaletkedi", L.N("Hayalet Kedi") },
            { "w_sapka", L.N("Sivri Şapka") }, { "w_supurge", L.N("Uçan Süpürge") }, { "w_kitap", L.N("Büyü Kitabı") },
            { "w_kure", L.N("Kristal Küre") }, { "w_kazan", L.N("Eski Kazan") }, { "w_mektup", L.N("Kayıp Mektup") },
            { "r0", L.N("Turuncu Tekir Çırak") }, { "r1", L.N("Gri Çırak") }, { "r2", L.N("Beyaz Çırak") },
            { "r3", L.N("Siyah-Beyaz Çırak") }, { "r4", L.N("Üç Renkli Çırak") },
        };

        public static string Name(string id)
        {
            if (id.StartsWith("g") && int.TryParse(id.Substring(1), out int gi) && gi >= 0 && gi < Ghosts.Names.Length)
                return Ghosts.Names[gi];
            return names.TryGetValue(id, out var n) ? L.T(n) : id;
        }

        public static StickerSet SetOf(string id)
        {
            foreach (var s in Sets)
                if (Array.IndexOf(s.ids, id) >= 0) return s;
            return null;
        }
    }

    public class StoryTask
    {
        string title_, speaker_, line_;
        public string id;
        public string title { get => L.T(title_); set => title_ = value; }
        public string speaker { get => L.T(speaker_); set => speaker_ = value; }
        public string line { get => L.T(line_); set => line_ = value; }
        public int cost;
    }

    public class Chapter
    {
        string name_, endLine_;
        public string id;
        public string name { get => L.T(name_); set => name_ = value; }
        public string endLine { get => L.T(endLine_); set => endLine_ = value; }
        public StoryTask[] tasks;
    }

    /// <summary>A hidden clue in a scene. Becomes findable once enough story tasks are done.</summary>
    public class Clue
    {
        string title_, text_;
        public string id;
        public string title { get => L.T(title_); set => title_ = value; }
        public string text { get => L.T(text_); set => text_ = value; }
        public int chapter;
        public float x, y;          // position in the scene (scene-local units)
        public int afterTasks;      // global number of finished tasks needed before it shows up
    }

    public static class Story
    {
        public static string IntroLine => L.T("Cadı ortadan kayboldu, dükkân da toz içinde kaldı. Hayaletlere iksir malzemesi verip Pati Mührü kazanalım, dükkânı adım adım toparlayalım. İlk iş: şu örümcek ağları!");

        /// <summary>Epilogue after the very last chapter.</summary>
        public static readonly TrList[] Finale =
        {
            new TrList(L.N("Cadı Hanım"), L.N("Dükkânım! Ne kadar güzel olmuş... Kara Pati, hayaletler, sizi çok özledim.")),
            new TrList(L.N("Gölge"), L.N("Ben de... Beni unuttun sandım. Yeni kedi gelince aynaya küstüm. Özür dilerim.")),
            new TrList(L.N("Cadı Hanım"), L.N("Seni hiç unutmadım Gölge. Afişleri hâlâ saklıyorum, bak!")),
            new TrList(L.N("Kara Pati"), L.N("Dükkânda iki kedilik yer var. Hatta yavrularla beraber... bir sürü kedilik!")),
            new TrList(L.N("Cadı Hanım"), L.N("O zaman Kara Pati'nin İksir Dükkânı yeniden açılıyor! Bu bir son değil, yeni bir başlangıç.")),
        };

        public const int FinaleGold = 300, FinaleStamps = 10;

        public static string FirstBoardHint => L.T("Mühür bitti! 'Dükkânı Aç'a bas, hayaletlerin siparişlerini hazırla.");

        public static readonly Chapter[] Chapters =
        {
            new Chapter
            {
                id = "dukkan", name = L.N("Bölüm 1: Tozlu Dükkân"),
                endLine = L.N("Bölüm 1 bitti, dükkân pırıl pırıl! Sıra mahzende... ama kapı kilitli. Notta ne yazıyordu? 'Anahtar kedide.'"),
                tasks = new[]
                {
                    new StoryTask { id = "ag", title = L.N("Örümcek ağlarını temizle"), cost = 1, speaker = L.N("Kara Pati"),
                        line = L.N("Hapşu! Cadı gittiğinden beri örümcekler dükkânı kendi evleri sanmış.") },
                    new StoryTask { id = "pencere", title = L.N("Pencereyi sil"), cost = 2, speaker = L.N("Kara Pati"),
                        line = L.N("Ay ışığı içeri süzülünce dükkân bambaşka oldu. Cadı her gece aya bakardı...") },
                    new StoryTask { id = "raf", title = L.N("Rafları onar"), cost = 2, speaker = L.N("Kara Pati"),
                        line = L.N("Rafın arkasından buruşuk bir not düştü: 'Mahzenin anahtarı... kedide.' Kedide mi?!") },
                    new StoryTask { id = "kazan", title = L.N("Kazanı yak"), cost = 3, speaker = L.N("Minik Boo"),
                        line = L.N("Kazan yeniden fokurduyor! Kasabadaki bütün hayaletler bu sesi duydu, hepsi geliyor!") },
                    new StoryTask { id = "hali", title = L.N("Halıyı ser"), cost = 2, speaker = L.N("Kara Pati"),
                        line = L.N("Halının altında pati izleri var... ama benimkilerden çok daha büyük. Hmm.") },
                    new StoryTask { id = "fener", title = L.N("Fenerleri yak"), cost = 3, speaker = L.N("Sis Teyze"),
                        line = L.N("Fener ışığında duvarda bir harita belirdi canım! Ok işareti... aşağıyı gösteriyor.") },
                    new StoryTask { id = "tezgah", title = L.N("Tezgâhı onar"), cost = 3, speaker = L.N("Kara Pati"),
                        line = L.N("Çekmecede cadının günlüğü! Son sayfa: 'Mahzene iniyorum. Dükkâna Kara Pati baksın.'") },
                    new StoryTask { id = "yatak", title = L.N("Kara Pati'ye yatak al"), cost = 4, speaker = L.N("Kara Pati"),
                        line = L.N("Ohh, kendime ait bir yatak... Biraz kestireyim, sonra o mahzene ineceğiz.") },
                }
            },
            new Chapter
            {
                id = "mahzen", name = L.N("Bölüm 2: Mahzenin Sırrı"),
                endLine = L.N("Aynadan cadının sesi yankılandı: 'Kara Pati! Aynanın içinde sıkışıp kaldım. Dolunayda beni çıkarabilirsin...' Bölüm 3 yakında!"),
                tasks = new[]
                {
                    new StoryTask { id = "kapi", title = L.N("Mahzen kapısını aç"), cost = 2, speaker = L.N("Kara Pati"),
                        line = L.N("Tasmamda tıngırdayan o şey... anahtarmış! Not haklıymış: anahtar kedideymiş. Yani bendeymiş!") },
                    new StoryTask { id = "mum", title = L.N("Mumları yak"), cost = 2, speaker = L.N("Kara Pati"),
                        line = L.N("Mumlar yanınca duvardaki gölgeler kıpırdadı. Sadece gölge... değil mi? Değil mi?!") },
                    new StoryTask { id = "kavanoz", title = L.N("Kavanoz rafını düzelt"), cost = 3, speaker = L.N("Profesör Uğultu"),
                        line = L.N("Şu etiketlere bak: 'Dolunay Tozu', 'Ayna Cilası'... Cadı büyük bir büyü hazırlıyormuş!") },
                    new StoryTask { id = "masa", title = L.N("Çalışma masasını topla"), cost = 3, speaker = L.N("Kara Pati"),
                        line = L.N("Masadaki kâğıtların hepsi aynı şeyi çizmiş: bir ayna, bir kedi ve dolunay.") },
                    new StoryTask { id = "zincir", title = L.N("Zincirli sandığı aç"), cost = 4, speaker = L.N("Kont Gıcırtı"),
                        line = L.N("Gıcırrr... Bu zincirleri ben bile açamazdım! İçinde ne var bakalım, ne var?") },
                    new StoryTask { id = "daire", title = L.N("Büyü dairesini temizle"), cost = 3, speaker = L.N("Leydi Ürperti"),
                        line = L.N("Daire parlıyor! Bu bir geçit büyüsü tatlım. Biri buradan bir yerlere gitmiş.") },
                    new StoryTask { id = "portre", title = L.N("Tozlu portreyi sil"), cost = 3, speaker = L.N("Sis Teyze"),
                        line = L.N("Aa, bu cadının portresi! Kucağındaki kara kediye bak canım... sen değil misin o?") },
                    new StoryTask { id = "ayna", title = L.N("Sihirli aynayı onar"), cost = 5, speaker = L.N("Kara Pati"),
                        line = L.N("Ayna titredi ve camın ardında bir yüz belirdi... Cadı! Aynanın içinde!") },
                }
            },
            new Chapter
            {
                id = "bahce", name = L.N("Bölüm 3: Dolunay Bahçesi"),
                endLine = L.N("Dolunay doğdu ve ayin başladı... ama aynadan simsiyah, dev bir kedi fırladı! Pati izlerinin sahibi oymuş. Cadıyı da kapıp aynaya geri daldı. Rüzgârda bir fısıltı: 'Hayalet Kasabası'na git...'"),
                tasks = new[]
                {
                    new StoryTask { id = "b_kapi", title = L.N("Bahçe kapısını aç"), cost = 3, speaker = L.N("Kara Pati"),
                        line = L.N("Dükkânın arkasında koca bir bahçe varmış! Cadı ayin için burayı hazırlıyormuş.") },
                    new StoryTask { id = "b_ot", title = L.N("Yabani otları biç"), cost = 3, speaker = L.N("Kara Pati"),
                        line = L.N("Otların arasında yine o kocaman pati izleri... Biri burada dolaşıyor.") },
                    new StoryTask { id = "b_cesme", title = L.N("Çeşmeyi çalıştır"), cost = 4, speaker = L.N("Minik Boo"),
                        line = L.N("Su şırıl şırıl! Ayin için ay ışığını yansıtacak su lazımmış, kitapta öyle yazıyor.") },
                    new StoryTask { id = "b_cicek", title = L.N("Ay çiçeklerini dik"), cost = 3, speaker = L.N("Bayan Fısıltı"),
                        line = L.N("Ay çiçekleri sadece dolunayda açar canım. Cadı onları hep buraya dikerdi.") },
                    new StoryTask { id = "b_korkuluk", title = L.N("Korkuluğu giydir"), cost = 4, speaker = L.N("Kaptan Pus"),
                        line = L.N("Ahoy! Korkuluğa cadının eski şapkasını giydirdik. Kargalar artık bize selam veriyor!") },
                    new StoryTask { id = "b_bal", title = L.N("Balkabağı tarlasını düzelt"), cost = 4, speaker = L.N("Kara Pati"),
                        line = L.N("Balkabakları tombul tombul! Hayaletler şenlikte bunlara bayılıyor.") },
                    new StoryTask { id = "b_fener", title = L.N("Ateşböceği fenerlerini as"), cost = 5, speaker = L.N("Leydi Ürperti"),
                        line = L.N("Ateşböcekleri fenerlere doldu. Ne romantik... ama çalılarda iki sarı göz parladı!") },
                    new StoryTask { id = "b_kadran", title = L.N("Ay kadranını kur"), cost = 5, speaker = L.N("Kara Pati"),
                        line = L.N("Kadran kuruldu. Dolunay tam tepede. Ayna parlıyor... Hazırım cadı hanım!") },
                }
            },
            new Chapter
            {
                id = "kasaba", name = L.N("Bölüm 4: Hayalet Kasabası"),
                endLine = L.N("Çan çaldı ve kasabadaki bütün hayaletler aynı yönü gösterdi: tepedeki Kayıp Kule. 'Aynalar Diyarı'nın kapısı orada,' dedi Profesör Uğultu."),
                tasks = new[]
                {
                    new StoryTask { id = "k_kopru", title = L.N("Sisli köprüyü onar"), cost = 3, speaker = L.N("Sis Teyze"),
                        line = L.N("Hoş geldin canım, Hayalet Kasabası'na! Köprü sisten yapılmıştı, hep çökerdi zaten.") },
                    new StoryTask { id = "k_lamba", title = L.N("Sokak lambalarını yak"), cost = 4, speaker = L.N("Kont Gıcırtı"),
                        line = L.N("Gıcır! Lambalar yandı. Bu kasabada en son ışık yüz yıl önce yanmıştı.") },
                    new StoryTask { id = "k_tezgah", title = L.N("Pazar tezgâhlarını kur"), cost = 4, speaker = L.N("Bayan Fısıltı"),
                        line = L.N("Pazar yeniden kuruldu! Satıcılardan biri o dev kediyi tanıyor: adı Gölge'ymiş.") },
                    new StoryTask { id = "k_cesme", title = L.N("Meydan çeşmesini temizle"), cost = 4, speaker = L.N("Minik Boo"),
                        line = L.N("Çeşmeden bir şişe çıktı, içinde not var: 'Gölge, cadının ilk kedisiydi.' Vay!") },
                    new StoryTask { id = "k_ev", title = L.N("Evlerin camlarını sil"), cost = 5, speaker = L.N("Leydi Ürperti"),
                        line = L.N("Pencereler ışıl ışıl. Gölge yıllar önce buralarda yaşarmış, yalnız ve küskün...") },
                    new StoryTask { id = "k_bayrak", title = L.N("Şenlik bayraklarını as"), cost = 4, speaker = L.N("Minik Boo"),
                        line = L.N("Bayraklar asıldı! Kasaba ilk kez şenlik havasında. Herkes sana yardım edecek Kara Pati!") },
                    new StoryTask { id = "k_saat", title = L.N("Saat kulesini onar"), cost = 5, speaker = L.N("Profesör Uğultu"),
                        line = L.N("Saat tekrar işliyor delikanlı. Akrep dolunayı, yelkovan da tepedeki kuleyi gösteriyor.") },
                    new StoryTask { id = "k_can", title = L.N("Kule çanını çal"), cost = 6, speaker = L.N("Kaptan Pus"),
                        line = L.N("DONG! Bu çan sadece kayıp bir dost geri dönecekken çalarmış. Rotamız belli!") },
                }
            },
            new Chapter
            {
                id = "kule", name = L.N("Bölüm 5: Kayıp Kule"),
                endLine = L.N("Geçit dairesi parladı ve mor bir kapı açıldı: Aynalar Diyarı! Kara Pati derin bir nefes aldı: 'Geliyorum cadı hanım. Gölge... seninle de konuşacağız.'"),
                tasks = new[]
                {
                    new StoryTask { id = "t_merdiven", title = L.N("Döner merdiveni sağlamlaştır"), cost = 4, speaker = L.N("Kara Pati"),
                        line = L.N("Bu kule cadının gözlemeviymiş! Merdivenler gıcırdıyor ama artık sağlam.") },
                    new StoryTask { id = "t_kitap", title = L.N("Kitaplığı düzenle"), cost = 4, speaker = L.N("Profesör Uğultu"),
                        line = L.N("Şu kitaba bak: 'Kediler ve Kıskançlık'. Altı çizili satır: 'Unutulduğunu sanan kedi, gölgeye dönüşür.'") },
                    new StoryTask { id = "t_harita", title = L.N("Yıldız haritasını as"), cost = 5, speaker = L.N("Kara Pati"),
                        line = L.N("Haritadaki yıldızlar bir kedi şeklinde dizilmiş... Gölge'nin takımyıldızı!") },
                    new StoryTask { id = "t_teleskop", title = L.N("Teleskobu onar"), cost = 5, speaker = L.N("Leydi Ürperti"),
                        line = L.N("Teleskopla aya baktım tatlım: ayın içinde bir kafes ve içinde cadı hanım var!") },
                    new StoryTask { id = "t_kure", title = L.N("Kristal küreyi parlat"), cost = 5, speaker = L.N("Sis Teyze"),
                        line = L.N("Kürede Gölge görünüyor canım. Kızgın değil... sadece çok, çok yalnız.") },
                    new StoryTask { id = "t_kazan", title = L.N("Kule kazanını yak"), cost = 5, speaker = L.N("Minik Boo"),
                        line = L.N("Kazan mor fokurduyor! Geçit iksiri neredeyse hazır, son bir adım kaldı.") },
                    new StoryTask { id = "t_pencere", title = L.N("Ay penceresini aç"), cost = 6, speaker = L.N("Kaptan Pus"),
                        line = L.N("Ay ışığı içeri doldu! Pusula ilk kez dümdüz gösteriyor: aynanın içine!") },
                    new StoryTask { id = "t_gecit", title = L.N("Geçit dairesini çiz"), cost = 6, speaker = L.N("Kara Pati"),
                        line = L.N("Daireyi pati izimle mühürledim. Anahtar kedideymiş, hatırladın mı? Hep bendeymiş.") },
                }
            },
            new Chapter
            {
                id = "diyar", name = L.N("Bölüm 6: Aynalar Diyarı"),
                endLine = L.N("Kafes açıldı! Cadı hanım, Gölge ve Kara Pati birlikte geçitten süzüldü... ve dükkânın sıcak ışığına geri döndüler."),
                tasks = new[]
                {
                    new StoryTask { id = "d_kopru", title = L.N("Kristal köprüyü kur"), cost = 4, speaker = L.N("Kara Pati"),
                        line = L.N("Burası Aynalar Diyarı... Her şey mor ve parlak. Köprüyü kristallerle tamamladım.") },
                    new StoryTask { id = "d_fener", title = L.N("Yıldız fenerlerini yak"), cost = 5, speaker = L.N("Sis Teyze"),
                        line = L.N("Fenerler yandı canım! Biz hayaletler de geldik, yalnız değilsin.") },
                    new StoryTask { id = "d_ayna", title = L.N("Kırık aynaları birleştir"), cost = 5, speaker = L.N("Profesör Uğultu"),
                        line = L.N("Aynalar birleşince Gölge'nin anıları göründü: cadıyla oynadığı mutlu günler.") },
                    new StoryTask { id = "d_yol", title = L.N("Pati izli yolu takip et"), cost = 6, speaker = L.N("Kara Pati"),
                        line = L.N("İzler bir tahta çıkıyor. Tahtta Gölge oturuyor. Dev ama... ürkek bakıyor.") },
                    new StoryTask { id = "d_yastik", title = L.N("Gölge için yastık bırak"), cost = 6, speaker = L.N("Gölge"),
                        line = L.N("Bu yastık... benim eski yastığım mı? Cadı onu saklamış mı? Mrr...") },
                    new StoryTask { id = "d_yumak", title = L.N("Gölge'yle yumak oyna"), cost = 6, speaker = L.N("Gölge"),
                        line = L.N("Hah! Yakaladım! Uzun zamandır kimse benimle oynamamıştı, küçük kara kedi.") },
                    new StoryTask { id = "d_kafes", title = L.N("Kafesin kilidini aç"), cost = 7, speaker = L.N("Cadı Hanım"),
                        line = L.N("Kara Pati! Beni buldun! Gölge'yi de yumuşatmışsın, ah benim akıllı kedim.") },
                    new StoryTask { id = "d_eve", title = L.N("Herkesle eve dön"), cost = 8, speaker = L.N("Gölge"),
                        line = L.N("Ben de gelebilir miyim? Söz, artık aynalara saklanmak yok. Sadece... bir yastık lazım.") },
                }
            },
        };

        public static readonly Clue[] Clues =
        {
            new Clue { id = "c1_not", chapter = 0, x = 400f, y = 60f, afterTasks = 3, title = L.N("Buruşuk Not"),
                text = L.N("'Mahzenin anahtarı kedide.' Cadının el yazısı... ama çok aceleyle yazılmış.") },
            new Clue { id = "c1_iz", chapter = 0, x = 200f, y = -430f, afterTasks = 5, title = L.N("Dev Pati İzi"),
                text = L.N("Bu iz kocaman! Bir kedinin ama dev gibi bir kedinin. Cadının başka bir yardımcısı mı vardı?") },
            new Clue { id = "c1_gunluk", chapter = 0, x = 200f, y = -150f, afterTasks = 7, title = L.N("Günlük Sayfası"),
                text = L.N("'Ayna çatladığı gece bir şeyler ters gitti. Kara Pati'yi korumalıyım, ne olursa olsun.'") },
            new Clue { id = "c2_supurge", chapter = 1, x = -400f, y = -390f, afterTasks = 10, title = L.N("Kırık Süpürge"),
                text = L.N("Cadının süpürgesi ikiye ayrılmış. Buradan aceleyle kaçmış olmalı... ya da biri onu çekmiş.") },
            new Clue { id = "c2_takvim", chapter = 1, x = 170f, y = -40f, afterTasks = 12, title = L.N("Ay Takvimi"),
                text = L.N("Dolunay günleri kırmızıyla daire içine alınmış. Bir sonraki dolunay... çok yakında!") },
            new Clue { id = "c2_tasma", chapter = 1, x = -20f, y = -170f, afterTasks = 13, title = L.N("Gümüş Tasma"),
                text = L.N("Sandıktan minik bir gümüş tasma çıktı. Üzerinde 'K.P.' yazıyor. Bu benim bebeklik tasmam mı?!") },
            new Clue { id = "c2_iz", chapter = 1, x = 220f, y = -430f, afterTasks = 14, title = L.N("Parlayan İzler"),
                text = L.N("Büyü dairesinden aynaya doğru uzanan parlak pati izleri. Dev kedi buradan geçmiş!") },
            new Clue { id = "c2_mektup", chapter = 1, x = 170f, y = 380f, afterTasks = 15, title = L.N("Mühürlü Mektup"),
                text = L.N("Portrenin arkasında bir mektup: 'Aynayı onaran, beni geri getirir. -C.'") },
            new Clue { id = "c3_iz", chapter = 2, x = 120f, y = -430f, afterTasks = 18, title = L.N("Çamurdaki Dev İz"),
                text = L.N("Bahçedeki çamurda kocaman bir pati izi. Kenarında siyah tüyler var. Bu kedi benden üç kat büyük!") },
            new Clue { id = "c3_tuy", chapter = 2, x = -300f, y = -270f, afterTasks = 20, title = L.N("Siyah Tüy Yumağı"),
                text = L.N("Korkuluğun dibinde simsiyah tüyler. Dokununca hafifçe titriyorlar... sanki gölgeden yapılmışlar.") },
            new Clue { id = "c3_not", chapter = 2, x = 430f, y = -20f, afterTasks = 23, title = L.N("Yırtık Ayin Notu"),
                text = L.N("'Dolunayda ayna açılırsa, içeride bekleyen de dışarı çıkar. Dikkat et.' Hmm, keşke daha önce okusaydım.") },
            new Clue { id = "c4_afis", chapter = 3, x = 390f, y = 40f, afterTasks = 26, title = L.N("Eski Afiş"),
                text = L.N("Solmuş bir afiş: 'Kayıp kedi: Gölge. Siyah, iri, çok sevimli. Bulana kocaman teşekkür. -Cadı'") },
            new Clue { id = "c4_foto", chapter = 3, x = -170f, y = -130f, afterTasks = 29, title = L.N("Soluk Fotoğraf"),
                text = L.N("Genç cadı kucağında koca siyah bir kediyle gülümsüyor. Arkasında: 'Gölge ile, ilk dükkân günü.'") },
            new Clue { id = "c5_gunluk", chapter = 4, x = 330f, y = 30f, afterTasks = 35, title = L.N("Cadının Son Notu"),
                text = L.N("'Gölge kaybolduğunda çok ağladım. Sonra Kara Pati geldi. Gölge bunu duyunca küstü sanırım...'") },
            new Clue { id = "c5_tasma", chapter = 4, x = -330f, y = -330f, afterTasks = 38, title = L.N("Kocaman Tasma"),
                text = L.N("Yerde dev bir tasma. Üzerinde 'G.' harfi. Benim 'K.P.' tasmamın kocaman bir ikizi gibi.") },
            new Clue { id = "c6_oyuncak", chapter = 5, x = -160f, y = -340f, afterTasks = 43, title = L.N("Eski Oyuncak Fare"),
                text = L.N("Yıpranmış bir oyuncak fare. Gölge bunu yıllardır saklamış. Demek ki hep eve dönmek istemiş.") },
            new Clue { id = "c6_mektup", chapter = 5, x = 430f, y = -40f, afterTasks = 46, title = L.N("Gölge'nin Mektubu"),
                text = L.N("Pati izleriyle yazılmış: 'Beni unuttuklarını sandım. Belki... yeni kedi ile arkadaş olabiliriz?'") },
        };

        public static int TotalTasks
        {
            get
            {
                int n = 0;
                foreach (var c in Chapters) n += c.tasks.Length;
                return n;
            }
        }

        public static int ChapterStart(int ch)
        {
            int n = 0;
            for (int i = 0; i < ch && i < Chapters.Length; i++) n += Chapters[i].tasks.Length;
            return n;
        }

        /// <summary>The chapter the player is currently working on (last one when everything is done).</summary>
        public static int CurrentChapter(int done)
        {
            for (int i = 0; i < Chapters.Length; i++)
                if (done < ChapterStart(i) + Chapters[i].tasks.Length) return i;
            return Chapters.Length - 1;
        }

        public static StoryTask TaskAt(int global)
        {
            int ch = 0;
            while (ch < Chapters.Length - 1 && global >= Chapters[ch].tasks.Length)
            {
                global -= Chapters[ch].tasks.Length;
                ch++;
            }
            return Chapters[ch].tasks[Mathf.Clamp(global, 0, Chapters[ch].tasks.Length - 1)];
        }

        public static bool IsDone(string id, int done)
        {
            for (int i = 0; i < done && i < TotalTasks; i++)
                if (TaskAt(i).id == id) return true;
            return false;
        }
    }

    /// <summary>Player level curve and what each level gives.</summary>
    /// <summary>The witch's tower: floors bought with gold, and Kara Pati's food and water bowls.</summary>
    public static class Tower
    {
        public static readonly TrList FloorNames = new TrList
            (L.N("Kara Pati'nin Odası"), L.N("Mutfak"), L.N("Kütüphane"), L.N("Yatak Odası"), L.N("Teras"), L.N("Gözlem Odası"));
        public static readonly int[] FloorPrice = { 0, 500, 1200, 2500, 4500, 8000 };
        public static readonly Color[] Wall =
        {
            new Color(0.32f, 0.22f, 0.42f), new Color(0.2f, 0.3f, 0.33f), new Color(0.32f, 0.23f, 0.18f),
            new Color(0.19f, 0.21f, 0.38f), new Color(0.12f, 0.15f, 0.32f), new Color(0.22f, 0.14f, 0.3f),
        };
        public static int Floors => FloorNames.Length;

        public const float BowlHours = 8f;          // a full bowl lasts this long
        public const float HappyBonus = 0.10f;      // extra order gold while Kara Pati is happy
        public const float HappyAbove = 0.15f;      // both bowls must be above this

        /// <summary>1 = just filled, 0 = empty (or never filled).</summary>
        public static float Level(long filledTicks)
        {
            if (filledTicks <= 0) return 0f;
            double hours = TimeSpan.FromTicks(Math.Max(0, DateTime.UtcNow.Ticks - filledTicks)).TotalHours;
            return Mathf.Clamp01(1f - (float)(hours / BowlHours));
        }
    }

    /// <summary>
    /// Shop decoration: every chapter scene has 4 slots (garland, plant, cat corner, light);
    /// each slot has "none" (free) plus 3 ornaments bought with gold. Bought ornaments can be swapped for free.
    /// </summary>
    public static class Decor
    {
        public const int Slots = 4, Variants = 4;   // variant 0 = empty
        public static readonly TrList SlotNames = new TrList(L.N("Süs Zinciri"), L.N("Saksı"), L.N("Kedi Köşesi"), L.N("Işık"));
        public static readonly TrList[] VariantNames =
        {
            new TrList(L.N("Yok"), L.N("Bayraklar"), L.N("Balkabakları"), L.N("Yıldızlar")),
            new TrList(L.N("Yok"), L.N("Ay Çiçeği"), L.N("Peri Mantarı"), L.N("Kristal Küme")),
            new TrList(L.N("Yok"), L.N("Minder"), L.N("Yumak Sepeti"), L.N("Kedi Kulesi")),
            new TrList(L.N("Yok"), L.N("Mumlar"), L.N("Fener"), L.N("Kristal Küre")),
        };
        static readonly int[][] basePrice =
        {
            new[] { 0, 120, 250, 400 },
            new[] { 0, 150, 300, 500 },
            new[] { 0, 200, 400, 700 },
            new[] { 0, 180, 350, 600 },
        };

        /// <summary>Later chapters are fancier and cost more (x1 .. x3.5), rounded to tens.</summary>
        public static int Price(int chapter, int slot, int variant) =>
            Mathf.RoundToInt(basePrice[slot][variant] * (1f + 0.5f * chapter) / 10f) * 10;

        public static string OwnedId(int chapter, int slot, int variant) => chapter + "_" + slot + "_" + variant;

        // ---- free-placement ornaments for the witch's tower ----
        public static string ItemId(int slot, int variant) => "s" + slot + "v" + variant;

        public static bool ParseItem(string id, out int slot, out int variant)
        {
            slot = variant = 0;
            if (string.IsNullOrEmpty(id) || id.Length < 4 || id[0] != 's') return false;
            int v = id.IndexOf('v');
            return v > 1 && int.TryParse(id.Substring(1, v - 1), out slot) && int.TryParse(id.Substring(v + 1), out variant)
                   && slot >= 0 && slot < Slots && variant > 0 && variant < Variants;
        }

        public static string ItemName(string id) => ParseItem(id, out var s, out var v) ? VariantNames[s][v] : id;

        /// <summary>Tower ornaments cost a bit more than the old scene ones, rounded to tens.</summary>
        public static int TowerPrice(int slot, int variant) => Mathf.RoundToInt(basePrice[slot][variant] * 1.5f / 10f) * 10;
        public static string SlotId(int chapter, int slot) => chapter + "_" + slot;

        // where each slot sits in each chapter scene (scene is 1020 x 940, centre origin); garlands also get a width
        public static readonly Vector3[][] Places =
        {
            new[] { new Vector3(100f, 430f, 640f), new Vector3(455f, -395f), new Vector3(215f, -420f), new Vector3(-455f, -80f) },   // shop
            new[] { new Vector3(60f, 440f, 560f), new Vector3(460f, -400f), new Vector3(-300f, -420f), new Vector3(230f, 40f) },     // cellar
            new[] { new Vector3(-150f, 330f, 500f), new Vector3(200f, -300f), new Vector3(-320f, -410f), new Vector3(470f, 120f) },  // garden
            new[] { new Vector3(-320f, 285f, 320f), new Vector3(-160f, -410f), new Vector3(330f, -420f), new Vector3(320f, 330f) },  // town
            new[] { new Vector3(0f, 440f, 500f), new Vector3(450f, -380f), new Vector3(300f, -420f), new Vector3(-300f, 100f) },     // tower
            new[] { new Vector3(0f, 440f, 600f), new Vector3(-140f, -430f), new Vector3(60f, -430f), new Vector3(-470f, 180f) },     // realm
        };
    }

    /// <summary>Gold sink: upgrading the Kazan / Sandık makes them drop higher-level ingredients.</summary>
    public static class Generators
    {
        public const int MaxLevel = 5;
        static readonly int[] cost = { 0, 150, 400, 900, 1800 };   // price to go from level i to i+1

        public static int UpgradeCost(int level) => level >= 1 && level < MaxLevel ? cost[level] : 0;
        public static float Level2Chance(int level) => 0.12f + 0.08f * (level - 1);           // 12% .. 44%
        public static float Level3Chance(int level) => level >= 3 ? 0.05f * (level - 2) : 0f;  // 0 .. 15%

        public static int Roll(int level)
        {
            float r = UnityEngine.Random.value;
            if (r < Level3Chance(level)) return 3;
            if (r < Level3Chance(level) + Level2Chance(level)) return 2;
            return 1;
        }

        public static string ChanceText(int level)
        {
            string t = L.F("2. seviye şansı %{0}", Mathf.RoundToInt(Level2Chance(level) * 100f));
            if (Level3Chance(level) > 0f) t += L.F(", 3. seviye %{0}", Mathf.RoundToInt(Level3Chance(level) * 100f));
            return t;
        }
    }

    /// <summary>Gift chests on the chapter progress bar: one halfway, one at the end.</summary>
    public static class ChapterGifts
    {
        public static readonly float[] At = { 0.5f, 1f };

        public static string Id(int chapter, int gift) => chapter + "_" + gift;

        /// <summary>How many tasks of the chapter must be done to open this gift.</summary>
        public static int TasksNeeded(int chapter, int gift) =>
            Mathf.CeilToInt(Story.Chapters[chapter].tasks.Length * At[gift]);

        // later chapters pay more
        public static int Gold(int chapter, int gift) => gift == 0 ? 30 + chapter * 20 : 60 + chapter * 30;
        public static int Energy(int chapter, int gift) => gift == 0 ? 10 : 0;
        public static int Stamps(int chapter, int gift) => gift == 0 ? 0 : 3;
        public static bool Sticker(int chapter, int gift) => gift == 1;

        public static string Text(int chapter, int gift)
        {
            var parts = new List<string> { L.F("+{0} altın", Gold(chapter, gift)) };
            if (Energy(chapter, gift) > 0) parts.Add(L.F("+{0} enerji", Energy(chapter, gift)));
            if (Stamps(chapter, gift) > 0) parts.Add(L.F("+{0} Pati Mührü", Stamps(chapter, gift)));
            if (Sticker(chapter, gift)) parts.Add(L.T("bir etiket"));
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Game Room mini games. Each game has a few free plays a day; after that a play costs energy.
    /// </summary>
    /// <summary>
    /// The Master Cat grows with its work: ingredients brought, kittens trained, trips. Each level adds a small perk.
    /// </summary>
    public static class UstaLevels
    {
        public const int Max = 5;
        static readonly int[] need = { 0, 30, 80, 160, 300 };   // total experience for level 1..5
        public const int XpIngredient = 1, XpTraining = 5;
        public static readonly int[] XpTrip = { 5, 10, 20 };    // per expedition kind

        public static int LevelOf(int xp)
        {
            int lv = 1;
            while (lv < Max && xp >= need[lv]) lv++;
            return lv;
        }
        public static int XpFor(int level) => need[Mathf.Clamp(level - 1, 0, Max - 1)];

        public static float Interval(int level) => level >= 5 ? 35f : level >= 2 ? 40f : 45f;    // seconds between ingredients
        public static float Level3Chance(int level) => level >= 3 ? 0.25f : 0f;                  // a level 3 ingredient now and then
        public static int TripBonus(int level) => level >= 4 ? 2 : 0;                             // extra ingredients from trips
        public static float TrainMinutes(int level) => level >= 5 ? 2f : 3f;

        /// <summary>The perk a level brings (shown when it is reached and as the next goal).</summary>
        public static string Perk(int level)
        {
            switch (level)
            {
                case 2: return L.T("Daha sık malzeme getirir (40 sn).");
                case 3: return L.T("Bazen 3. seviye malzeme getirir.");
                case 4: return L.T("Keşiften 2 malzeme fazla döner.");
                case 5: return L.T("35 sn'de bir malzeme, eğitim 2 dakika.");
                default: return "";
            }
        }
    }

    public static class MiniGames
    {
        public const int Fare = 0, Yumak = 1, Hafiza = 2, Count = 3;
        public const int UnlockLevel = 5;
        public const int FreePerDay = 2;
        public const int EnergyCost = 10;
        public const float FareSeconds = 30f;

        public static readonly TrList Names = new TrList(L.N("Fare Avı"), L.N("Yumak Atma"), L.N("İksir Hafızası"));
        public static readonly TrList Blurbs = new TrList
        (
            L.N("Fıçılardan çıkan fareleri yakala! Hayaletlere dokunma."),
            L.N("Yumağı doğru anda at, kediler yakalasın."),
            L.N("Kartları çevir, aynı malzemeleri eşleştir.")
        );
        /// <summary>Games that are built so far; the others show "Yakında".</summary>
        public static bool Ready(int g) => g >= 0 && g < Count;

        // Free play of Fare Avı gives energy. A paid play (energy in) gives gold instead, so energy can never
        // turn into more energy. It stays under 20 gold: 10 energy costs 20 gold in the shop, so gold -> energy -> gold
        // can not make money either.
        public static int FareEnergy(int score) => score <= 0 ? 0 : Mathf.Min(15, 3 + score / 3);
        public static int FareGold(int score) => score <= 0 ? 0 : Mathf.Min(18, 2 + score / 2);

        // Yumak Atma gives gold; a free play with a long catch streak also opens a gift chest (ingredients).
        // The chest is for free plays only: with energy in, it would be worth more than the energy.
        public const int YarnChestStreak = 15, YarnLives = 3;
        public static int YarnGold(int score) => score <= 0 ? 0 : Mathf.Min(30, 3 + score);
        public static int YarnGoldPaid(int score) => score <= 0 ? 0 : Mathf.Min(18, 2 + score / 2);

        // İksir Hafızası: a free play gives ingredients (one per 4 points, up to 4), a paid one gold (under 20, as above)
        public const float MemorySeconds = 60f, HourglassBonus = 5f;
        public static readonly int[] MemoryPairs = { 6, 8, 10 };   // boards get bigger; after the last one it stays at 10
        public static int MemoryItems(int score) => Mathf.Clamp(score / 4, 0, 4);
        public static int MemoryGoldPaid(int score) => score <= 0 ? 0 : Mathf.Min(18, 2 + score);
    }

    public static class Levels
    {
        // XP only comes from the story (tasks, chapter ends, hidden clues); merging and orders give none
        public const int TaskXp = 15, ChapterXp = 25, ClueXp = 5;

        public const int MaxStorage = 12;
        public static int XpToNext(int level) => 30 + (level - 1) * 20;
        public static int MaxEnergy(int level) => Mathf.Min(50, 30 + (level - 1) * 2);
        public static int StorageSlots(int level) => Mathf.Min(MaxStorage, 3 + level);
        public static int GoldReward(int level) => 15 * level;
    }

    public class EventMilestone
    {
        public int need;          // total pumpkins
        public string type;       // gold | energy | stamps | sticker
        public int amount;
    }

    /// <summary>Time-limited Halloween event: collect pumpkins from orders and tasks, walk the reward path.</summary>
    public static class PumpkinEvent
    {
        public static string Name => L.T("Balkabağı Şenliği");
        public const int Days = 7;

        public static readonly EventMilestone[] Path =
        {
            new EventMilestone { need = 3, type = "gold", amount = 30 },
            new EventMilestone { need = 6, type = "energy", amount = 15 },
            new EventMilestone { need = 10, type = "sticker", amount = 1 },
            new EventMilestone { need = 15, type = "stamps", amount = 2 },
            new EventMilestone { need = 20, type = "gold", amount = 60 },
            new EventMilestone { need = 26, type = "energy", amount = 25 },
            new EventMilestone { need = 33, type = "sticker", amount = 1 },
            new EventMilestone { need = 40, type = "stamps", amount = 3 },
            new EventMilestone { need = 48, type = "gold", amount = 120 },
            new EventMilestone { need = 56, type = "stamps", amount = 6 },
        };

        public static string RewardText(EventMilestone m)
        {
            switch (m.type)
            {
                case "gold": return L.F("{0} altın", m.amount);
                case "energy": return L.F("{0} enerji", m.amount);
                case "stamps": return L.F("{0} Pati Mührü", m.amount);
                default: return L.T("Sürpriz etiket");
            }
        }
    }

    /// <summary>7-day login calendar. The day only moves forward when you claim; it loops after day 7.</summary>
    public static class DailyRewards
    {
        public static readonly EventMilestone[] Days =
        {
            new EventMilestone { type = "gold", amount = 20 },
            new EventMilestone { type = "energy", amount = 10 },
            new EventMilestone { type = "stamps", amount = 1 },
            new EventMilestone { type = "gold", amount = 40 },
            new EventMilestone { type = "energy", amount = 20 },
            new EventMilestone { type = "stamps", amount = 2 },
            new EventMilestone { type = "sticker", amount = 100 },   // sticker + 100 gold
        };

        public static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;

        public static string Text(EventMilestone m) =>
            m.type == "sticker" ? L.F("{0} altın + etiket", m.amount) : PumpkinEvent.RewardText(m);
    }

    /// <summary>
    /// Kitten fur colours. Two kittens merge into an Apprentice of one of their colours, and each colour's
    /// Apprentice has a small talent. "Gece" is only the old dark look of Masters from earlier saves.
    /// </summary>
    public static class KittenColors
    {
        public const int Turuncu = 0, Gri = 1, Beyaz = 2, SiyahBeyaz = 3, UcRenkli = 4, Gece = 5;
        public const int Count = 5;   // colours that can arrive on the table
        public static readonly TrList Names = new TrList(L.N("Turuncu Tekir"), L.N("Gri"), L.N("Beyaz"), L.N("Siyah-Beyaz"), L.N("Üç Renkli"), L.N("Gece"));
        public static string Name(int c) => Names[Mathf.Clamp(c, 0, Names.Length - 1)];
        static readonly int[] weights = { 35, 30, 20, 12, 3 };   // out of 100: the calico is rare

        public static readonly TrList Talents = new TrList
        (
            L.N("Şanslı: bazen 2. seviye malzeme getirir."),
            L.N("Usta karıştırıcı: kazanı 2,5 kat hızlı karıştırır."),
            L.N("Çalışkan: daha sık malzeme getirir (8 sn'de bir)."),
            L.N("Çevik: yavruları çok hızlı kovalar."),
            L.N("Sadık: 2 değil 3 saat kalır."),
            ""
        );

        public static int Pick()
        {
            int r = UnityEngine.Random.Range(0, 100);
            for (int i = 0; i < weights.Length; i++) { if (r < weights[i]) return i; r -= weights[i]; }
            return 0;
        }

        /// <summary>The look cats had before colours existed (old saves).</summary>
        public static int Legacy(int level) => level == 2 ? Gri : level >= 3 ? Gece : Turuncu;
    }

    /// <summary>The Master cat's expeditions: longer trips bring better finds, but it doesn't work at the table meanwhile.</summary>
    public static class Expeditions
    {
        public static readonly TrList Names = new TrList(L.N("Kısa Gezinti"), L.N("Orman Turu"), L.N("Dolunay Seferi"));
        public static readonly int[] Minutes = { 30, 120, 480 };
        public static readonly int[] Energy = { 5, 10, 20 };     // energy it costs to send the Master
        public static readonly int[] Items = { 3, 3, 4 };      // how many ingredients it brings back
        public static readonly int[] MinLevel = { 2, 3, 4 };
        public static readonly int[] MaxLevel = { 3, 4, 5 };
        public static readonly int[] Gold = { 20, 0, 0 };
        public static readonly int[] Stamps = { 0, 2, 4 };
        public static readonly float[] StickerChance = { 0f, 0.4f, 1f };
        public static readonly bool[] Ornament = { false, false, true };   // the long trip finds a tower ornament

        public static string Rewards(int k)
        {
            var parts = new List<string> { L.F("{0} malzeme (Sv. {1}-{2})", Items[k], MinLevel[k], MaxLevel[k]) };
            if (Gold[k] > 0) parts.Add(L.F("{0} altın", Gold[k]));
            if (Stamps[k] > 0) parts.Add(L.F("{0} mühür", Stamps[k]));
            if (StickerChance[k] >= 1f) parts.Add(L.T("etiket"));
            else if (StickerChance[k] > 0f) parts.Add(L.T("belki etiket"));
            if (Ornament[k]) parts.Add(L.T("kule süsü"));
            return string.Join(", ", parts);
        }

        public static string Duration(int k) => Minutes[k] < 60 ? L.F("{0} dk", Minutes[k]) : L.F("{0} saat", Minutes[k] / 60);
    }

    /// <summary>One customer of the shop: who they are, how hard they are to befriend, what they tell you.</summary>
    public class CustomerDef
    {
        string name_, difficulty_;
        public string id;
        public string name { get => L.T(name_); set => name_ = value; }
        public bool ghost;          // one of the town's ghosts (the others are fantasy creatures)
        public string difficulty { get => L.T(difficulty_); set => difficulty_ = value; }   // Kolay / Orta / Zor / En zor
        public int unlockLevel;     // player level at which they start visiting
        public int[] steps;         // points needed for each next friendship stage
        public TrList lines;        // what they say when you reach stage 1..max
        public float size;          // height compared to Kara Pati (for the tower)
        public int MaxStage => steps.Length;

        /// <summary>Points needed to reach `stage` (cumulative).</summary>
        public int PointsFor(int stage)
        {
            int n = 0;
            for (int i = 0; i < stage && i < steps.Length; i++) n += steps[i];
            return n;
        }

        public int StageOf(int points)
        {
            int st = 0;
            while (st < steps.Length && points >= PointsFor(st + 1)) st++;
            return st;
        }

        /// <summary>From this stage on the customer shows its signature items (hat, sword, goggles...).</summary>
        public int FullLookStage => Mathf.CeilToInt(steps.Length * 0.6f);
    }

    /// <summary>
    /// The shop's customers. Everyone starts as a shadow in the fog ("???"); each delivered order is a friendship
    /// point (a potion counts double) and every new stage shows a bit more of who they are.
    /// </summary>
    public static class Customers
    {
        public static readonly TrList StageNames = new TrList(L.N("Yabancı"), L.N("Tanıdık"), L.N("Arkadaş"), L.N("Dost"), L.N("Yakın Dost"), L.N("Sırdaş"));
        public static string BestFriend => L.T("Can Dostu");

        /// <summary>Yarasa Kontes only visits at night (the phone's clock): 20:00 - 06:00.</summary>
        public const string NightOwl = "kontes";
        public static bool IsNight { get { int h = System.DateTime.Now.Hour; return h >= 20 || h < 6; } }
        public static bool Visits(CustomerDef c, int level) => c.unlockLevel <= level && (c.id != NightOwl || IsNight);

        public static readonly CustomerDef[] All =
        {
            new CustomerDef { id = "kurbaga", name = L.N("Kurbağa Prens"), difficulty = L.N("Kolay"), unlockLevel = 1, size = 0.5f, steps = new[] { 2, 3, 4 }, lines = new TrList(
                L.N("Vırak! Ben Kurbağa Prens. Bir cadı beni... neyse, uzun hikâye."),
                L.N("İksirlerin dudaklarımı gıdıklıyor. Belki biri beni yeniden prens yapar?"),
                L.N("Prens olmasam da olur, burada dostlarım var. Tacımı sana göstermek isterim!") ) },
            new CustomerDef { id = "boo", name = L.N("Minik Boo"), ghost = true, difficulty = L.N("Kolay"), unlockLevel = 1, size = 0.7f, steps = new[] { 2, 3, 4 }, lines = new TrList(
                L.N("Boo! Korktun mu? ...Korkmadın. Ben Minik Boo."),
                L.N("Ayıcığım Pamuk da seni sevdi. Onu kimseye göstermem ama sana gösterdim!"),
                L.N("En iyi arkadaşım sensin! Kurdelemi senin için taktım.") ) },
            new CustomerDef { id = "bocek", name = L.N("Bay Böcek"), difficulty = L.N("Kolay"), unlockLevel = 1, size = 0.4f, steps = new[] { 2, 3, 4, 5 }, lines = new TrList(
                L.N("Şapkamı çıkarırım efendim. Bay Böcek, emrinize amade."),
                L.N("Bu dükkân annemin bahçesi gibi kokuyor: lavanta ve sihir."),
                L.N("Cuma akşamları uğur dağıtırım. Bu hafta hepsi senin!"),
                L.N("En sevdiğim papyonumu taktım. Dostluğumuza, efendim!") ) },
            new CustomerDef { id = "sis", name = L.N("Sis Teyze"), ghost = true, difficulty = L.N("Kolay"), unlockLevel = 2, size = 1.3f, steps = new[] { 2, 3, 5, 6 }, lines = new TrList(
                L.N("Ah, yeni dükkâncı! Ben Sis Teyze. Her sabah sisi ben örerim."),
                L.N("Kasabayı saran bu kalın sis benim örgüm değil canım. Başka bir şey bu."),
                L.N("Sana bir şal örüyorum. Rengi sürpriz!"),
                L.N("Şalın hazır! Bundan sonra sis seni üşütemez.") ) },
            new CustomerDef { id = "tirtil", name = L.N("Korsan Tırtıl"), difficulty = L.N("Orta"), unlockLevel = 3, size = 0.55f, steps = new[] { 3, 4, 6, 7 }, lines = new TrList(
                L.N("Yo-ho! Hazine kokusu aldım. Bu dükkânda bir şey var!"),
                L.N("Bir gözümü martıya kaptırdım ama hazineyi kaptırmadım!"),
                L.N("Bu kılıç mı? Aslında tereyağı bıçağı. Sakın kimseye söyleme."),
                L.N("Kelebek olunca bile senin tayfan olacağım!") ) },
            new CustomerDef { id = "pus", name = L.N("Kaptan Pus"), ghost = true, difficulty = L.N("Orta"), unlockLevel = 4, size = 1.3f, steps = new[] { 3, 5, 6, 8 }, lines = new TrList(
                L.N("Ahoy! Kaptan Pus. Gemim sisin içinde kayboldu..."),
                L.N("Batan gemim Gümüş Martı'ydı. Tayfam hâlâ beni arıyor olabilir."),
                L.N("Pipomu yaktım. Bu, bir kaptanın en büyük güvenidir."),
                L.N("Bu dükkân benim limanım oldu. Demir attım!") ) },
            new CustomerDef { id = "kartal", name = L.N("Pilot Kartal"), difficulty = L.N("Orta"), unlockLevel = 5, size = 1.35f, steps = new[] { 3, 4, 6, 8, 10 }, lines = new TrList(
                L.N("Acil teslimat! Ben Pilot Kartal, kanatlarım emrinde."),
                L.N("Sis yüzünden üç mektubu yanlış bacaya attım. Aramızda kalsın!"),
                L.N("Gözlüğüm dedemden kalma. Fırtınaların içini gösterir."),
                L.N("Bir gün seni bulutların üstüne götüreceğim. Söz!"),
                L.N("Atkım artık senin. Her kanat çırpışımda seni düşüneceğim.") ) },
            new CustomerDef { id = "baykus", name = L.N("Baykuş Kütüphaneci"), difficulty = L.N("Zor"), unlockLevel = 6, size = 0.8f, steps = new[] { 4, 5, 7, 9, 11 }, lines = new TrList(
                L.N("Hu-hu. Kütüphaneden geliyorum. Biraz sessiz lütfen."),
                L.N("Cadının kitaplarını ben ödünç verirdim. Hepsinin süresi geçti!"),
                L.N("Sen benim gözlüğüm gibisin: işleri netleştiriyorsun."),
                L.N("Sisin kaydı eski bir kitapta var: 'Kaybolan dostlukların gölgesi'..."),
                L.N("Kütüphanenin anahtarı artık sende. Kitaplar senin de.") ) },
            new CustomerDef { id = "ugultu", name = L.N("Profesör Uğultu"), ghost = true, difficulty = L.N("En zor"), unlockLevel = 7, size = 1.4f, steps = new[] { 4, 6, 8, 10, 12, 15 }, lines = new TrList(
                L.N("Hmm. Profesör Uğultu. Eski bir öğretmen, yorgun bir hayalet."),
                L.N("Cadı Hanım'a ilk büyüsünü ben öğrettim. Kaşlarını yakmıştı!"),
                L.N("Papyonum mu? Mezuniyet günümden kalma. Yüz yıl önceydi."),
                L.N("İksirlerin dengesi kusursuz. Öğrencim olsaydın birinci olurdun."),
                L.N("Bir sır vereyim: Cadı, aynanın arkasındaki kediyi hiç unutmadı."),
                L.N("Son dersim bitti. Bundan sonra öğretmen sensin, sevgili dostum.") ) },
            new CustomerDef { id = "ejderha", name = L.N("Ejderha Yavrusu"), difficulty = L.N("En zor"), unlockLevel = 8, size = 0.9f, steps = new[] { 5, 6, 8, 11, 13, 16 }, lines = new TrList(
                L.N("*hık* Pardon! Ben bir ejderha yavrusuyum. Adım Kıvılcım."),
                L.N("Hıçkırınca alev çıkıyor. Kütüphaneyi bir kere az kalsın yakıyordum."),
                L.N("Annem dağlarda. Ben burada senin iksirlerini seviyorum."),
                L.N("Bak, uçabiliyorum! ...Üç saniye kadar."),
                L.N("Kazanının altını ben yakabilirim. Bedava!"),
                L.N("Büyüyünce seni sırtımda taşırım. Ejderha sözü! *hık*") ) },
            new CustomerDef { id = "kontes", name = L.N("Yarasa Kontes"), difficulty = L.N("En zor"), unlockLevel = 9, size = 0.6f, steps = new[] { 5, 7, 9, 12, 14, 18 }, lines = new TrList(
                L.N("İyi geceler, sevgili dükkâncı. Ben Yarasa Kontes. Güneş mi? Ah, o kaba ışık..."),
                L.N("İksirlerin karanlıkta parlıyor. Tıpkı şatomun eski avizeleri gibi."),
                L.N("Bu sis... yüz yıl önce de gelmişti. O zaman da birinin kalbi kırılmıştı."),
                L.N("Pelerinimin altında bir sır saklarım: sis, gözyaşı gibi kokar."),
                L.N("Gölge'yi tanırım. Geceleri kuleme konardı, ikimiz de yalnızdık."),
                L.N("Şatomun kapıları artık sana açık, sevgili dostum. Gecenin kraliçesi senin yanında.") ) },
        };

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].id == id) return i;
            return -1;
        }

        public static CustomerDef Get(string id) { int i = IndexOf(id); return i >= 0 ? All[i] : null; }

        public static CustomerDef ByName(string name)
        {
            foreach (var c in All) if (c.name == name) return c;
            return null;
        }

        public static string StageName(CustomerDef c, int stage) =>
            stage >= c.MaxStage ? BestFriend : StageNames[Mathf.Clamp(stage, 0, StageNames.Length - 1)];

        /// <summary>What a best friend says in the tower when it hands over its daily gift.</summary>
        public static string GiftLine(string id)
        {
            switch (id)
            {
                case "kurbaga": return L.T("Tacımı parlatırken bunları buldum. Al, senin olsun!");
                case "boo": return L.T("Pamuk'la birlikte sana enerji topladık. Boo!");
                case "bocek": return L.T("Bu haftanın uğuru: bir Pati Mührü, efendim.");
                case "sis": return L.T("Şalı örerken artan ipek yumağı. Senin için!");
                case "tirtil": return L.T("Hazine sandığımdan bir avuç altın. Yo-ho!");
                case "pus": return L.T("Deniz feneri yağı: enerjin hiç sönmesin, tayfa!");
                case "kartal": return L.T("Acil teslimat! Dağlardan taze malzemeler.");
                case "baykus": return L.T("Kütüphaneden iki eski mühür buldum. Hu-hu.");
                case "ugultu": return L.T("Bugünkü ders: cömertlik. İki Pati Mührü senin.");
                case "ejderha": return L.T("*hık* Kazanını ateşledim, enerjin doldu!");
                case "kontes": return L.T("Gece yarısı mühürleri: üç tane, sadece sana.");
                default: return L.T("Bugün de uğradım, al bakalım!");
            }
        }

        /// <summary>Reward for reaching `stage`: a bit of energy, stamps, more at the top.</summary>
        public static int StageEnergy(int stage) => 5 + 2 * stage;
        public static int StageStamps(CustomerDef c, int stage) => stage >= c.MaxStage ? 3 : 1;
    }

    public static class Ghosts
    {
        public static readonly TrList Names = new TrList
        (
            L.N("Bayan Fısıltı"), L.N("Sör Tıkırtı"), L.N("Minik Boo"), L.N("Kont Gıcırtı"),
            L.N("Sis Teyze"), L.N("Kaptan Pus"), L.N("Profesör Uğultu"), L.N("Leydi Ürperti")
        );

        public static readonly TrList Lines = new TrList
        (
            L.N("Oh, teşekkürler! Artık rahat uyuyabilirim..."),
            L.N("Muhteşem! Kedin de çok tatlı bu arada."),
            L.N("Boo-hoo! Mutluluktan ağlıyorum!"),
            L.N("Bu iksirle duvarlardan iki kat hızlı geçeceğim!"),
            L.N("Cadı hanım da tam böyle yapardı... Nereye kayboldu acaba?"),
            L.N("Miyavlayan dükkân, harika hizmet!"),
            L.N("Kara Pati! Cadıyı en son mahzende görmüştüm, biliyor muydun?"),
            L.N("Zincirlerimi şıngırdatmadan teşekkür ederim."),
            L.N("Cadının süpürgesi hâlâ çatıda duruyor, garip değil mi?"),
            L.N("Bir dahaki sefere kedi maması getiririm!")
        );
    }
}
