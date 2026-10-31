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

        public ItemData(string chain, int level)
        {
            this.chain = chain;
            this.level = level;
        }

        public bool IsKitten => chain == Chains.Kedi;
        public bool IsWildKitten => chain == Chains.Kedi && level == 1;
        public string Name => Chains.Name(chain, level);
    }

    [Serializable] public class CellSave { public int x, y; public string chain; public int level; public bool nap; public float cd; }
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
        public int version = 2;
        public int gold;
        public int energy;
        public float energyTimer;
        public long stamp;      // save time (ticks)
        public int served;
        public bool kittenHint;
        public int stamps;      // Pati Mührü
        public int tasksDone;
        public bool introSeen;
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

    public static class Story
    {
        public const string ChapterName = "Bölüm 1: Tozlu Dükkân";

        public const string IntroLine =
            "Cadı ortadan kayboldu, dükkân da toz içinde kaldı. Hayaletlere iksir malzemesi verip Pati Mührü kazanalım, " +
            "dükkânı adım adım toparlayalım. İlk iş: şu örümcek ağları!";

        public const string FirstBoardHint = "Mühür bitti! 'Dükkânı Aç'a bas, hayaletlerin siparişlerini hazırla.";

        public const string EndLine = "Bölüm 1 bitti, dükkân pırıl pırıl! Ama mahzen kapısı kilitli... Bölüm 2 yakında.";

        public static readonly StoryTask[] Tasks =
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
        };
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
