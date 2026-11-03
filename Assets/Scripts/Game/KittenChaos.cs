using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// The cat system. Wild kittens hop onto the table, push items around and fall asleep on them.
    /// Drag one kitten onto another and they become an Apprentice (2 real hours). Kittens keep coming,
    /// but the Apprentice chases them off; dropped next to the alchemy pot it stirs and potions brew twice as fast.
    /// The Apprentice can be promoted to a permanent Master, who hisses kittens away and can go on expeditions.
    /// </summary>
    public class KittenChaos : MonoBehaviour
    {
        public const float ApprenticeCooldown = 12f;
        public const float MasterInterval = 20f;
        public const double ApprenticeHours = 2.0;
        public const int PromoteCost = 150;
        const int MaxWild = 3;
        const float SpawnMin = 25f, SpawnMax = 40f;   // seconds between kittens jumping onto the table
        public const int HouseSlots = 2;              // kittens that fit in the cat house
        const float GraceSeconds = 10f;               // a kitten that just arrived is left alone for a bit

        readonly List<KittenAgent> agents = new List<KittenAgent>();
        RectTransform layer;
        float spawnTimer = 10f;

        // chasing (apprentice) and hissing (master)
        KittenAgent chaseTarget;
        float chaseCd = 2f, hissCd = 4f;
        bool chaseHintShown, hissHintShown, houseHintShown;
        float houseCheck;

        // the master's expedition
        public int Trip { get; private set; } = -1;     // -1 = at home
        public long TripUntil { get; private set; }
        public bool UstaAway => Trip >= 0;
        public int TripColor { get; private set; } = KittenColors.Gece;

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
                if (a.level == 2 && a.Stirring) StirTick(a, dt);
                if (a.level == 3 && a.cd <= 0f && a.CanAct)
                    a.cd = ProduceNear(a, 2) ? MasterInterval : 3f;
            }

            ChaseTick(dt);
            HissTick(dt);
            TripTick(now);
            houseCheck -= dt;
            if (houseCheck <= 0f) { houseCheck = 3f; EnsureHouse(); }

            // kittens keep coming even with an apprentice around - it's the one who shoos them
            if (WildCount < MaxWild && !B.AnyHeld)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    SpawnWild();
                    spawnTimer = Random.Range(SpawnMin, SpawnMax) * (HasMaster ? 1.6f : 1f);
                }
            }
        }

        KittenAgent Create(int level, Vector2 pos, int color)
        {
            var rt = UIKit.Rect("Kitten", layer);
            var a = rt.gameObject.AddComponent<KittenAgent>();
            a.Init(this, level, pos, color);
            agents.Add(a);
            return a;
        }

        void SpawnWild()
        {
            var h = Half;
            float side = Random.value < 0.5f ? -1f : 1f;
            float y = Random.Range(-h.y * 0.8f, h.y * 0.8f);
            var a = Create(1, new Vector2(side * (h.x + 200f), y - 80f), KittenColors.Pick());
            a.JumpIn(a.Pos, new Vector2(side * (h.x - 30f), y));

            string msg = arriveLines[Random.Range(0, arriveLines.Length)];
            if (!GM.KittenHintShown)
            {
                msg += " İki yavruyu üst üste sürükle: Çırak Kedi olurlar!";
                GM.KittenHintShown = true;
            }
            if (!houseHintShown && (HasMaster || HasApprentice) && FindHouse(out _))
            {
                houseHintShown = true;
                msg += " Kovalanmasın diye onu Kedi Evi'ne sürükle!";
            }
            GM.Toast(msg, UIKit.Orange, 4.5f);
            Sfx.Play("paw", 1.15f, 0.8f);
            GM.GiveSticker("c_yavru", false);
            GM.MarkDirty();
        }

        // ------------------------------------------------------------ mischief
        public void PlanMischief(KittenAgent a)
        {
            if (B.AnyHeld) return;
            var cands = B.Cells(d => !d.Locked && !Chains.IsStation(d.chain) && !Chains.IsHouse(d.chain)); // never nap on the cauldron or the cat house
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
            Sfx.Play("paw", 1.3f, 0.6f);
            GM.OnBoardChanged();
        }

        void DoNap(KittenAgent a, Vector2Int c)
        {
            if (!a || B.AnyHeld) return;
            var d = B.Get(c);
            if (d == null || d.Locked || Chains.IsStation(d.chain) || Chains.IsHouse(d.chain) || NapperAt(c) != null) return;
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
            Sfx.Play("paw", 1.1f, 0.7f);
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
            Sfx.Play("paw", a.level == 1 ? 1.2f : a.level == 2 ? 1f : 0.85f);
            switch (a.level)
            {
                case 1:
                    GM.Toast("Miyav! (İki yavruyu üst üste sürükle.)");
                    break;
                case 2:
                    if (a.cd > 0f) GM.Toast("Çırak dinleniyor... " + Mathf.CeilToInt(a.cd) + " sn");
                    else if (ProduceNear(a, a.color == KittenColors.Turuncu && Random.value < 0.3f ? 2 : 1))
                    {
                        a.cd = a.color == KittenColors.Beyaz ? 8f : ApprenticeCooldown;
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
            if (a.level == 2) { TryStir(a); return; }
            if (!a.IsWild) return;
            if (TryEnterHouse(a)) return;
            KittenAgent other = null;
            float best = 100f * 100f;
            foreach (var o in agents)
            {
                if (!o || o == a || !o.IsWild || o.IsGone || o.state == KittenAgent.State.Entering) continue;
                float d = (o.Pos - a.Pos).sqrMagnitude;
                if (d < best) { best = d; other = o; }
            }
            if (!other) return;
            if (HasApprentice) { GM.Toast("Dükkânda zaten bir Çırak var. Yavruları o kovalıyor!"); return; }
            MergeKittens(a, other);
        }

        void MergeKittens(KittenAgent a, KittenAgent b)
        {
            var pos = Clamp(b.Pos);
            int col = Random.value < 0.5f ? a.color : b.color;   // takes after one of the two
            Release(a);
            Release(b);
            var c = Create(2, pos, col);
            c.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(ApprenticeHoursFor(col)).Ticks;
            Tween.Pop(c.rt, 0.35f);
            FX.Burst(B.FxLayer, pos, UIKit.Orange, 10);

            foreach (var o in agents.ToArray())
            {
                if (!o || !o.IsWild || o.IsGone) continue;
                if (o.state == KittenAgent.State.Napping) WakeItem(o);
                o.Leave();
            }

            Select(c);
            ApprenticeBorn(c);
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
            spawnTimer = 15f;
            GM.MarkDirty();
        }

        public void Promote(KittenAgent a)
        {
            if (!a || a.level != 2 || a.IsGone) return;
            if (HasMaster) { GM.Toast("Dükkânda zaten bir Usta Kedi var."); return; }
            if (UstaAway) { GM.Toast("Usta Kedi keşifte. Dönünce yine burada olacak."); return; }
            if (!GM.TrySpendGold(PromoteCost)) return;
            var pos = a.Pos;
            int col = a.color;
            Release(a);
            var m = Create(3, pos, col);
            m.cd = MasterInterval;
            Tween.Pop(m.rt, 0.4f);
            FX.Burst(B.FxLayer, pos, new Color(0.8f, 0.5f, 1f), 14);
            Select(m);
            GM.Toast("Usta Kedi! Artık hep burada: malzeme getirir, yavrulara tıslar, keşfe de çıkabilir.", UIKit.Orange, 5f);
            EnsureHouse();
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
                    expires = a.expires, cd = a.cd, colorPlus = a.color + 1,
                    stir = a.Stirring, stirX = a.stirCell.x, stirY = a.stirCell.y
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
                    var a = Create(s.level, Clamp(new Vector2(s.x, s.y)), s.colorPlus > 0 ? s.colorPlus - 1 : KittenColors.Legacy(s.level));
                    a.expires = s.expires;
                    a.cd = s.cd;
                    if (s.stir && s.level == 2)
                    {
                        var pc = new Vector2Int(s.stirX, s.stirY);
                        var pd = B.Get(pc);
                        if (pd != null && pd.chain == Chains.Simya) a.StartStir(pc, BoardController.CellPos(pc));
                        continue;
                    }
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
            chaseTarget = null;
            spawnTimer = 10f;
            Trip = -1;
            TripUntil = 0;
        }

        // ------------------------------------------------------------ test helpers (editor menu)
        public void DebugSpawn(int level)
        {
            if (level == 1) { SpawnWild(); return; }
            if (level == 2 && HasApprentice) return;
            if (level == 3 && (HasMaster || UstaAway)) return;
            var a = Create(level, RandomPoint(), KittenColors.Pick());
            if (level == 2) a.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(ApprenticeHours).Ticks;
            else a.cd = MasterInterval;
            Tween.Pop(a.rt, 0.35f);
            GM.MarkDirty();
        }

        public void DebugHouseKitten()
        {
            EnsureHouse();
            if (!FindHouse(out var hc)) return;
            var h = B.Get(hc);
            if (h.cats < HouseSlots) { h.catCols = (h.catCols ?? "") + KittenColors.Pick(); h.cats = h.catCols.Length; B.RefreshView(hc); GM.MarkDirty(); }
        }

        public void DebugFinishTrip() { if (Trip >= 0) TripUntil = DateTime.UtcNow.Ticks; }

        /// <summary>Test: put the apprentice at the pot; a finished potion gets 60 s back on its timer so the speed-up shows.</summary>
        public void DebugStir()
        {
            var ap = Apprentice();
            if (!ap || !B.FindFirst(d => d.chain == Chains.Simya, out var pot)) return;
            var pd = B.Get(pot);
            if (pd.brew > 0 && pd.BrewReady) { pd.brewUntil = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(60).Ticks; pd.readyNotified = false; B.RefreshView(pot); }
            ap.Pos = BoardController.CellPos(pot) + new Vector2(-100f, -20f);
            TryStir(ap);
        }

        // ------------------------------------------------------------ cat house
        // Kittens dropped on the house wait inside, safe from the Apprentice and the Master.
        // Two of them inside -> tap the house to make a new Apprentice.
        public bool FindHouse(out Vector2Int cell) => B.FindFirst(d => Chains.IsHouse(d.chain), out cell);

        /// <summary>Once there is a Master (home or away) the table gets a cat house, if there is room.</summary>
        void EnsureHouse()
        {
            if (!(HasMaster || UstaAway) || FindHouse(out _)) return;
            var centre = new Vector2Int(BoardController.Cols / 2, BoardController.Rows - 2);
            if (!B.NearestEmpty(centre, out var e))
            {
                if (!houseHintShown) { houseHintShown = true; GM.Toast("Kedi Evi geldi ama masada yer yok. Bir kare boşalt!"); }
                return;
            }
            B.Set(e, new ItemData(Chains.KediEvi, 1), true);
            var v = B.ViewAt(e);
            if (v) Tween.Pop(v.rt, 0.4f);
            FX.Burst(B.FxLayer, BoardController.CellPos(e), new Color(1f, 0.8f, 0.55f), 10);
            GM.Toast("Kedi Evi masaya geldi! Yavruları içine sürükle: Usta onlara tıslamaz. İki yavru olunca dokun, Çırak çıksın.", UIKit.Orange, 6f);
            GM.OnBoardChanged();
        }

        bool TryEnterHouse(KittenAgent a)
        {
            if (!FindHouse(out var hc)) return false;
            if ((BoardController.CellPos(hc) - a.Pos).sqrMagnitude > 80f * 80f) return false;   // not dropped on the house
            var h = B.Get(hc);
            if (h.cats >= HouseSlots) { GM.Toast("Kedi Evi dolu! Dokun, içindekiler Çırak olsun."); return true; }
            if ((h.catCols ?? "").Length < h.cats) h.catCols = (h.catCols ?? "").PadRight(h.cats, '0');
            h.catCols = (h.catCols ?? "") + a.color;
            h.cats = h.catCols.Length;
            Release(a);
            B.RefreshView(hc);
            var v = B.ViewAt(hc);
            if (v) Tween.Punch(v.rt, 0.15f, 0.3f);
            Sfx.Play("paw", 1.25f, 0.7f);
            GM.Toast(h.cats >= HouseSlots
                ? "İki yavru da evde! Kedi Evi'ne dokun: Çırak Kedi çıksın."
                : "Yavru Kedi Evi'ne girdi. Bir yavru daha getir!", UIKit.Orange, 4f);
            GM.OnBoardChanged();
            return true;
        }

        public void TapHouse(Vector2Int hc)
        {
            var h = B.Get(hc);
            if (h == null || !Chains.IsHouse(h.chain)) return;
            if (h.cats < HouseSlots) { GM.Toast("Evde " + h.cats + "/" + HouseSlots + " yavru var. İki yavru olunca dokun, Çırak çıksın."); return; }
            if (HasApprentice) { GM.Toast("Masada zaten bir Çırak var. O gidince buradan yenisini çıkarırsın."); return; }
            string cols = h.catCols ?? "";
            int col = cols.Length > 0 ? cols[Random.Range(0, cols.Length)] - '0' : KittenColors.Pick();
            h.cats = 0;
            h.catCols = "";
            B.RefreshView(hc);
            var pos = Clamp(BoardController.CellPos(hc) + new Vector2(0f, 20f));
            var c = Create(2, pos, Mathf.Clamp(col, 0, KittenColors.Count - 1));
            c.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(ApprenticeHoursFor(c.color)).Ticks;
            c.JumpIn(pos, Clamp(pos + new Vector2(Random.value < 0.5f ? -120f : 120f, -40f)));
            FX.Burst(B.FxLayer, pos, UIKit.Orange, 10);
            Select(c);
            ApprenticeBorn(c);
        }

        // ------------------------------------------------------------ colours
        static double ApprenticeHoursFor(int color) => color == KittenColors.UcRenkli ? 3.0 : ApprenticeHours;

        /// <summary>A new apprentice: say which colour it is and what it is good at, add its album sticker.</summary>
        void ApprenticeBorn(KittenAgent c)
        {
            int col = Mathf.Clamp(c.color, 0, KittenColors.Count - 1);
            GM.Toast(KittenColors.Names[col] + " Çırak doğdu! " + KittenColors.Talents[col], UIKit.Orange, 6f);
            Sfx.Play("levelup", 1.2f);
            GM.GiveSticker("c_cirak", false);
            GM.GiveSticker("r" + col);   // "Renkli Çıraklar" album page
            GM.OnBoardChanged();
        }

        // ------------------------------------------------------------ apprentice: chase kittens
        KittenAgent Apprentice()
        {
            foreach (var a in agents) if (a && a.level == 2 && !a.IsGone) return a;
            return null;
        }

        KittenAgent Master()
        {
            foreach (var a in agents) if (a && a.level == 3 && !a.IsGone) return a;
            return null;
        }

        static bool Shooable(KittenAgent k) =>
            k && k.IsWild && !k.IsGone && !k.IsHeld && k.state != KittenAgent.State.Entering && k.age >= GraceSeconds;

        KittenAgent NearestWild(Vector2 from, float maxDist)
        {
            KittenAgent best = null;
            float bd = maxDist * maxDist;
            foreach (var k in agents)
            {
                if (!Shooable(k)) continue;
                float d = (k.Pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = k; }
            }
            return best;
        }

        void ChaseTick(float dt)
        {
            var ap = Apprentice();
            if (!ap || ap.Stirring || ap.IsHeld) { chaseTarget = null; return; }

            if (chaseTarget == null)
            {
                chaseCd -= dt;
                if (chaseCd > 0f || !ap.CanAct) return;
                chaseTarget = NearestWild(ap.Pos, 99999f);
                if (chaseTarget == null) { chaseCd = 1f; return; }
                // the kitten notices and scampers away (unless it's asleep)
                if (chaseTarget.CanAct)
                    chaseTarget.WalkTo(chaseTarget.Pos + (chaseTarget.Pos - ap.Pos).normalized * 260f);
                Say(ap, "Kışt!", UIKit.Orange);
                Sfx.Play("paw", 0.9f, 0.6f);
            }

            if (!Shooable(chaseTarget)) { chaseTarget = null; ap.Idle(1f); return; }
            if (ap.state == KittenAgent.State.Idle && !ap.chasing && ap.IsHeld) return;
            ap.ChaseTo(chaseTarget.Pos);
            if ((chaseTarget.Pos - ap.Pos).sqrMagnitude < 75f * 75f)
            {
                Scare(chaseTarget);
                chaseTarget = null;
                chaseCd = ap.color == KittenColors.SiyahBeyaz ? Random.Range(2f, 3f) : Random.Range(4f, 7f);
                ap.Idle(Random.Range(1.5f, 2.5f));
                Tween.Punch(ap.rt, 0.15f, 0.25f);
                if (!chaseHintShown)
                {
                    chaseHintShown = true;
                    GM.Toast("Çırak Kedi yavruyu kovaladı! Kazanın yanına sürüklersen iksir karıştırır.", UIKit.Orange, 5f);
                }
            }
        }

        /// <summary>A kitten gets a fright and runs off the table.</summary>
        void Scare(KittenAgent k)
        {
            if (k.state == KittenAgent.State.Napping) WakeItem(k);
            if (Selected == k) { Selected = null; GM.ShowInfo(null); }
            Say(k, "Miy!", new Color(1f, 0.75f, 0.85f));
            FX.Burst(B.FxLayer, k.Pos + new Vector2(0f, 20f), new Color(1f, 0.85f, 0.6f), 5);
            Sfx.Play("scurry");
            k.runBoost = 2f;
            k.Leave();
            GM.OnBoardChanged();
        }

        // ------------------------------------------------------------ master: hiss
        void HissTick(float dt)
        {
            var m = Master();
            if (!m || !m.CanAct) return;
            hissCd -= dt;
            if (hissCd > 0f) return;
            var k = NearestWild(m.Pos, 560f);
            if (k == null || k == chaseTarget) { hissCd = 1.5f; return; }
            hissCd = Random.Range(7f, 11f);
            m.Idle(2.5f);
            m.Face(k.Pos.x >= m.Pos.x ? 1 : -1);
            // first a warning: the Master turns, a "!" pops over the kitten - then the hiss
            Say(k, "!", new Color(1f, 0.85f, 0.4f));
            Tween.Run(0.9f, t => { }, () =>
            {
                if (!m || !Shooable(k)) return;   // the player rescued it in time
                Tween.Punch(m.rt, 0.2f, 0.3f);
                Say(m, "Tısss!", new Color(0.85f, 0.7f, 1f));
                Sfx.Play("hiss");
                Scare(k);
                if (!hissHintShown)
                {
                    hissHintShown = true;
                    GM.Toast("Usta Kedi tısladı, yavru masadan atladı! Yavruları Kedi Evi'ne sürüklersen kovalanmazlar.", UIKit.Orange, 5f);
                }
            });
        }

        /// <summary>A little word that floats up from a cat and fades.</summary>
        void Say(KittenAgent a, string word, Color c)
        {
            var t = UIKit.Txt("Say", layer, word, 34, c);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.05f, 0.15f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            var rt = t.rectTransform;
            var start = a.Pos + new Vector2(0f, 80f);
            rt.At(start, new Vector2(200f, 50f));
            Tween.Run(0.9f, k =>
            {
                if (!rt) return;
                rt.anchoredPosition = start + new Vector2(0f, 50f * Tween.OutCubic(k));
                var cc = c; cc.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                t.color = cc;
            }, () => { if (rt) Destroy(rt.gameObject); });
        }

        // ------------------------------------------------------------ apprentice: stir the pot
        public bool PotBrewing(Vector2Int cell)
        {
            var d = B.Get(cell);
            return d != null && d.Brewing;
        }

        void TryStir(KittenAgent a)
        {
            Vector2Int best = new Vector2Int(-1, -1);
            float bd = 170f * 170f;
            foreach (var c in B.AllCells())
            {
                var d = B.Get(c);
                if (d == null || d.chain != Chains.Simya) continue;
                float dist = (BoardController.CellPos(c) - a.Pos).sqrMagnitude;
                if (dist < bd) { bd = dist; best = c; }
            }
            if (best.x < 0) return;
            chaseTarget = null;
            a.StartStir(best, BoardController.CellPos(best));
            Tween.Pop(a.rt, 0.3f);
            Sfx.Play("brew", 1.3f, 0.6f);
            var pot = B.Get(best);
            GM.Toast(pot != null && pot.Brewing
                ? "Çırak kazanı karıştırıyor: iksir 2 kat hızlı pişiyor!"
                : "Çırak kazanın başında bekliyor. İksir koyunca 2 kat hızlı karıştıracak.", UIKit.Orange, 4.5f);
            GM.ShowKittenInfo(a);
            GM.MarkDirty();
        }

        void StirTick(KittenAgent a, float dt)
        {
            var d = B.Get(a.stirCell);
            if (d == null || d.chain != Chains.Simya) { a.StopStir(); return; }   // the pot was moved away
            // every real second counts double: take one extra second off the timer
            // a grey apprentice is the best stirrer: 2.5x instead of 2x
            float extra = a.color == KittenColors.Gri ? 1.5f : 1f;
            if (d.Brewing) d.brewUntil -= TimeSpan.FromSeconds(dt * extra).Ticks;
        }

        // ------------------------------------------------------------ master: expeditions
        public void SetTrip(int trip, long until, int color)
        {
            Trip = trip;
            TripUntil = until;
            TripColor = color;
        }

        public void SendOnTrip(int k)
        {
            var m = Master();
            if (!m) { GM.Toast("Masada Usta Kedi yok."); return; }
            if (m.state == KittenAgent.State.Napping || m.IsHeld) return;
            if (Selected == m) { Selected = null; GM.ShowInfo(null); }
            Trip = k;
            TripColor = m.color;
            TripUntil = DateTime.UtcNow.Ticks + TimeSpan.FromMinutes(Expeditions.Minutes[k]).Ticks;
            Say(m, "Görüşürüz!", UIKit.Cream);
            m.Leave();
            Sfx.Play("whoosh");
            GM.Toast("Usta Kedi " + Expeditions.Names[k] + " için yola çıktı. " + Expeditions.Duration(k) + " sonra dönecek.", UIKit.Orange, 4.5f);
            GM.ShowInfo(null);
            GM.MarkDirty();
        }

        void TripTick(long now)
        {
            if (Trip >= 0 && now >= TripUntil && !B.AnyHeld && !GM.ModalOpen) ReturnFromTrip();
        }

        /// <summary>"Dolunay Seferi · 7sa 12dk kaldı" for the info bar while the Master is away.</summary>
        public string TripStatus()
        {
            if (Trip < 0) return "";
            var left = TimeSpan.FromTicks(Math.Max(0L, TripUntil - DateTime.UtcNow.Ticks));
            string time = left.TotalHours >= 1.0 ? (int)left.TotalHours + " sa " + left.Minutes + " dk" : left.Minutes + " dk " + left.Seconds + " sn";
            return Expeditions.Names[Mathf.Clamp(Trip, 0, Expeditions.Names.Length - 1)] + ": " + time + " sonra döner, getirdiklerini masaya bırakır.";
        }

        void ReturnFromTrip()
        {
            int k = Mathf.Clamp(Trip, 0, Expeditions.Names.Length - 1);
            Trip = -1;

            var h = Half;
            float side = Random.value < 0.5f ? -1f : 1f;
            var land = new Vector2(side * (h.x - 60f), Random.Range(-h.y * 0.5f, h.y * 0.5f));
            var m = Create(3, new Vector2(side * (h.x + 200f), land.y - 80f), TripColor);
            m.JumpIn(m.Pos, land);
            m.cd = MasterInterval;

            // what it found
            int placed = 0, stored = 0, sold = 0;
            var cell = BoardController.LocalToCellClamped(land);
            for (int i = 0; i < Expeditions.Items[k]; i++)
            {
                var item = new ItemData(Chains.RandomIngredient(), Random.Range(Expeditions.MinLevel[k], Expeditions.MaxLevel[k] + 1));
                if (B.NearestEmpty(cell, out var e)) { B.SpawnFromPos(land, e, item); GM.Discover(item.chain, item.level); placed++; }
                else if (GM.Storage.Count < GM.StorageSlots) { GM.Storage.Add(item); stored++; }
                else { GM.AddGold(Chains.Value(item.level)); sold++; }
            }
            var found = new List<string> { Expeditions.Items[k] + " malzeme" };
            if (Expeditions.Gold[k] > 0) { GM.AddGold(Expeditions.Gold[k]); found.Add(Expeditions.Gold[k] + " altın"); }
            if (Expeditions.Stamps[k] > 0) { GM.AddStamps(Expeditions.Stamps[k]); found.Add(Expeditions.Stamps[k] + " Pati Mührü"); }
            if (Expeditions.Ornament[k])
            {
                string id = Decor.ItemId(Random.Range(0, Decor.Slots), Random.Range(1, Decor.Variants));
                GM.GiftTowerDecor(id);
                found.Add("kule için " + Decor.ItemName(id));
            }
            if (Random.value < Expeditions.StickerChance[k])
            {
                var cats = Stickers.Sets[1].ids;
                GM.GiveSticker(cats[Random.Range(0, cats.Length)]);
            }

            string where = stored > 0 || sold > 0
                ? " Masa doluydu: " + (stored > 0 ? stored + " tanesi depoya" : "") + (stored > 0 && sold > 0 ? ", " : "") + (sold > 0 ? sold + " tanesi altına" : "") + " gitti."
                : "";
            Sfx.Play("gift");
            GM.ShowDialog("Usta Kedi", Expeditions.Names[k] + " bitti, miyav! Getirdiklerim: " + string.Join(", ", found) + "." + where);
            GM.RefreshHud();
            if (Selected == null) GM.ShowInfo(null);
            GM.OnBoardChanged();
        }
    }
}
