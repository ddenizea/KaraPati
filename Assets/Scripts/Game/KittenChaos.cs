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
        public const float MasterInterval = 45f;
        public const double ApprenticeHours = 2.0;
        public const int PromoteCost = 150;
        const int MaxWild = 3;
        const float SpawnMin = 75f, SpawnMax = 105f;  // seconds between kittens jumping onto the table
        const float DoubleJump = 0.08f;               // sometimes two come at once
        public const int HouseSlots = 2;              // kittens that fit in the cat house
        public const double MiniHours = 1.0;          // a Mini Apprentice helps for one hour
        public const float TrainMinutes = 3f;         // the Master trains a kitten for 3 minutes
        const float GraceSeconds = 10f;               // a kitten that just arrived is left alone for a bit

        readonly List<KittenAgent> agents = new List<KittenAgent>();
        RectTransform layer;
        float spawnTimer = 30f;

        // chasing (apprentice) and hissing (master)
        KittenAgent chaseTarget;
        float chaseCd = 2f, hissCd = 4f;
        bool chaseHintShown, hissHintShown, houseHintShown;
        float houseCheck;

        // the master's expedition
        public int Trip { get; private set; } = -1;     // -1 = at home
        public long TripUntil { get; private set; }
        public bool UstaAway => Trip >= 0;
        /// <summary>The trip is over: the Master waits in the cat house with a chest until you tap it.</summary>
        public bool TripReady => Trip >= 0 && DateTime.UtcNow.Ticks >= TripUntil;
        bool readyAnnounced;
        public int TripColor { get; private set; } = KittenColors.Gece;

        public KittenAgent Selected { get; private set; }

        GameManager GM => GameManager.I;
        BoardController B => GM.Board;
        public bool Active => GM != null && GM.OnBoard && !GM.ModalOpen;

        static readonly TrList arriveLines = new TrList
        (
            L.N("Miyav! Bir yavru pencereden masaya zıpladı."),
            L.N("Kapı gıcırdadı... bir yavru masaya hopladı!"),
            L.N("Bacadan minik bir yavru düştü, pıt diye masaya!")
        );

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
                if (a.Training && !a.IsGone && now >= a.trainUntil) { FinishTraining(a); continue; }
                if (a.level < 2 || a.IsGone) continue;
                if (a.level == 4 && now >= a.expires && miniJob == null) { MiniFarewell(a); continue; }
                if (a.cd > 0f) a.cd = Mathf.Max(0f, a.cd - dt);
                if (a.level == 2 && now >= a.expires) { Farewell(a); continue; }
                if (a.level == 2 && a.Stirring) StirTick(a, dt);
                if (a.level == 3 && a.cd <= 0f && a.CanAct && a.homed && a.AtHome)
                    a.cd = ProduceForMaster(a) ? MasterInterval : 3f;
            }

            ChaseTick(dt);
            HissTick(dt);
            MiniTick();
            TrainYarnTick(dt);
            ToyTick(dt);
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
                    if (Random.value < DoubleJump && WildCount < MaxWild) SpawnWild();   // a pair!
                    spawnTimer = Random.Range(SpawnMin, SpawnMax);
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
                msg += " " + L.T("Onu tezgahtaki Kedi Evi'ne sürükle: iki yavru toplanınca Çırak Kedi olurlar!");
                GM.KittenHintShown = true;
            }
            if (!houseHintShown && (HasMaster || HasApprentice) && FindHouse(out _))
            {
                houseHintShown = true;
                msg += " " + L.T("Kovalanmasın diye onu Kedi Evi'ne sürükle!");
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
                if (BoardController.Playable(n) && B.Get(n) == null) opts.Add(d);
            }
            dir = opts.Count > 0 ? opts[Random.Range(0, opts.Count)] : Vector2Int.zero;
            return opts.Count > 0;
        }

        void DoPush(KittenAgent a, Vector2Int c, Vector2Int dir)
        {
            if (!a || B.AnyHeld) return;
            var d = B.Get(c);
            var n = c + dir;
            if (d == null || d.Locked || !Chains.IsIngredient(d.chain) || !BoardController.Playable(n) || B.Get(n) != null) return;
            a.Face(dir.x != 0 ? dir.x : (BoardController.CellPos(c).x >= a.Pos.x ? 1 : -1));
            Tween.Punch(a.rt, 0.15f, 0.25f);
            bool sel = B.Selected == c;
            B.MoveItem(c, n, 0.35f, true);
            if (sel) B.Select(n);
            GM.Toast(L.F("Hop! Yavru patisiyle itti: {0}", d.Name), UIKit.Orange);
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
            GM.Toast(L.F("Yavru, {0} üstüne kıvrılıp uyudu. Dokunup okşa!", d.Name), UIKit.Orange);
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
            GM.Toast(L.T("Mırrr... Yavru uyandı ve gerindi."));
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
                    GM.Toast(a.Training ? L.T("Usta'dan ders alıyor, rahatsız etmeyelim!") : L.T("Miyav! (Beni tezgahtaki Kedi Evi'ne sürükle.)"));
                    break;
                case 4:
                    GM.Toast(MiniBusy ? L.T("Minik Çırak malzeme taşıyor!") : L.T("Tarif Defteri'nde bir iksire 'Yaptır' de, malzemeleri kazana taşısın."));
                    break;
                case 2:
                    if (a.cd > 0f) GM.Toast(L.F("Çırak dinleniyor... {0} sn", Mathf.CeilToInt(a.cd)));
                    else if (ProduceNear(a, a.color == KittenColors.Turuncu && Random.value < 0.3f ? 2 : 1))
                    {
                        a.cd = a.color == KittenColors.Beyaz ? 8f : ApprenticeCooldown;
                        GM.Toast(L.T("Çırak kedi bir malzeme getirdi!"));
                    }
                    else GM.Toast(L.T("Masa dolu! Çırak malzemeyi koyacak yer bulamadı."));
                    break;
                default:
                    GM.Toast(a.homed ? L.F("Usta Kedi yastığından her {0} saniyede bir malzeme getirir.", (int)MasterInterval)
                                     : L.T("Usta'yı tezgahın sağındaki mor yastığa sürükle, oradan çalışsın."));
                    break;
            }
        }

        public void OnDropped(KittenAgent a)
        {
            if (a.level == 3) { DropMaster(a); return; }
            if (a.level == 4) { DropMini(a); return; }
            if (a.level == 2) { TryStir(a); return; }
            if (!a.IsWild || a.Training) return;
            if (TryStartTraining(a)) return;
            if (TryEnterHouse(a)) return;
            KittenAgent other = null;
            float best = 100f * 100f;
            foreach (var o in agents)
            {
                if (!o || o == a || !o.IsWild || o.Training || o.IsGone || o.state == KittenAgent.State.Entering) continue;
                float d = (o.Pos - a.Pos).sqrMagnitude;
                if (d < best) { best = d; other = o; }
            }
            if (!other) return;
            if (HasApprentice) { GM.Toast(L.T("Dükkânda zaten bir Çırak var. Yavruları o kovalıyor!")); return; }
            MergeKittens(a, other);
        }

        void DropMini(KittenAgent m)
        {
            if ((m.Pos - ToySeat).sqrMagnitude > 110f * 110f) { m.parked = false; return; }   // put down elsewhere: it roams again
            if (m.parked) { ParkMini(m); return; }
            ParkMini(m);
            Tween.Pop(m.rt, 0.3f);
            Sfx.Play("pop", 1.3f);
            Say(m, "Topum!", UIKit.Orange);
            GM.Toast(L.T("Minik Çırak topuyla oynuyor, masada gezmez. 'Yaptır' dersen yine koşar."), UIKit.Orange, 4.5f);
            GM.MarkDirty();
        }

        void DropMaster(KittenAgent m)
        {
            if (m.homed) return;   // it walks back to its cushion by itself
            if ((m.Pos - CushionPos).sqrMagnitude > 120f * 120f) return;
            SeatMaster(m);
            Tween.Pop(m.rt, 0.35f);
            FX.Burst(B.FxLayer, m.Pos, new Color(0.8f, 0.5f, 1f), 12);
            Sfx.Play("gift", 1.2f);
            Say(m, L.T("Burası benim!"), UIKit.Orange);
            GM.Toast(L.F("{0} yastığına kuruldu! Buradan malzeme getirir, yavrulara tıslar, yavru eğitir.", GM.UstaDisplayName(m.color)), UIKit.Orange, 5f);
            GM.ShowKittenInfo(m);
            GM.MarkDirty();
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

        /// <summary>The Master on its cushion sends an ingredient flying to a random free cell of the table.</summary>
        bool ProduceForMaster(KittenAgent a)
        {
            var empty = B.EmptyCells();
            if (empty.Count == 0) return false;
            var e = empty[Random.Range(0, empty.Count)];
            B.SpawnFromPos(a.Pos, e, new ItemData(Chains.RandomIngredient(), 2));
            Tween.Punch(a.rt, 0.15f, 0.25f);
            GM.OnBoardChanged();
            return true;
        }

        /// <summary>Where the Mini Apprentice sits to play with its ball (just right of it).</summary>
        public Vector2 ToySeat => BoardController.ToyPos + new Vector2(52f, 26f);

        void ParkMini(KittenAgent m)
        {
            m.parked = true;
            m.Pos = ToySeat;
            m.Face(-1);
            m.Idle(2f);
        }

        float toyT;

        /// <summary>A parked Mini Apprentice bats its ball between its paws.</summary>
        void ToyTick(float dt)
        {
            var ball = B.ToyBall;
            if (!ball) return;
            var mini = Mini();
            bool playing = mini && mini.parked && mini.state == KittenAgent.State.Idle && (mini.Pos - ToySeat).sqrMagnitude < 30f * 30f;
            var brt = ball.rectTransform;
            if (!playing) { brt.anchoredPosition = BoardController.ToyPos; brt.localEulerAngles = Vector3.zero; return; }
            toyT += dt;
            // a little hop and roll, then a tap from the paw
            float cyc = Mathf.Repeat(toyT, 1.6f);
            float hop = cyc < 0.6f ? Mathf.Sin(cyc / 0.6f * Mathf.PI) * 26f : 0f;
            brt.anchoredPosition = BoardController.ToyPos + new Vector2(Mathf.Sin(toyT * 2.2f) * 10f, hop);
            brt.localEulerAngles = new Vector3(0f, 0f, -toyT * 160f);
            if (cyc < dt) Tween.Punch(mini.rt, 0.08f, 0.2f);
        }

        /// <summary>Where the Master sits: its cushion on the right end of the counter.</summary>
        public Vector2 CushionPos => BoardController.CellPos(BoardController.CushionCell) + new Vector2(0f, 4f);

        void SeatMaster(KittenAgent m)
        {
            m.homed = true;
            m.Pos = CushionPos;
            m.Face(-1);
            m.Idle(2f);
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
                ? L.T("Çırak kedi vedalaştı ve sana bir hediye bıraktı. Yavrular yeniden gelebilir!")
                : L.T("Çırak kedi vedalaştı. Yavrular yeniden gelebilir!"), UIKit.Orange, 5f);
            if (Selected == a) { Selected = null; GM.ShowInfo(null); }
            a.Leave();
            spawnTimer = 30f;
            GM.MarkDirty();
        }

        public void Promote(KittenAgent a)
        {
            if (!a || a.level != 2 || a.IsGone) return;
            if (HasMaster) { GM.Toast(L.T("Dükkânda zaten bir Usta Kedi var.")); return; }
            if (UstaAway) { GM.Toast(L.T("Usta Kedi keşifte. Dönünce yine burada olacak.")); return; }
            if (!GM.TrySpendGold(PromoteCost)) return;
            var pos = a.Pos;
            int col = a.color;
            Release(a);
            var m = Create(3, pos, col);
            m.cd = MasterInterval;
            Tween.Pop(m.rt, 0.4f);
            FX.Burst(B.FxLayer, pos, new Color(0.8f, 0.5f, 1f), 14);
            Select(m);
            GM.Toast(L.T("Usta Kedi! Onu tezgahın sağındaki mor yastığa sürükle, oradan çalışsın. Adını ve rengini ana ekrandaki Usta Kedim'den seçebilirsin."), UIKit.Orange, 7f);
            EnsureHouse();
            GM.GiveSticker("c_usta", false);
            GM.OnBoardChanged();
            GM.UstaPromoted();   // "!" on the Usta Kedim button: name it and pick a colour there
            if (GM.FirstTime("usta_cushion"))
                GM.Tut?.CatHint(m, B.CushionRect, L.T("Usta'yı tezgahtaki mor yastığına sürükle: oradan malzeme getirir, yavruları kovar."), () => m && m.homed);
        }

        /// <summary>There is a Master Cat: in the shop or away on a trip.</summary>
        public bool HasAnyMaster => HasMaster || UstaAway;

        /// <summary>The Master's colour, also while it is away.</summary>
        public int MasterColor
        {
            get
            {
                var m = Master();
                return m ? m.color : UstaAway ? TripColor : KittenColors.Gece;
            }
        }

        public void SetMasterColor(int col)
        {
            var m = Master();
            if (m)
            {
                m.SetColor(col);
                Tween.Pop(m.rt, 0.35f);
                if (Selected == m) GM.ShowKittenInfo(m);
            }
            else if (UstaAway) TripColor = col;
            else return;
            Sfx.Play("pop");
            GM.Toast(L.F("{0} yeni rengini çok sevdi!", GM.UstaDisplayName(col)), UIKit.Orange);
            GM.SeenUsta();
            GM.RefreshUstaViews();
            GM.MarkDirty();
        }

        public void Adopt(KittenAgent a)
        {
            if (!a || !a.IsWild || a.IsGone) return;
            if (a.state == KittenAgent.State.Napping) WakeItem(a);
            Deselect();
            a.Leave();
            GM.AddGold(3);
            GM.Toast(L.T("Yavru yeni ailesine kavuştu! +3 altın"));
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
                    trainUntil = a.trainUntil, carryChain = a.carryChain, carryLevel = a.carryLevel,
                    stir = a.Stirring, stirX = a.stirCell.x, stirY = a.stirCell.y,
                    homeState = a.level == 3 ? (a.homed ? 2 : 1) : a.level == 4 && a.parked ? 2 : 0
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
                    if (s.level < 1 || s.level > 4) continue;
                    var a = Create(s.level, Clamp(new Vector2(s.x, s.y)), s.colorPlus > 0 ? s.colorPlus - 1 : KittenColors.Legacy(s.level));
                    a.expires = s.expires;
                    a.cd = s.cd;
                    // the Master sits on its cushion (saves from before the cushion: it moves there by itself)
                    if (s.level == 3 && s.homeState != 1) SeatMaster(a);
                    if (s.level == 4 && s.homeState == 2) ParkMini(a);
                    if (!string.IsNullOrEmpty(s.carryChain))
                    {
                        // it was carrying an ingredient when the game closed: put it back on the table
                        var back = new ItemData(s.carryChain, s.carryLevel);
                        if (B.NearestEmpty(BoardController.LocalToCellClamped(a.Pos), out var be)) B.Set(be, back);
                        else if (GM.Storage.Count < GM.StorageSlots) GM.Storage.Add(back);
                    }
                    if (s.trainUntil > 0 && s.level == 1) { a.trainUntil = s.trainUntil; a.Sit(a.Pos, 1); continue; }
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
            var tr = Trainee();
            var ms = Master();
            if (tr && ms) ms.Sit(ms.Pos, tr.Pos.x > ms.Pos.x ? 1 : -1);
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
            readyAnnounced = false;
            miniJob = null;
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

        public void DebugFinishTraining() { var t = Trainee(); if (t) t.trainUntil = DateTime.UtcNow.Ticks; }

        public void DebugMini()
        {
            if (HasMini) return;
            var mini = Create(4, RandomPoint(), KittenColors.Pick());
            mini.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(MiniHours).Ticks;
            Tween.Pop(mini.rt, 0.35f);
            GM.MarkDirty();
        }

        /// <summary>Test: send the Mini away if there is one, then the Master starts training a new kitten.</summary>
        public void DebugStartTraining()
        {
            var old = Mini();
            if (old) { ReleaseReservations(); miniJob = null; Release(old); }
            var m = Master();
            if (!m || Trainee()) return;
            var k = Create(1, m.Pos + new Vector2(110f, 0f), KittenColors.Pick());
            StartTraining(m, k);
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
            // the cat house is always on the counter, from the very start
            if (FindHouse(out _)) return;
            var e = BoardController.HouseCell;   // its place in the middle of the counter
            B.Evict(e);
            B.Set(e, new ItemData(Chains.KediEvi, 1));
            GM.OnBoardChanged();
        }

        bool TryEnterHouse(KittenAgent a)
        {
            if (!FindHouse(out var hc)) return false;
            if ((BoardController.CellPos(hc) - a.Pos).sqrMagnitude > 80f * 80f) return false;   // not dropped on the house
            var h = B.Get(hc);
            if (h.cats >= HouseSlots) { GM.Toast(L.T("Kedi Evi dolu! Dokun, içindekiler Çırak olsun.")); return true; }
            if ((h.catCols ?? "").Length < h.cats) h.catCols = (h.catCols ?? "").PadRight(h.cats, '0');
            h.catCols = (h.catCols ?? "") + a.color;
            h.cats = h.catCols.Length;
            Release(a);
            B.RefreshView(hc);
            var v = B.ViewAt(hc);
            if (v) Tween.Punch(v.rt, 0.15f, 0.3f);
            Sfx.Play("paw", 1.25f, 0.7f);
            GM.Toast(h.cats >= HouseSlots
                ? L.T("İki yavru da evde! Kedi Evi'ne dokun: Çırak Kedi çıksın.")
                : L.T("Yavru Kedi Evi'ne girdi. Bir yavru daha getir!"), UIKit.Orange, 4f);
            GM.OnBoardChanged();
            return true;
        }

        public void TapHouse(Vector2Int hc)
        {
            var h = B.Get(hc);
            if (h == null || !Chains.IsHouse(h.chain)) return;
            if (TripReady) { ClaimTrip(); return; }   // the Master is home with its chest
            GM.OpenHouse(hc);
        }

        /// <summary>"Sahiplendir" in the cat house window: the kitten at `index` goes to a new family (+3 gold).</summary>
        public void AdoptFromHouse(Vector2Int hc, int index)
        {
            var h = B.Get(hc);
            if (h == null || index >= (h.catCols ?? "").Length) return;
            int col = h.catCols[index] - '0';
            h.catCols = h.catCols.Remove(index, 1);
            h.cats = h.catCols.Length;
            B.RefreshView(hc);
            GM.AddGold(3);
            Sfx.Play("coin");
            GM.Toast(L.F("{0} yavru yeni ailesine kavuştu! +3 altın", KittenColors.Name(col)));
            if (B.Selected == hc) GM.ShowInfo(h);   // keep the "1/2" in the info bar up to date
            GM.OnBoardChanged();
        }

        /// <summary>"Çırak Yap" in the cat house window: the two kittens inside become an Apprentice.</summary>
        public void MakeApprenticeFromHouse(Vector2Int hc)
        {
            var h = B.Get(hc);
            if (h == null || !Chains.IsHouse(h.chain)) return;
            if (h.cats < HouseSlots) { GM.Toast(L.F("Evde {0}/{1} yavru var. İki yavru olunca Çırak çıkar.", h.cats, HouseSlots)); return; }
            if (HasApprentice) { GM.Toast(L.T("Masada zaten bir Çırak var. O gidince buradan yenisini çıkarırsın.")); return; }
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

        // ------------------------------------------------------------ Mini Apprentice: training
        // A kitten dropped on the Master (or sent from the cat house) sits with it for 10 minutes and becomes a
        // Mini Apprentice for an hour. It carries potion ingredients to the alchemy pot when you order a recipe.
        public KittenAgent Trainee()
        {
            foreach (var a in agents) if (a && a.Training && !a.IsGone) return a;
            return null;
        }

        public KittenAgent Mini()
        {
            foreach (var a in agents) if (a && a.level == 4 && !a.IsGone) return a;
            return null;
        }

        public bool HasMini => Mini() != null;

        /// <summary>Why the Master can't train right now, or null if it can.</summary>
        public string TrainBlock()
        {
            if (UstaAway) return L.T("Usta keşifte. Dönünce eğitebilir.");
            if (!Master()) return L.T("Eğitim için masada bir Usta Kedi lazım.");
            if (Trainee()) return L.T("Usta zaten bir yavruyu eğitiyor.");
            if (HasMini) return L.T("Masada zaten bir Minik Çırak var.");
            return null;
        }

        bool TryStartTraining(KittenAgent k)
        {
            var m = Master();
            if (!m || (m.Pos - k.Pos).sqrMagnitude > 95f * 95f) return false;   // not dropped on the Master
            var why = TrainBlock();
            if (why != null) { GM.Toast(why); return true; }
            StartTraining(m, k);
            return true;
        }

        /// <summary>From the cat house window: the kitten at `index` goes to the Master for training.</summary>
        public void TrainFromHouse(Vector2Int hc, int index)
        {
            var why = TrainBlock();
            if (why != null) { GM.Toast(why); return; }
            var h = B.Get(hc);
            if (h == null || index >= (h.catCols ?? "").Length) return;
            int col = h.catCols[index] - '0';
            h.catCols = h.catCols.Remove(index, 1);
            h.cats = h.catCols.Length;
            B.RefreshView(hc);
            if (B.Selected == hc) GM.ShowInfo(h);
            var m = Master();
            var k = Create(1, BoardController.CellPos(hc), Mathf.Clamp(col, 0, KittenColors.Count - 1));
            StartTraining(m, k);
        }

        void StartTraining(KittenAgent m, KittenAgent k)
        {
            if (k.state == KittenAgent.State.Napping) WakeItem(k);
            if (chaseTarget == k) chaseTarget = null;
            if (Selected == m || Selected == k) { Selected = null; GM.ShowInfo(null); }
            int side = m.Pos.x > 0f ? -1 : 1;   // the kitten sits on the roomier side
            m.Sit(m.Pos, side);
            k.trainUntil = DateTime.UtcNow.Ticks + TimeSpan.FromMinutes(TrainMinutes).Ticks;
            k.Sit(m.Pos + new Vector2(side * 165f, -10f), -side);   // room between them for the yarn ball
            Tween.Pop(k.rt, 0.3f);
            Say(m, L.T("Bak ve öğren!"), UIKit.Cream);
            Sfx.Play("paw", 0.9f, 0.7f);
            GM.Toast(L.F("Usta, {0} yavruyu eğitiyor. {1} dakika sonra Minik Çırak olacak!", KittenColors.Name(k.color), (int)TrainMinutes), UIKit.Orange, 5f);
            GM.MarkDirty();
        }

        void FinishTraining(KittenAgent k)
        {
            var pos = k.Pos;
            int col = Mathf.Clamp(k.color, 0, KittenColors.Count - 1);
            var m = Master();
            // graduation: the Master tosses the little red cap onto the kitten's head
            if (m) Graduation(m.Pos + new Vector2(0f, 60f), pos + new Vector2(0f, 46f));
            Release(k);
            var mini = Create(4, pos, col);
            mini.expires = DateTime.UtcNow.Ticks + TimeSpan.FromHours(MiniHours).Ticks;
            Tween.Pop(mini.rt, 0.4f);
            FX.Burst(B.FxLayer, pos, new Color(0.95f, 0.45f, 0.45f), 12);
            Say(mini, L.T("Artık Minik Çıraksın!"), UIKit.Orange);
            if (GM.FirstTime("mini_toy"))
                GM.Tut?.CatHint(mini, B.ToyBall.rectTransform, L.T("Minik Çırak masada gezmesin istersen onu tezgahtaki topuna sürükle."), () => mini && mini.parked);
            if (m && m.state == KittenAgent.State.Sitting) m.Idle(1.5f);
            Sfx.Play("levelup", 1.3f);
            GM.Toast(L.F("{0} Minik Çırak hazır! Tarif Defteri'nde bir iksire 'Yaptır' de, malzemeleri kazana taşısın.", KittenColors.Name(col)), UIKit.Orange, 6f);
            GM.OnBoardChanged();
        }

        void Graduation(Vector2 from, Vector2 to)
        {
            var cap = UIKit.Img("Cap", layer, ProcArt.RedCap, Color.white);
            cap.raycastTarget = false;
            var crt = cap.rectTransform;
            crt.sizeDelta = new Vector2(70f, 70f);
            crt.anchoredPosition = from;
            Tween.Run(0.6f, t =>
            {
                if (!crt) return;
                crt.anchoredPosition = Vector2.Lerp(from, to, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 90f);
                crt.localEulerAngles = new Vector3(0f, 0f, (1f - t) * 360f);
            }, () =>
            {
                if (!crt) return;
                FX.Burst(B.FxLayer, to, new Color(1f, 0.85f, 0.4f), 10);
                Destroy(crt.gameObject);
            });
        }

        // ------------------------------------------------------------ training: the Master and the kitten pass a yarn ball
        Image trainYarn;
        float yarnT;

        void TrainYarnTick(float dt)
        {
            var k = Trainee();
            var m = Master();
            bool on = k && m && m.state == KittenAgent.State.Sitting && !k.IsHeld;
            if (!on) { if (trainYarn) trainYarn.gameObject.SetActive(false); return; }
            if (!trainYarn)
            {
                trainYarn = UIKit.Img("TrainYarn", layer, ProcArt.Item(Chains.Ag, 2), Color.white);
                trainYarn.raycastTarget = false;
                trainYarn.rectTransform.sizeDelta = new Vector2(46f, 46f);
            }
            if (!trainYarn.gameObject.activeSelf) { trainYarn.gameObject.SetActive(true); yarnT = 0f; }
            trainYarn.transform.SetAsLastSibling();
            const float pass = 1.3f;   // seconds per pass
            int before = (int)(yarnT / pass);
            yarnT += dt;
            int leg = (int)(yarnT / pass);
            if (leg != before) { Tween.Punch((leg % 2 == 0 ? m : k).rt, 0.1f, 0.2f); Sfx.Play("paw", 1.4f, 0.35f); }
            float e = (yarnT % pass) / pass;
            Vector2 a = m.Pos + new Vector2(0f, -20f), b = k.Pos + new Vector2(0f, -20f);
            if (leg % 2 == 1) { var tmp = a; a = b; b = tmp; }
            trainYarn.rectTransform.anchoredPosition = Vector2.Lerp(a, b, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * 75f);
            trainYarn.rectTransform.localEulerAngles = new Vector3(0f, 0f, -e * 360f);
        }

        void MiniFarewell(KittenAgent a)
        {
            if (Selected == a) { Selected = null; GM.ShowInfo(null); }
            Say(a, L.T("Hoşça kal!"), UIKit.Cream);
            a.Leave();
            GM.Toast(L.T("Minik Çırak'ın süresi doldu, el salladı. Usta yeni bir yavru eğitebilir."), UIKit.Orange, 4.5f);
            GM.MarkDirty();
        }

        // ------------------------------------------------------------ Mini Apprentice: fetching
        class MiniJob
        {
            public Recipe recipe;
            public readonly List<ReqSave> needs = new List<ReqSave>();   // still to bring
            public Vector2Int target = new Vector2Int(-1, -1);          // item cell it is walking to
            public readonly List<Vector2Int> reserved = new List<Vector2Int>();   // locked items, one per need
            public bool carrying;
        }

        /// <summary>Lock or unlock an item the Mini Apprentice is going to fetch (orange frame + its little head).</summary>
        void Reserve(Vector2Int c, bool on)
        {
            var d = B.Get(c);
            if (d == null || d.reserved == on) return;
            d.reserved = on;
            B.RefreshView(c);
        }

        void ReleaseReservations()
        {
            if (miniJob == null) return;
            foreach (var c in miniJob.reserved) Reserve(c, false);
            miniJob.reserved.Clear();
        }

        /// <summary>"Vazgeç": the Mini Apprentice drops the job; what it carries goes back on the table.</summary>
        public void CancelMiniJob()
        {
            var mini = Mini();
            if (miniJob == null) return;
            ReleaseReservations();
            if (mini && miniJob.carrying && miniJob.needs.Count > 0)
            {
                var q = miniJob.needs[0];
                var from = mini.Pos;
                var near = BoardController.LocalToCellClamped(from);
                if (B.NearestEmpty(near, out var e)) B.SpawnFromPos(from, e, new ItemData(q.chain, q.level));
                else GM.Storage.Add(new ItemData(q.chain, q.level));
                mini.carryChain = null;
                mini.SetCarry(null);
            }
            miniJob = null;
            if (mini) { mini.Idle(1.5f); Say(mini, "Peki!", UIKit.Cream); }
            GM.Toast(L.T("Minik Çırak işi bıraktı, malzemeler serbest."));
            GM.OnBoardChanged();
        }
        MiniJob miniJob;

        public bool MiniBusy => miniJob != null;

        /// <summary>"Yaptır" in the recipe book: the Mini Apprentice brings both ingredients of `r` to the pot.</summary>
        public void MiniBrew(Recipe r)
        {
            var mini = Mini();
            if (!mini) { GM.Toast(L.T("Önce Usta bir yavruyu Minik Çırak olarak eğitmeli.")); return; }
            if (miniJob != null) { GM.Toast(L.T("Minik Çırak şu an başka bir tarif için koşturuyor.")); return; }
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc)) return;
            var pot = B.Get(pc);
            if (pot.brew > 0) { GM.Toast(pot.BrewReady ? L.T("Kazanda hazır iksir var. Önce onu al.") : L.T("Kazan şu an kaynıyor, bitmesini bekle.")); return; }

            var job = new MiniJob { recipe = r };
            job.needs.Add(new ReqSave { chain = r.aChain, level = r.aLevel });
            job.needs.Add(new ReqSave { chain = r.bChain, level = r.bLevel });
            if (!string.IsNullOrEmpty(pot.mix))
            {
                // one ingredient may already be waiting in the pot
                int idx = job.needs.FindIndex(q => Recipes.Key(q.chain, q.level) == pot.mix);
                if (idx < 0) { GM.Toast(L.T("Kazanda bu tarife uymayan bir malzeme var. Kazanı seçip 'Geri Al'a bas.")); return; }
                job.needs.RemoveAt(idx);
            }

            // check everything is on the table before it starts running
            var used = new HashSet<Vector2Int>();
            var missing = new List<Sprite>();
            var missingNames = new List<string>();
            foreach (var q in job.needs)
            {
                if (FindIngredient(q, used, out var c)) used.Add(c);
                else { missing.Add(ProcArt.Item(q.chain, q.level)); missingNames.Add(Chains.Name(q.chain, q.level)); }
            }
            if (missing.Count > 0)
            {
                mini.ShowWant(missing, 3f);
                Tween.Punch(mini.rt, 0.15f, 0.3f);
                Sfx.Play("error");
                GM.Toast(L.F("Masada eksik: {0}", string.Join(", ", missingNames)), new Color(1f, 0.7f, 0.6f));
                return;
            }
            miniJob = job;
            mini.parked = false;   // up from the toy and off to work (it roams afterwards)
            // lock exactly the items it found, in the same order as the needs
            var lockUsed = new HashSet<Vector2Int>();
            foreach (var q in job.needs)
                if (FindIngredient(q, lockUsed, out var rc)) { lockUsed.Add(rc); job.reserved.Add(rc); Reserve(rc, true); }
            Say(mini, "Hemen!", UIKit.Orange);
            Sfx.Play("paw", 1.3f, 0.7f);
        }

        bool FindIngredient(ReqSave q, HashSet<Vector2Int> skip, out Vector2Int cell)
        {
            cell = new Vector2Int(-1, -1);
            var mini = Mini();
            float best = float.MaxValue;
            foreach (var c in B.AllCells())
            {
                if (skip != null && skip.Contains(c)) continue;
                var d = B.Get(c);
                if (d == null || d.Locked || d.chain != q.chain || d.level != q.level) continue;   // reserved items count as locked
                float dist = mini ? (BoardController.CellPos(c) - mini.Pos).sqrMagnitude : 0f;
                if (dist < best) { best = dist; cell = c; }
            }
            return cell.x >= 0;
        }

        void MiniTick()
        {
            if (miniJob == null) return;
            var mini = Mini();
            if (!mini || mini.IsGone) { ReleaseReservations(); miniJob = null; return; }
            if (mini.IsHeld || mini.state == KittenAgent.State.Entering) return;
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc)) { miniJob = null; return; }

            if (!miniJob.carrying)
            {
                if (miniJob.needs.Count == 0) { FinishMiniJob(mini, pc); return; }
                var q = miniJob.needs[0];
                // go for the item that was locked for it
                if (miniJob.reserved.Count > 0) miniJob.target = miniJob.reserved[0];
                var d = B.Get(miniJob.target);
                // (re)pick the nearest matching item if the target is gone after all
                if (d == null || d.nap || d.bubble || d.chain != q.chain || d.level != q.level)
                {
                    if (miniJob.reserved.Count > 0) { Reserve(miniJob.reserved[0], false); miniJob.reserved.RemoveAt(0); }
                    if (!FindIngredient(q, null, out miniJob.target))
                    {
                        mini.ShowWant(new List<Sprite> { ProcArt.Item(q.chain, q.level) }, 3f);
                        GM.Toast(L.F("Minik Çırak {0} bulamadı.", Chains.Name(q.chain, q.level)), new Color(1f, 0.7f, 0.6f));
                        ReleaseReservations();
                        miniJob = null;
                        mini.Idle(1.5f);
                        return;
                    }
                    miniJob.reserved.Insert(0, miniJob.target);
                    Reserve(miniJob.target, true);
                }
                var tp = BoardController.CellPos(miniJob.target);
                mini.ChaseTo(tp);
                if ((mini.Pos - tp).sqrMagnitude < 30f * 30f)
                {
                    // grab it
                    if (miniJob.reserved.Count > 0) miniJob.reserved.RemoveAt(0);
                    var grabbed = B.Get(miniJob.target);
                    if (grabbed != null) grabbed.reserved = false;
                    B.Remove(miniJob.target);
                    mini.carryChain = q.chain;
                    mini.carryLevel = q.level;
                    mini.SetCarry(ProcArt.Item(q.chain, q.level));
                    miniJob.carrying = true;
                    Sfx.Play("pop", 1.4f, 0.6f);
                    GM.OnBoardChanged();
                }
                return;
            }

            // carrying: run to the pot and drop it in
            var pp = BoardController.CellPos(pc) + new Vector2(pc.x > 3 ? -70f : 70f, 0f);
            mini.ChaseTo(pp);
            if ((mini.Pos - pp).sqrMagnitude > 30f * 30f) return;
            var item = miniJob.needs[0];
            miniJob.needs.RemoveAt(0);
            miniJob.carrying = false;
            mini.carryChain = null;
            mini.SetCarry(null);
            if (!B.FeedPot(pc, item.chain, item.level))
            {
                // the pot changed meanwhile: put the ingredient back on the table
                if (B.NearestEmpty(pc, out var e)) B.SpawnFromPos(mini.Pos, e, new ItemData(item.chain, item.level));
                GM.Toast(L.T("Kazan şu an malzeme almıyor, Minik Çırak malzemeyi masaya bıraktı."));
                ReleaseReservations();
                miniJob = null;
                mini.Idle(1.5f);
                return;
            }
            Tween.Punch(mini.rt, 0.12f, 0.25f);
            if (miniJob.needs.Count == 0) FinishMiniJob(mini, pc);
        }

        void FinishMiniJob(KittenAgent mini, Vector2Int pc)
        {
            ReleaseReservations();
            miniJob = null;
            mini.Idle(2f);
            Say(mini, "Tamam!", UIKit.Orange);
        }

        // ------------------------------------------------------------ colours
        static double ApprenticeHoursFor(int color) => color == KittenColors.UcRenkli ? 3.0 : ApprenticeHours;

        /// <summary>A new apprentice: say which colour it is and what it is good at, add its album sticker.</summary>
        void ApprenticeBorn(KittenAgent c)
        {
            int col = Mathf.Clamp(c.color, 0, KittenColors.Count - 1);
            GM.Toast(L.F("{0} Çırak doğdu!", KittenColors.Names[col]) + " " + KittenColors.Talents[col], UIKit.Orange, 6f);
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
            k && k.IsWild && !k.Training && !k.IsGone && !k.IsHeld && k.state != KittenAgent.State.Entering && k.age >= GraceSeconds;

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
            if (!ap || ap.IsHeld) { chaseTarget = null; return; }

            if (chaseTarget == null)
            {
                chaseCd -= dt;
                if (chaseCd > 0f) return;
                if (!ap.CanAct && !ap.Stirring) return;
                // with the Master on its cushion, it does the shooing: the apprentice keeps stirring
                var guard = Master();
                if (guard && guard.homed)
                {
                    chaseCd = 1f;
                    if (!ap.Stirring && ap.state == KittenAgent.State.Idle) GoBackToPot(ap);
                    return;
                }
                chaseTarget = NearestWild(ap.Pos, 99999f);
                if (chaseTarget == null)
                {
                    chaseCd = 1f;
                    // nothing to chase: back to its cauldron on the counter
                    if (!ap.Stirring && ap.state == KittenAgent.State.Idle) GoBackToPot(ap);
                    return;
                }
                if (ap.Stirring) ap.StopStir();   // jumps off the counter to chase
                // the kitten notices and scampers away (unless it's asleep)
                if (chaseTarget.CanAct)
                    chaseTarget.WalkTo(chaseTarget.Pos + (chaseTarget.Pos - ap.Pos).normalized * 260f);
                Say(ap, L.T("Kışt!"), UIKit.Orange);
                Sfx.Play("paw", 0.9f, 0.6f);
            }

            if (!Shooable(chaseTarget)) { chaseTarget = null; GoBackToPot(ap); return; }
            ap.ChaseTo(chaseTarget.Pos);
            if ((chaseTarget.Pos - ap.Pos).sqrMagnitude < 75f * 75f)
            {
                Scare(chaseTarget);
                chaseTarget = null;
                chaseCd = ap.color == KittenColors.SiyahBeyaz ? Random.Range(2f, 3f) : Random.Range(4f, 7f);
                Tween.Punch(ap.rt, 0.15f, 0.25f);
                GoBackToPot(ap);
                if (!chaseHintShown)
                {
                    chaseHintShown = true;
                    GM.Toast(L.T("Çırak Kedi yavruyu kovaladı! Sonra kazanının başına döner, iksiri hızlı karıştırır."), UIKit.Orange, 5f);
                }
            }
        }

        /// <summary>The apprentice walks back to the pot and starts stirring again.</summary>
        void GoBackToPot(KittenAgent ap)
        {
            if (!ap || ap.IsHeld || ap.Stirring || !B.FindFirst(d => d.chain == Chains.Simya, out var pc)) return;
            var pot = BoardController.CellPos(pc);
            ap.WalkTo(pot + KittenAgent.StirOffset, () =>
            {
                if (ap && !ap.IsGone && ap.level == 2 && !ap.IsHeld && chaseTarget == null) ap.StartStir(pc, pot);
            });
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
            if (!m || !m.CanAct || !m.homed || !m.AtHome) return;
            hissCd -= dt;
            if (hissCd > 0f) return;
            var k = NearestWild(m.Pos, 99999f);
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
                Say(m, L.T("Tısss!"), new Color(0.85f, 0.7f, 1f));
                Sfx.Play("hiss");
                Scare(k);
                if (!hissHintShown)
                {
                    hissHintShown = true;
                    GM.Toast(L.T("Usta Kedi tısladı, yavru masadan atladı! Yavruları Kedi Evi'ne sürüklersen kovalanmazlar."), UIKit.Orange, 5f);
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
                ? L.T("Çırak kazanı karıştırıyor: iksir 2 kat hızlı pişiyor!")
                : L.T("Çırak kazanın başında bekliyor. İksir koyunca 2 kat hızlı karıştıracak."), UIKit.Orange, 4.5f);
            GM.ShowKittenInfo(a);
            GM.MarkDirty();
        }

        /// <summary>A cat started or stopped stirring at this pot: redraw it (the timer moves up so the cat doesn't cover it).</summary>
        public void StirChanged(Vector2Int cell) { if (B != null) B.RefreshView(cell); }

        public KittenAgent StirrerAt(Vector2Int cell)
        {
            foreach (var a in agents) if (a && a.Stirring && a.stirCell == cell && !a.IsGone) return a;
            return null;
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
            if (!m) { GM.Toast(L.T("Masada Usta Kedi yok.")); return; }
            if (m.state == KittenAgent.State.Napping || m.IsHeld) return;
            if (Trainee()) { GM.Toast(L.T("Usta şu an bir yavruyu eğitiyor. Eğitim bitince keşfe çıkabilir.")); return; }
            if (!GM.TrySpendEnergy(Expeditions.Energy[k])) return;
            if (Selected == m) { Selected = null; GM.ShowInfo(null); }
            Trip = k;
            TripColor = m.color;
            TripUntil = DateTime.UtcNow.Ticks + TimeSpan.FromMinutes(Expeditions.Minutes[k]).Ticks;
            Say(m, L.T("Görüşürüz!"), UIKit.Cream);
            m.Leave();
            Sfx.Play("whoosh");
            GM.Toast(L.F("{0} {1} için yola çıktı. {2} sonra dönecek.", GM.UstaDisplayName(m.color), Expeditions.Names[k], Expeditions.Duration(k)), UIKit.Orange, 4.5f);
            GM.RefreshTripButton();
            GM.ShowInfo(null);
            GM.RefreshUstaViews();
            GM.MarkDirty();
        }

        void TripTick(long now)
        {
            // when the trip ends the Master does not just appear: it waits in the cat house with its chest
            if (Trip < 0 || now < TripUntil || readyAnnounced) return;
            readyAnnounced = true;
            if (FindHouse(out var hc)) B.RefreshView(hc);
            GM.Toast(L.F("{0} döndü! Kedi Evi'nde sandığıyla bekliyor. Eve dokun!", GM.UstaDisplayName(TripColor)), UIKit.Orange, 5f);
            GM.RefreshUstaViews();
            Sfx.Play("ready");
        }

        /// <summary>The button next to the storage box: shows the trip, or brings the Master out when it is back.</summary>
        public void TripButton()
        {
            if (Trip < 0) return;
            if (TripReady) { ClaimTrip(); return; }
            GM.Toast(TripStatus());
        }

        /// <summary>The Master jumps out of the cat house with its chest; the finds fly out of the chest onto the table.</summary>
        public void ClaimTrip()
        {
            if (!TripReady || B.AnyHeld) return;
            Vector2 origin;
            if (FindHouse(out var hc)) origin = BoardController.CellPos(hc);
            else { var hh = Half; origin = new Vector2(-hh.x + 60f, -hh.y + 60f); }
            ReturnFromTrip(origin);
            readyAnnounced = false;
            GM.RefreshTripButton();
            if (FindHouse(out var hc2)) B.RefreshView(hc2);
        }

        /// <summary>"Dolunay Seferi · 7sa 12dk kaldı" for the info bar while the Master is away.</summary>
        public string TripStatus()
        {
            if (Trip < 0) return "";
            var left = TimeSpan.FromTicks(Math.Max(0L, TripUntil - DateTime.UtcNow.Ticks));
            string time = left.TotalHours >= 1.0 ? L.F("{0} sa {1} dk", (int)left.TotalHours, left.Minutes) : L.F("{0} dk {1} sn", left.Minutes, left.Seconds);
            if (TripReady) return L.T("Usta Kedi döndü! Kedi Evi'ne dokun, sandığı açılsın.");
            return L.F("Usta Kedi {0} yolunda: {1} sonra döner.", Expeditions.Names[Mathf.Clamp(Trip, 0, Expeditions.Names.Length - 1)], time);
        }

        void ReturnFromTrip(Vector2 origin)
        {
            int k = Mathf.Clamp(Trip, 0, Expeditions.Names.Length - 1);
            Trip = -1;

            // the Master hops out of the house...
            var land = Clamp(origin + new Vector2(origin.x > 0f ? -150f : 150f, 30f));
            var m = Create(3, origin, TripColor);
            m.JumpIn(origin, land);
            m.homed = true;   // then it walks back to its cushion
            m.cd = MasterInterval;
            // ...and its chest pops open on top of the house
            var chest = UIKit.Img("TripChest", layer, ProcArt.Item(Chains.Sandik, 1), Color.white);
            chest.raycastTarget = false;
            var crt = chest.rectTransform;
            crt.At(origin + new Vector2(0f, 40f), new Vector2(110f, 110f));
            Tween.Pop(crt, 0.35f);
            Tween.Run(1.1f, t =>
            {
                if (!crt) return;
                crt.anchoredPosition = origin + new Vector2(0f, 40f + 50f * Tween.OutCubic(Mathf.Min(1f, t * 2f)));
                float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                chest.color = new Color(1f, 1f, 1f, fade);
            }, () => { if (crt) Destroy(crt.gameObject); });
            FX.Burst(B.FxLayer, origin + new Vector2(0f, 80f), new Color(1f, 0.85f, 0.4f), 14);
            var land2 = origin + new Vector2(0f, 90f);   // items fly out of the chest

            // what it found
            int placed = 0, stored = 0, sold = 0;
            var cell = BoardController.LocalToCellClamped(origin);
            for (int i = 0; i < Expeditions.Items[k]; i++)
            {
                var item = new ItemData(Chains.RandomIngredient(), Random.Range(Expeditions.MinLevel[k], Expeditions.MaxLevel[k] + 1));
                if (B.NearestEmpty(cell, out var e)) { B.SpawnFromPos(land2, e, item); GM.Discover(item.chain, item.level); placed++; }
                else if (GM.Storage.Count < GM.StorageSlots) { GM.Storage.Add(item); stored++; }
                else { GM.AddGold(Chains.Value(item.level)); sold++; }
            }
            var found = new List<string> { L.F("{0} malzeme", Expeditions.Items[k]) };
            if (Expeditions.Gold[k] > 0) { GM.AddGold(Expeditions.Gold[k]); found.Add(L.F("{0} altın", Expeditions.Gold[k])); }
            if (Expeditions.Stamps[k] > 0) { GM.AddStamps(Expeditions.Stamps[k]); found.Add(L.F("{0} Pati Mührü", Expeditions.Stamps[k])); }
            if (Expeditions.Ornament[k])
            {
                string id = Decor.ItemId(Random.Range(0, Decor.Slots), Random.Range(1, Decor.Variants));
                GM.GiftTowerDecor(id);
                found.Add(L.F("kule için {0}", Decor.ItemName(id)));
            }
            if (Random.value < Expeditions.StickerChance[k])
            {
                var cats = Stickers.Sets[1].ids;
                GM.GiveSticker(cats[Random.Range(0, cats.Length)]);
            }

            string where = stored > 0 || sold > 0
                ? " " + L.F("Masa doluydu: {0} gitti.", (stored > 0 ? L.F("{0} tanesi depoya", stored) : "") + (stored > 0 && sold > 0 ? ", " : "") + (sold > 0 ? L.F("{0} tanesi altına", sold) : ""))
                : "";
            Sfx.Play("gift");
            GM.ShowDialog(GM.UstaDisplayName(m.color), L.F("{0} bitti, miyav! Getirdiklerim: {1}.", Expeditions.Names[k], string.Join(", ", found)) + where);
            GM.RefreshHud();
            if (Selected == null) GM.ShowInfo(null);
            GM.OnBoardChanged();
        }
    }
}
