using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// Yumak Atma: the Master and the Apprentice sit on two shelves and take turns jumping. Tap while the jumping
    /// cat is near the top (its ring glows) and Kara Pati throws the yarn to it. Right at the top = "Perfect!" (2 points).
    /// A throw at the wrong moment, or waiting through 3 jumps, is a miss; 3 misses end the game.
    /// Jumps get quicker and the catch window smaller as the catches pile up.
    /// </summary>
    public class YarnToss : MiniGame
    {
        const float ShelfX = 280f, ShelfY = 60f, JumpH = 300f, CatSize = 270f;
        const float KaraY = -560f, CatchUp = 150f;   // the yarn is caught at chest height
        const float FlyTime = 0.3f, HoldTime = 0.35f, BackTime = 0.35f, DropTime = 0.55f;
        const int PatienceJumps = 4;

        enum St { Idle, Count, Aim, Fly, Hold, Back, Drop, Over }

        class Catcher
        {
            public RectTransform rt;
            public Image img;
            public int level, color;
            public float side;             // -1 left shelf, +1 right shelf
            public bool jumping, frozen;
            public float t, dur, rest, y;
            public int jumps;
            public Vector2 Base => new Vector2(side * ShelfX, ShelfY);
            public Vector2 CatchPoint => Base + new Vector2(0f, y + CatchUp);
        }

        readonly Catcher[] cats = new Catcher[2];
        readonly Image[] hearts = new Image[MiniGames.YarnLives];
        RectTransform yarn, ring, kara, bigRt, sayRt;
        Image ringImg, yarnImg;
        Text scoreText, streakText, bigText, sayText;
        Button endBtn;
        Action<int> done;
        St st;
        int active, score, streak, bestStreak, lives, catches;
        float countdown, timer, endWait;
        Vector2 flyFrom, flyTo;
        bool flyHit, flyPerfect, chestShown;

        public override bool BonusEarned => bestStreak >= MiniGames.YarnChestStreak;
        float Difficulty => Mathf.Clamp01(catches / 25f);
        float Window => Mathf.Lerp(0.17f, 0.085f, Difficulty);   // half-width of the catch window, in jump progress
        Vector2 YarnRest => new Vector2(cats[active] != null ? cats[active].side * 80f : 80f, KaraY + 120f);

        public override void Build(RectTransform area)
        {
            // the whole screen is the button: tap anywhere to throw (the HUD buttons are added later, on top)
            var tap = UIKit.Img("Tap", area, ProcArt.Rounded, new Color(1f, 1f, 1f, 0f));
            tap.raycastTarget = true;
            UIKit.Stretch(tap.rectTransform);
            tap.gameObject.AddComponent<TapDown>().onDown = OnTap;

            // the tower room
            var room = UIKit.Img("Room", area, ProcArt.Rounded, new Color(0.22f, 0.15f, 0.3f, 0.97f), true);
            UIKit.Stretch(room.rectTransform, 30f, 30f, 190f, 200f);
            var window = UIKit.Img("Window", area, ProcArt.Circle, new Color(0.1f, 0.12f, 0.28f));
            window.rectTransform.At(new Vector2(0f, 560f), new Vector2(240f, 240f));
            var moon = UIKit.Img("Moon", window.transform, ProcArt.Moon, Color.white);
            moon.rectTransform.At(new Vector2(30f, 30f), new Vector2(110f, 110f));
            for (int s = -1; s <= 1; s += 2)
            {
                var shelf = UIKit.Img("Shelf", area, ProcArt.Rounded, new Color(0.55f, 0.36f, 0.22f), true);
                shelf.rectTransform.At(new Vector2(s * ShelfX, ShelfY + 26f), new Vector2(320f, 40f));   // the sprite has room under the paws
            }
            var floor = UIKit.Img("Floor", area, ProcArt.Rounded, new Color(0.3f, 0.2f, 0.32f), true);
            floor.rectTransform.At(new Vector2(0f, KaraY + 16f), new Vector2(960f, 70f));

            ringImg = UIKit.Img("Ring", area, ProcArt.TargetRing, new Color(1f, 1f, 1f, 0.3f));
            ring = ringImg.rectTransform;
            ring.sizeDelta = new Vector2(210f, 210f);

            for (int i = 0; i < 2; i++)
            {
                var c = new Catcher { side = i == 0 ? -1f : 1f };
                c.img = UIKit.Img(i == 0 ? "Usta" : "Cirak", area, ProcArt.SideCat(3, 4), Color.white);
                c.rt = c.img.rectTransform;
                c.rt.pivot = new Vector2(0.5f, 0f);
                c.rt.sizeDelta = new Vector2(CatSize, CatSize);
                c.rt.localScale = new Vector3(c.side < 0f ? 1f : -1f, 1f, 1f);   // both face the middle
                cats[i] = c;
            }

            var karaImg = UIKit.Img("KaraPati", area, ProcArt.SideCat(0, 4), Color.white);
            kara = karaImg.rectTransform;
            kara.pivot = new Vector2(0.5f, 0f);
            kara.At(new Vector2(0f, KaraY), new Vector2(300f, 300f));

            yarnImg = UIKit.Img("Yarn", area, ProcArt.YarnBall, Color.white);
            yarn = yarnImg.rectTransform;
            yarn.sizeDelta = new Vector2(110f, 110f);

            // HUD
            var scorePill = UIKit.Img("Score", area, ProcArt.Rounded, UIKit.PanelDark, true);
            scorePill.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -40f), new Vector2(230f, 96f));
            var icon = UIKit.Img("Icon", scorePill.transform, ProcArt.YarnBall, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 0f), new Vector2(76f, 76f));
            scoreText = UIKit.Txt("Value", scorePill.transform, "0", 46, UIKit.Cream, TextAnchor.MiddleLeft);
            scoreText.fontStyle = FontStyle.Bold;
            UIKit.Stretch(scoreText.rectTransform, 96f, 8f);

            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIKit.Img("Heart" + i, area, ProcArt.Heart, Color.white);
                hearts[i].rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-70f + i * 70f, -70f), new Vector2(62f, 62f));
            }
            streakText = UIKit.Txt("Streak", area, "", 32, UIKit.Cream);
            streakText.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -132f), new Vector2(500f, 44f));

            endBtn = UIKit.Btn("End", area, L.T("Bitir"), new Color(0.45f, 0.36f, 0.55f), () => { if (st == St.Aim) Finish(); }, 34);
            ((RectTransform)endBtn.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-30f, -40f), new Vector2(170f, 96f));

            var hint = UIKit.Txt("Hint", area, L.T("Halka parlayınca ekrana dokun!"), 32, new Color(1f, 1f, 1f, 0.75f));
            hint.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, 120f), new Vector2(900f, 50f));

            sayText = UIKit.TxtRaw("Say", area, "", 44, UIKit.Orange);
            sayText.fontStyle = FontStyle.Bold;
            sayRt = sayText.rectTransform;
            sayRt.sizeDelta = new Vector2(500f, 70f);

            bigText = UIKit.TxtRaw("Big", area, "", 140, UIKit.Orange);
            bigText.fontStyle = FontStyle.Bold;
            bigRt = bigText.rectTransform;
            bigRt.At(new Vector2(0f, -160f), new Vector2(900f, 220f));
        }

        // ------------------------------------------------------------------ round
        public override void Play(Action<int> onDone)
        {
            done = onDone;
            score = streak = bestStreak = catches = 0;
            lives = MiniGames.YarnLives;
            chestShown = false;
            // the Master in the colour the player picked, and an Apprentice
            int ustaCol = GameManager.I.Cats != null && GameManager.I.Cats.HasAnyMaster ? GameManager.I.Cats.MasterColor : KittenColors.Gece;
            Setup(cats[0], 3, ustaCol);
            Setup(cats[1], 2, ustaCol == KittenColors.Turuncu ? KittenColors.Gri : KittenColors.Turuncu);
            for (int i = 0; i < hearts.Length; i++) hearts[i].color = Color.white;
            active = 0;
            PlaceYarnAtKara();
            ring.gameObject.SetActive(false);
            sayText.text = "";
            UpdateHud();
            st = St.Count;
            countdown = 3f;
            endBtn.gameObject.SetActive(false);
            ShowBig("3");
        }

        void Setup(Catcher c, int level, int color)
        {
            c.level = level;
            c.color = color;
            c.img.sprite = ProcArt.SideCat(level, 4, color);
            c.jumping = c.frozen = false;
            c.y = 0f;
            c.jumps = 0;
            c.rt.anchoredPosition = c.Base;
        }

        public override void Stop()
        {
            st = St.Idle;
            bigText.text = sayText.text = "";
        }

        void StartRest(Catcher c)
        {
            float d = Difficulty;
            c.jumping = false;
            c.frozen = false;
            c.t = 0f;
            c.rest = Random.Range(Mathf.Lerp(0.45f, 0.25f, d), Mathf.Lerp(1.1f, 0.6f, d));
            c.dur = Mathf.Lerp(0.95f, 0.55f, d);
        }

        void ShowBig(string s)
        {
            bigText.text = s;
            Tween.Pop(bigRt, 0.3f);
        }

        void Say(string s, Vector2 at, Color col)
        {
            sayText.text = s;
            sayText.color = col;
            sayRt.anchoredPosition = at + new Vector2(0f, 90f);
            Tween.Pop(sayRt, 0.25f);
        }

        void PlaceYarnAtKara()
        {
            yarn.anchoredPosition = YarnRest;
            yarnImg.color = Color.white;
            kara.localScale = new Vector3(cats[active].side, 1f, 1f);   // Kara Pati turns to the cat it throws to
        }

        void UpdateHud()
        {
            scoreText.text = score.ToString();
            streakText.text = streak > 1 ? L.F("Seri: {0}", streak) : "";
        }

        void Finish()
        {
            st = St.Over;
            endWait = 1.2f;
            endBtn.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            ShowBig(L.T("Bitti!"));
            Sfx.Play("ready");
        }

        // ------------------------------------------------------------------ input
        void OnTap()
        {
            if (st != St.Aim) return;
            var c = cats[active];
            float off = c.jumping ? Mathf.Abs(c.t / c.dur - 0.5f) : 1f;
            flyHit = off <= Window;
            flyPerfect = flyHit && off <= Window * 0.35f;
            flyFrom = yarn.anchoredPosition;
            if (flyHit) { c.frozen = true; flyTo = c.CatchPoint; }
            else flyTo = c.Base + new Vector2(0f, JumpH + CatchUp);   // where the cat should have been
            st = St.Fly;
            timer = 0f;
            ring.gameObject.SetActive(false);
            Sfx.Play("whoosh", 1.3f, 0.7f);
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            if (st == St.Idle) return;
            float dt = Time.unscaledDeltaTime;

            if (st == St.Count)
            {
                int before = Mathf.CeilToInt(countdown);
                countdown -= dt;
                int now = Mathf.CeilToInt(countdown);
                if (countdown <= 0f)
                {
                    st = St.Aim;
                    endBtn.gameObject.SetActive(true);
                    StartRest(cats[active]);
                    ShowBig(L.T("Hadi!"));
                    Sfx.Play("whoosh", 1.2f);
                }
                else if (now != before) { ShowBig(now.ToString()); Sfx.Play("click", 1.2f); }
                Idle(cats[0], dt);
                Idle(cats[1], dt);
                return;
            }
            if (bigText.text != "" && st == St.Aim && cats[active].t > 0.7f) bigText.text = "";

            for (int i = 0; i < 2; i++)
            {
                if (i == active && st != St.Over) Animate(cats[i], dt);
                else Idle(cats[i], dt);
            }

            switch (st)
            {
                case St.Aim: UpdateRing(); break;
                case St.Fly:
                {
                    timer += dt;
                    float k = Mathf.Clamp01(timer / FlyTime);
                    yarn.anchoredPosition = Vector2.Lerp(flyFrom, flyTo, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 160f);
                    yarn.localEulerAngles = new Vector3(0f, 0f, -k * 540f);
                    if (k >= 1f) Landed();
                    break;
                }
                case St.Hold:
                {
                    timer += dt;
                    var c = cats[active];
                    yarn.anchoredPosition = c.CatchPoint + new Vector2(-c.side * 40f, -10f);
                    if (timer >= HoldTime) { st = St.Back; timer = 0f; flyFrom = yarn.anchoredPosition; }
                    break;
                }
                case St.Back:
                {
                    timer += dt;
                    float k = Mathf.Clamp01(timer / BackTime);
                    var to = new Vector2(0f, KaraY + 120f);
                    yarn.anchoredPosition = Vector2.Lerp(flyFrom, to, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 120f);
                    if (k >= 1f) NextCat();
                    break;
                }
                case St.Drop:
                {
                    timer += dt;
                    float k = Mathf.Clamp01(timer / DropTime);
                    yarn.anchoredPosition = flyTo + new Vector2(0f, -900f * k * k);
                    yarnImg.color = new Color(1f, 1f, 1f, 1f - k);
                    if (k >= 1f)
                    {
                        if (lives <= 0) Finish();
                        else NextCat();
                    }
                    break;
                }
                case St.Over:
                    endWait -= dt;
                    if (endWait <= 0f)
                    {
                        st = St.Idle;
                        bigText.text = sayText.text = "";
                        done?.Invoke(score);
                    }
                    break;
            }
        }

        void Idle(Catcher c, float dt)
        {
            // sitting and breathing; a cat that was frozen in the air comes back down to its shelf
            c.y = Mathf.MoveTowards(c.y, 0f, dt * 900f);
            c.rt.anchoredPosition = c.Base + new Vector2(0f, c.y);
            c.img.sprite = ProcArt.SideCat(c.level, c.y > 1f ? 1 : 4, c.color);
            float b = 1f + Mathf.Sin(Time.unscaledTime * 3f + c.side) * 0.015f;
            c.rt.localScale = new Vector3(c.side < 0f ? b : -b, 1f / b, 1f);
        }

        void Animate(Catcher c, float dt)
        {
            if (c.frozen)
            {
                c.rt.anchoredPosition = c.Base + new Vector2(0f, c.y);
                return;
            }
            if (st == St.Hold)
            {
                // coming down with the yarn in its paws
                Idle(c, dt);
                return;
            }
            c.t += dt;
            if (!c.jumping)
            {
                c.y = 0f;
                if (c.t >= c.rest) { c.jumping = true; c.t = 0f; Sfx.Play("paw", 1.3f, 0.4f); }
            }
            else
            {
                float u = Mathf.Clamp01(c.t / c.dur);
                c.y = Mathf.Sin(u * Mathf.PI) * JumpH;
                if (u >= 1f)
                {
                    c.jumps++;
                    StartRest(c);
                    if (st == St.Aim && c.jumps >= PatienceJumps) Miss(L.T("Çok bekledin!"), true);
                }
            }
            c.rt.anchoredPosition = c.Base + new Vector2(0f, c.y);
            c.img.sprite = ProcArt.SideCat(c.level, c.jumping ? 1 : 4, c.color);
            c.rt.localScale = new Vector3(c.side < 0f ? 1f : -1f, 1f, 1f);
        }

        void UpdateRing()
        {
            var c = cats[active];
            ring.gameObject.SetActive(true);
            ring.anchoredPosition = c.Base + new Vector2(0f, JumpH + CatchUp);
            bool inWindow = c.jumping && Mathf.Abs(c.t / c.dur - 0.5f) <= Window;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
            ring.localScale = Vector3.one * (inWindow ? 1.1f * pulse : 1f);
            ringImg.color = inWindow ? new Color(1f, 0.85f, 0.3f, 0.95f) : new Color(1f, 1f, 1f, 0.3f);
        }

        void Landed()
        {
            var c = cats[active];
            if (flyHit)
            {
                int gain = flyPerfect ? 2 : 1;
                score += gain;
                streak++;
                catches++;
                bestStreak = Mathf.Max(bestStreak, streak);
                c.frozen = false;
                c.jumping = false;
                Say(flyPerfect ? L.T("Mükemmel!") : L.T("Yakaladı!"), c.CatchPoint, flyPerfect ? new Color(1f, 0.85f, 0.3f) : UIKit.Cream);
                Sfx.Play(flyPerfect ? "coin" : "pop", 1.1f + Mathf.Min(streak, 10) * 0.03f);
                Tween.Punch(scoreText.transform, 0.25f, 0.25f);
                if (!chestShown && !Paid && bestStreak >= MiniGames.YarnChestStreak)
                {
                    chestShown = true;
                    ShowBig(L.T("Hediye sandığı!"));
                    Sfx.Play("gift");
                }
                UpdateHud();
                st = St.Hold;
                timer = 0f;
            }
            else Miss(L.T("Kaçtı!"), false);
        }

        void Miss(string msg, bool waited)
        {
            lives--;
            streak = 0;
            if (lives >= 0 && lives < hearts.Length) hearts[lives].color = new Color(1f, 1f, 1f, 0.2f);
            Say(msg, cats[active].Base + new Vector2(0f, 200f), new Color(1f, 0.55f, 0.5f));
            Sfx.Play("error");
            UpdateHud();
            cats[active].frozen = false;
            if (waited)
            {
                // the yarn never left Kara Pati's paws: drop it in place
                flyTo = yarn.anchoredPosition;
            }
            st = St.Drop;
            timer = 0f;
        }

        void NextCat()
        {
            active = 1 - active;
            cats[active].jumps = 0;
            StartRest(cats[active]);
            yarn.localEulerAngles = Vector3.zero;
            PlaceYarnAtKara();
            Tween.Pop(yarn, 0.2f);
            st = St.Aim;
        }
    }
}
