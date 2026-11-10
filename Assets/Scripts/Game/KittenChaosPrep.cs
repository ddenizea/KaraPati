using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// The Mini Apprentice's work, as one plan: a list of small steps done in order (a "task queue").
    ///   Make    - have one (chain, level) item on the table and keep it for the plan (locked, with its little head).
    ///             A star Mini Apprentice builds it when it is missing: carries one of two twins onto the other to
    ///             merge them, or taps the cauldron / chest (the player's energy, no limit). A plain one can only
    ///             use what is already there.
    ///   Feed    - carry a kept item into the alchemy pot.
    ///   Collect - take the finished potion out of the pot onto the table.
    /// "Yaptır" (recipe book, or an order with a plain Mini) = Make + Feed for both ingredients.
    /// "Hazırla" (an order with a star Mini) = the potion's Make + Feed, then the order's ingredients while it boils.
    /// It does not stand by the pot: it is free for other orders, and when the potion is done it comes back for it
    /// (potionDue). If the pot is busy when "Hazırla" is pressed, it does the ingredients now and the potion later.
    /// The player still presses "Ver". "Vazgeç" drops the plan and frees everything.
    /// </summary>
    public partial class KittenChaos
    {
        enum StepKind { Make, Feed, Collect }

        class Step
        {
            public StepKind kind;
            public string chain;
            public int level;
            public Step(StepKind k, string c = null, int l = 0) { kind = k; chain = c; level = l; }
        }

        class MiniPlan
        {
            public bool star;                // may merge and tap the cauldron / chest
            public bool order;               // a whole order ("Hazırla"), not just a recipe ("Yaptır")
            public readonly List<Step> steps = new List<Step>();
            public readonly List<Vector2Int> kept = new List<Vector2Int>();   // items kept for later steps (locked)
            public int phase;                // inside a step: 0 decide, 1 walk to an item, 2 carry it, 3 to the cauldron/chest
            public Vector2Int pick, target;  // pick up here; bring it onto `target` (merge) - or the cauldron/chest to tap
            public ItemData carried;
            public float wait;               // a short pause between moves so the player can follow
            public bool waitSaid;            // said "waiting for it to boil" once
            public Step Current => steps.Count > 0 ? steps[0] : null;
        }

        MiniPlan plan;
        bool potionDue;   // a star Mini started a potion for an order: it takes it out when it is done

        // ------------------------------------------------------------ state for the UI
        public bool MiniBusy => plan != null;

        /// <summary>A star Mini Apprentice is on the table (orders get "Hazırla").</summary>
        public bool HasStarMini { get { var m = Mini(); return m && m.star; } }

        /// <summary>The pot is boiling or holds a finished potion: no new brew now.</summary>
        public bool PotBusy => B != null && B.FindFirst(d => d.chain == Chains.Simya, out var pc) && B.Get(pc).brew > 0;

        /// <summary>No new "Yaptır" right now: the Mini is working, or the pot is busy.</summary>
        public bool BrewBusy => MiniBusy || PotBusy;

        /// <summary>Why a new job can't start now (toast text).</summary>
        public string BrewBusyReason()
        {
            if (plan != null) return plan.order ? L.T("Yıldızlı Minik Çırak şu an bir sipariş hazırlıyor.") : L.T("Minik Çırak şu an başka bir tarif için koşturuyor.");
            if (B != null && B.FindFirst(d => d.chain == Chains.Simya, out var pc))
            {
                var pot = B.Get(pc);
                if (pot.brew > 0) return pot.BrewReady ? L.T("Kazanda hazır iksir var. Önce onu al.") : L.T("Kazan şu an kaynıyor, bitmesini bekle.");
            }
            return "";
        }

        /// <summary>What the Mini is doing (kitten info panel).</summary>
        public string MiniStatus => plan == null ? "" : plan.order ? L.T("Siparişi hazırlıyor...") : L.T("Malzemeleri kazana taşıyor...");

        // ------------------------------------------------------------ starting a plan
        /// <summary>"Yaptır" in the recipe book (or on an order, with a plain Mini): both ingredients into the pot.</summary>
        public void MiniBrew(Recipe r)
        {
            var mini = Mini();
            if (!mini) { GM.Toast(L.T("Önce Usta bir yavruyu Minik Çırak olarak eğitmeli.")); return; }
            if (BrewBusy) { GM.Toast(BrewBusyReason()); Sfx.Play("error"); return; }
            var p = new MiniPlan { star = mini.star };
            if (!AddBrew(p, r)) return;
            StartPlan(mini, p, L.T("Hemen!"));
        }

        /// <summary>"Hazırla" on an order (star Mini): the whole order. `potion` is the recipe to brew, or null
        /// when the order has no potion or it is already on the table.</summary>
        public void MiniOrder(OrderSave o, Recipe potion)
        {
            var mini = Mini();
            if (!mini || !mini.star || o == null) return;
            if (MiniBusy) { GM.Toast(BrewBusyReason()); Sfx.Play("error"); return; }
            bool hasIngredient = false;
            foreach (var q in o.reqs) if (Chains.IsIngredient(q.chain)) hasIngredient = true;
            if (potion != null && PotBusy)
            {
                // the pot is in use: the ingredients now, the potion once the pot is free
                if (!hasIngredient) { GM.Toast(BrewBusyReason()); Sfx.Play("error"); return; }
                potion = null;
            }
            var p = new MiniPlan { star = true, order = true };
            if (potion != null && !AddBrew(p, potion)) return;
            foreach (var q in o.reqs)
                if (Chains.IsIngredient(q.chain)) p.steps.Add(new Step(StepKind.Make, q.chain, q.level));   // made while it boils
            StartPlan(mini, p, L.T("Hazırlıyorum!"));
        }

        /// <summary>Make both ingredients of `r` first (so both get kept), then feed them. One may already wait in the pot.</summary>
        bool AddBrew(MiniPlan p, Recipe r)
        {
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc)) return false;
            var pot = B.Get(pc);
            var needs = new List<ReqSave>
            {
                new ReqSave { chain = r.aChain, level = r.aLevel },
                new ReqSave { chain = r.bChain, level = r.bLevel },
            };
            if (!string.IsNullOrEmpty(pot.mix))
            {
                int idx = needs.FindIndex(q => Recipes.Key(q.chain, q.level) == pot.mix);
                if (idx < 0) { GM.Toast(L.T("Kazanda bu tarife uymayan bir malzeme var. Kazanı seçip 'Geri Al'a bas.")); return false; }
                needs.RemoveAt(idx);
            }
            foreach (var q in needs) p.steps.Add(new Step(StepKind.Make, q.chain, q.level));
            foreach (var q in needs) p.steps.Add(new Step(StepKind.Feed, q.chain, q.level));
            return true;
        }

        void StartPlan(KittenAgent mini, MiniPlan p, string say)
        {
            if (!p.star)
            {
                // a plain Mini can't make anything: everything must be on the table before it starts running
                var used = new HashSet<Vector2Int>();
                var missing = new List<Sprite>();
                var names = new List<string>();
                foreach (var s in p.steps)
                {
                    if (s.kind != StepKind.Make) continue;
                    if (FindIngredient(s.chain, s.level, used, out var c)) used.Add(c);
                    else { missing.Add(ProcArt.Item(s.chain, s.level)); names.Add(Chains.Name(s.chain, s.level)); }
                }
                if (missing.Count > 0)
                {
                    mini.ShowWant(missing, 3f);
                    Tween.Punch(mini.rt, 0.15f, 0.3f);
                    Sfx.Play("error");
                    GM.Toast(L.F("Masada eksik: {0}", string.Join(", ", names)), new Color(1f, 0.7f, 0.6f));
                    return;
                }
            }
            if (p.steps.Count == 0) return;
            plan = p;
            mini.parked = false;   // up from the toy and off to work
            // keep what is already on the table right away, so nobody takes it meanwhile
            while (plan.Current != null && plan.Current.kind == StepKind.Make && FindIngredient(plan.Current.chain, plan.Current.level, null, out var have))
            {
                Keep(have);
                NextStep();
            }
            Say(mini, say, UIKit.Orange);
            Sfx.Play("paw", 1.3f, 0.7f);
            GM.MarkDirty();
            GM.OnBoardChanged();
        }

        // ------------------------------------------------------------ ending a plan
        /// <summary>"Vazgeç": the Mini drops the job; what it carries goes back on the table.</summary>
        public void CancelMiniJob()
        {
            if (plan == null) return;
            var mini = Mini();
            DropPlan(L.T("Minik Çırak işi bıraktı, malzemeler serbest."));
            if (mini) Say(mini, L.T("Peki!"), UIKit.Cream);
        }

        /// <summary>Stop the plan: free the kept items, put down what it carries, say why (if any).</summary>
        void DropPlan(string why)
        {
            if (plan == null) return;
            var mini = Mini();
            ReleaseAll();
            if (plan.carried != null)
            {
                PutDown(mini ? mini.Pos : Vector2.zero, plan.carried);
                if (mini) { mini.carryChain = null; mini.SetCarry(null); }
            }
            plan = null;
            if (!string.IsNullOrEmpty(why)) GM.Toast(why, new Color(1f, 0.7f, 0.6f), 4f);
            if (mini) mini.Idle(1.5f);
            GM.OnBoardChanged();
        }

        void FinishPlan(KittenAgent mini)
        {
            bool order = plan.order;
            ReleaseAll();
            plan = null;
            mini.Idle(2f);
            if (order && potionDue)
            {
                Say(mini, L.T("Hazır!"), UIKit.Orange);
                GM.Toast(L.T("Malzemeler hazır! İksir kaynayınca Yıldızlı Minik Çırak onu da masaya çıkaracak."), UIKit.Orange, 4f);
            }
            else if (order)
            {
                Say(mini, L.T("Hazır!"), UIKit.Orange);
                Sfx.Play("ready", 1.2f);
                GM.Toast(L.T("Yıldızlı Minik Çırak siparişi hazırladı! Şimdi 'Ver'e bas."), UIKit.Orange, 4f);
            }
            else Say(mini, L.T("Tamam!"), UIKit.Orange);
            GM.OnBoardChanged();
        }

        void ReleaseAll()
        {
            if (plan == null) return;
            foreach (var c in plan.kept) Reserve(c, false);
            plan.kept.Clear();
            if (plan.phase == 1 || plan.phase == 2) { Reserve(plan.pick, false); Reserve(plan.target, false); }
        }

        // ------------------------------------------------------------ the loop
        void PlanTick(float dt)
        {
            if (plan == null) { if (potionDue) PotionWatch(); return; }
            var mini = Mini();
            if (!mini || mini.IsGone) { DropPlan(null); return; }
            if (mini.IsHeld || mini.state == KittenAgent.State.Entering) return;
            if (plan.wait > 0f) { plan.wait -= dt; return; }
            var s = plan.Current;
            if (s == null) { FinishPlan(mini); return; }
            mini.parked = false;
            switch (s.kind)
            {
                case StepKind.Make: MakeTick(mini, s); break;
                case StepKind.Feed: FeedTick(mini, s); break;
                case StepKind.Collect: CollectTick(mini); break;
            }
        }

        void NextStep()
        {
            plan.steps.RemoveAt(0);
            plan.phase = 0;
        }

        void MakeTick(KittenAgent mini, Step s)
        {
            switch (plan.phase)
            {
                case 0:
                {
                    // already on the table: keep it
                    if (FindIngredient(s.chain, s.level, null, out var have)) { Keep(have); NextStep(); return; }
                    if (!plan.star)
                    {
                        mini.ShowWant(new List<Sprite> { ProcArt.Item(s.chain, s.level) }, 3f);
                        DropPlan(L.F("Minik Çırak {0} bulamadı.", Chains.Name(s.chain, s.level)));
                        return;
                    }
                    // two of a lower level: merge them (the highest level first - the shortest way up)
                    for (int k = s.level - 1; k >= 1; k--)
                    {
                        var cells = FreeCells(s.chain, k);
                        if (cells.Count < 2) continue;
                        cells.Sort((x, y) => (BoardController.CellPos(x) - mini.Pos).sqrMagnitude.CompareTo((BoardController.CellPos(y) - mini.Pos).sqrMagnitude));
                        plan.pick = cells[0];
                        plan.target = cells[1];
                        Reserve(plan.pick, true);
                        Reserve(plan.target, true);
                        plan.phase = 1;
                        return;
                    }
                    // nothing to merge: make more at the cauldron (herbs, mushrooms) or the chest (crystals, webs)
                    string gen = s.chain == Chains.Ot || s.chain == Chains.Mantar ? Chains.Kazan : Chains.Sandik;
                    if (!B.FindFirst(d => d.chain == gen && !d.Locked, out var g)) { DropPlan(L.T("Yıldızlı Minik Çırak üretecek bir kazan ya da sandık bulamadı.")); return; }
                    if (GM.Energy < 1) { DropPlan(L.T("Enerjin bitti, Yıldızlı Minik Çırak durdu.")); return; }
                    if (!B.NearestEmpty(g, out _)) { DropPlan(L.T("Masa dolu, Yıldızlı Minik Çırak yeni malzeme çıkaramıyor. Biraz yer aç, sonra yine 'Hazırla'ya bas.")); return; }
                    plan.target = g;
                    plan.phase = 3;
                    return;
                }
                case 1:
                    // to the first of the two twins and pick it up
                    if (!WalkTo(mini, BoardController.CellPos(plan.pick))) return;
                    Reserve(plan.pick, false);
                    if (!PickUp(mini, plan.pick)) { Reserve(plan.target, false); plan.phase = 0; return; }
                    plan.phase = 2;
                    return;
                case 2:
                {
                    // ...and onto its twin: they merge
                    if (!WalkTo(mini, BoardController.CellPos(plan.target))) return;
                    Reserve(plan.target, false);
                    var item = plan.carried;
                    ClearCarry(mini);
                    if (!B.MergeCarriedInto(plan.target, item)) PutDown(mini.Pos, item);   // its twin is gone: put it down again
                    Tween.Punch(mini.rt, 0.12f, 0.25f);
                    plan.phase = 0;
                    plan.wait = 0.35f;
                    return;
                }
                case 3:
                    // to the cauldron or the chest and give it a tap
                    if (!WalkTo(mini, BoardController.CellPos(plan.target) + new Vector2(0f, -40f))) return;
                    if (!B.BotUseGenerator(plan.target)) { DropPlan(L.T("Yıldızlı Minik Çırak devam edemedi: masa dolu ya da enerji bitti.")); return; }
                    Tween.Punch(mini.rt, 0.12f, 0.25f);
                    plan.phase = 0;
                    plan.wait = 0.45f;
                    return;
            }
        }

        void FeedTick(KittenAgent mini, Step s)
        {
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc)) { DropPlan(null); return; }
            switch (plan.phase)
            {
                case 0:
                {
                    int i = plan.kept.FindIndex(c => Same(B.Get(c), s.chain, s.level));
                    if (i < 0) { plan.steps.Insert(0, new Step(StepKind.Make, s.chain, s.level)); return; }   // the kept one is gone: get another first
                    plan.pick = plan.kept[i];
                    plan.target = plan.pick;
                    plan.kept.RemoveAt(i);   // still locked until it is picked up
                    plan.phase = 1;
                    return;
                }
                case 1:
                    if (!WalkTo(mini, BoardController.CellPos(plan.pick))) return;
                    Reserve(plan.pick, false);
                    plan.phase = PickUp(mini, plan.pick) ? 2 : 0;
                    return;
                case 2:
                {
                    // carrying: run to the pot and drop it in
                    if (!WalkTo(mini, PotSide(pc))) return;
                    var item = plan.carried;
                    ClearCarry(mini);
                    if (!B.FeedPot(pc, item.chain, item.level))
                    {
                        PutDown(mini.Pos, item);
                        DropPlan(L.T("Kazan şu an malzeme almıyor, Minik Çırak malzemeyi masaya bıraktı."));
                        return;
                    }
                    Tween.Punch(mini.rt, 0.12f, 0.25f);
                    if (plan.order && B.Get(pc).brew > 0) potionDue = true;   // it comes back for the potion
                    NextStep();
                    return;
                }
            }
        }

        /// <summary>No plan running and a potion it started is done: go and take it out.</summary>
        void PotionWatch()
        {
            var mini = Mini();
            if (!mini || !mini.star) { potionDue = false; return; }
            if (mini.IsHeld || mini.state == KittenAgent.State.Entering) return;
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc) || B.Get(pc).brew == 0) { potionDue = false; return; }   // the player took it
            if (!B.Get(pc).BrewReady) return;
            potionDue = false;
            plan = new MiniPlan { star = true, order = true };
            plan.steps.Add(new Step(StepKind.Collect));
            mini.parked = false;
            Say(mini, L.T("İksir hazır!"), UIKit.Orange);
            GM.OnBoardChanged();
        }

        void CollectTick(KittenAgent mini)
        {
            if (!B.FindFirst(d => d.chain == Chains.Simya, out var pc)) { NextStep(); return; }
            var pot = B.Get(pc);
            if (pot.brew == 0) { NextStep(); return; }   // the player took it out already
            if (!WalkTo(mini, PotSide(pc))) return;
            if (!pot.BrewReady)
            {
                mini.Idle(1f);   // sits by the pot
                if (!plan.waitSaid) { plan.waitSaid = true; Say(mini, L.T("Kaynamasını bekliyorum..."), UIKit.Cream); }
                return;
            }
            if (!B.BotCollectPotion(pc)) { DropPlan(L.T("İksir hazır ama masada yer yok. Yer açıp kazana dokun.")); return; }
            Tween.Punch(mini.rt, 0.12f, 0.25f);
            NextStep();
        }

        // ------------------------------------------------------------ small helpers
        static Vector2 PotSide(Vector2Int pc) => BoardController.CellPos(pc) + new Vector2(pc.x > 3 ? -70f : 70f, 0f);

        /// <summary>Run towards p; true once it is there.</summary>
        static bool WalkTo(KittenAgent mini, Vector2 p)
        {
            if ((mini.Pos - p).sqrMagnitude <= 30f * 30f) return true;
            mini.ChaseTo(p);
            return false;
        }

        void Keep(Vector2Int c)
        {
            Reserve(c, true);
            plan.kept.Add(c);
        }

        bool PickUp(KittenAgent mini, Vector2Int c)
        {
            var d = B.Get(c);
            if (d == null || d.nap || d.bubble) return false;
            plan.carried = new ItemData(d.chain, d.level);
            B.Remove(c);
            mini.carryChain = d.chain;
            mini.carryLevel = d.level;
            mini.SetCarry(ProcArt.Item(d.chain, d.level));
            Sfx.Play("pop", 1.4f, 0.6f);
            GM.OnBoardChanged();
            return true;
        }

        void ClearCarry(KittenAgent mini)
        {
            plan.carried = null;
            mini.carryChain = null;
            mini.SetCarry(null);
        }

        /// <summary>Put an item back: on the nearest free cell, else into storage, else sell it.</summary>
        void PutDown(Vector2 from, ItemData item)
        {
            var near = BoardController.LocalToCellClamped(from);
            if (B.NearestEmpty(near, out var e)) B.SpawnFromPos(from, e, item);
            else if (GM.Storage.Count < GM.StorageSlots) GM.Storage.Add(item);
            else GM.AddGold(Chains.Value(item.level));
        }

        bool Same(ItemData d, string chain, int level) => d != null && !d.nap && !d.bubble && d.chain == chain && d.level == level;

        /// <summary>Free items of a kind (not locked, not kept by the plan).</summary>
        List<Vector2Int> FreeCells(string chain, int level) =>
            B.Cells(d => !d.Locked && d.chain == chain && d.level == level);

        /// <summary>The nearest free item of a kind.</summary>
        bool FindIngredient(string chain, int level, HashSet<Vector2Int> skip, out Vector2Int cell)
        {
            cell = new Vector2Int(-1, -1);
            var mini = Mini();
            float best = float.MaxValue;
            foreach (var c in FreeCells(chain, level))
            {
                if (skip != null && skip.Contains(c)) continue;
                float dist = mini ? (BoardController.CellPos(c) - mini.Pos).sqrMagnitude : 0f;
                if (dist < best) { best = dist; cell = c; }
            }
            return cell.x >= 0;
        }

        /// <summary>Lock or unlock an item the Mini is keeping (orange frame + its little head).</summary>
        void Reserve(Vector2Int c, bool on)
        {
            var d = B.Get(c);
            if (d == null || d.reserved == on) return;
            d.reserved = on;
            B.RefreshView(c);
        }
    }
}
