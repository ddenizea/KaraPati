using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Visual + input for one item on the board.</summary>
    public class ItemView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public BoardController board;
        public Vector2Int cell;
        public ItemData data;
        public RectTransform rt;
        public bool IsDragging { get; private set; }
        public bool IsPressed { get; private set; }
        public bool IsHeld => IsDragging || IsPressed;

        Image icon, cooldown, napCat, badge, needed, bubble, bubbleRing, potExtra, doorCat, tripChest;
        Text badgeText, zzz, potTimer;
        CanvasGroup cg;
        Vector2 dragOffset;
        Transform homeParent;
        float seed;

        public void Init(BoardController b, Vector2Int c, ItemData d)
        {
            board = b; cell = c; data = d;
            rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(BoardController.CS, BoardController.CS);
            seed = Random.value * 10f;

            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            cg = gameObject.AddComponent<CanvasGroup>();

            icon = UIKit.Img("Icon", rt, null, Color.white);
            icon.rectTransform.sizeDelta = new Vector2(122f, 122f);
            icon.preserveAspect = true;

            cooldown = UIKit.Img("Cooldown", rt, ProcArt.Circle, new Color(0.06f, 0.02f, 0.1f, 0.55f));
            cooldown.rectTransform.sizeDelta = new Vector2(112f, 112f);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;

            badge = UIKit.Img("Badge", rt, ProcArt.Circle, new Color(0.14f, 0.09f, 0.2f, 0.92f));
            badge.rectTransform.At(new Vector2(46f, -46f), new Vector2(40f, 40f));
            badgeText = UIKit.Txt("Lv", badge.transform, "1", 24, UIKit.Cream, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(badgeText.rectTransform);

            bubble = UIKit.Img("Bubble", rt, ProcArt.Bubble, Color.white);
            bubble.rectTransform.sizeDelta = new Vector2(138f, 138f);
            bubbleRing = UIKit.Img("BubbleTimer", rt, ProcArt.Circle, new Color(1f, 1f, 1f, 0.22f));
            bubbleRing.rectTransform.sizeDelta = new Vector2(128f, 128f);
            bubbleRing.type = Image.Type.Filled;
            bubbleRing.fillMethod = Image.FillMethod.Radial360;
            bubbleRing.fillOrigin = (int)Image.Origin360.Top;

            // cauldron: what is inside / what is brewing
            potExtra = UIKit.Img("PotExtra", rt, null, Color.white);
            potExtra.rectTransform.At(new Vector2(40f, 40f), new Vector2(60f, 60f));
            potExtra.raycastTarget = false;
            // cat house: the Master's chest on the roof when it is back from an expedition
            tripChest = UIKit.Img("TripChest", rt, ProcArt.Item(Chains.Sandik, 1), Color.white);
            tripChest.rectTransform.At(new Vector2(30f, 44f), new Vector2(64f, 64f));
            tripChest.raycastTarget = false;
            var bang = UIKit.Img("Bang", tripChest.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            bang.rectTransform.At(new Vector2(24f, 22f), new Vector2(30f, 30f));
            var bt = UIKit.Txt("!", bang.transform, "!", 22, Color.white, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(bt.rectTransform);
            tripChest.gameObject.SetActive(false);
            // cat house: the second kitten sits in the doorway
            doorCat = UIKit.Img("DoorCat", rt, ProcArt.SideCat(1, 4), Color.white);
            doorCat.rectTransform.At(new Vector2(0f, -24f), new Vector2(60f, 60f));
            doorCat.raycastTarget = false;
            doorCat.gameObject.SetActive(false);
            potTimer = UIKit.Txt("PotTimer", rt, "", 24, UIKit.Cream, TextAnchor.MiddleCenter, false);
            potTimer.rectTransform.At(new Vector2(0f, -52f), new Vector2(130f, 30f));
            potTimer.raycastTarget = false;
            var sh = potTimer.gameObject.AddComponent<Outline>();
            sh.effectColor = new Color(0.1f, 0.05f, 0.15f, 0.9f);

            needed = UIKit.Img("Needed", rt, ProcArt.Check, Color.white);
            needed.rectTransform.At(new Vector2(-46f, -46f), new Vector2(38f, 38f));
            needed.gameObject.SetActive(false);

            napCat = UIKit.Img("NapCat", rt, ProcArt.SleepCat, Color.white);
            napCat.rectTransform.At(new Vector2(26f, 24f), new Vector2(84f, 84f));
            zzz = UIKit.Txt("Zzz", napCat.transform, "z", 34, UIKit.Cream);
            zzz.rectTransform.At(new Vector2(38f, 40f), new Vector2(60f, 50f));

            Refresh();
        }

        public void Refresh()
        {
            if (data == null) return;
            icon.sprite = ProcArt.Item(data.chain, data.level);
            icon.color = data.nap ? new Color(0.7f, 0.7f, 0.78f) : Color.white;
            napCat.gameObject.SetActive(false); // the sleeping kitten itself sits on top now
            bool gen = Chains.IsSpecial(data.chain);
            bool machine = Chains.IsGenerator(data.chain);
            badge.gameObject.SetActive(!gen || machine);
            // machines show their upgrade level instead of an item level
            badgeText.text = machine ? GameManager.I.GeneratorLevel(data.chain).ToString() : data.level.ToString();
            badge.color = machine ? new Color(0.85f, 0.55f, 0.15f, 0.95f) : new Color(0.14f, 0.09f, 0.2f, 0.92f);
            cooldown.gameObject.SetActive((data.IsKitten && data.level >= 2) || data.Brewing);
            UpdateCooldown();
            RefreshPot();
            bubble.gameObject.SetActive(data.bubble);
            bubbleRing.gameObject.SetActive(data.bubble);
            icon.rectTransform.sizeDelta = data.bubble ? new Vector2(96f, 96f) : new Vector2(122f, 122f);
            if (data.bubble) icon.color = new Color(1f, 1f, 1f, 0.85f);
        }

        /// <summary>Green tick: a ghost customer is asking for this item.</summary>
        public void SetNeeded(bool on)
        {
            if (needed && needed.gameObject.activeSelf != on) needed.gameObject.SetActive(on);
        }

        void RefreshPot()
        {
            bool pot = Chains.IsStation(data.chain);
            if (Chains.IsHouse(data.chain))
            {
                // cat house: "1/2" under it; first kitten looks out of the attic window, the second sits in the door
                potTimer.gameObject.SetActive(true);
                potTimer.text = data.cats + "/" + KittenChaos.HouseSlots;
                string cols = data.catCols ?? "";
                int c0 = cols.Length > 0 ? cols[0] - '0' : 0, c1 = cols.Length > 1 ? cols[1] - '0' : 0;
                potExtra.gameObject.SetActive(data.cats >= 1);
                potExtra.sprite = ProcArt.SideCat(1, 4, c0);
                doorCat.sprite = ProcArt.SideCat(1, 4, c1);
                potExtra.rectTransform.At(new Vector2(0f, 26f), new Vector2(44f, 44f));
                potExtra.color = Color.white;
                doorCat.gameObject.SetActive(data.cats >= 2);
                return;
            }
            doorCat.gameObject.SetActive(false);
            potExtra.rectTransform.localEulerAngles = Vector3.zero;   // undo the house kitten's look-around
            potExtra.rectTransform.localScale = Vector3.one;
            bool showExtra = pot && (data.brew > 0 || !string.IsNullOrEmpty(data.mix));
            potExtra.gameObject.SetActive(showExtra);
            potTimer.gameObject.SetActive(pot && data.brew > 0);
            // an apprentice standing in front of the pot covers the bottom: show the timer over the pot instead
            bool stirred = pot && GameManager.I != null && GameManager.I.Cats != null && GameManager.I.Cats.StirrerAt(cell) != null;
            potTimer.rectTransform.anchoredPosition = new Vector2(0f, stirred ? 12f : -52f);
            if (!showExtra) return;
            if (data.brew > 0)
            {
                potExtra.sprite = ProcArt.Item(Chains.Iksir, data.brew);
                bool ready = data.BrewReady;
                potExtra.rectTransform.At(ready ? new Vector2(0f, 46f) : new Vector2(40f, 40f), ready ? new Vector2(84f, 84f) : new Vector2(56f, 56f));
                potExtra.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.8f);
            }
            else if (Recipes.ParseKey(data.mix, out var mc, out var ml))
            {
                potExtra.sprite = ProcArt.Item(mc, ml);
                potExtra.rectTransform.At(new Vector2(40f, 40f), new Vector2(56f, 56f));
                potExtra.color = Color.white;
            }
        }

        void UpdateCooldown()
        {
            if (!cooldown.gameObject.activeSelf) return;
            if (Chains.IsStation(data.chain))
            {
                var r = Recipes.Get(data.brew);
                float left = (float)System.TimeSpan.FromTicks(data.brewUntil - System.DateTime.UtcNow.Ticks).TotalSeconds;
                cooldown.fillAmount = r == null ? 0f : Mathf.Clamp01(left / r.seconds);
                return;
            }
            float max = data.level == 2 ? KittenChaos.ApprenticeCooldown : KittenChaos.MasterInterval;
            cooldown.fillAmount = Mathf.Clamp01(data.cd / max);
        }

        void Update()
        {
            if (data == null) return;
            UpdateCooldown();
            float t = Time.time + seed;
            if (data.nap)
            {
                zzz.rectTransform.anchoredPosition = new Vector2(38f + Mathf.Sin(t * 2f) * 4f, 40f + Mathf.Repeat(t * 12f, 18f));
                var c = zzz.color; c.a = 1f - Mathf.Repeat(t * 12f, 18f) / 18f; zzz.color = c;
            }
            if (data.bubble)
            {
                float left = (float)System.TimeSpan.FromTicks(data.bubbleUntil - System.DateTime.UtcNow.Ticks).TotalSeconds;
                bubbleRing.fillAmount = Mathf.Clamp01(left / Bubbles.Seconds);
                float bob = Mathf.Sin(t * 2.2f) * 5f;
                bubble.rectTransform.anchoredPosition = new Vector2(0f, bob);
                icon.rectTransform.anchoredPosition = new Vector2(0f, bob);
                bubble.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.1f) * 0.03f);
            }
            else if (data.IsWildKitten && !IsDragging)
                icon.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 3f) * 6f);
            else if (data.chain == Chains.Kazan)
                icon.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.5f) * 0.025f);
            if (Chains.IsHouse(data.chain))
            {
                var cats = GameManager.I != null ? GameManager.I.Cats : null;
                bool ready = cats != null && cats.TripReady;
                if (tripChest.gameObject.activeSelf != ready) tripChest.gameObject.SetActive(ready);
                if (ready) tripChest.rectTransform.anchoredPosition = new Vector2(30f, 44f + Mathf.Abs(Mathf.Sin(t * 4f)) * 8f);
            }
            if (data.bubble) return;   // a bubble item has nothing more to animate
            if (Chains.IsHouse(data.chain) && data.cats > 0)
            {
                // the kittens inside look around a little
                potExtra.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.7f) * 8f);
                potExtra.rectTransform.localScale = new Vector3(Mathf.Sin(t * 0.4f) > 0f ? 1f : -1f, 1f, 1f);
                doorCat.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 2.3f + 1f) * 6f);
            }
            else if (Chains.IsStation(data.chain) && data.brew > 0)
            {
                if (data.Brewing)
                {
                    float left = (float)System.TimeSpan.FromTicks(data.brewUntil - System.DateTime.UtcNow.Ticks).TotalSeconds;
                    int sec = Mathf.CeilToInt(left);
                    potTimer.text = sec >= 60 ? (sec / 60) + ":" + (sec % 60).ToString("00") : sec + " sn";
                    icon.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.03f);
                }
                else
                {
                    if (cooldown.gameObject.activeSelf) Refresh();
                    potTimer.text = "Hazır!";
                    potExtra.rectTransform.anchoredPosition = new Vector2(0f, 46f + Mathf.Sin(t * 3f) * 6f);
                    icon.rectTransform.localScale = Vector3.one;
                }
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            IsPressed = true;
            board.Select(cell);
        }

        public void OnPointerUp(PointerEventData e) => IsPressed = false;

        public void OnBeginDrag(PointerEventData e)
        {
            if (data == null || data.Locked) return;
            IsDragging = true;
            cg.blocksRaycasts = false;
            // lift the item above every UI layer while it is being dragged
            homeParent = rt.parent;
            rt.SetParent(GameManager.I.Root, true);
            rt.SetAsLastSibling();
            Tween.Stop(Tween.Key(rt, "m"));
            Tween.Stop(Tween.Key(rt, "s"));
            rt.localScale = Vector3.one * 1.15f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var lp);
            dragOffset = rt.anchoredPosition - lp;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var lp);
            rt.anchoredPosition = lp + dragOffset;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            IsDragging = false;
            cg.blocksRaycasts = true;
            rt.localScale = Vector3.one;
            if (homeParent) rt.SetParent(homeParent, true);
            var center = RectTransformUtility.WorldToScreenPoint(e.pressEventCamera, rt.position);
            // dropping on the storage box goes by the finger, everything else by the item's centre
            board.OnItemDropped(this, GameManager.I.IsOverStorage(e.position) ? e.position : center);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            board.OnItemClicked(this);
        }
    }
}
