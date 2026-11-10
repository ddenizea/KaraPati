using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// Fare Avı: mice pop out of 9 cellar barrels and Kara Pati catches them with a paw tap.
    /// Mouse +1, golden mouse +3, a ghost customer -2 (it only came to look around). 30 seconds; it gets faster
    /// towards the end.
    /// </summary>
    public class MouseHunt : MiniGame
    {
        const int Cols = 3, Rows = 3;
        const float HiddenY = -230f, UpY = -46f;
        const float RiseTime = 0.12f, FallTime = 0.16f, HitTime = 0.28f;

        enum Phase { Hidden, Rising, Up, Falling, Hit }
        const int Mouse = 0, GoldMouse = 1, Ghost = 2;

        class Hole
        {
            public RectTransform critter, paw;
            public Image img, pawImg;
            public Phase phase;
            public int kind;
            public float t, stay;
        }

        readonly Hole[] holes = new Hole[Cols * Rows];
        RectTransform field, timeFill, bigRt;
        Image timeFillImg;
        Text scoreText, timeText, bigText;
        Button endBtn;
        Action<int> done;
        int score;
        float timeLeft, spawnIn, countdown, endWait;
        bool running, counting, ending;
        const float BarW = 560f;

        public override void Build(RectTransform area)
        {
            // HUD
            var scorePill = UIKit.Img("Score", area, ProcArt.Rounded, UIKit.PanelDark, true);
            scorePill.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -40f), new Vector2(230f, 96f));
            var mini = UIKit.Img("Icon", scorePill.transform, ProcArt.Mouse(false), Color.white);
            mini.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(6f, 0f), new Vector2(84f, 84f));
            scoreText = UIKit.Txt("Value", scorePill.transform, "0", 46, UIKit.Cream, TextAnchor.MiddleLeft);
            scoreText.fontStyle = FontStyle.Bold;
            UIKit.Stretch(scoreText.rectTransform, 96f, 8f);

            var barBg = UIKit.Img("TimeBar", area, ProcArt.Rounded, new Color(0.08f, 0.05f, 0.12f, 0.9f), true);
            barBg.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -66f), new Vector2(BarW + 16f, 48f));
            timeFillImg = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, new Color(0.55f, 0.85f, 0.45f), true);
            timeFill = timeFillImg.rectTransform;
            timeFill.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f, 0f), new Vector2(BarW, 32f));
            timeText = UIKit.Txt("Time", area, "", 32, UIKit.Cream);
            timeText.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -112f), new Vector2(300f, 44f));

            endBtn = UIKit.Btn("End", area, L.T("Bitir"), new Color(0.45f, 0.36f, 0.55f), () => { if (running) Finish(); }, 34);
            ((RectTransform)endBtn.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-30f, -40f), new Vector2(170f, 96f));

            // the cellar
            var cellar = UIKit.Img("Cellar", area, ProcArt.Rounded, new Color(0.2f, 0.13f, 0.24f, 0.97f), true);
            UIKit.Stretch(cellar.rectTransform, 30f, 30f, 190f, 260f);
            // a few stones on the wall
            for (int i = 0; i < 14; i++)
            {
                var st = UIKit.Img("Stone", cellar.transform, ProcArt.Rounded, new Color(0.27f, 0.19f, 0.31f, 0.8f), true);
                st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(Random.Range(0.05f, 0.95f), Random.Range(0.05f, 0.95f));
                st.rectTransform.sizeDelta = new Vector2(Random.Range(90f, 170f), Random.Range(50f, 80f));
            }

            field = UIKit.Rect("Field", cellar.transform);
            field.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -10f), new Vector2(960f, 1440f));
            for (int i = 0; i < holes.Length; i++) holes[i] = BuildHole(i);

            // Kara Pati cheering at the bottom
            var head = UIKit.Img("KaraPati", area, ProcArt.KaraPatiHead, Color.white);
            head.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(-330f, 140f), new Vector2(170f, 170f));
            var say = UIKit.Txt("Say", area, L.T("Pati hazır! Fareler kaçmasın!"), 34, UIKit.Cream, TextAnchor.MiddleLeft);
            say.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0f, 0.5f)).At(new Vector2(-230f, 140f), new Vector2(620f, 60f));

            bigText = UIKit.Txt("Big", area, "", 150, UIKit.Orange);
            bigText.fontStyle = FontStyle.Bold;
            bigRt = bigText.rectTransform;
            bigRt.At(new Vector2(0f, 60f), new Vector2(900f, 240f));
        }

        Hole BuildHole(int i)
        {
            int cx = i % Cols, cy = i / Cols;
            var cell = UIKit.Rect("Hole" + i, field);
            cell.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2((cx - 1) * 310f, -cy * 480f), new Vector2(300f, 460f));

            const float barrelSize = 280f;
            var back = UIKit.Img("Barrel", cell, ProcArt.Barrel, Color.white);
            back.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 10f), new Vector2(barrelSize, barrelSize));

            // the mouth of the barrel is 3/4 up the sprite: the critter climbs out of a masked window that starts there
            float mouthY = 10f + barrelSize * 0.75f;
            var window = UIKit.Rect("Window", cell);
            window.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, mouthY), new Vector2(230f, 240f));
            window.gameObject.AddComponent<RectMask2D>();
            var img = UIKit.Img("Critter", window, ProcArt.Mouse(false), Color.white);
            var critter = img.rectTransform;
            critter.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, HiddenY), new Vector2(210f, 210f));

            var front = UIKit.Img("Front", cell, ProcArt.BarrelFront, Color.white);
            front.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 10f), new Vector2(barrelSize, barrelSize));

            var pawImg = UIKit.Img("Paw", cell, ProcArt.PawPrint, new Color(0.15f, 0.1f, 0.2f, 0f));
            var paw = pawImg.rectTransform;
            paw.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, mouthY + 80f), new Vector2(150f, 150f));

            // the whole cell is the tap target (pointer down, so fast taps count at once)
            var hit = UIKit.Img("Tap", cell, ProcArt.Rounded, new Color(1f, 1f, 1f, 0f));
            hit.raycastTarget = true;
            UIKit.Stretch(hit.rectTransform);
            int hi = i;
            hit.gameObject.AddComponent<TapDown>().onDown = () => OnTap(hi);

            return new Hole { critter = critter, img = img, paw = paw, pawImg = pawImg, phase = Phase.Hidden, kind = -1 };
        }

        // ------------------------------------------------------------------ round
        public override void Play(Action<int> onDone)
        {
            done = onDone;
            score = 0;
            timeLeft = MiniGames.FareSeconds;
            foreach (var h in holes) ResetHole(h);
            UpdateHud();
            running = false;
            ending = false;
            counting = true;
            countdown = 3f;
            endBtn.gameObject.SetActive(false);
            ShowBig("3");
        }

        public override void Stop()
        {
            running = counting = ending = false;
            foreach (var h in holes) ResetHole(h);
            bigText.text = "";
        }

        void ResetHole(Hole h)
        {
            h.phase = Phase.Hidden;
            h.kind = -1;
            h.critter.anchoredPosition = new Vector2(0f, HiddenY);
            h.critter.localScale = Vector3.one;
            h.pawImg.color = new Color(0.15f, 0.1f, 0.2f, 0f);
        }

        void ShowBig(string s)
        {
            bigText.text = s;
            Tween.Pop(bigRt, 0.3f);
        }

        void Finish()
        {
            running = false;
            ending = true;
            endWait = 1.2f;
            endBtn.gameObject.SetActive(false);
            foreach (var h in holes)
                if (h.phase == Phase.Rising || h.phase == Phase.Up) { h.phase = Phase.Falling; h.t = 0f; }
            ShowBig(L.T("Bitti!"));
            Sfx.Play("ready");
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (counting)
            {
                int before = Mathf.CeilToInt(countdown);
                countdown -= dt;
                int now = Mathf.CeilToInt(countdown);
                if (countdown <= 0f)
                {
                    counting = false;
                    running = true;
                    spawnIn = 0.3f;
                    endBtn.gameObject.SetActive(true);
                    ShowBig(L.T("Yakala!"));
                    Sfx.Play("whoosh", 1.2f);
                }
                else if (now != before) { ShowBig(now.ToString()); Sfx.Play("click", 1.2f); }
                return;
            }

            if (running)
            {
                timeLeft -= dt;
                if (bigText.text != "" && timeLeft < MiniGames.FareSeconds - 0.8f) bigText.text = "";
                if (timeLeft <= 0f) { timeLeft = 0f; UpdateHud(); Finish(); }
                else
                {
                    spawnIn -= dt;
                    if (spawnIn <= 0f) Spawn();
                    UpdateHud();
                }
            }

            foreach (var h in holes) Animate(h, dt);

            if (ending)
            {
                endWait -= dt;
                if (endWait <= 0f)
                {
                    ending = false;
                    bigText.text = "";
                    done?.Invoke(score);
                }
            }
        }

        float Progress => 1f - timeLeft / MiniGames.FareSeconds;   // 0 at the start, 1 at the end

        void Spawn()
        {
            float p = Progress;
            spawnIn = Mathf.Lerp(0.85f, 0.45f, p) * Random.Range(0.8f, 1.2f);
            SpawnOne(p);
            if (p > 0.35f && Random.value < 0.18f) SpawnOne(p);   // later on two at once now and then
        }

        void SpawnOne(float p)
        {
            int free = 0;
            foreach (var h in holes) if (h.phase == Phase.Hidden) free++;
            if (free == 0) return;
            int pick = Random.Range(0, free);
            foreach (var h in holes)
            {
                if (h.phase != Phase.Hidden) continue;
                if (pick-- > 0) continue;
                float r = Random.value;
                h.kind = r < 0.09f ? GoldMouse : (p > 0.1f && r < 0.27f) ? Ghost : Mouse;
                h.img.sprite = h.kind == Ghost ? ProcArt.Ghost(Random.Range(0, ProcArt.GhostTintCount)) : ProcArt.Mouse(h.kind == GoldMouse);
                h.stay = Mathf.Lerp(1.15f, 0.7f, p) * (h.kind == GoldMouse ? 0.8f : 1f);
                h.phase = Phase.Rising;
                h.t = 0f;
                h.critter.localScale = Vector3.one;
                if (h.kind != Ghost) Sfx.Play("scurry", Random.Range(1f, 1.2f), 0.5f);
                return;
            }
        }

        void Animate(Hole h, float dt)
        {
            if (h.phase == Phase.Hidden) return;
            h.t += dt;
            float y = HiddenY;
            switch (h.phase)
            {
                case Phase.Rising:
                    y = Mathf.Lerp(HiddenY, UpY, Tween.OutCubic(Mathf.Clamp01(h.t / RiseTime)));
                    if (h.t >= RiseTime) { h.phase = Phase.Up; h.t = 0f; }
                    break;
                case Phase.Up:
                    y = UpY + Mathf.Sin(h.t * 9f) * 5f;   // a little bob while it looks around
                    if (h.t >= h.stay) { h.phase = Phase.Falling; h.t = 0f; }
                    break;
                case Phase.Falling:
                    y = Mathf.Lerp(UpY, HiddenY, Mathf.Clamp01(h.t / FallTime));
                    if (h.t >= FallTime) { h.phase = Phase.Hidden; h.kind = -1; }
                    break;
                case Phase.Hit:
                    // squashed for a moment, then gone
                    float k = Mathf.Clamp01(h.t / HitTime);
                    h.critter.localScale = new Vector3(1f + 0.25f * (1f - k), 1f - 0.4f * (1f - k), 1f);
                    y = k < 0.4f ? UpY : Mathf.Lerp(UpY, HiddenY, (k - 0.4f) / 0.6f);
                    if (h.t >= HitTime) { h.phase = Phase.Hidden; h.kind = -1; h.critter.localScale = Vector3.one; }
                    break;
            }
            h.critter.anchoredPosition = new Vector2(0f, y);
        }

        void OnTap(int i)
        {
            if (!running) return;
            var h = holes[i];
            if (h.phase != Phase.Rising && h.phase != Phase.Up) return;   // empty barrel: a miss costs nothing
            int gain;
            switch (h.kind)
            {
                case GoldMouse: gain = 3; Sfx.Play("coin", 1.1f); break;
                case Ghost: gain = -2; Sfx.Play("error"); Sfx.Play("hiss", 1.2f, 0.6f); break;
                default: gain = 1; Sfx.Play("pop", Random.Range(1.05f, 1.25f)); Sfx.Play("paw", 1.2f, 0.7f); break;
            }
            score = Mathf.Max(0, score + gain);
            h.phase = Phase.Hit;
            h.t = 0f;
            UpdateHud();
            Tween.Punch(scoreText.transform, gain > 0 ? 0.25f : 0.1f, 0.25f);
            PawStamp(h, gain);
        }

        void PawStamp(Hole h, int gain)
        {
            var pr = h.paw;
            var img = h.pawImg;
            var col = gain < 0 ? new Color(0.95f, 0.35f, 0.35f) : new Color(0.15f, 0.1f, 0.2f);
            pr.localEulerAngles = new Vector3(0f, 0f, Random.Range(-20f, 20f));
            Tween.Run(0.35f, t =>
            {
                if (!pr) return;
                pr.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.95f, Tween.OutCubic(Mathf.Min(1f, t * 3f)));
                img.color = new Color(col.r, col.g, col.b, t < 0.5f ? 0.85f : Mathf.Lerp(0.85f, 0f, (t - 0.5f) * 2f));
            }, null, Tween.Key(pr, "paw"));

            // floating "+1" / "+3" / "-2"
            var label = UIKit.Txt("Gain", pr.parent, (gain > 0 ? "+" : "") + gain, 64,
                gain >= 3 ? new Color(1f, 0.85f, 0.3f) : gain > 0 ? UIKit.Cream : new Color(1f, 0.5f, 0.45f));
            label.fontStyle = FontStyle.Bold;
            var lr = label.rectTransform;
            lr.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(pr.anchoredPosition + new Vector2(70f, 40f), new Vector2(200f, 90f));
            var start = lr.anchoredPosition;
            Tween.Run(0.7f, t =>
            {
                if (!lr) return;
                lr.anchoredPosition = start + new Vector2(0f, 90f * Tween.OutCubic(t));
                label.color = new Color(label.color.r, label.color.g, label.color.b, 1f - t * t);
            }, () => { if (lr) Destroy(lr.gameObject); });
        }

        void UpdateHud()
        {
            scoreText.text = score.ToString();
            float frac = Mathf.Clamp01(timeLeft / MiniGames.FareSeconds);
            timeFill.sizeDelta = new Vector2(Mathf.Max(32f, BarW * frac), 32f);
            timeFillImg.color = timeLeft > 5f ? new Color(0.55f, 0.85f, 0.45f) : new Color(1f, 0.45f, 0.4f);
            timeText.text = L.F("{0} sn", Mathf.CeilToInt(timeLeft));
        }
    }
}
