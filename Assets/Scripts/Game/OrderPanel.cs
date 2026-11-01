using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Three ghost customers asking for potion ingredients.</summary>
    public class OrderPanel : MonoBehaviour
    {
        const int SlotCount = 3;

        class Card
        {
            public RectTransform root;
            public CanvasGroup group;
            public Image ghost;
            public Text name;
            public RectTransform[] tiles = new RectTransform[2];
            public Image[] icons = new Image[2];
            public Image[] checks = new Image[2];
            public Button give;
            public GameObject stampBadge;
            public Text stampText;
            public Image giveImg;
            public Text giveText;
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

                card.ghost = UIKit.Img("Ghost", card.root, ProcArt.Ghost(0), Color.white);
                card.ghost.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -8f), new Vector2(118f, 118f));

                card.name = UIKit.Txt("Name", card.root, "", 28, UIKit.Cream);
                card.name.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -126f), new Vector2(320f, 36f));

                for (int k = 0; k < 2; k++)
                {
                    var tile = UIKit.Img("Req" + k, card.root, ProcArt.Rounded, UIKit.PanelDark, true);
                    card.tiles[k] = tile.rectTransform;
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

                card.give = UIKit.Btn("Give", card.root, "Ver", UIKit.Disabled, () => Deliver(slot), 34);
                card.giveImg = card.give.GetComponent<Image>();
                card.giveText = card.give.Label();
                ((RectTransform)card.give.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 14f), new Vector2(296f, 84f));

                cards[i] = card;
                ShowCard(i);
            }
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < SlotCount; i++)
            {
                var g = cards[i]?.ghost;
                if (g) g.rectTransform.anchoredPosition = new Vector2(0f, -8f + Mathf.Sin(t * 2f + i * 1.7f) * 6f);
                if (ready[i] && cards[i].give)
                    cards[i].give.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.03f);
            }
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

            var used = new HashSet<string>();
            foreach (var o in orders) if (o != null) used.Add(o.ghost);
            int gi;
            int guard = 0;
            do { gi = Random.Range(0, Ghosts.Names.Length); } while (used.Contains(Ghosts.Names[gi]) && guard++ < 20);

            var order = new OrderSave
            {
                ghost = Ghosts.Names[gi],
                tint = gi,
                line = Random.Range(0, Ghosts.Lines.Length)
            };
            // sometimes a ghost wants a potion instead (only recipes the player has unlocked)
            int unlocked = Recipes.UnlockedCount(GM.TasksDone);
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

        static int Reward(OrderSave o)
        {
            int r = 0;
            foreach (var q in o.reqs)
            {
                if (Chains.IsPotion(q.chain)) { var rc = Recipes.Get(q.level); r += rc != null ? rc.Gold : 10; }
                else r += Chains.Value(q.level);
            }
            if (o.reqs.Count > 1) r += 5;
            return r;
        }

        // harder ingredients pay more stamps, so later chapters don't turn into a grind
        static int StampReward(OrderSave o)
        {
            int r = 0;
            foreach (var q in o.reqs) r += Chains.IsPotion(q.chain) ? Recipes.PotionStamps : q.level >= 4 ? 2 : 1;
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
                card.ghost.sprite = ProcArt.Ghost(o.tint);
                card.giveText.text = "Ver  +" + Reward(o);
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
            GM.AddGold(reward);
            GM.AddStamps(StampReward(o));
            GM.AddPumpkins(o.reqs.Count + (o.reqs.Count > 1 ? 1 : 0));
            GM.RegisterServed();
            GM.Toast(o.ghost + ": \"" + Ghosts.Lines[o.line % Ghosts.Lines.Length] + "\"", UIKit.Cream, 4f);

            GM.OnOrderDelivered(Mathf.Max(0, System.Array.IndexOf(Ghosts.Names, o.ghost)));
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
