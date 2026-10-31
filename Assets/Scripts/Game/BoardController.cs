using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>The merge grid (the witch's table): data model, item views, moving, merging and spawning.
    /// Cats are not part of the grid; they walk around on their own layer above the items.</summary>
    public class BoardController : MonoBehaviour, IPointerClickHandler
    {
        public const int Cols = 7, Rows = 9;
        public const float CS = 140f;

        readonly ItemData[,] data = new ItemData[Cols, Rows];
        readonly ItemView[,] views = new ItemView[Cols, Rows];

        RectTransform grid, itemsLayer, catsLayer, fxLayer;
        Image selector;
        public Vector2Int Selected { get; private set; } = new Vector2Int(-1, -1);
        public bool Ready { get; private set; }
        public RectTransform CatsLayer => catsLayer;
        public RectTransform FxLayer => fxLayer;

        GameManager GM => GameManager.I;

        // ---------------------------------------------------------------- build
        public void Build(RectTransform area)
        {
            var catcher = area.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);

            // the wooden table the grid sits on
            var table = UIKit.Img("Table", area, ProcArt.Rounded, new Color(0.36f, 0.23f, 0.17f), true);
            table.rectTransform.At(new Vector2(0f, -6f), new Vector2(Cols * CS + 56f, Rows * CS + 56f));
            var tableTop = UIKit.Img("TableTop", area, ProcArt.Rounded, new Color(0.47f, 0.31f, 0.22f), true);
            tableTop.rectTransform.At(Vector2.zero, new Vector2(Cols * CS + 40f, Rows * CS + 40f));
            var cloth = UIKit.Img("Cloth", area, ProcArt.Rounded, new Color(0.2f, 0.13f, 0.27f, 0.97f), true);
            cloth.rectTransform.At(Vector2.zero, new Vector2(Cols * CS + 14f, Rows * CS + 14f));

            grid = UIKit.Rect("Grid", area).At(Vector2.zero, new Vector2(Cols * CS, Rows * CS));

            var cA = new Color(0.29f, 0.21f, 0.4f);
            var cB = new Color(0.25f, 0.18f, 0.35f);
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                var cell = UIKit.Img("Cell", grid, ProcArt.Rounded, ((c + r) % 2 == 0) ? cA : cB, true);
                cell.rectTransform.At(CellPos(c, r), new Vector2(CS - 8f, CS - 8f));
            }

            selector = UIKit.Img("Selector", grid, ProcArt.Frame, UIKit.Orange, true);
            selector.rectTransform.sizeDelta = new Vector2(CS + 2f, CS + 2f);
            selector.gameObject.SetActive(false);

            itemsLayer = UIKit.Rect("Items", grid).At(Vector2.zero, grid.sizeDelta);
            catsLayer = UIKit.Rect("Cats", grid).At(Vector2.zero, grid.sizeDelta);
            fxLayer = UIKit.Rect("FX", grid).At(Vector2.zero, grid.sizeDelta);
            Ready = true;
        }

        // ---------------------------------------------------------------- geometry
        public static Vector2 CellPos(int c, int r) => new Vector2((c - (Cols - 1) / 2f) * CS, ((Rows - 1) / 2f - r) * CS);
        public static Vector2 CellPos(Vector2Int c) => CellPos(c.x, c.y);
        public static bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Cols && c.y < Rows;

        public static Vector2Int LocalToCell(Vector2 lp) =>
            new Vector2Int(Mathf.FloorToInt(lp.x / CS + Cols / 2f), Mathf.FloorToInt(Rows / 2f - lp.y / CS));

        public static Vector2Int LocalToCellClamped(Vector2 lp)
        {
            var c = LocalToCell(lp);
            return new Vector2Int(Mathf.Clamp(c.x, 0, Cols - 1), Mathf.Clamp(c.y, 0, Rows - 1));
        }

        public bool ScreenToCell(Vector2 screen, out Vector2Int cell)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, screen, null, out var lp);
            cell = LocalToCell(lp);
            return InBounds(cell);
        }

        // ---------------------------------------------------------------- queries
        public ItemData Get(Vector2Int c) => InBounds(c) ? data[c.x, c.y] : null;
        public ItemView ViewAt(Vector2Int c) => InBounds(c) ? views[c.x, c.y] : null;
        public bool IsDragging(Vector2Int c) { var v = ViewAt(c); return v != null && v.IsHeld; }

        /// <summary>True while the player is touching or dragging any item or cat.</summary>
        public bool AnyHeld
        {
            get
            {
                foreach (var v in itemsLayer.GetComponentsInChildren<ItemView>())
                    if (v.IsHeld) return true;
                return GM.Cats != null && GM.Cats.AnyHeld;
            }
        }

        public IEnumerable<Vector2Int> AllCells()
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                yield return new Vector2Int(c, r);
        }

        public List<Vector2Int> Cells(System.Func<ItemData, bool> pred)
        {
            var list = new List<Vector2Int>();
            foreach (var c in AllCells())
            {
                var d = data[c.x, c.y];
                if (d != null && pred(d) && !IsDragging(c)) list.Add(c);
            }
            return list;
        }

        public List<Vector2Int> EmptyCells()
        {
            var list = new List<Vector2Int>();
            foreach (var c in AllCells()) if (data[c.x, c.y] == null) list.Add(c);
            return list;
        }

        public bool NearestEmpty(Vector2Int from, out Vector2Int best)
        {
            best = new Vector2Int(-1, -1);
            float bestD = float.MaxValue;
            foreach (var c in AllCells())
            {
                if (data[c.x, c.y] != null) continue;
                float d = (c - from).sqrMagnitude + Random.value * 0.1f;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best.x >= 0;
        }

        /// <summary>Greedy match of order requirements against free (non-napping) items.</summary>
        public bool TryCollect(List<ReqSave> reqs, List<Vector2Int> outCells, List<bool> perReq)
        {
            var used = new HashSet<Vector2Int>();
            bool all = true;
            foreach (var req in reqs)
            {
                bool found = false;
                foreach (var c in AllCells())
                {
                    var d = data[c.x, c.y];
                    if (d == null || d.Locked || d.chain != req.chain || d.level != req.level) continue;
                    if (used.Contains(c) || IsDragging(c)) continue;
                    used.Add(c);
                    outCells?.Add(c);
                    found = true;
                    break;
                }
                perReq?.Add(found);
                all &= found;
            }
            return all;
        }

        public void MarkNeeded(HashSet<Vector2Int> cells)
        {
            foreach (var c in AllCells())
            {
                var v = views[c.x, c.y];
                if (v) v.SetNeeded(cells.Contains(c));
            }
        }

        // ---------------------------------------------------------------- mutation
        public ItemView Set(Vector2Int c, ItemData d, bool pop = false)
        {
            if (views[c.x, c.y]) Destroy(views[c.x, c.y].gameObject);
            views[c.x, c.y] = null;
            data[c.x, c.y] = d;
            if (d == null) return null;

            var rt = UIKit.Rect("Item", itemsLayer);
            var v = rt.gameObject.AddComponent<ItemView>();
            v.Init(this, c, d);
            rt.anchoredPosition = CellPos(c);
            views[c.x, c.y] = v;
            if (pop) Tween.Pop(rt);
            return v;
        }

        public void ClearAll()
        {
            foreach (var c in AllCells()) Set(c, null);
            HideSelector();
        }

        public void RefreshView(Vector2Int c)
        {
            var v = ViewAt(c);
            if (v) v.Refresh();
        }

        public void MoveItem(Vector2Int from, Vector2Int to, float dur, bool spin = false)
        {
            var d = data[from.x, from.y];
            var v = views[from.x, from.y];
            data[to.x, to.y] = d; views[to.x, to.y] = v;
            data[from.x, from.y] = null; views[from.x, from.y] = null;
            if (!v) return;
            v.cell = to;
            Tween.Move(v.rt, CellPos(to), dur);
            if (spin) Tween.Spin(v.rt, dur);
        }

        void Swap(Vector2Int a, Vector2Int b)
        {
            var da = data[a.x, a.y]; var va = views[a.x, a.y];
            var db = data[b.x, b.y]; var vb = views[b.x, b.y];
            data[a.x, a.y] = db; views[a.x, a.y] = vb;
            data[b.x, b.y] = da; views[b.x, b.y] = va;
            if (va) { va.cell = b; Tween.Move(va.rt, CellPos(b), 0.12f); }
            if (vb) { vb.cell = a; Tween.Move(vb.rt, CellPos(a), 0.18f); }
        }

        /// <summary>Spawn an item that flies from a board-local position into the destination cell.</summary>
        public void SpawnFromPos(Vector2 from, Vector2Int dst, ItemData item)
        {
            var v = Set(dst, item);
            v.rt.anchoredPosition = from;
            v.rt.localScale = Vector3.one * 0.3f;
            Tween.Move(v.rt, CellPos(dst), 0.3f);
            var tr = v.rt;
            Tween.Run(0.3f, t => { if (tr) tr.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Tween.OutBack(t)); }, null, Tween.Key(tr, "s"));
        }

        public void SpawnFrom(Vector2Int src, Vector2Int dst, ItemData item) => SpawnFromPos(CellPos(src), dst, item);

        /// <summary>Remove an item, flying it to a world position (e.g. an order card).</summary>
        public void FlyAway(Vector2Int c, Vector3 worldTarget)
        {
            var v = views[c.x, c.y];
            data[c.x, c.y] = null; views[c.x, c.y] = null;
            if (Selected == c) Select(new Vector2Int(-1, -1));
            if (!v) return;
            var rt = v.rt;
            rt.SetParent(GM.Root, true);
            Vector3 from = rt.position;
            Tween.Run(0.45f, t =>
            {
                if (!rt) return;
                float e = Tween.OutCubic(t);
                rt.position = Vector3.Lerp(from, worldTarget, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 80f * GM.Root.lossyScale.y;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, e);
            }, () => { if (rt) Destroy(rt.gameObject); });
        }

        public void Remove(Vector2Int c) => RemoveWithShrink(c);

        void RemoveWithShrink(Vector2Int c)
        {
            var v = views[c.x, c.y];
            data[c.x, c.y] = null; views[c.x, c.y] = null;
            if (!v) return;
            var tr = v.rt;
            Tween.Run(0.2f, t => { if (tr) tr.localScale = Vector3.one * (1f - t); }, () => { if (tr) Destroy(tr.gameObject); }, Tween.Key(tr, "s"));
        }

        // ---------------------------------------------------------------- selection
        public void Select(Vector2Int c)
        {
            Selected = c;
            var d = Get(c);
            if (d == null)
            {
                selector.gameObject.SetActive(false);
                GM.ShowInfo(null);
                return;
            }
            selector.gameObject.SetActive(true);
            selector.rectTransform.anchoredPosition = CellPos(c);
            GM.ShowInfo(d);
        }

        public void HideSelector()
        {
            Selected = new Vector2Int(-1, -1);
            if (selector) selector.gameObject.SetActive(false);
        }

        public void RefreshSelection()
        {
            if (Get(Selected) != null) Select(Selected);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) Select(new Vector2Int(-1, -1));
        }

        // ---------------------------------------------------------------- input
        public void OnItemDropped(ItemView v, Vector2 screen)
        {
            var from = v.cell;
            var a = data[from.x, from.y];
            if (a != null && !a.Locked && GM.IsOverStorage(screen))
            {
                if (!GM.TryStore(from)) Tween.Move(v.rt, CellPos(from), 0.15f);
                return;
            }
            if (a == null || a.Locked || !ScreenToCell(screen, out var to) || to == from)
            {
                Tween.Move(v.rt, CellPos(from), 0.15f);
                return;
            }

            var b = data[to.x, to.y];
            if (b == null)
            {
                MoveItem(from, to, 0.08f);
                Select(to);
                GM.OnBoardChanged();
                return;
            }
            if (Chains.IsStation(b.chain) && Chains.IsIngredient(a.chain) && !b.nap)
            {
                DropIntoPot(from, to);
                return;
            }
            if (b.Locked)
            {
                Tween.Move(v.rt, CellPos(from), 0.15f);
                GM.Toast(b.bubble ? "Önce baloncuğu aç! (Seçip alttaki düğmeye bas.)" : "Orada bir yavru uyuyor! Önce dokunup okşa.");
                return;
            }
            if (a.chain == b.chain && a.level == b.level && Chains.IsIngredient(a.chain))
            {
                if (a.level < Chains.MaxLevel(a.chain)) { Merge(from, to); return; }
                GM.Toast(a.Name + " zaten en üst seviyede!");
            }
            Swap(from, to);
            Select(to);
            GM.OnBoardChanged();
        }

        public void OnItemClicked(ItemView v)
        {
            var c = v.cell;
            var d = data[c.x, c.y];
            if (d == null) return;
            if (d.nap) { GM.Cats.PetAt(c); return; }
            if (d.bubble) { Tween.Punch(v.rt, 0.12f, 0.25f); return; }
            if (Chains.IsGenerator(d.chain)) { UseGenerator(c); return; }
            if (Chains.IsStation(d.chain)) { TapPot(c); return; }
            Tween.Punch(v.rt, 0.1f, 0.2f);
        }

        void UseGenerator(Vector2Int c)
        {
            var gen = data[c.x, c.y];
            var v = ViewAt(c);
            if (!NearestEmpty(c, out var e))
            {
                GM.Toast("Masa dolu! Birleştir ya da sat.");
                if (v) Tween.Punch(v.rt, 0.08f, 0.2f);
                return;
            }
            if (!GM.TrySpendEnergy(1)) return;
            string chain = gen.chain == Chains.Kazan
                ? (Random.value < 0.5f ? Chains.Ot : Chains.Mantar)
                : (Random.value < 0.5f ? Chains.Kristal : Chains.Ag);
            int level = Random.value < 0.12f ? 2 : 1;
            if (v) Tween.Punch(v.rt, 0.15f, 0.25f);
            SpawnFrom(c, e, new ItemData(chain, level));
            GM.OnBoardChanged();
        }

        void Merge(Vector2Int from, Vector2Int to)
        {
            var a = data[from.x, from.y];
            var fv = views[from.x, from.y];
            data[from.x, from.y] = null; views[from.x, from.y] = null;
            if (fv) Destroy(fv.gameObject);

            var nd = new ItemData(a.chain, a.level + 1);
            Set(to, nd, true);
            FX.Burst(fxLayer, CellPos(to), ProcArt.ChainColor(nd.chain), nd.level >= 4 ? 12 : 7);
            Select(to);
            if (nd.level >= 3) GM.AddXp(nd.level - 2);
            TrySpawnBubble(to, nd);
            if (nd.level == Chains.MaxLevel(nd.chain))
                GM.Toast("Harika! " + nd.Name + " - en üst seviye!", UIKit.Orange);
            GM.OnBoardChanged();
        }

        public void SellSelected()
        {
            var c = Selected;
            var d = Get(c);
            if (d == null || d.Locked) return;
            int price;
            if (Chains.IsIngredient(d.chain)) price = d.level;
            else if (Chains.IsPotion(d.chain)) price = PotionSellPrice(d.level);
            else return;
            GM.AddGold(price);
            GM.Toast(d.Name + " satıldı: +" + price + " altın");
            RemoveWithShrink(c);
            Select(new Vector2Int(-1, -1));
            GM.OnBoardChanged();
        }

        // ---------------------------------------------------------------- alchemy cauldron
        public static int PotionSellPrice(int id) { var r = Recipes.Get(id); return r == null ? 1 : r.Gold / 4; }

        void ConsumeInto(Vector2Int from, Vector2Int pot)
        {
            var v = views[from.x, from.y];
            data[from.x, from.y] = null; views[from.x, from.y] = null;
            if (Selected == from) HideSelector();
            if (!v) return;
            var tr = v.rt;
            tr.SetParent(itemsLayer, true);
            Tween.Move(tr, CellPos(pot) + new Vector2(0f, 20f), 0.18f);
            Tween.Run(0.22f, t => { if (tr) tr.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.2f, t); }, () => { if (tr) Destroy(tr.gameObject); }, Tween.Key(tr, "s"));
        }

        void DropIntoPot(Vector2Int from, Vector2Int potCell)
        {
            var a = data[from.x, from.y];
            var pot = data[potCell.x, potCell.y];
            var v = views[from.x, from.y];
            int done = GM.TasksDone;
            if (pot.brew > 0)
            {
                Tween.Move(v.rt, CellPos(from), 0.15f);
                GM.Toast(pot.BrewReady ? "İksir hazır! Önce kazana dokunup al." : "Kazan kaynıyor, biraz bekle.");
                return;
            }
            if (string.IsNullOrEmpty(pot.mix))
            {
                if (!Recipes.AnyUses(a.chain, a.level, done))
                {
                    Tween.Move(v.rt, CellPos(from), 0.15f);
                    GM.Toast(a.Name + " hiçbir tarifte yok. Kazanı seçip 'Tarifler'e bak.");
                    return;
                }
                pot.mix = Recipes.Key(a.chain, a.level);
                ConsumeInto(from, potCell);
                var partners = Recipes.PartnersOf(a.chain, a.level, done);
                var names = new List<string>();
                foreach (var r in partners)
                {
                    bool isA = r.aChain == a.chain && r.aLevel == a.level;
                    names.Add(Chains.Name(isA ? r.bChain : r.aChain, isA ? r.bLevel : r.aLevel));
                }
                GM.Toast("Kazana " + a.Name + " girdi. Şimdi ekle: " + string.Join(" ya da ", names), new Color(0.85f, 0.7f, 1f), 4f);
            }
            else
            {
                Recipes.ParseKey(pot.mix, out var mc, out var ml);
                var r = Recipes.Match(mc, ml, a.chain, a.level, done);
                if (r == null)
                {
                    Tween.Move(v.rt, CellPos(from), 0.15f);
                    GM.Toast("Bu ikisi karışmaz! " + Chains.Name(mc, ml) + " için başka bir eş lazım.");
                    return;
                }
                pot.mix = "";
                pot.brew = r.id;
                pot.brewUntil = System.DateTime.UtcNow.Ticks + System.TimeSpan.FromSeconds(r.seconds).Ticks;
                pot.readyNotified = false;
                ConsumeInto(from, potCell);
                FX.Burst(fxLayer, CellPos(potCell), ProcArt.PotionColor(r.id), 12);
                GM.Toast(r.Name + " kaynamaya başladı! (" + r.seconds + " sn)", new Color(0.85f, 0.7f, 1f));
                GM.AddXp(1);
            }
            RefreshView(potCell);
            var pv = ViewAt(potCell);
            if (pv) Tween.Punch(pv.rt, 0.15f, 0.3f);
            Select(potCell);
            GM.OnBoardChanged();
        }

        void TapPot(Vector2Int c)
        {
            var pot = data[c.x, c.y];
            var v = ViewAt(c);
            if (pot.BrewReady) { CollectPotion(c); return; }
            if (v) Tween.Punch(v.rt, 0.1f, 0.2f);
            if (pot.brew == 0 && string.IsNullOrEmpty(pot.mix)) GM.Toast("Kazana iki malzeme sürükle, iksir kaynasın! Tarifler için kazanı seç.");
        }

        void CollectPotion(Vector2Int c)
        {
            var pot = data[c.x, c.y];
            if (!NearestEmpty(c, out var e)) { GM.Toast("Masa dolu! İksire yer aç."); return; }
            var r = Recipes.Get(pot.brew);
            pot.brew = 0;
            pot.brewUntil = 0;
            pot.readyNotified = false;
            SpawnFrom(c, e, new ItemData(Chains.Iksir, r != null ? r.id : 1));
            FX.Burst(fxLayer, CellPos(e), ProcArt.PotionColor(r != null ? r.id : 1), 12);
            RefreshView(c);
            GM.AddXp(2);
            Select(e);
            GM.OnBoardChanged();
        }

        /// <summary>Info-panel button for the selected cauldron.</summary>
        public void PotAction()
        {
            var c = Selected;
            var pot = Get(c);
            if (pot == null || !Chains.IsStation(pot.chain)) return;
            if (pot.BrewReady) { CollectPotion(c); return; }
            if (pot.Brewing)
            {
                int price = Recipes.SkipPrice(pot.brewUntil - System.DateTime.UtcNow.Ticks);
                if (!GM.TrySpendGold(price)) return;
                pot.brewUntil = System.DateTime.UtcNow.Ticks;
                RefreshView(c);
                CollectPotion(c);
                return;
            }
            if (!string.IsNullOrEmpty(pot.mix))
            {
                // take the waiting ingredient back out
                if (!NearestEmpty(c, out var e)) { GM.Toast("Masa dolu!"); return; }
                Recipes.ParseKey(pot.mix, out var mc, out var ml);
                pot.mix = "";
                SpawnFrom(c, e, new ItemData(mc, ml));
                RefreshView(c);
                Select(c);
                GM.OnBoardChanged();
                return;
            }
            GM.OpenRecipes();
        }

        /// <summary>Older saves have no cauldron yet: put one on the table.</summary>
        public void EnsurePot()
        {
            foreach (var d in AllCells()) if (data[d.x, d.y] != null && Chains.IsStation(data[d.x, d.y].chain)) return;
            var pref = new Vector2Int(3, 4);
            if (data[pref.x, pref.y] == null || NearestEmpty(pref, out pref))
                Set(pref, new ItemData(Chains.Simya, 1), true);
        }

        // ---------------------------------------------------------------- ghost bubbles
        float bubbleCheck;

        void TrySpawnBubble(Vector2Int from, ItemData merged)
        {
            if (!Chains.IsIngredient(merged.chain) || Random.value > Bubbles.Chance(merged.level)) return;
            if (!NearestEmpty(from, out var e)) return;
            var b = new ItemData(merged.chain, merged.level)
            {
                bubble = true,
                bubbleUntil = System.DateTime.UtcNow.Ticks + System.TimeSpan.FromSeconds(Bubbles.Seconds).Ticks
            };
            SpawnFromPos(CellPos(from), e, b);
            FX.Burst(fxLayer, CellPos(e), new Color(0.75f, 0.9f, 1f), 8);
            GM.Toast("Bir hayalet baloncuk belirdi! İçindeki " + b.Name + " " + (int)Bubbles.Seconds + " sn boyunca senin olabilir.", new Color(0.7f, 0.9f, 1f), 4f);
        }

        /// <summary>Test helper: force a bubble next to the middle of the table.</summary>
        public void DebugSpawnBubble()
        {
            TrySpawnBubbleForced(new Vector2Int(Cols / 2, Rows / 2), new ItemData(Chains.RandomIngredient(), 2));
        }

        void TrySpawnBubbleForced(Vector2Int from, ItemData merged)
        {
            if (!NearestEmpty(from, out var e)) return;
            var b = new ItemData(merged.chain, merged.level)
            {
                bubble = true,
                bubbleUntil = System.DateTime.UtcNow.Ticks + System.TimeSpan.FromSeconds(Bubbles.Seconds).Ticks
            };
            Set(e, b, true);
        }

        public void OpenBubble(Vector2Int c)
        {
            var d = Get(c);
            if (d == null || !d.bubble) return;
            if (!GM.TrySpendGold(Bubbles.Price(d.level))) return;
            d.bubble = false;
            RefreshView(c);
            var v = ViewAt(c);
            if (v) Tween.Pop(v.rt, 0.35f);
            FX.Burst(fxLayer, CellPos(c), new Color(0.75f, 0.9f, 1f), 12);
            GM.Toast(d.Name + " baloncuktan çıktı!");
            Select(c);
            GM.OnBoardChanged();
        }

        void Update()
        {
            if (!Ready) return;
            bubbleCheck -= Time.unscaledDeltaTime;
            if (bubbleCheck > 0f) return;
            bubbleCheck = 0.25f;
            long now = System.DateTime.UtcNow.Ticks;
            bool changed = false;
            foreach (var c in AllCells())
            {
                var pd = data[c.x, c.y];
                if (pd != null && pd.brew > 0 && !pd.readyNotified && pd.brewUntil <= now)
                {
                    pd.readyNotified = true;
                    RefreshView(c);
                    FX.Burst(fxLayer, CellPos(c), ProcArt.PotionColor(pd.brew), 10);
                    GM.Toast(Chains.Name(Chains.Iksir, pd.brew) + " hazır! Kazana dokun.", new Color(0.85f, 0.7f, 1f));
                }
            }
            foreach (var c in AllCells())
            {
                var d = data[c.x, c.y];
                if (d == null || !d.bubble || d.bubbleUntil > now) continue;
                FX.Burst(fxLayer, CellPos(c), new Color(0.75f, 0.9f, 1f), 10);
                bool sel = Selected == c;
                RemoveWithShrink(c);
                if (sel) Select(new Vector2Int(-1, -1));
                changed = true;
            }
            if (changed)
            {
                GM.Toast("Pof! Baloncuk uçup gitti.");
                GM.OnBoardChanged();
            }
            else
            {
                var sd = Get(Selected);
                if (sd != null && (sd.bubble || sd.brew > 0)) GM.ShowInfo(sd); // live countdown
            }
        }

        // ---------------------------------------------------------------- save
        public void WriteSave(List<CellSave> list)
        {
            list.Clear();
            foreach (var c in AllCells())
            {
                var d = data[c.x, c.y];
                if (d == null) continue;
                list.Add(new CellSave { x = c.x, y = c.y, chain = d.chain, level = d.level, nap = d.nap, cd = d.cd, bubble = d.bubble, bubbleUntil = d.bubbleUntil, mix = d.mix, brew = d.brew, brewUntil = d.brewUntil });
            }
        }

        public void ReadSave(List<CellSave> list)
        {
            ClearAll();
            foreach (var s in list)
            {
                var c = new Vector2Int(s.x, s.y);
                // cats used to live on the grid in the first prototype; they walk freely now
                if (!InBounds(c) || string.IsNullOrEmpty(s.chain) || s.chain == Chains.Kedi) continue;
                if (s.bubble && s.bubbleUntil <= System.DateTime.UtcNow.Ticks) continue; // popped while away
                var item = new ItemData(s.chain, s.level) { nap = s.nap, bubble = s.bubble, bubbleUntil = s.bubbleUntil, mix = s.mix ?? "", brew = s.brew, brewUntil = s.brewUntil };
                item.readyNotified = item.BrewReady; // finished while away: no extra toast on load
                Set(c, item);
            }
        }
    }

    /// <summary>Little particle bursts made of sparkle images.</summary>
    public static class FX
    {
        public static void Burst(RectTransform layer, Vector2 pos, Color color, int count = 7)
        {
            for (int i = 0; i < count; i++)
            {
                var img = UIKit.Img("Spark", layer, ProcArt.Sparkle, color);
                var rt = img.rectTransform;
                float size = Random.Range(22f, 40f);
                rt.At(pos, new Vector2(size, size));
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.value * 0.5f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float dist = Random.Range(70f, 120f);
                Tween.Run(Random.Range(0.4f, 0.6f), t =>
                {
                    if (!rt) return;
                    float e = Tween.OutCubic(t);
                    rt.anchoredPosition = pos + dir * dist * e;
                    rt.localScale = Vector3.one * (1f - t * 0.7f);
                    rt.localEulerAngles = new Vector3(0, 0, t * 180f);
                    var c = color; c.a = 1f - t; img.color = c;
                }, () => { if (rt) Object.Destroy(rt.gameObject); });
            }
        }
    }
}
