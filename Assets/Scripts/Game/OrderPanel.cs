using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Three customers (ghosts and fantasy creatures) asking for ingredients; each order makes them a better friend.</summary>
    public class OrderPanel : MonoBehaviour
    {
        const int SlotCount = 3;

        class Card
        {
            public RectTransform root;
            public CanvasGroup group;
            public Image ghost;
            public Image fog, glow, heart;
            public Text name, heartText;
            public RectTransform[] tiles = new RectTransform[2];
            public Image[] icons = new Image[2];
            public Image[] checks = new Image[2];
            public Button give;
            public GameObject stampBadge;
            public Text stampText;
            public Image giveImg;
            public Text giveText;
            public Button make;          // L.T("Yaptır"): a Mini Apprentice fetches the potion's ingredients
            public Image makeImg;
            public Text makeText;
        }

        readonly Card[] cards = new Card[SlotCount];
        readonly OrderSave[] orders = new OrderSave[SlotCount];
        readonly bool[] ready = new bool[SlotCount];

        GameManager GM => GameManager.I;

        public void Build(RectTransform area)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                int slot = i;
                var card = new Card();
                var bg = UIKit.Img("Order" + i, area, ProcArt.Rounded, UIKit.Panel, true);
                card.root = bg.rectTransform;
                card.root.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((i - 1) * 345f, 0f), new Vector2(330f, 395f));
                card.group = bg.gameObject.AddComponent<CanvasGroup>();

                // golden glow behind a best friend
                card.glow = UIKit.Img("Glow", card.root, ProcArt.Sparkle, new Color(1f, 0.85f, 0.35f, 0.8f));
                card.glow.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -4f), new Vector2(150f, 150f));
                card.ghost = UIKit.Img("Ghost", card.root, ProcArt.Ghost(0), Color.white);
                card.ghost.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -8f), new Vector2(124f, 124f));
                card.ghost.preserveAspect = true;
                // tap the customer: how good friends you are
                card.ghost.raycastTarget = true;
                var cb = card.ghost.gameObject.AddComponent<Button>();
                cb.transition = Selectable.Transition.None;
                cb.onClick.AddListener(() => ShowFriendInfo(slot));
                card.fog = UIKit.Img("Fog", card.ghost.transform, ProcArt.Fog, Color.white);
                card.fog.rectTransform.At(new Vector2(0f, -34f), new Vector2(150f, 80f));
                card.fog.raycastTarget = false;
                // friendship heart, top left
                card.heart = UIKit.Img("Heart", card.root, ProcArt.Heart, Color.white);
                card.heart.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(36f, -36f), new Vector2(56f, 56f));
                card.heartText = UIKit.Txt("N", card.heart.transform, "0", 26, Color.white);
                card.heartText.fontStyle = FontStyle.Bold;
                card.heartText.rectTransform.At(new Vector2(0f, 3f), new Vector2(50f, 34f));

                card.name = UIKit.Txt("Name", card.root, "", 28, UIKit.Cream);
                card.name.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -126f), new Vector2(320f, 36f));

                for (int k = 0; k < 2; k++)
                {
                    var tile = UIKit.Img("Req" + k, card.root, ProcArt.Rounded, UIKit.PanelDark, true);
                    card.tiles[k] = tile.rectTransform;
                    // tap what the customer wants: its info sheet opens (chain, where it comes from)
                    tile.raycastTarget = true;
                    var tb = tile.gameObject.AddComponent<Button>();
                    tb.transition = Selectable.Transition.None;
                    int kk = k;
                    tb.onClick.AddListener(() => ShowReqInfo(slot, kk));
                    card.tiles[k].Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -166f), new Vector2(104f, 104f));
                    card.icons[k] = UIKit.Img("Icon", tile.transform, null, Color.white);
                    card.icons[k].rectTransform.sizeDelta = new Vector2(92f, 92f);
                    card.icons[k].preserveAspect = true;
                    card.checks[k] = UIKit.Img("Check", tile.transform, ProcArt.Check, Color.white);
                    card.checks[k].rectTransform.At(new Vector2(38f, 38f), new Vector2(40f, 40f));
                }

                var stamp = UIKit.Img("Stamp", card.root, ProcArt.Stamp, Color.white);
                stamp.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-38f, -38f), new Vector2(58f, 58f));
                card.stampBadge = stamp.gameObject;
                card.stampText = UIKit.Txt("N", stamp.transform, "1", 26, UIKit.Cream);
                card.stampText.rectTransform.At(new Vector2(18f, -18f), new Vector2(40f, 34f));

                card.make = UIKit.Btn("Make", card.root, L.T("Yaptır"), UIKit.Green, () => MakePotion(slot), 26);
                ((RectTransform)card.make.transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -250f), new Vector2(150f, 54f));
                card.makeImg = card.make.GetComponent<Image>();
                card.makeText = card.make.Label();
                card.make.gameObject.SetActive(false);

                card.give = UIKit.Btn("Give", card.root, L.T("Ver"), UIKit.Disabled, () => Deliver(slot), 34);
                card.giveImg = card.give.GetComponent<Image>();
                card.giveText = card.give.Label();
                ((RectTransform)card.give.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 14f), new Vector2(296f, 84f));

                cards[i] = card;
                ShowCard(i);
            }
        }

        float makeTick;

        /// <summary>The first visible "Hazırla"/"Yaptır" button (for the star Mini Apprentice hint), or null.</summary>
        public RectTransform FirstMakeButton()
        {
            for (int i = 0; i < SlotCount; i++)
                if (cards[i] != null && cards[i].make && cards[i].make.gameObject.activeInHierarchy) return (RectTransform)cards[i].make.transform;
            return null;
        }

        bool countessIntro;   // the Countess introduced herself (checked until she first comes)

        /// <summary>The first night the Countess is among the orders, she introduces herself.</summary>
        void CountessIntro()
        {
            if (countessIntro || GM == null || !GM.OnBoard || GM.ModalOpen) return;
            for (int i = 0; i < SlotCount; i++)
            {
                if (orders[i] == null || orders[i].cust != Customers.NightOwl) continue;
                countessIntro = true;
                if (!GM.FirstTime("intro_kontes")) return;
                var c = Customers.Get(Customers.NightOwl);
                GM.ShowDialog(c.name, L.T("İyi geceler, genç simyacı... Ben Yarasa Kontes. Yalnızca geceleri, 20:00 ile 06:00 arasında uğrarım. Siparişlerim zordur ama karşılığında 1,5 kat altın ve fazladan Pati Mührü öderim."));
                return;
            }
        }

        void Update()
        {
            float t = Time.time;
            // the Mini Apprentice comes, goes and gets busy without the board changing: check now and then
            makeTick -= Time.unscaledDeltaTime;
            if (makeTick <= 0f) { makeTick = 0.5f; for (int i = 0; i < SlotCount; i++) RefreshMake(i); CountessIntro(); }
            for (int i = 0; i < SlotCount; i++)
            {
                var g = cards[i]?.ghost;
                if (g) g.rectTransform.anchoredPosition = new Vector2(0f, -8f + Mathf.Sin(t * 2f + i * 1.7f) * 6f);
                var f = cards[i]?.fog;
                if (f && f.gameObject.activeSelf) f.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 0.7f + i) * 8f, -34f);
                var gl = cards[i]?.glow;
                if (gl && gl.gameObject.activeSelf) gl.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 20f);
                if (ready[i] && cards[i].give)
                    cards[i].give.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.03f);
            }
        }

        /// <summary>The potion this order wants (recipe), or null for an ingredient order.</summary>
        Recipe WantedPotion(int slot)
        {
            var o = orders[slot];
            if (o == null) return null;
            foreach (var q in o.reqs) if (Chains.IsPotion(q.chain)) return Recipes.Get(q.level);
            return null;
        }

        void RefreshMake(int i)
        {
            var card = cards[i];
            if (card == null || card.make == null || GM == null || GM.Cats == null) return;
            var r = WantedPotion(i);
            // star Mini Apprentice: "Hazırla" does the whole order (potion and ingredients);
            // a plain one: "Yaptır" only brings a missing potion's ingredients to the pot
            bool potionMissing = r != null && Missing(i, true);
            bool star = GM.Cats.HasStarMini;
            bool ingredientMissing = Missing(i, false);
            bool show = !ready[i] && (star ? potionMissing || ingredientMissing : potionMissing && GM.Cats.HasMini);
            if (card.make.gameObject.activeSelf != show) card.make.gameObject.SetActive(show);
            if (!show) return;
            // busy: the Mini is working, or the pot is in use and there is nothing else to prepare meanwhile
            bool busy = GM.Cats.MiniBusy || (potionMissing && GM.Cats.PotBusy && !(star && ingredientMissing));
            card.makeImg.color = busy ? UIKit.Disabled : UIKit.Green;
            card.makeText.text = busy ? L.T("Meşgul") : star ? L.T("Hazırla") : L.T("Yaptır");
        }

        /// <summary>Is a potion (potion = true) or an ingredient (false) of this order still missing from the table?</summary>
        bool Missing(int slot, bool potion)
        {
            var o = orders[slot];
            var card = cards[slot];
            if (o == null || card == null) return false;
            for (int k = 0; k < o.reqs.Count && k < card.checks.Length; k++)
            {
                if (Chains.IsPotion(o.reqs[k].chain) != potion) continue;
                if (card.checks[k] && !card.checks[k].gameObject.activeSelf) return true;
            }
            return false;
        }

        void MakePotion(int slot)
        {
            var r = WantedPotion(slot);
            var potion = r != null && Missing(slot, true) ? r : null;   // brew only when it is not on the table yet
            Tween.Punch(cards[slot].make.transform, 0.1f, 0.2f);
            if (GM.Cats.HasStarMini) GM.Cats.MiniOrder(orders[slot], potion);
            else if (potion != null) GM.Cats.MiniBrew(potion);
            RefreshMake(slot);
        }

        void ShowFriendInfo(int slot)
        {
            var o = orders[slot];
            var c = o != null ? Customers.Get(o.cust) : null;
            if (c == null) return;
            Tween.Punch(cards[slot].ghost.rectTransform, 0.1f, 0.25f);
            Sfx.Play("click");
            int st = GM.FriendStage(c), pts = GM.FriendPoints(c);
            string who = st == 0 ? L.T("Sisin içindeki yabancı") : c.name;
            if (st >= c.MaxStage) { GM.Toast(L.F("{0} artık {1}! Siparişleri +1 Pati Mührü verir.", who, Customers.BestFriend), UIKit.Orange, 4f); return; }
            int need = c.PointsFor(st + 1) - pts;
            GM.Toast(who + " · " + Customers.StageName(c, st) + " (" + st + "/" + c.MaxStage + ", " + c.difficulty + ")\n"
                + L.F("{0} olmak için {1} sipariş puanı daha. İksir 2 sayar.", Customers.StageName(c, st + 1), need), UIKit.Cream, 4f);
        }

        void ShowReqInfo(int slot, int k)
        {
            var o = orders[slot];
            if (o == null || k >= o.reqs.Count) return;
            var q = o.reqs[k];
            Tween.Punch(cards[slot].tiles[k], 0.1f, 0.2f);
            GM.OpenItemInfo(new ItemData(q.chain, q.level));
        }

        // ------------------------------------------------------------ tutorial helpers
        public RectTransform ReadyGiveButton()
        {
            for (int i = 0; i < SlotCount; i++)
                if (ready[i] && orders[i] != null) return (RectTransform)cards[i].give.transform;
            return null;
        }

        public RectTransform FirstReqTile()
        {
            for (int i = 0; i < SlotCount; i++)
                if (orders[i] != null) return cards[i].tiles[0];
            return null;
        }

        // ------------------------------------------------------------ orders
        OrderSave Generate()
        {
            int served = GM.Served;
            int maxLvl = Mathf.Clamp(2 + served / 3, 2, 5);
            int minLvl = Mathf.Max(1, maxLvl - 2);
            int count = (served >= 2 && Random.value < 0.45f) ? 2 : 1;

            // only customers that already visit at the player's level, and not one already waiting
            var used = new HashSet<string>();
            foreach (var o in orders) if (o != null && o.cust != null) used.Add(o.cust);
            var pool = new List<CustomerDef>();
            foreach (var c in Customers.All) if (Customers.Visits(c, GM.Level) && !used.Contains(c.id)) pool.Add(c);
            if (pool.Count == 0) foreach (var c in Customers.All) if (Customers.Visits(c, GM.Level)) pool.Add(c);
            var cust = pool[Random.Range(0, pool.Count)];
            // at night the Countess drops in more often than the others
            var countess = pool.Find(c => c.id == Customers.NightOwl);
            if (countess != null && Random.value < 0.3f) cust = countess;

            var order = new OrderSave
            {
                cust = cust.id,
                ghost = cust.name,
                tint = 0,
                line = Random.Range(0, ThankLines.Length)
            };
            // the Countess always wants a potion and a fine ingredient (and pays for it, see Reward)
            int unlocked = Recipes.UnlockedCount(GM.TasksDone);
            if (cust.id == Customers.NightOwl)
            {
                if (unlocked > 0)
                {
                    var open = new List<int>();
                    foreach (var r in Recipes.All) if (Recipes.Unlocked(r, GM.TasksDone)) open.Add(r.id);
                    order.reqs.Add(new ReqSave { chain = Chains.Iksir, level = open[Random.Range(0, open.Count)] });
                }
                order.reqs.Add(new ReqSave { chain = Chains.RandomIngredient(), level = Random.Range(4, 6) });
                return order;
            }
            // sometimes a ghost wants a potion instead (only recipes the player has unlocked)
            if (served >= 4 && unlocked > 0 && Random.value < 0.25f)
            {
                var open = new List<int>();
                foreach (var r in Recipes.All) if (Recipes.Unlocked(r, GM.TasksDone)) open.Add(r.id);
                order.reqs.Add(new ReqSave { chain = Chains.Iksir, level = open[Random.Range(0, open.Count)] });
                return order;
            }
            for (int i = 0; i < count; i++)
                order.reqs.Add(new ReqSave { chain = Chains.RandomIngredient(), level = Random.Range(minLvl, maxLvl + 1) });
            return order;
        }

        static readonly TrList ThankLines = new TrList
        (
            L.N("Tam aradığım şey! Teşekkürler!"),
            L.N("Bu dükkânı çok seviyorum."),
            L.N("Kara Pati'ye selamlar!"),
            L.N("Harika, yine geleceğim!"),
            L.N("Mis gibi kokuyor. Sağ ol!"),
            L.N("Bunu bulmak ne zordu, iyi ki varsın.")
        );

        /// <summary>Friendship points for an order: one per item, a potion counts double.</summary>
        static int FriendPointsOf(OrderSave o)
        {
            int n = 0;
            foreach (var q in o.reqs) n += Chains.IsPotion(q.chain) ? 2 : 1;
            return n;
        }

        static bool IsBestFriend(OrderSave o)
        {
            var c = o != null ? Customers.Get(o.cust) : null;
            return c != null && GameManager.I != null && GameManager.I.IsBestFriend(c);
        }

        static int Reward(OrderSave o)
        {
            int r = 0;
            foreach (var q in o.reqs)
            {
                if (Chains.IsPotion(q.chain)) { var rc = Recipes.Get(q.level); r += rc != null ? rc.Gold : 10; }
                else r += Chains.Value(q.level);
            }
            // a well-fed Kara Pati charms the customers
            var gm = GameManager.I;
            if (gm != null && gm.CatHappy) r = Mathf.RoundToInt(r * (1f + Tower.HappyBonus));
            if (o.reqs.Count > 1) r += 5;
            if (o.cust == Customers.NightOwl) r = Mathf.RoundToInt(r * 1.5f);   // the Countess pays half as much again
            return r;
        }

        // harder ingredients pay more stamps, so later chapters don't turn into a grind
        static int StampReward(OrderSave o)
        {
            int r = 0;
            foreach (var q in o.reqs) r += Chains.IsPotion(q.chain) ? Recipes.PotionStamps : q.level >= 4 ? 2 : 1;
            if (IsBestFriend(o)) r += 1;   // a best friend tips one extra stamp
            if (o.cust == Customers.NightOwl) r += 2;
            return r;
        }

        public void FillEmpty()
        {
            for (int i = 0; i < SlotCount; i++)
                if (orders[i] == null) { orders[i] = Generate(); ShowCard(i); }
            Refresh();
        }

        void ShowCard(int i)
        {
            var card = cards[i];
            var o = orders[i];
            bool has = o != null;
            card.ghost.gameObject.SetActive(has);
            card.heart.gameObject.SetActive(has);
            card.glow.gameObject.SetActive(false);
            card.name.text = has ? o.ghost : "...";
            card.give.gameObject.SetActive(has);
            card.stampBadge.SetActive(has);
            for (int k = 0; k < 2; k++)
            {
                bool on = has && k < o.reqs.Count;
                card.tiles[k].gameObject.SetActive(on);
                if (!on) continue;
                var q = o.reqs[k];
                card.icons[k].sprite = ProcArt.Item(q.chain, q.level);
                float x = o.reqs.Count == 1 ? 0f : (k == 0 ? -58f : 58f);
                card.tiles[k].anchoredPosition = new Vector2(x, -166f);
            }
            if (has)
            {
                var c = Customers.Get(o.cust);
                int st = c != null ? GM.FriendStage(c) : 0;
                bool best = c != null && st >= c.MaxStage;
                if (c == null) card.ghost.sprite = ProcArt.Ghost(o.tint);
                else if (st == 0) card.ghost.sprite = ProcArt.CustomerSilhouette(c.id);
                else card.ghost.sprite = ProcArt.Customer(c.id, st >= c.FullLookStage);
                // the fog thins out stage by stage
                float fog = c == null ? 0f : st == 0 ? 0.6f : st == 1 ? 0.4f : st == 2 && c.MaxStage > 3 ? 0.25f : 0f;
                card.fog.gameObject.SetActive(fog > 0f);
                card.fog.color = new Color(1f, 1f, 1f, fog);
                card.ghost.color = Color.white;
                card.name.text = c == null ? o.ghost : st == 0 ? "???" : c.name;
                card.heart.color = Color.white;
                card.heartText.text = st.ToString();
                card.glow.gameObject.SetActive(best);
                card.giveText.text = L.F("Ver  +{0}", Reward(o));
                card.stampText.text = StampReward(o).ToString();
            }
        }

        public void Refresh()
        {
            if (GM == null || GM.Board == null) return;
            var needed = new HashSet<Vector2Int>();
            for (int i = 0; i < SlotCount; i++)
            {
                var o = orders[i];
                var card = cards[i];
                if (o == null) { ready[i] = false; continue; }
                var per = new List<bool>();
                var found = new List<Vector2Int>();
                ready[i] = GM.Board.TryCollect(o.reqs, found, per);
                foreach (var c in found) needed.Add(c);
                for (int k = 0; k < o.reqs.Count && k < 2; k++)
                {
                    card.checks[k].gameObject.SetActive(per[k]);
                    card.icons[k].color = per[k] ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                }
                card.giveImg.color = ready[i] ? UIKit.Green : UIKit.Disabled;
                card.give.interactable = ready[i];
                if (!ready[i]) card.give.transform.localScale = Vector3.one;
            }
            GM.Board.MarkNeeded(needed);
        }

        void Deliver(int i)
        {
            var o = orders[i];
            if (o == null) return;
            var cells = new List<Vector2Int>();
            if (!GM.Board.TryCollect(o.reqs, cells, null)) { Refresh(); return; }

            var card = cards[i];
            Vector3 target = card.root.TransformPoint(new Vector3(0f, -60f, 0f));
            foreach (var c in cells) GM.Board.FlyAway(c, target);
            GM.Tut?.Notify("deliver");

            int reward = Reward(o);
            Sfx.Play("coin");
            GM.AddGold(reward);
            GM.AddStamps(StampReward(o));
            GM.AddPumpkins(o.reqs.Count + (o.reqs.Count > 1 ? 1 : 0));
            GM.RegisterServed();
            if (o.cust == Customers.NightOwl) GM.AddStat("kontes");
            var cust = Customers.Get(o.cust);
            string who = cust != null && GM.FriendStage(cust) == 0 ? "???" : o.ghost;
            GM.Toast(who + ": \"" + ThankLines[o.line % ThankLines.Length] + "\"", UIKit.Cream, 4f);

            GM.OnOrderDelivered(Random.Range(0, Ghosts.Names.Length));   // the old ghost sticker set stays collectable
            if (cust != null) GM.AddFriendship(cust.id, FriendPointsOf(o));
            orders[i] = null;
            ready[i] = false;
            Tween.Punch(card.root, 0.08f, 0.3f);
            StartCoroutine(NextCustomer(i));
            GM.OnBoardChanged();
        }

        IEnumerator NextCustomer(int i)
        {
            var card = cards[i];
            yield return new WaitForSeconds(0.45f);
            float t = 0f;
            while (t < 0.25f) { t += Time.deltaTime; card.group.alpha = 1f - t / 0.25f; yield return null; }
            ShowCard(i);
            yield return new WaitForSeconds(0.6f);
            orders[i] = Generate();
            ShowCard(i);
            card.group.alpha = 1f;
            Tween.Pop(card.root, 0.35f);
            Refresh();
            GM.MarkDirty();
        }

        /// <summary>Test helper: the first customer asks for a potion (the first recipe) instead.</summary>
        public void DebugPotionOrder()
        {
            if (orders[0] == null) return;
            orders[0].reqs.Clear();
            orders[0].reqs.Add(new ReqSave { chain = Chains.Iksir, level = 1 });
            ShowCard(0);
            Refresh();
            RefreshMake(0);
        }

        /// <summary>Test helper: friendship points for the first customer waiting, then redraw the cards.</summary>
        public void DebugFriendship(int pts)
        {
            foreach (var o in orders)
                if (o != null && Customers.Get(o.cust) != null) { GM.AddFriendship(o.cust, pts); break; }
            for (int i = 0; i < SlotCount; i++) ShowCard(i);
        }

        // ------------------------------------------------------------ save
        public void WriteSave(List<OrderSave> list)
        {
            list.Clear();
            foreach (var o in orders) if (o != null) list.Add(o);
        }

        public void ReadSave(List<OrderSave> list)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                orders[i] = (list != null && i < list.Count && list[i].reqs != null && list[i].reqs.Count > 0) ? list[i] : null;
                // orders from before the customers update: give them to a customer who visits now
                if (orders[i] != null && Customers.Get(orders[i].cust) == null)
                {
                    var c = Customers.ByName(orders[i].ghost);
                    if (c == null || c.unlockLevel > GM.Level) c = Customers.All[i % 3];
                    orders[i].cust = c.id;
                    orders[i].ghost = c.name;
                }
                cards[i].group.alpha = 1f;
                ShowCard(i);
            }
            FillEmpty();
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            for (int i = 0; i < SlotCount; i++) { orders[i] = null; cards[i].group.alpha = 1f; ShowCard(i); }
        }
    }
}
