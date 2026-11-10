using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Best friends ("Can Dostu") visiting the Witch's Tower: up to 3 a day, a different mix each day, each on one of
    /// the open floors, drifting slowly from side to side. Tap one to get its daily gift (a little gift box floats
    /// over the ones that still have it).
    /// </summary>
    public class TowerFriends : MonoBehaviour
    {
        public const int MaxVisitors = 3;
        const float FloorLine = -290f, MinX = -400f, MaxX = 400f;

        class Visitor
        {
            public CustomerDef c;
            public RectTransform rt, gift;
            public float x, dir, speed, phase, h;
            public bool hangs;   // the Countess hangs from the ceiling instead of walking
        }

        GameManager GM => GameManager.I;
        TowerScreen tower;
        readonly List<Visitor> visitors = new List<Visitor>();
        string shownKey = "";

        public void Init(TowerScreen t) { tower = t; }

        /// <summary>Called whenever the tower refreshes: rebuild only when today's guests or the open floors changed.</summary>
        public void Refresh()
        {
            if (!tower || GM == null) return;
            var guests = GM.BestFriendsToday(MaxVisitors);
            int open = Mathf.Max(1, GM.TowerFloorsOpen);
            string key = DailyRewards.Today + "|" + open;
            foreach (var c in guests) key += "|" + c.id;
            if (key != shownKey)
            {
                shownKey = key;
                Rebuild(guests, open);
            }
            foreach (var v in visitors) v.gift.gameObject.SetActive(GM.FriendGiftReady(v.c));
        }

        void Rebuild(List<CustomerDef> guests, int open)
        {
            foreach (var v in visitors) if (v.rt) Destroy(v.rt.gameObject);
            visitors.Clear();
            var rng = new System.Random(DailyRewards.Today * 31 + guests.Count);
            for (int k = 0; k < guests.Count; k++)
            {
                var c = guests[k];
                int floor = rng.Next(open);
                var layer = tower.FriendLayer(floor);
                if (!layer) continue;
                float h = Mathf.Lerp(150f, 300f, Mathf.InverseLerp(0.4f, 1.4f, c.size));   // the tower keeps everyone's real size
                var img = UIKit.Img("Friend_" + c.id, layer, ProcArt.Customer(c.id, true), Color.white);
                img.preserveAspect = true;
                img.raycastTarget = true;
                var rt = img.rectTransform;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(h, h);
                var v = new Visitor
                {
                    c = c, rt = rt, h = h,
                    x = Mathf.Lerp(MinX, MaxX, (float)rng.NextDouble()),
                    dir = rng.Next(2) == 0 ? -1f : 1f,
                    speed = 25f + (float)rng.NextDouble() * 25f,
                    phase = (float)rng.NextDouble() * 6f,
                    hangs = c.id == Customers.NightOwl,
                };
                if (v.hangs) v.x = floor % 2 == 0 ? 250f : -250f;   // away from the floor's name plaque
                var giftImg = UIKit.Img("Gift", rt, ProcArt.Item(Chains.Sandik, 1), Color.white);
                giftImg.raycastTarget = false;
                v.gift = giftImg.rectTransform;
                v.gift.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 10f), new Vector2(80f, 80f));
                var b = img.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Tap(v));
                visitors.Add(v);
            }
        }

        void Tap(Visitor v)
        {
            if (!GM.FriendGiftReady(v.c))
            {
                GM.Toast(L.F("{0}: \"Yarın yine uğrarım!\"", v.c.name));
                Tween.Punch(v.rt, 0.08f, 0.25f);
                return;
            }
            string what = GM.TakeFriendGift(v.c);
            v.gift.gameObject.SetActive(false);
            Tween.Punch(v.rt, 0.15f, 0.35f);
            GM.ShowDialog(v.c.name, Customers.GiftLine(v.c.id) + "\n\n" + L.F("Hediye: {0}", what));
        }

        void Update()
        {
            if (visitors.Count == 0) return;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            foreach (var v in visitors)
            {
                if (!v.rt) continue;
                if (v.hangs)
                {
                    // upside down from the ceiling, swaying a little; the gift box stays the right way up
                    v.rt.anchoredPosition = new Vector2(v.x, TowerScreen.FloorH / 2f - 40f);
                    v.rt.localScale = new Vector3(1f, -1f, 1f);
                    v.rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.2f + v.phase) * 7f);
                    v.gift.localScale = new Vector3(1f, -1f, 1f) * (1f + Mathf.Sin(t * 4f + v.phase) * 0.08f);
                    continue;
                }
                // drift along the floor, turn around at the walls, float a little (ghosts float more)
                v.x += v.dir * v.speed * dt;
                if (v.x > MaxX) { v.x = MaxX; v.dir = -1f; }
                else if (v.x < MinX) { v.x = MinX; v.dir = 1f; }
                float bob = Mathf.Sin(t * 1.6f + v.phase) * (v.c.ghost ? 14f : 4f) + (v.c.ghost ? 30f : 0f);
                v.rt.anchoredPosition = new Vector2(v.x, FloorLine + bob);
                v.rt.localScale = new Vector3(v.dir, 1f, 1f);
                v.gift.localScale = new Vector3(v.dir, 1f, 1f) * (1f + Mathf.Sin(t * 4f + v.phase) * 0.08f);   // the gift box doesn't mirror
            }
        }
    }
}
