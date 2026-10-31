using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>The merge grid: data model, item views, moving, merging and spawning.</summary>
    public class BoardController : MonoBehaviour, IPointerClickHandler
    {
        public const int Cols = 7, Rows = 8;
        public const float CS = 140f;

        readonly ItemData[,] data = new ItemData[Cols, Rows];
        readonly ItemView[,] views = new ItemView[Cols, Rows];

        RectTransform grid, itemsLayer, fxLayer;
        Image selector;
        public Vector2Int Selected { get; private set; } = new Vector2Int(-1, -1);
        public bool Ready { get; private set; }

        GameManager GM => GameManager.I;

        // ---------------------------------------------------------------- build
        public void Build(RectTransform area)
        {
            var catcher = area.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);

            var frame = UIKit.Img("Frame", area, ProcArt.Rounded, new Color(0.17f, 0.11f, 0.25f, 0.95f), true);
            frame.rectTransform.At(Vector2.zero, new Vector2(Cols * CS + 30f, Rows * CS + 30f));

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
            fxLayer = UIKit.Rect("FX", grid).At(Vector2.zero, grid.sizeDelta);
            Ready = true;
        }

        // ---------------------------------------------------------------- geometry
        public static Vector2 CellPos(int c, int r) => new Vector2((c - (Cols - 1) / 2f) * CS, ((Rows - 1) / 2f - r) * CS);
        public static Vector2 CellPos(Vector2Int c) => CellPos(c.x, c.y);
        public static bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Cols && c.y < Rows;

        public bool ScreenToCell(Vector2 screen, out Vector2Int cell)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, screen, null, out var lp);
            cell = new Vector2Int(Mathf.FloorToInt(lp.x / CS + Cols / 2f), Mathf.FloorToInt(Rows / 2f - lp.y / CS));
            return InBounds(cell);
        }

        public Vector3 CellWorld(Vector2Int c) => grid.TransformPoint(CellPos(c));

        // ---------------------------------------------------------------- queries
        public ItemData Get(Vector2Int c) => InBounds(c) ? data[c.x, c.y] : null;
        public ItemView ViewAt(Vector2Int c) => InBounds(c) ? views[c.x, c.y] : null;
        public bool IsDragging(Vector2Int c) { var v = ViewAt(c); return v != null && v.IsHeld; }

        /// <summary>True while the player is touching or dragging any item.</summary>
        public bool AnyHeld
        {
            get
            {
                foreach (var v in itemsLayer.GetComponentsInChildren<ItemView>())
                    if (v.IsHeld) return true;
                return false;
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
                    if (d == null || d.nap || d.chain != req.chain || d.level != req.level) continue;
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
            Select(new Vector2Int(-1, -1));
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

        /// <summary>Spawn an item that flies out of a source cell into the destination.</summary>
        public void SpawnFrom(Vector2Int src, Vector2Int dst, ItemData item)
        {
            var v = Set(dst, item);
            v.rt.anchoredPosition = CellPos(src);
            v.rt.localScale = Vector3.one * 0.3f;
            Tween.Move(v.rt, CellPos(dst), 0.3f);
            var tr = v.rt;
            Tween.Run(0.3f, t => { if (tr) tr.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Tween.OutBack(t)); }, null, Tween.Key(tr, "s"));
        }

        /// <summary>A wild kitten leaps onto another item and falls asleep there.</summary>
        public void KittenJumpOnto(Vector2Int kitten, Vector2Int target)
        {
            var v = views[kitten.x, kitten.y];
            data[kitten.x, kitten.y] = null; views[kitten.x, kitten.y] = null;
            var t = data[target.x, target.y];
            if (t != null) t.nap = true;
            if (!v) { RefreshView(target); return; }
            v.rt.SetAsLastSibling();
            Tween.Move(v.rt, CellPos(target), 0.35f, () =>
            {
                if (v) Destroy(v.gameObject);
                RefreshView(target);
                var tv = ViewAt(target);
                if (tv) Tween.Punch(tv.rt);
            });
        }

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

        public void RefreshSelection() => Select(Selected);

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) Select(new Vector2Int(-1, -1));
        }

        // ---------------------------------------------------------------- input
        public void OnItemDropped(ItemView v, Vector2 screen)
        {
            var from = v.cell;
            var a = data[from.x, from.y];
            if (a == null || a.nap || !ScreenToCell(screen, out var to) || to == from)
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
            if (b.nap)
            {
                Tween.Move(v.rt, CellPos(from), 0.15f);
                GM.Toast("Orada bir yavru uyuyor! Önce dokunup okşa.");
                return;
            }
            if (a.chain == b.chain && a.level == b.level && !Chains.IsGenerator(a.chain))
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
            if (d.nap) { GM.Chaos.Pet(c); return; }
            if (Chains.IsGenerator(d.chain)) { UseGenerator(c); return; }
            if (d.IsKitten) { GM.Chaos.OnKittenTapped(c); return; }
            Tween.Punch(v.rt, 0.1f, 0.2f);
        }

        void UseGenerator(Vector2Int c)
        {
            var gen = data[c.x, c.y];
            var v = ViewAt(c);
            if (!NearestEmpty(c, out var e))
            {
                GM.Toast("Tahta dolu! Birleştir ya da sat.");
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
            if (nd.IsKitten) nd.cd = nd.level == 3 ? KittenChaos.MasterInterval : 0f;
            Set(to, nd, true);
            FX.Burst(fxLayer, CellPos(to), ProcArt.ChainColor(nd.chain), nd.level >= 4 ? 12 : 7);
            Select(to);

            if (nd.IsKitten)
                GM.Toast(nd.level == 2
                    ? "Yavrular birleşti: Çırak Kedi! Dokununca sana malzeme getirir."
                    : "Usta Kedi geldi! Kendiliğinden malzeme getirir, yavruları da sakinleştirir.", UIKit.Orange);
            else if (nd.level == Chains.MaxLevel(nd.chain))
                GM.Toast("Harika! " + nd.Name + " - en üst seviye!", UIKit.Orange);

            GM.OnBoardChanged();
        }

        public void SellSelected()
        {
            var c = Selected;
            var d = Get(c);
            if (d == null || d.nap) return;
            if (Chains.IsIngredient(d.chain))
            {
                GM.AddGold(d.level);
                GM.Toast(d.Name + " satıldı: +" + d.level + " altın");
            }
            else if (d.IsWildKitten)
            {
                GM.AddGold(3);
                GM.Toast("Yavru yeni ailesine kavuştu! +3 altın");
            }
            else return;
            RemoveWithShrink(c);
            Select(new Vector2Int(-1, -1));
            GM.OnBoardChanged();
        }

        // ---------------------------------------------------------------- save
        public void WriteSave(List<CellSave> list)
        {
            list.Clear();
            foreach (var c in AllCells())
            {
                var d = data[c.x, c.y];
                if (d == null) continue;
                list.Add(new CellSave { x = c.x, y = c.y, chain = d.chain, level = d.level, nap = d.nap, cd = d.cd });
            }
        }

        public void ReadSave(List<CellSave> list)
        {
            ClearAll();
            foreach (var s in list)
            {
                var c = new Vector2Int(s.x, s.y);
                if (!InBounds(c) || string.IsNullOrEmpty(s.chain)) continue;
                Set(c, new ItemData(s.chain, s.level) { nap = s.nap, cd = s.cd });
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
