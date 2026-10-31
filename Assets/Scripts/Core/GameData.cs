using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    public static class Chains
    {
        public const string Ot = "ot", Mantar = "mantar", Kristal = "kristal", Ag = "ag";
        public const string Kedi = "kedi", Kazan = "kazan", Sandik = "sandik";

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
        };

        public static int MaxLevel(string chain) => names[chain].Length;
        public static string Name(string chain, int level) => names[chain][Mathf.Clamp(level - 1, 0, names[chain].Length - 1)];
        public static bool IsIngredient(string c) => c == Ot || c == Mantar || c == Kristal || c == Ag;
        public static bool IsGenerator(string c) => c == Kazan || c == Sandik;
        public static int Value(int level) => 5 * (1 << (level - 1));
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

    public static class Bubbles
    {
        public const float Seconds = 60f;
        public static float Chance(int level) => level >= 4 ? 0.14f : 0.1f;
        public static int Price(int level) => 6 * level + 4 * (level - 1) * (level - 1);
    }

    [Serializable] public class CellSave { public int x, y; public string chain; public int level; public bool nap; public float cd; public bool bubble; public long bubbleUntil; }
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
        public List<string> clues = new List<string>();
        public int level = 1;
        public int xp;
        public List<ReqSave> storage = new List<ReqSave>();
        public long eventStart;
        public int pumpkins;
        public int eventClaimed;
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
    public static class Levels
    {
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
