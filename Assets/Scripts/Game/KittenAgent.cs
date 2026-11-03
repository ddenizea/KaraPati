using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>One cat walking around on the table. Behaviour decisions come from <see cref="KittenChaos"/>.</summary>
    public class KittenAgent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public enum State { Entering, Idle, Walking, Napping, Leaving, Dragged, Stirring }

        public int level;
        public State state { get; private set; }
        public Vector2Int napCell = new Vector2Int(-1, -1);
        public long expires;   // apprentice leaves at this UTC tick
        public float cd;       // helper cooldown
        public RectTransform rt;
        public Vector2Int stirCell = new Vector2Int(-1, -1);   // pot this apprentice is stirring
        public bool chasing;   // apprentice running after a kitten
        public float runBoost = 1f;   // a scared kitten runs off faster

        KittenChaos sys;
        Image body, ring;
        Text label;
        readonly Sprite[] frames = new Sprite[6];
        Vector2 target;
        float idleTimer, animT, speed, mischiefTimer;
        int facing = 1;
        Action onArrive;
        bool pressed, jumping;
        Vector2 dragOffset;
        RectTransform spoon;

        public bool IsHeld => pressed || state == State.Dragged;
        public bool IsWild => level == 1;
        public bool IsGone => state == State.Leaving;
        public bool CanAct => state == State.Idle || state == State.Walking;
        public bool Stirring => state == State.Stirring;
        public Vector2 Pos { get => rt.anchoredPosition; set => rt.anchoredPosition = value; }

        public void Init(KittenChaos s, int lvl, Vector2 pos)
        {
            sys = s;
            level = lvl;
            rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(130f, 130f);

            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);

            var shadow = UIKit.Img("Shadow", rt, ProcArt.Circle, new Color(0f, 0f, 0f, 0.28f));
            shadow.rectTransform.At(new Vector2(0f, -46f), new Vector2(92f, 22f));
            ring = UIKit.Img("Ring", rt, ProcArt.Circle, new Color(1f, 0.66f, 0.3f, 0.45f));
            ring.rectTransform.At(new Vector2(0f, -46f), new Vector2(126f, 36f));
            ring.gameObject.SetActive(false);

            for (int i = 0; i < frames.Length; i++) frames[i] = ProcArt.SideCat(level, i);
            float size = level == 1 ? 128f : 140f;
            body = UIKit.Img("Body", rt, frames[4], Color.white);
            body.rectTransform.sizeDelta = new Vector2(size, size);

            label = UIKit.Txt("Label", rt, "", 22, UIKit.Cream);
            label.rectTransform.At(new Vector2(0f, -72f), new Vector2(170f, 30f));
            label.gameObject.SetActive(level == 2);

            speed = level == 1 ? 95f : 75f;
            Pos = pos;
            mischiefTimer = Random.Range(5f, 10f);
            Idle(Random.Range(0.5f, 2f));
        }

        // ------------------------------------------------------------ commands
        public void SetSelected(bool on) { if (ring) ring.gameObject.SetActive(on); }
        public void Face(int dir) { if (dir != 0) facing = dir > 0 ? 1 : -1; }

        public void WalkTo(Vector2 p, Action arrive = null)
        {
            target = sys.Clamp(p);
            onArrive = arrive;
            state = State.Walking;
        }

        public void Idle(float t)
        {
            state = State.Idle;
            idleTimer = t;
            onArrive = null;
            chasing = false;
        }

        /// <summary>Run towards p (re-aimed every frame while chasing a kitten).</summary>
        public void ChaseTo(Vector2 p)
        {
            target = sys.Clamp(p);
            onArrive = null;
            chasing = true;
            state = State.Walking;
        }

        /// <summary>Sit next to the alchemy pot and stir it with a big wooden spoon.</summary>
        public void StartStir(Vector2Int cell, Vector2 potPos)
        {
            Tween.Stop(Tween.Key(rt, "m"));
            jumping = false;
            chasing = false;
            onArrive = null;
            stirCell = cell;
            int side = Pos.x >= potPos.x ? 1 : -1;            // sit on whichever side it was dropped
            Pos = sys.Clamp(potPos + new Vector2(side * 92f, -8f));
            Face(-side);                                       // look at the pot
            state = State.Stirring;
            if (!spoon) BuildSpoon();
            spoon.gameObject.SetActive(true);
            spoon.anchoredPosition = new Vector2(-side * 30f, 34f);   // in the paw that faces the pot
            spoon.localScale = new Vector3(-side, 1f, 1f);   // the spoon reaches towards the pot
        }

        public void StopStir()
        {
            stirCell = new Vector2Int(-1, -1);
            if (spoon) spoon.gameObject.SetActive(false);
            if (state == State.Stirring) Idle(1f);
        }

        void BuildSpoon()
        {
            // pivot sits in the cat's paw; the spoon hangs into the pot and swings around it
            spoon = UIKit.Rect("Spoon", rt);
            spoon.sizeDelta = new Vector2(20f, 20f);
            var wood = new Color(0.62f, 0.42f, 0.26f);
            // drawn pointing right (+x): from the paw down into the pot; localScale.x flips it for the other side
            var handle = UIKit.Img("Handle", spoon, ProcArt.Rounded, wood, true);
            handle.rectTransform.At(new Vector2(35f, 2f), new Vector2(16f, 92f));
            handle.rectTransform.localEulerAngles = new Vector3(0f, 0f, 70f);
            handle.raycastTarget = false;
            var bowl = UIKit.Img("Bowl", spoon, ProcArt.Circle, new Color(0.72f, 0.5f, 0.3f));
            bowl.rectTransform.At(new Vector2(74f, -12f), new Vector2(40f, 30f));
            bowl.raycastTarget = false;
        }

        public void JumpIn(Vector2 from, Vector2 to)
        {
            state = State.Entering;
            Pos = from;
            Arc(from, to, 0.6f, () => Idle(Random.Range(0.5f, 1.5f)));
        }

        public void Nap(Vector2Int cell, Vector2 p)
        {
            state = State.Napping;
            napCell = cell;
            onArrive = null;
            Pos = p;
        }

        public void WakeUp()
        {
            napCell = new Vector2Int(-1, -1);
            Idle(0.3f);
        }

        public void Leave()
        {
            if (state == State.Leaving) return;
            Tween.Stop(Tween.Key(rt, "m"));
            jumping = false;
            onArrive = null;
            state = State.Leaving;
            target = sys.NearestEdge(Pos);
            SetSelected(false);
        }

        void Arc(Vector2 from, Vector2 to, float dur, Action done)
        {
            jumping = true;
            Face(to.x >= from.x ? 1 : -1);
            Tween.Run(dur, t =>
            {
                if (!rt) return;
                Pos = Vector2.Lerp(from, to, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 130f;
            }, () => { jumping = false; done?.Invoke(); }, Tween.Key(rt, "m"));
        }

        void JumpOut()
        {
            var from = Pos;
            float side = from.x < 0f ? -1f : 1f;
            var to = from + new Vector2(side * 190f, -60f);
            Arc(from, to, 0.5f, () => sys.Remove(this));
            var img = body;
            Tween.Run(0.5f, t => { if (img) img.color = new Color(1f, 1f, 1f, 1f - t * t); });
        }

        // ------------------------------------------------------------ update
        void Update()
        {
            float dt = Time.deltaTime;
            animT += dt;
            if (sys == null || !sys.Active) { Animate(false); return; }

            bool moving = false;
            switch (state)
            {
                case State.Idle:
                    if (sys.Selected == this) break; // the cat you picked waits for you
                    idleTimer -= dt;
                    if (idleTimer <= 0f) WalkTo(sys.RandomPoint());
                    break;
                case State.Stirring:
                    if (spoon)
                    {
                        // a slow swirl; quicker while something is brewing
                        float rate = sys.PotBrewing(stirCell) ? 7f : 2.5f;
                        spoon.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(animT * rate) * 22f);
                    }
                    break;
                case State.Walking:
                case State.Leaving:
                    if (jumping) break;
                    moving = true;
                    var d = target - Pos;
                    float mult = state == State.Leaving ? 1.5f * runBoost : chasing ? 2.1f : 1f;
                    float step = speed * dt * mult;
                    if (Mathf.Abs(d.x) > 2f) Face(d.x > 0f ? 1 : -1);
                    if (d.magnitude <= step)
                    {
                        Pos = target;
                        if (state == State.Leaving) JumpOut();
                        else
                        {
                            var a = onArrive;
                            Idle(Random.Range(1f, 3f));
                            a?.Invoke();
                        }
                    }
                    else Pos += d.normalized * step;
                    break;
            }

            if (IsWild && CanAct && onArrive == null && !IsHeld && !jumping && sys.Selected != this)
            {
                mischiefTimer -= dt;
                if (mischiefTimer <= 0f)
                {
                    mischiefTimer = Random.Range(10f, 16f);
                    sys.PlanMischief(this);
                }
            }

            UpdateLabel();
            Animate(moving);
        }

        void UpdateLabel()
        {
            if (level == 2)
            {
                var left = TimeSpan.FromTicks(Math.Max(0L, expires - DateTime.UtcNow.Ticks));
                label.text = left.TotalHours >= 1.0
                    ? (int)left.TotalHours + "sa " + left.Minutes + "dk"
                    : left.Minutes + "dk " + left.Seconds + "sn";
                label.color = cd > 0f ? new Color(1f, 1f, 1f, 0.55f) : UIKit.Cream;
            }
            else if (level == 1)
            {
                bool nap = state == State.Napping;
                if (label.gameObject.activeSelf != nap) label.gameObject.SetActive(nap);
                if (nap)
                {
                    float z = Mathf.Repeat(animT * 14f, 26f);
                    label.text = "z z";
                    label.rectTransform.anchoredPosition = new Vector2(34f, 40f + z);
                    label.color = new Color(1f, 0.96f, 0.88f, 1f - z / 26f);
                }
            }
        }

        void Animate(bool moving)
        {
            int frame;
            if (state == State.Napping) frame = 5;
            else if (jumping) frame = 1;
            else if (moving) frame = (int)(animT * (chasing || runBoost > 1f ? 13f : 8f)) % 4;
            else frame = 4;
            body.sprite = frames[frame];

            float bob = moving ? Mathf.Abs(Mathf.Sin(animT * 16f)) * 5f
                : state == State.Napping ? Mathf.Sin(animT * 2f) * 1.5f
                : state == State.Stirring ? Mathf.Abs(Mathf.Sin(animT * 3.5f)) * 3f : 0f;
            body.rectTransform.anchoredPosition = new Vector2(0f, bob);
            body.rectTransform.localScale = new Vector3(facing, 1f, 1f);
            body.rectTransform.localEulerAngles = state == State.Dragged ? new Vector3(0f, 0f, Mathf.Sin(animT * 10f) * 8f) : Vector3.zero;
        }

        // ------------------------------------------------------------ input
        public void OnPointerDown(PointerEventData e)
        {
            pressed = true;
            if (state == State.Walking) Idle(2f); // stop under the finger (cancels any mischief plan or chase)
            sys.Select(this);
        }

        public void OnPointerUp(PointerEventData e) => pressed = false;

        public void OnBeginDrag(PointerEventData e)
        {
            if (state == State.Napping || state == State.Entering || state == State.Leaving || jumping) return;
            if (state == State.Stirring) StopStir();   // picked up from the pot
            Tween.Stop(Tween.Key(rt, "m"));
            onArrive = null;
            state = State.Dragged;
            rt.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var lp);
            dragOffset = Pos - lp;
        }

        public void OnDrag(PointerEventData e)
        {
            if (state != State.Dragged) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var lp);
            Pos = lp + dragOffset;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (state != State.Dragged) return;
            pressed = false;
            Pos = sys.Clamp(Pos);
            Idle(Random.Range(1.5f, 3f));
            sys.OnDropped(this);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            sys.OnTapped(this);
        }
    }
}
