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
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int energy;
        public float energyTimer;
        public long stamp;
        public int served;
        public bool kittenHint;
        public List<CellSave> cells = new List<CellSave>();
        public List<OrderSave> orders = new List<OrderSave>();
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
