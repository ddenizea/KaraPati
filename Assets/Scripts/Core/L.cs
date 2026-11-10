using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Languages. Every text the player sees is written in Turkish in the code and wrapped in L.T("..."); the Turkish
    /// text itself is the key. Assets/Resources/Lang/strings.csv has one row per text: tr, en (more languages = more
    /// columns). Tools/loc_extract.py collects every L.T / L.F / L.N text from the code into that table.
    /// When a Turkish text changes, its row changes too, so the new text shows up as "not translated yet" instead of
    /// keeping an old, wrong translation.
    /// </summary>
    public static class L
    {
        const string PrefKey = "karapati_lang";
        public static readonly string[] Codes = { "tr", "en" };
        public static readonly string[] Names = { "Türkçe", "English" };   // each in its own language

        static Dictionary<string, string> map;
        static string lang;

        public static string Lang { get { Ensure(); return lang; } }
        public static bool IsTr => Lang == "tr";

#if UNITY_EDITOR
        /// <summary>Texts asked for in a language that has no translation for them yet (editor only, for the test menu).</summary>
        public static readonly HashSet<string> Missing = new HashSet<string>();
#endif

        static void Ensure()
        {
            if (map != null) return;
            lang = PlayerPrefs.GetString(PrefKey, "");
            if (System.Array.IndexOf(Codes, lang) < 0)
                lang = Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";   // first start: the phone's language
            Load();
        }

        static void Load()
        {
            map = new Dictionary<string, string>();
            if (lang == "tr") return;
            var asset = Resources.Load<TextAsset>("Lang/strings");
            if (!asset) { Debug.LogWarning("KaraPati: Lang/strings.csv bulunamadı."); return; }
            var rows = ParseCsv(asset.text);
            if (rows.Count == 0) return;
            int col = rows[0].IndexOf(lang), tr = rows[0].IndexOf("tr");
            if (col < 0 || tr < 0) return;
            for (int i = 1; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Count <= col || r.Count <= tr) continue;
                string key = Unescape(r[tr]), val = Unescape(r[col]);
                if (key.Length > 0 && val.Length > 0) map[key] = val;
            }
        }

        /// <summary>The text in the player's language (the Turkish text if there is no translation yet).</summary>
        public static string T(string tr)
        {
            if (string.IsNullOrEmpty(tr)) return tr;
            Ensure();
            if (lang == "tr") return tr;
            if (map.TryGetValue(tr, out var s)) return s;
#if UNITY_EDITOR
            Missing.Add(tr);
#endif
            return tr;
        }

        /// <summary>A template with {0}, {1}...: L.F("Seviye {0}!", level). Word order may differ per language.</summary>
        public static string F(string tr, params object[] args) => string.Format(T(tr), args);

        /// <summary>Marks a text for the table without translating it now (data lists); translate it with T() where it is shown.</summary>
        public static string N(string tr) => tr;

        /// <summary>Save the choice; the caller rebuilds the screens.</summary>
        public static void SetLanguage(string code)
        {
            if (System.Array.IndexOf(Codes, code) < 0) return;
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
            map = null;
            Ensure();
        }

        // "\n" in the table is a line break
        static string Unescape(string s) => s.Replace("\\n", "\n");

        /// <summary>Plain CSV: commas, "quoted, fields" and "" for a quote inside quotes.</summary>
        static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else quoted = false;
                    }
                    else cell.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(cell.ToString()); cell.Clear();
                    rows.Add(row); row = new List<string>();
                }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            if (rows.Count > 0 && rows[0].Count > 0 && rows[0][0].Length > 0 && rows[0][0][0] == '﻿') rows[0][0] = rows[0][0].Substring(1);   // Excel BOM
            return rows;
        }
    }

    /// <summary>
    /// A list of Turkish texts that comes back in the player's language when read: list[i] is already translated.
    /// Works like a string[] (indexer, Length, foreach), so data lists only change their declaration.
    /// </summary>
    public sealed class TrList : IEnumerable<string>
    {
        readonly string[] items;
        public TrList(params string[] items) { this.items = items; }
        public string this[int i] => L.T(items[i]);
        public int Length => items.Length;
        /// <summary>Position of a text as shown (in the player's language), -1 if it is not in the list.</summary>
        public int IndexOf(string shown)
        {
            for (int i = 0; i < items.Length; i++) if (L.T(items[i]) == shown) return i;
            return -1;
        }
        public IEnumerator<string> GetEnumerator() { foreach (var s in items) yield return L.T(s); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
