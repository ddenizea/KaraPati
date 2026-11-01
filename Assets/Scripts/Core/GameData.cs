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

        public static readonly string[] Ingredients = { Ot, Mantar, Kristal, Ag };

        static readonly Dictionary<string, string[]> names = new Dictionary<string, string[]>
        {
            { Ot, new[] { "Yaprak", "Ot Demeti", "Adaçayı Destesi", "Büyülü Ot", "Ay Çiçeği" } },
            { Mantar, new[] { "Mantar", "Benekli Mantar", "Mantar Kümesi", "Parlayan Mantar", "Peri Mantarı" } },
            { Kristal, new[] { "Kum Tanesi", "Çakıl Taşı", "Kristal", "Ay Taşı", "Yıldız Taşı" } },
            { Ag, new[] { "Örümcek Ağı", "İplik Yumağı", "İpek Yumak", "Gölge Kumaşı", "Hayalet Şal" } },
            { Kedi, new[] { "Yaramaz Yavru", "Çırak Kedi", "Usta Kedi" } },
            { Kazan, new[] { "Kazan" } },
            { Sandik, new[] { "Eski Sandık" } },
            { Iksir, new[] { "Uyku İksiri", "Işıltı İksiri", "Gölge İksiri", "Ay İksiri", "Hayalet İksiri", "Yıldız İksiri" } },
            { Simya, new[] { "Simya Kazanı" } },
        };

        public static int MaxLevel(string chain) => names[chain].Length;
        public static string Name(string chain, int level) => names[chain][Mathf.Clamp(level - 1, 0, names[chain].Length - 1)];
        public static bool IsIngredient(string c) => c == Ot || c == Mantar || c == Kristal || c == Ag;
        public static bool IsGenerator(string c) => c == Kazan || c == Sandik;
        public static bool IsPotion(string c) => c == Iksir;
        public static bool IsStation(string c) => c == Simya;
        /// <summary>Items that show no level badge (machines and potions).</summary>
        public static bool IsSpecial(string c) => IsGenerator(c) || IsPotion(c) || IsStation(c);
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
                case Ot: return "Otlar";
                case Mantar: return "Mantarlar";
                case Kristal: return "Taşlar ve Kristaller";
                case Ag: return "Ağlar ve Kumaşlar";
                case Iksir: return "İksirler";
                default: return "Dükkân Eşyası";
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
        public bool Locked => nap || bubble;
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

    [Serializable] public class CellSave { public int x, y; public string chain; public int level; public bool nap; public float cd; public bool bubble; public long bubbleUntil; public string mix; public int brew; public long brewUntil; }
    [Serializable] public class ReqSave { public string chain; public int level; }

    [Serializable]
    public class OrderSave
    {
        public string ghost;
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
        public List<string> chapterGifts = new List<string>();   // claimed progress-bar gifts, e.g. "2_1" 
        public List<string> clues = new List<string>();
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
        public string id, name;
        public string[] ids;
    }

    public static class Stickers
    {
        public const int SetRewardGold = 100, SetRewardStamps = 3;
        public const int GrandGold = 300, GrandStamps = 10, GrandEnergy = 30;
        public const string GrandId = "all";

        public static readonly StickerSet[] Sets =
        {
            new StickerSet { id = "ghosts", name = "Hayalet Dostlar", ids = new[] { "g0", "g1", "g2", "g3", "g4", "g5", "g6", "g7" } },
            new StickerSet { id = "cats", name = "Dükkân Kedileri", ids = new[] { "c_yavru", "c_uykucu", "c_cirak", "c_usta", "c_karapati", "c_hayaletkedi" } },
            new StickerSet { id = "witch", name = "Cadının Eşyaları", ids = new[] { "w_sapka", "w_supurge", "w_kitap", "w_kure", "w_kazan", "w_mektup" } },
        };

        static readonly Dictionary<string, string> names = new Dictionary<string, string>
        {
            { "c_yavru", "Yaramaz Yavru" }, { "c_uykucu", "Uykucu Yavru" }, { "c_cirak", "Çırak Kedi" },
            { "c_usta", "Usta Kedi" }, { "c_karapati", "Kara Pati" }, { "c_hayaletkedi", "Hayalet Kedi" },
            { "w_sapka", "Sivri Şapka" }, { "w_supurge", "Uçan Süpürge" }, { "w_kitap", "Büyü Kitabı" },
            { "w_kure", "Kristal Küre" }, { "w_kazan", "Eski Kazan" }, { "w_mektup", "Kayıp Mektup" },
        };

        public static string Name(string id)
        {
            if (id.StartsWith("g") && int.TryParse(id.Substring(1), out int gi) && gi >= 0 && gi < Ghosts.Names.Length)
                return Ghosts.Names[gi];
            return names.TryGetValue(id, out var n) ? n : id;
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
        public string id, title, speaker, line;
        public int cost;
    }

    public class Chapter
    {
        public string id, name, endLine;
        public StoryTask[] tasks;
    }

    /// <summary>A hidden clue in a scene. Becomes findable once enough story tasks are done.</summary>
    public class Clue
    {
        public string id, title, text;
        public int chapter;
        public float x, y;          // position in the scene (scene-local units)
        public int afterTasks;      // global number of finished tasks needed before it shows up
    }

    public static class Story
    {
        public const string IntroLine =
            "Cadı ortadan kayboldu, dükkân da toz içinde kaldı. Hayaletlere iksir malzemesi verip Pati Mührü kazanalım, " +
            "dükkânı adım adım toparlayalım. İlk iş: şu örümcek ağları!";

        /// <summary>Epilogue after the very last chapter.</summary>
        public static readonly string[][] Finale =
        {
            new[] { "Cadı Hanım", "Dükkânım! Ne kadar güzel olmuş... Kara Pati, hayaletler, sizi çok özledim." },
            new[] { "Gölge", "Ben de... Beni unuttun sandım. Yeni kedi gelince aynaya küstüm. Özür dilerim." },
            new[] { "Cadı Hanım", "Seni hiç unutmadım Gölge. Afişleri hâlâ saklıyorum, bak!" },
            new[] { "Kara Pati", "Dükkânda iki kedilik yer var. Hatta yavrularla beraber... bir sürü kedilik!" },
            new[] { "Cadı Hanım", "O zaman Kara Pati'nin İksir Dükkânı yeniden açılıyor! Bu bir son değil, yeni bir başlangıç." },
        };

        public const int FinaleGold = 300, FinaleStamps = 10;

        public const string FirstBoardHint = "Mühür bitti! 'Dükkânı Aç'a bas, hayaletlerin siparişlerini hazırla.";

        public static readonly Chapter[] Chapters =
        {
            new Chapter
            {
                id = "dukkan", name = "Bölüm 1: Tozlu Dükkân",
                endLine = "Bölüm 1 bitti, dükkân pırıl pırıl! Sıra mahzende... ama kapı kilitli. Notta ne yazıyordu? 'Anahtar kedide.'",
                tasks = new[]
                {
                    new StoryTask { id = "ag", title = "Örümcek ağlarını temizle", cost = 1, speaker = "Kara Pati",
                        line = "Hapşu! Cadı gittiğinden beri örümcekler dükkânı kendi evleri sanmış." },
                    new StoryTask { id = "pencere", title = "Pencereyi sil", cost = 2, speaker = "Kara Pati",
                        line = "Ay ışığı içeri süzülünce dükkân bambaşka oldu. Cadı her gece aya bakardı..." },
                    new StoryTask { id = "raf", title = "Rafları onar", cost = 2, speaker = "Kara Pati",
                        line = "Rafın arkasından buruşuk bir not düştü: 'Mahzenin anahtarı... kedide.' Kedide mi?!" },
                    new StoryTask { id = "kazan", title = "Kazanı yak", cost = 3, speaker = "Minik Boo",
                        line = "Kazan yeniden fokurduyor! Kasabadaki bütün hayaletler bu sesi duydu, hepsi geliyor!" },
                    new StoryTask { id = "hali", title = "Halıyı ser", cost = 2, speaker = "Kara Pati",
                        line = "Halının altında pati izleri var... ama benimkilerden çok daha büyük. Hmm." },
                    new StoryTask { id = "fener", title = "Fenerleri yak", cost = 3, speaker = "Sis Teyze",
                        line = "Fener ışığında duvarda bir harita belirdi canım! Ok işareti... aşağıyı gösteriyor." },
                    new StoryTask { id = "tezgah", title = "Tezgâhı onar", cost = 3, speaker = "Kara Pati",
                        line = "Çekmecede cadının günlüğü! Son sayfa: 'Mahzene iniyorum. Dükkâna Kara Pati baksın.'" },
                    new StoryTask { id = "yatak", title = "Kara Pati'ye yatak al", cost = 4, speaker = "Kara Pati",
                        line = "Ohh, kendime ait bir yatak... Biraz kestireyim, sonra o mahzene ineceğiz." },
                }
            },
            new Chapter
            {
                id = "mahzen", name = "Bölüm 2: Mahzenin Sırrı",
                endLine = "Aynadan cadının sesi yankılandı: 'Kara Pati! Aynanın içinde sıkışıp kaldım. Dolunayda beni çıkarabilirsin...' Bölüm 3 yakında!",
                tasks = new[]
                {
                    new StoryTask { id = "kapi", title = "Mahzen kapısını aç", cost = 2, speaker = "Kara Pati",
                        line = "Tasmamda tıngırdayan o şey... anahtarmış! Not haklıymış: anahtar kedideymiş. Yani bendeymiş!" },
                    new StoryTask { id = "mum", title = "Mumları yak", cost = 2, speaker = "Kara Pati",
                        line = "Mumlar yanınca duvardaki gölgeler kıpırdadı. Sadece gölge... değil mi? Değil mi?!" },
                    new StoryTask { id = "kavanoz", title = "Kavanoz rafını düzelt", cost = 3, speaker = "Profesör Uğultu",
                        line = "Şu etiketlere bak: 'Dolunay Tozu', 'Ayna Cilası'... Cadı büyük bir büyü hazırlıyormuş!" },
                    new StoryTask { id = "masa", title = "Çalışma masasını topla", cost = 3, speaker = "Kara Pati",
                        line = "Masadaki kâğıtların hepsi aynı şeyi çizmiş: bir ayna, bir kedi ve dolunay." },
                    new StoryTask { id = "zincir", title = "Zincirli sandığı aç", cost = 4, speaker = "Kont Gıcırtı",
                        line = "Gıcırrr... Bu zincirleri ben bile açamazdım! İçinde ne var bakalım, ne var?" },
                    new StoryTask { id = "daire", title = "Büyü dairesini temizle", cost = 3, speaker = "Leydi Ürperti",
                        line = "Daire parlıyor! Bu bir geçit büyüsü tatlım. Biri buradan bir yerlere gitmiş." },
                    new StoryTask { id = "portre", title = "Tozlu portreyi sil", cost = 3, speaker = "Sis Teyze",
                        line = "Aa, bu cadının portresi! Kucağındaki kara kediye bak canım... sen değil misin o?" },
                    new StoryTask { id = "ayna", title = "Sihirli aynayı onar", cost = 5, speaker = "Kara Pati",
                        line = "Ayna titredi ve camın ardında bir yüz belirdi... Cadı! Aynanın içinde!" },
                }
            },
            new Chapter
            {
                id = "bahce", name = "Bölüm 3: Dolunay Bahçesi",
                endLine = "Dolunay doğdu ve ayin başladı... ama aynadan simsiyah, dev bir kedi fırladı! Pati izlerinin sahibi oymuş. Cadıyı da kapıp aynaya geri daldı. Rüzgârda bir fısıltı: 'Hayalet Kasabası'na git...'",
                tasks = new[]
                {
                    new StoryTask { id = "b_kapi", title = "Bahçe kapısını aç", cost = 3, speaker = "Kara Pati",
                        line = "Dükkânın arkasında koca bir bahçe varmış! Cadı ayin için burayı hazırlıyormuş." },
                    new StoryTask { id = "b_ot", title = "Yabani otları biç", cost = 3, speaker = "Kara Pati",
                        line = "Otların arasında yine o kocaman pati izleri... Biri burada dolaşıyor." },
                    new StoryTask { id = "b_cesme", title = "Çeşmeyi çalıştır", cost = 4, speaker = "Minik Boo",
                        line = "Su şırıl şırıl! Ayin için ay ışığını yansıtacak su lazımmış, kitapta öyle yazıyor." },
                    new StoryTask { id = "b_cicek", title = "Ay çiçeklerini dik", cost = 3, speaker = "Bayan Fısıltı",
                        line = "Ay çiçekleri sadece dolunayda açar canım. Cadı onları hep buraya dikerdi." },
                    new StoryTask { id = "b_korkuluk", title = "Korkuluğu giydir", cost = 4, speaker = "Kaptan Pus",
                        line = "Ahoy! Korkuluğa cadının eski şapkasını giydirdik. Kargalar artık bize selam veriyor!" },
                    new StoryTask { id = "b_bal", title = "Balkabağı tarlasını düzelt", cost = 4, speaker = "Kara Pati",
                        line = "Balkabakları tombul tombul! Hayaletler şenlikte bunlara bayılıyor." },
                    new StoryTask { id = "b_fener", title = "Ateşböceği fenerlerini as", cost = 5, speaker = "Leydi Ürperti",
                        line = "Ateşböcekleri fenerlere doldu. Ne romantik... ama çalılarda iki sarı göz parladı!" },
                    new StoryTask { id = "b_kadran", title = "Ay kadranını kur", cost = 5, speaker = "Kara Pati",
                        line = "Kadran kuruldu. Dolunay tam tepede. Ayna parlıyor... Hazırım cadı hanım!" },
                }
            },
            new Chapter
            {
                id = "kasaba", name = "Bölüm 4: Hayalet Kasabası",
                endLine = "Çan çaldı ve kasabadaki bütün hayaletler aynı yönü gösterdi: tepedeki Kayıp Kule. 'Aynalar Diyarı'nın kapısı orada,' dedi Profesör Uğultu.",
                tasks = new[]
                {
                    new StoryTask { id = "k_kopru", title = "Sisli köprüyü onar", cost = 3, speaker = "Sis Teyze",
                        line = "Hoş geldin canım, Hayalet Kasabası'na! Köprü sisten yapılmıştı, hep çökerdi zaten." },
                    new StoryTask { id = "k_lamba", title = "Sokak lambalarını yak", cost = 4, speaker = "Kont Gıcırtı",
                        line = "Gıcır! Lambalar yandı. Bu kasabada en son ışık yüz yıl önce yanmıştı." },
                    new StoryTask { id = "k_tezgah", title = "Pazar tezgâhlarını kur", cost = 4, speaker = "Bayan Fısıltı",
                        line = "Pazar yeniden kuruldu! Satıcılardan biri o dev kediyi tanıyor: adı Gölge'ymiş." },
                    new StoryTask { id = "k_cesme", title = "Meydan çeşmesini temizle", cost = 4, speaker = "Minik Boo",
                        line = "Çeşmeden bir şişe çıktı, içinde not var: 'Gölge, cadının ilk kedisiydi.' Vay!" },
                    new StoryTask { id = "k_ev", title = "Evlerin camlarını sil", cost = 5, speaker = "Leydi Ürperti",
                        line = "Pencereler ışıl ışıl. Gölge yıllar önce buralarda yaşarmış, yalnız ve küskün..." },
                    new StoryTask { id = "k_bayrak", title = "Şenlik bayraklarını as", cost = 4, speaker = "Minik Boo",
                        line = "Bayraklar asıldı! Kasaba ilk kez şenlik havasında. Herkes sana yardım edecek Kara Pati!" },
                    new StoryTask { id = "k_saat", title = "Saat kulesini onar", cost = 5, speaker = "Profesör Uğultu",
                        line = "Saat tekrar işliyor delikanlı. Akrep dolunayı, yelkovan da tepedeki kuleyi gösteriyor." },
                    new StoryTask { id = "k_can", title = "Kule çanını çal", cost = 6, speaker = "Kaptan Pus",
                        line = "DONG! Bu çan sadece kayıp bir dost geri dönecekken çalarmış. Rotamız belli!" },
                }
            },
            new Chapter
            {
                id = "kule", name = "Bölüm 5: Kayıp Kule",
                endLine = "Geçit dairesi parladı ve mor bir kapı açıldı: Aynalar Diyarı! Kara Pati derin bir nefes aldı: 'Geliyorum cadı hanım. Gölge... seninle de konuşacağız.'",
                tasks = new[]
                {
                    new StoryTask { id = "t_merdiven", title = "Döner merdiveni sağlamlaştır", cost = 4, speaker = "Kara Pati",
                        line = "Bu kule cadının gözlemeviymiş! Merdivenler gıcırdıyor ama artık sağlam." },
                    new StoryTask { id = "t_kitap", title = "Kitaplığı düzenle", cost = 4, speaker = "Profesör Uğultu",
                        line = "Şu kitaba bak: 'Kediler ve Kıskançlık'. Altı çizili satır: 'Unutulduğunu sanan kedi, gölgeye dönüşür.'" },
                    new StoryTask { id = "t_harita", title = "Yıldız haritasını as", cost = 5, speaker = "Kara Pati",
                        line = "Haritadaki yıldızlar bir kedi şeklinde dizilmiş... Gölge'nin takımyıldızı!" },
                    new StoryTask { id = "t_teleskop", title = "Teleskobu onar", cost = 5, speaker = "Leydi Ürperti",
                        line = "Teleskopla aya baktım tatlım: ayın içinde bir kafes ve içinde cadı hanım var!" },
                    new StoryTask { id = "t_kure", title = "Kristal küreyi parlat", cost = 5, speaker = "Sis Teyze",
                        line = "Kürede Gölge görünüyor canım. Kızgın değil... sadece çok, çok yalnız." },
                    new StoryTask { id = "t_kazan", title = "Kule kazanını yak", cost = 5, speaker = "Minik Boo",
                        line = "Kazan mor fokurduyor! Geçit iksiri neredeyse hazır, son bir adım kaldı." },
                    new StoryTask { id = "t_pencere", title = "Ay penceresini aç", cost = 6, speaker = "Kaptan Pus",
                        line = "Ay ışığı içeri doldu! Pusula ilk kez dümdüz gösteriyor: aynanın içine!" },
                    new StoryTask { id = "t_gecit", title = "Geçit dairesini çiz", cost = 6, speaker = "Kara Pati",
                        line = "Daireyi pati izimle mühürledim. Anahtar kedideymiş, hatırladın mı? Hep bendeymiş." },
                }
            },
            new Chapter
            {
                id = "diyar", name = "Bölüm 6: Aynalar Diyarı",
                endLine = "Kafes açıldı! Cadı hanım, Gölge ve Kara Pati birlikte geçitten süzüldü... ve dükkânın sıcak ışığına geri döndüler.",
                tasks = new[]
                {
                    new StoryTask { id = "d_kopru", title = "Kristal köprüyü kur", cost = 4, speaker = "Kara Pati",
                        line = "Burası Aynalar Diyarı... Her şey mor ve parlak. Köprüyü kristallerle tamamladım." },
                    new StoryTask { id = "d_fener", title = "Yıldız fenerlerini yak", cost = 5, speaker = "Sis Teyze",
                        line = "Fenerler yandı canım! Biz hayaletler de geldik, yalnız değilsin." },
                    new StoryTask { id = "d_ayna", title = "Kırık aynaları birleştir", cost = 5, speaker = "Profesör Uğultu",
                        line = "Aynalar birleşince Gölge'nin anıları göründü: cadıyla oynadığı mutlu günler." },
                    new StoryTask { id = "d_yol", title = "Pati izli yolu takip et", cost = 6, speaker = "Kara Pati",
                        line = "İzler bir tahta çıkıyor. Tahtta Gölge oturuyor. Dev ama... ürkek bakıyor." },
                    new StoryTask { id = "d_yastik", title = "Gölge için yastık bırak", cost = 6, speaker = "Gölge",
                        line = "Bu yastık... benim eski yastığım mı? Cadı onu saklamış mı? Mrr..." },
                    new StoryTask { id = "d_yumak", title = "Gölge'yle yumak oyna", cost = 6, speaker = "Gölge",
                        line = "Hah! Yakaladım! Uzun zamandır kimse benimle oynamamıştı, küçük kara kedi." },
                    new StoryTask { id = "d_kafes", title = "Kafesin kilidini aç", cost = 7, speaker = "Cadı Hanım",
                        line = "Kara Pati! Beni buldun! Gölge'yi de yumuşatmışsın, ah benim akıllı kedim." },
                    new StoryTask { id = "d_eve", title = "Herkesle eve dön", cost = 8, speaker = "Gölge",
                        line = "Ben de gelebilir miyim? Söz, artık aynalara saklanmak yok. Sadece... bir yastık lazım." },
                }
            },
        };

        public static readonly Clue[] Clues =
        {
            new Clue { id = "c1_not", chapter = 0, x = 400f, y = 60f, afterTasks = 3, title = "Buruşuk Not",
                text = "'Mahzenin anahtarı kedide.' Cadının el yazısı... ama çok aceleyle yazılmış." },
            new Clue { id = "c1_iz", chapter = 0, x = 200f, y = -430f, afterTasks = 5, title = "Dev Pati İzi",
                text = "Bu iz kocaman! Bir kedinin ama dev gibi bir kedinin. Cadının başka bir yardımcısı mı vardı?" },
            new Clue { id = "c1_gunluk", chapter = 0, x = 200f, y = -150f, afterTasks = 7, title = "Günlük Sayfası",
                text = "'Ayna çatladığı gece bir şeyler ters gitti. Kara Pati'yi korumalıyım, ne olursa olsun.'" },
            new Clue { id = "c2_supurge", chapter = 1, x = -400f, y = -390f, afterTasks = 10, title = "Kırık Süpürge",
                text = "Cadının süpürgesi ikiye ayrılmış. Buradan aceleyle kaçmış olmalı... ya da biri onu çekmiş." },
            new Clue { id = "c2_takvim", chapter = 1, x = 170f, y = -40f, afterTasks = 12, title = "Ay Takvimi",
                text = "Dolunay günleri kırmızıyla daire içine alınmış. Bir sonraki dolunay... çok yakında!" },
            new Clue { id = "c2_tasma", chapter = 1, x = -20f, y = -170f, afterTasks = 13, title = "Gümüş Tasma",
                text = "Sandıktan minik bir gümüş tasma çıktı. Üzerinde 'K.P.' yazıyor. Bu benim bebeklik tasmam mı?!" },
            new Clue { id = "c2_iz", chapter = 1, x = 220f, y = -430f, afterTasks = 14, title = "Parlayan İzler",
                text = "Büyü dairesinden aynaya doğru uzanan parlak pati izleri. Dev kedi buradan geçmiş!" },
            new Clue { id = "c2_mektup", chapter = 1, x = 170f, y = 380f, afterTasks = 15, title = "Mühürlü Mektup",
                text = "Portrenin arkasında bir mektup: 'Aynayı onaran, beni geri getirir. -C.'" },
            new Clue { id = "c3_iz", chapter = 2, x = 120f, y = -430f, afterTasks = 18, title = "Çamurdaki Dev İz",
                text = "Bahçedeki çamurda kocaman bir pati izi. Kenarında siyah tüyler var. Bu kedi benden üç kat büyük!" },
            new Clue { id = "c3_tuy", chapter = 2, x = -300f, y = -270f, afterTasks = 20, title = "Siyah Tüy Yumağı",
                text = "Korkuluğun dibinde simsiyah tüyler. Dokununca hafifçe titriyorlar... sanki gölgeden yapılmışlar." },
            new Clue { id = "c3_not", chapter = 2, x = 430f, y = -20f, afterTasks = 23, title = "Yırtık Ayin Notu",
                text = "'Dolunayda ayna açılırsa, içeride bekleyen de dışarı çıkar. Dikkat et.' Hmm, keşke daha önce okusaydım." },
            new Clue { id = "c4_afis", chapter = 3, x = 390f, y = 40f, afterTasks = 26, title = "Eski Afiş",
                text = "Solmuş bir afiş: 'Kayıp kedi: Gölge. Siyah, iri, çok sevimli. Bulana kocaman teşekkür. -Cadı'" },
            new Clue { id = "c4_foto", chapter = 3, x = -170f, y = -130f, afterTasks = 29, title = "Soluk Fotoğraf",
                text = "Genç cadı kucağında koca siyah bir kediyle gülümsüyor. Arkasında: 'Gölge ile, ilk dükkân günü.'" },
            new Clue { id = "c5_gunluk", chapter = 4, x = 330f, y = 30f, afterTasks = 35, title = "Cadının Son Notu",
                text = "'Gölge kaybolduğunda çok ağladım. Sonra Kara Pati geldi. Gölge bunu duyunca küstü sanırım...'" },
            new Clue { id = "c5_tasma", chapter = 4, x = -330f, y = -330f, afterTasks = 38, title = "Kocaman Tasma",
                text = "Yerde dev bir tasma. Üzerinde 'G.' harfi. Benim 'K.P.' tasmamın kocaman bir ikizi gibi." },
            new Clue { id = "c6_oyuncak", chapter = 5, x = -160f, y = -340f, afterTasks = 43, title = "Eski Oyuncak Fare",
                text = "Yıpranmış bir oyuncak fare. Gölge bunu yıllardır saklamış. Demek ki hep eve dönmek istemiş." },
            new Clue { id = "c6_mektup", chapter = 5, x = 430f, y = -40f, afterTasks = 46, title = "Gölge'nin Mektubu",
                text = "Pati izleriyle yazılmış: 'Beni unuttuklarını sandım. Belki... yeni kedi ile arkadaş olabiliriz?'" },
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
            var parts = new List<string> { "+" + Gold(chapter, gift) + " altın" };
            if (Energy(chapter, gift) > 0) parts.Add("+" + Energy(chapter, gift) + " enerji");
            if (Stamps(chapter, gift) > 0) parts.Add("+" + Stamps(chapter, gift) + " Pati Mührü");
            if (Sticker(chapter, gift)) parts.Add("bir etiket");
            return string.Join(", ", parts);
        }
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
        public const string Name = "Balkabağı Şenliği";
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
                case "gold": return m.amount + " altın";
                case "energy": return m.amount + " enerji";
                case "stamps": return m.amount + " Pati Mührü";
                default: return "Sürpriz etiket";
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
            m.type == "sticker" ? m.amount + " altın + etiket" : PumpkinEvent.RewardText(m);
    }

    public static class Ghosts
    {
        public static readonly string[] Names =
        {
            "Bayan Fısıltı", "Sör Tıkırtı", "Minik Boo", "Kont Gıcırtı",
            "Sis Teyze", "Kaptan Pus", "Profesör Uğultu", "Leydi Ürperti"
        };

        public static readonly string[] Lines =
        {
            "Oh, teşekkürler! Artık rahat uyuyabilirim...",
            "Muhteşem! Kedin de çok tatlı bu arada.",
            "Boo-hoo! Mutluluktan ağlıyorum!",
            "Bu iksirle duvarlardan iki kat hızlı geçeceğim!",
            "Cadı hanım da tam böyle yapardı... Nereye kayboldu acaba?",
            "Miyavlayan dükkân, harika hizmet!",
            "Kara Pati! Cadıyı en son mahzende görmüştüm, biliyor muydun?",
            "Zincirlerimi şıngırdatmadan teşekkür ederim.",
            "Cadının süpürgesi hâlâ çatıda duruyor, garip değil mi?",
            "Bir dahaki sefere kedi maması getiririm!"
        };
    }
}
