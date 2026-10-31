using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// The cat system. Wild kittens hop onto the table, push items around and fall asleep on them.
    /// Drag one kitten onto another and they become an Apprentice: while it is here (2 real hours)
    /// no kittens come. The Apprentice can be promoted to a permanent Master for gold.
    /// </summary>
    public class KittenChaos : MonoBehaviour
    {
        public const float ApprenticeCooldown = 12f;
        public const float MasterInterval = 20f;
        public const double ApprenticeHours = 2.0;
        public const int PromoteCost = 150;
        const int MaxWild = 3;

        readonly List<KittenAgent> agents = new List<KittenAgent>();
        RectTransform layer;
        float spawnTimer = 6f;

        public KittenAgent Selected { get; private set; }

        GameManager GM => GameManager.I;
        BoardController B => GM.Board;
        public bool Active => GM != null && GM.OnBoard && !GM.ModalOpen;

        static readonly string[] arriveLines =
        {
            "Miyav! Bir yavru pencereden masaya zıpladı.",
            "Kapı gıcırdadı... bir yavru masaya hopladı!",
            "Bacadan minik bir yavru düştü, pıt diye masaya!",
        };

        public void Init(RectTransform catsLayer) => layer = catsLayer;

        // ------------------------------------------------------------ geometry
        Vector2 Half => new Vector2(BoardController.Cols * BoardController.CS / 2f - 45f, BoardController.Rows * BoardController.CS / 2f - 45f);

        public Vector2 Clamp(Vector2 p)
        {
            var h = Half;
            return new Vector2(Mathf.Clamp(p.x, -h.x, h.x), Mathf.Clamp(p.y, -h.y, h.y));
        }

        public Vector2 RandomPoint()
        {
            var h = Half;
            return new Vector2(Random.Range(-h.x, h.x), Random.Range(-h.y, h.y));
        }

        public Vector2 NearestEdge(Vector2 p)
        {
            var h = Half;
            return new Vector2(p.x < 0f ? -h.x : h.x, p.y);
        }

        // ------------------------------------------------------------ queries
        public bool AnyHeld
        {
            get
            {
                foreach (var a in agents) if (a && a.IsHeld) return true;
                return false;
            }
        }

        public bool HasApprentice
        {
            get
            {
                foreach (var a in agents) if (a && a.level == 2 && !a.IsGone) return true;
                return false;
            }
        }

        public bool HasMaster
        {
            get
            {
                foreach (var a in agents) if (a && a.level == 3 && !a.IsGone) return true;
                return false;
            }
        }

        int WildCount
        {
            get
            {
                int n = 0;
                foreach (var a in agents) if (a && a.IsWild && !a.IsGone) n++;
                return n;
            }
        }

        KittenAgent NapperAt(Vector2Int c)
        {
            foreach (var a in agents)
                if (a && a.state == KittenAgent.State.Napping && a.napCell == c) return a;
            return null;
        }

        // ------------------------------------------------------------ loop
        void Update()
        {
            if (!Active || layer == null) return;
            float dt = Time.deltaTime;
            long now = DateTime.UtcNow.Ticks;

            for (int i = agents.Count - 1; i >= 0; i--)
            {
                var a = agents[i];
                if (!a) { agents.RemoveAt(i); continue; }
                if (a.level < 2 || a.IsGone) continue;
                if (a.cd > 0f) a.cd = Mathf.Max(0f, a.cd - dt);
                if (a.level == 2 && now >= a.expires) { Farewell(a); continue; }
                if (a.level == 3 && a.cd <= 0f && a.CanAct)
                    a.cd = ProduceNear(a, 2) ? MasterInterval : 3f;
            }

            if (!HasApprentice && WildCount < MaxWild && !B.AnyHeld)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    SpawnWild();
                    spawnTimer = Random.Range(12f, 20f) * (HasMaster ? 1.8f : 1f);
                }
            }
        }

        KittenAgent Create(int level, Vector2 pos)
        {
            var rt = UIKit.Rect("Kitten", layer);
            var a = rt.gameObject.AddComponent<KittenAgent>();
            a.Init(this, level, pos);
            agents.Add(a);
            return a;
        }

        void SpawnWild()
        {
            var h = Half;
            float side = Random.value < 0.5f ? -1f : 1f;
            float y = Random.Range(-h.y * 0.8f, h.y * 0.8f);
            var a = Create(1, new Vector2(side * (h.x + 200f), y - 80f));
            a.JumpIn(a.Pos, new Vector2(side * (h.x - 30f), y));

            string msg = arriveLines[Random.Range(0, arriveLines.Length)];
            if (!GM.KittenHintShown)
            {
                msg += " İki yavruyu üst üste sürükle: Çırak Kedi olurlar!";
                GM.KittenHintShown = true;
            }
            GM.Toast(msg, UIKit.Orange, 4.5f);
            GM.GiveSticker("c_yavru", false);
            GM.MarkDirty();
        }

        // ------------------------------------------------------------ mischief
        public void PlanMischief(KittenAgent a)
        {
            if (B.AnyHeld) return;
            var cands = B.Cells(d => !d.Locked && !Chains.IsStation(d.chain)); // never nap on the alchemy cauldron
            cands.RemoveAll(c => IsTargeted(c));
            if (cands.Count == 0) return;
            cands.Sort((x, y) => (BoardController.CellPos(x) - a.Pos).sqrMagnitude
                .CompareTo((BoardController.CellPos(y) - a.Pos).sqrMagnitude));
            var cell = cands[Random.Range(0, Mathf.Min(3, cands.Count))];
            var d0 = B.Get(cell);

            if (Chains.IsIngredient(d0.chain) && Random.value < 0.55f && PushDir(cell, out var dir))
            {
                var stand = BoardController.CellPos(cell) - new Vector2(dir.x, -dir.y) * 82f;
                a.WalkTo(stand, () => DoPush(a, cell, dir));
            }
            else
            {
                a.WalkTo(BoardController.CellPos(cell) + new Vector2(0f, 8f), () => DoNap(a, cell));
            }
        }

        bool IsTargeted(Vector2Int c) => NapperAt(c) != null;

        bool PushDir(Vector2Int c, out Vector2Int dir)
        {
            var opts = new List<Vector2Int>();
            foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
            {
                var n = c + d;
                if (BoardController.InBounds(n) && B.Get(n) == null) opts.Add(d);
            }
            dir = opts.Count > 0 ? opts[Random.Range(0, opts.Count)] : Vector2Int.zero;
            return opts.Count > 0;
        }

        void DoPush(KittenAgent a, Vector2Int c, Vector2Int dir)
        {
            if (!a || B.AnyHeld) return;
            var d = B.Get(c);
            var n = c + dir;
            if (d == null || d.Locked || !Chains.IsIngredient(d.chain) || !BoardController.InBounds(n) || B.Get(n) != null) return;
            a.Face(dir.x != 0 ? dir.x : (BoardController.CellPos(c).x >= a.Pos.x ? 1 : -1));
            Tween.Punch(a.rt, 0.15f, 0.25f);
            bool sel = B.Selected == c;
            B.MoveItem(c, n, 0.35f, true);
            if (sel) B.Select(n);
            GM.Toast("Hop! Yavru patisiyle itti: " + d.Name, UIKit.Orange);
            GM.OnBoardChanged();
        }

        void DoNap(KittenAgent a, Vector2Int c)
        {
            if (!a || B.AnyHeld) return;
            var d = B.Get(c);
            if (d == null || d.Locked || Chains.IsStation(d.chain) || NapperAt(c) != null) return;
            d.nap = true;
            B.RefreshView(c);
            a.Nap(c, BoardController.CellPos(c) + new Vector2(0f, 12f));
            a.rt.SetAsLastSibling();
            B.RefreshSelection();
            GM.Toast("Yavru, " + d.Name + " üstüne kıvrılıp uyudu. Dokunup okşa!", UIKit.Orange);
            GM.OnBoardChanged();
        }

        // ------------------------------------------------------------ player interaction
        public void Select(KittenAgent a)
        {
            if (Selected && Selected != a) Selected.SetSelected(false);
            Selected = a;
            if (a) a.SetSelected(true);
            B.HideSelector();
            GM.ShowKittenInfo(a);
        }

        public void Deselect()
        {
            if (Selected) Selected.SetSelected(false);
            Selected = null;
        }

        public void PetAt(Vector2Int c)
        {
            var a = NapperAt(c);
            if (a) { Pet(a); return; }
            var d = B.Get(c);
            if (d == null) return;
            d.nap = false;
            B.RefreshView(c);
            GM.OnBoardChanged();
        }

        void Pet(KittenAgent a)
        {
            WakeItem(a);
            a.WakeUp();
            Tween.Punch(a.rt, 0.2f, 0.3f);
            a.WalkTo(a.Pos + Random.insideUnitCircle.normalized * 170f);
            FX.Burst(B.FxLayer, a.Pos + new Vector2(0f, 30f), new Color(1f, 0.6f, 0.75f), 5);
            GM.Toast("Mırrr... Yavru uyandı ve gerindi.");
            GM.GiveSticker("c_uykucu", false);
            B.RefreshSelection();
            if (Selected == a) GM.ShowKittenInfo(a);
            GM.OnBoardChanged();
        }

        void WakeItem(KittenAgent a)
        {
            var d = B.Get(a.napCell);
            if (d != null) { d.nap = false; B.RefreshView(a.napCell); }
            a.napCell = new Vector2Int(-1, -1);
        }

        public void OnTapped(KittenAgent a)
        {
            Select(a);
            if (a.state == KittenAgent.State.Napping) { Pet(a); return; }
            Tween.Punch(a.rt, 0.18f, 0.3f);
            switch (a.level)
            {
                case 1:
                    GM.Toast("Miyav! (İki yavruyu üst üste sürükle.)");
                    break;
                case 2:
                    if (a.cd > 0f) GM.Toast("Çırak dinleniyor... " + Mathf.CeilToInt(a.cd) + " sn");
                    else if (ProduceNear(a, 1))
                    {
                        a.cd = ApprenticeCooldown;
                        GM.Toast("Çırak kedi bir malzeme getirdi!");
                    }
                    else GM.Toast("Masa dolu! Çırak malzemeyi koyacak yer bulamadı.");
                    break;
                default:
                    GM.Toast("Usta Kedi kendi işini bilir: her " + (int)MasterInterval + " saniyede bir malzeme getirir.");
                    break;
            }
        }

        public void OnDropped(KittenAgent a)
        {
            if (!a.IsWild) return;
            KittenAgent other = null;
            float best = 100f * 100f;
            foreach (var o in agents)
            {
                if (!o || o == a || !o.IsWild || o.IsGone || o.state == KittenAgent.State.Entering) continue;
                float d = (o.Pos - a.Pos).sqrMagnitude;
                if (d < best) { best = d; other = o; }
            }
            if (other) MergeKittens(a, other);
        }

        void MergeKittens(KittenAgent a, KittenAgent b)
        {
            var pos = Clamp(b.Pos);
            Release(a);
            Release(b);
            var c = Create(2, pos);
            c.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(ApprenticeHours).Ticks;
            Tween.Pop(c.rt, 0.35f);
            FX.Burst(B.FxLayer, pos, UIKit.Orange, 10);

            foreach (var o in agents.ToArray())
            {
                if (!o || !o.IsWild || o.IsGone) continue;
                if (o.state == KittenAgent.State.Napping) WakeItem(o);
                o.Leave();
            }

            Select(c);
            GM.Toast("Yavrular birleşti: Çırak Kedi! 2 saat yanında kalacak, yavrular onu görünce kaçışıyor.", UIKit.Orange, 5f);
            GM.GiveSticker("c_cirak", false);
            GM.OnBoardChanged();
        }

        void Release(KittenAgent a)
        {
            if (a.state == KittenAgent.State.Napping) WakeItem(a);
            if (Selected == a) Selected = null;
            agents.Remove(a);
            Destroy(a.gameObject);
        }

        public void Remove(KittenAgent a)
        {
            if (Selected == a) { Selected = null; GM.ShowInfo(null); }
            agents.Remove(a);
            if (a) Destroy(a.gameObject);
            GM.MarkDirty();
        }

        bool ProduceNear(KittenAgent a, int lvl)
        {
            var cell = BoardController.LocalToCellClamped(a.Pos);
            if (!B.NearestEmpty(cell, out var e)) return false;
            B.SpawnFromPos(a.Pos, e, new ItemData(Chains.RandomIngredient(), lvl));
            Tween.Punch(a.rt, 0.15f, 0.25f);
            GM.OnBoardChanged();
            return true;
        }

        void Farewell(KittenAgent a)
        {
            bool gift = ProduceNear(a, 2);
            GM.Toast(gift
                ? "Çırak kedi vedalaştı ve sana bir hediye bıraktı. Yavrular yeniden gelebilir!"
                : "Çırak kedi vedalaştı. Yavrular yeniden gelebilir!", UIKit.Orange, 5f);
            if (Selected == a) { Selected = null; GM.ShowInfo(null); }
            a.Leave();
            spawnTimer = 8f;
            GM.MarkDirty();
        }

        public void Promote(KittenAgent a)
        {
            if (!a || a.level != 2 || a.IsGone) return;
            if (HasMaster) { GM.Toast("Dükkânda zaten bir Usta Kedi var."); return; }
            if (!GM.TrySpendGold(PromoteCost)) return;
            var pos = a.Pos;
            Release(a);
            var m = Create(3, pos);
            m.cd = MasterInterval;
            Tween.Pop(m.rt, 0.4f);
            FX.Burst(B.FxLayer, pos, new Color(0.8f, 0.5f, 1f), 14);
            Select(m);
            GM.Toast("Usta Kedi! Artık hep burada, kendi kendine malzeme getirir.", UIKit.Orange, 4.5f);
            GM.GiveSticker("c_usta", false);
            GM.OnBoardChanged();
        }

        public void Adopt(KittenAgent a)
        {
            if (!a || !a.IsWild || a.IsGone) return;
            if (a.state == KittenAgent.State.Napping) WakeItem(a);
            Deselect();
            a.Leave();
            GM.AddGold(3);
            GM.Toast("Yavru yeni ailesine kavuştu! +3 altın");
            GM.ShowInfo(null);
            GM.OnBoardChanged();
        }

        // ------------------------------------------------------------ save
        public void WriteSave(List<KittenSave> list)
        {
            list.Clear();
            foreach (var a in agents)
            {
                if (!a || a.IsGone) continue;
                list.Add(new KittenSave
                {
                    level = a.level, x = a.Pos.x, y = a.Pos.y,
                    napping = a.state == KittenAgent.State.Napping,
                    napX = a.napCell.x, napY = a.napCell.y,
                    expires = a.expires, cd = a.cd
                });
            }
        }

        public void ReadSave(List<KittenSave> list)
        {
            ClearAll();
            if (list != null)
            {
                foreach (var s in list)
                {
                    if (s.level < 1 || s.level > 3) continue;
                    var a = Create(s.level, Clamp(new Vector2(s.x, s.y)));
                    a.expires = s.expires;
                    a.cd = s.cd;
                    if (!s.napping) continue;
                    var c = new Vector2Int(s.napX, s.napY);
                    var d = B.Get(c);
                    if (d == null || NapperAt(c) != null) continue;
                    d.nap = true;
                    a.Nap(c, BoardController.CellPos(c) + new Vector2(0f, 12f));
                }
            }
            // no sleeping kitten -> item is free again
            foreach (var c in B.AllCells())
            {
                var d = B.Get(c);
                if (d != null && d.nap && NapperAt(c) == null) { d.nap = false; B.RefreshView(c); }
            }
        }

        public void ClearAll()
        {
            foreach (var a in agents) if (a) Destroy(a.gameObject);
            agents.Clear();
            Selected = null;
            spawnTimer = 6f;
        }
    }
}
