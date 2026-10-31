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
        public enum State { Entering, Idle, Walking, Napping, Leaving, Dragged }

        public int level;
        public State state { get; private set; }
        public Vector2Int napCell = new Vector2Int(-1, -1);
        public long expires;   // apprentice leaves at this UTC tick
        public float cd;       // helper cooldown
        public RectTransform rt;

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

        public bool IsHeld => pressed || state == State.Dragged;
        public bool IsWild => level == 1;
        public bool IsGone => state == State.Leaving;
        public bool CanAct => state == State.Idle || state == State.Walking;
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
                case State.Walking:
                case State.Leaving:
                    if (jumping) break;
                    moving = true;
                    var d = target - Pos;
                    float step = speed * dt * (state == State.Leaving ? 1.5f : 1f);
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
                    ? (int)left.TotalHours + "s " + left.Minutes + "dk"
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
            else if (moving) frame = (int)(animT * 8f) % 4;
            else frame = 4;
            body.sprite = frames[frame];

            float bob = moving ? Mathf.Abs(Mathf.Sin(animT * 16f)) * 5f : (state == State.Napping ? Mathf.Sin(animT * 2f) * 1.5f : 0f);
            body.rectTransform.anchoredPosition = new Vector2(0f, bob);
            body.rectTransform.localScale = new Vector3(facing, 1f, 1f);
            body.rectTransform.localEulerAngles = state == State.Dragged ? new Vector3(0f, 0f, Mathf.Sin(animT * 10f) * 8f) : Vector3.zero;
        }

        // ------------------------------------------------------------ input
        public void OnPointerDown(PointerEventData e)
        {
            pressed = true;
            if (state == State.Walking) Idle(2f); // stop under the finger (cancels any mischief plan)
            sys.Select(this);
        }

        public void OnPointerUp(PointerEventData e) => pressed = false;

        public void OnBeginDrag(PointerEventData e)
        {
            if (state == State.Napping || state == State.Entering || state == State.Leaving || jumping) return;
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
