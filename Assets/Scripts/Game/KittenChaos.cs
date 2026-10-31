using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Mischievous kittens: they wander in, push items around and fall asleep on top of them.
    /// Merge two kittens into an Apprentice (tap for free items), two Apprentices into a Master (auto-producer).
    /// </summary>
    public class KittenChaos : MonoBehaviour
    {
        public const float ApprenticeCooldown = 12f;
        public const float MasterInterval = 20f;
        const int MaxWild = 3;

        float chaosTimer = 7f;
        float hopTimer = 4f;

        GameManager GM => GameManager.I;
        BoardController B => GM.Board;

        static readonly string[] arriveLines =
        {
            "Miyav! Bir yavru pencereden süzüldü.",
            "Kapı gıcırdadı... içeri bir yavru daldı!",
            "Bacadan minik bir yavru düştü. Pıt!",
        };

        void Update()
        {
            if (GM == null || B == null || !B.Ready) return;
            float dt = Time.deltaTime;
            bool hasMaster = false;

            foreach (var c in B.AllCells())
            {
                var d = B.Get(c);
                if (d == null || !d.IsKitten || d.level < 2) continue;
                if (d.cd > 0f) d.cd = Mathf.Max(0f, d.cd - dt);
                if (d.level < 3) continue;
                hasMaster = true;
                if (d.cd > 0f || d.nap) continue;
                if (B.NearestEmpty(c, out var e))
                {
                    B.SpawnFrom(c, e, new ItemData(Chains.RandomIngredient(), 2));
                    d.cd = MasterInterval;
                    var v = B.ViewAt(c);
                    if (v) Tween.Punch(v.rt);
                    GM.OnBoardChanged();
                }
                else d.cd = 3f;
            }

            if (B.AnyHeld) return; // never surprise the player mid-drag

            chaosTimer -= dt;
            if (chaosTimer <= 0f)
            {
                DoChaos();
                chaosTimer = Random.Range(11f, 18f) * (hasMaster ? 1.8f : 1f);
            }

            hopTimer -= dt;
            if (hopTimer <= 0f)
            {
                Hop();
                hopTimer = Random.Range(8f, 14f);
            }
        }

        void DoChaos()
        {
            var wild = B.Cells(d => d.IsWildKitten);
            wild.Remove(B.Selected);
            var empties = B.EmptyCells();
            var targets = B.Cells(d => !d.nap && !d.IsKitten);
            var ingredients = B.Cells(d => !d.nap && Chains.IsIngredient(d.chain));

            float wSpawn = (wild.Count < MaxWild && empties.Count > 0) ? (wild.Count == 0 ? 3f : 1.2f) : 0f;
            float wNap = (wild.Count > 0 && targets.Count > 0) ? 2f : 0f;
            float wKnock = (wild.Count > 0 && ingredients.Count > 0 && empties.Count > 0) ? 2f : 0f;
            float total = wSpawn + wNap + wKnock;
            if (total <= 0f) return;

            float roll = Random.value * total;
            if (roll < wSpawn)
            {
                var cell = empties[Random.Range(0, empties.Count)];
                B.Set(cell, new ItemData(Chains.Kedi, 1), true);
                string msg = arriveLines[Random.Range(0, arriveLines.Length)];
                if (!GM.KittenHintShown)
                {
                    msg += " İki yavruyu birleştirirsen sana yardım ederler!";
                    GM.KittenHintShown = true;
                }
                GM.Toast(msg, UIKit.Orange, 4f);
            }
            else if (roll < wSpawn + wNap)
            {
                var k = wild[Random.Range(0, wild.Count)];
                var t = Closest(targets, k);
                string name = B.Get(t).Name;
                B.KittenJumpOnto(k, t);
                GM.Toast("Yavru, " + name + " üstüne kıvrılıp uyudu. Dokunup okşa!", UIKit.Orange);
            }
            else
            {
                var k = wild[Random.Range(0, wild.Count)];
                var item = ingredients[Random.Range(0, ingredients.Count)];
                var dest = empties[Random.Range(0, empties.Count)];
                string name = B.Get(item).Name;
                var kv = B.ViewAt(k);
                if (kv) Tween.Punch(kv.rt, 0.25f, 0.3f);
                B.MoveItem(item, dest, 0.5f, true);
                GM.Toast("Hop! Bir yavru patisiyle itti: " + name, UIKit.Orange);
            }
            B.RefreshSelection();
            GM.OnBoardChanged();
        }

        static Vector2Int Closest(List<Vector2Int> cells, Vector2Int from)
        {
            var best = cells[0];
            float bd = float.MaxValue;
            foreach (var c in cells)
            {
                float d = (c - from).sqrMagnitude + Random.value * 3f;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        void Hop()
        {
            var wild = B.Cells(d => d.IsWildKitten);
            wild.Remove(B.Selected); // the kitten the player is looking at stays put
            if (wild.Count == 0) return;
            var k = wild[Random.Range(0, wild.Count)];
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var options = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                var n = k + d;
                if (BoardController.InBounds(n) && B.Get(n) == null) options.Add(n);
            }
            if (options.Count == 0) return;
            var to = options[Random.Range(0, options.Count)];
            B.MoveItem(k, to, 0.22f);
            GM.MarkDirty();
        }

        public void Pet(Vector2Int c)
        {
            var d = B.Get(c);
            if (d == null || !d.nap) return;
            d.nap = false;
            B.RefreshView(c);
            var v = B.ViewAt(c);
            if (v) Tween.Punch(v.rt, 0.2f, 0.3f);
            if (B.NearestEmpty(c, out var e))
            {
                B.SpawnFrom(c, e, new ItemData(Chains.Kedi, 1));
                GM.Toast("Mırrr... Yavru uyandı ve yana zıpladı.");
            }
            else
            {
                GM.AddGold(2);
                GM.Toast("Mırrr... Yavru kaçtı ama arkasında 2 altın bıraktı!");
            }
            B.RefreshSelection();
            GM.OnBoardChanged();
        }

        public void OnKittenTapped(Vector2Int c)
        {
            var d = B.Get(c);
            var v = B.ViewAt(c);
            if (d == null) return;
            if (v) Tween.Punch(v.rt, 0.2f, 0.3f);

            if (d.level == 1)
            {
                GM.Toast("Miyav! (Yavrular birleşince işe yararlar.)");
                return;
            }
            if (d.level == 3)
            {
                GM.Toast("Usta Kedi kendi işini bilir: her " + (int)MasterInterval + " saniyede bir malzeme getirir.");
                return;
            }
            if (d.cd > 0f)
            {
                GM.Toast("Çırak dinleniyor... " + Mathf.CeilToInt(d.cd) + " sn");
                return;
            }
            if (!B.NearestEmpty(c, out var e))
            {
                GM.Toast("Tahta dolu! Çırak malzemeyi koyacak yer bulamadı.");
                return;
            }
            B.SpawnFrom(c, e, new ItemData(Chains.RandomIngredient(), 1));
            d.cd = ApprenticeCooldown;
            GM.Toast("Çırak kedi bir malzeme getirdi!");
            GM.OnBoardChanged();
        }
    }
}
