using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KaraPati
{
    /// <summary>
    /// İksir Hafızası: face-down cards with the shop's ingredients and potions. Flip two; a pair stays open.
    /// Every new table is first shown face up for a moment (the clock waits), then the cards turn over.
    /// 60 seconds; a cleared table brings a bigger one (6, 8, then 10 pairs). A pair is 1 point, matches in a row
    /// give +1 extra, a cleared table +3. Now and then a golden hourglass pair adds 5 seconds.
    /// </summary>
    public class PotionMemory : MiniGame
    {
        const int Cols = 4;
        const float CardW = 196f, CardH = 236f, Gap = 18f;
        const float FlipTime = 0.16f, ShowWrong = 0.75f;
        const int Hourglass = -1;   // the kind of the bonus card

        class Card
        {
            public RectTransform rt;
            public Image face, back;
            public int kind;
            public bool open, matched;
        }

        readonly List<Card> cards = new List<Card>();
        readonly List<Card> faceUp = new List<Card>();
        RectTransform table, timeFill, bigRt;
        Image timeFillImg;
        Text scoreText, timeText, comboText, bigText;
        Button endBtn;
        Action<int> done;
        int score, combo, round, pairsLeft;
        float timeLeft, countdown, resolveIn, nextIn, endWait, bigTimer, previewIn;
        bool running, counting, ending, resolving;
        const float BarW = 560f;

        // every ingredient level 1-4 and every potion: plenty of different faces for the 10-pair table
        static readonly List<(string chain, int level)> faces = BuildFaces();
        static List<(string, int)> BuildFaces()
        {
            var l = new List<(string, int)>();
            foreach (var c in Chains.Ingredients) for (int lv = 1; lv <= 4; lv++) l.Add((c, lv));
            for (int lv = 1; lv <= 6; lv++) l.Add((Chains.Iksir, lv));
            return l;
        }

        public override void Build(RectTransform area)
        {
            var scorePill = UIKit.Img("Score", area, ProcArt.Rounded, UIKit.PanelDark, true);
            scorePill.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -40f), new Vector2(230f, 96f));
            var icon = UIKit.Img("Icon", scorePill.transform, ProcArt.Item(Chains.Iksir, 2), Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f, 0f), new Vector2(80f, 80f));
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

            comboText = UIKit.TxtRaw("Combo", area, "", 34, UIKit.Orange);
            comboText.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -175f), new Vector2(700f, 50f));

            var bg = UIKit.Img("Table", area, ProcArt.Rounded, new Color(0.22f, 0.15f, 0.3f, 0.97f), true);
            UIKit.Stretch(bg.rectTransform, 30f, 30f, 210f, 200f);
            table = UIKit.Rect("Cards", bg.transform);
            table.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(Vector2.zero, new Vector2(Cols * CardW + (Cols - 1) * Gap, 1400f));

            var hint = UIKit.Txt("Hint", area, L.T("İki kart çevir, aynılarını bul!"), 32, new Color(1f, 1f, 1f, 0.75f));
            hint.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, 110f), new Vector2(900f, 50f));

            bigText = UIKit.TxtRaw("Big", area, "", 130, UIKit.Orange);
            bigText.fontStyle = FontStyle.Bold;
            bigRt = bigText.rectTransform;
            bigRt.At(Vector2.zero, new Vector2(900f, 220f));
        }

        // ------------------------------------------------------------------ round
        public override void Play(Action<int> onDone)
        {
            done = onDone;
            score = combo = round = 0;
            timeLeft = MiniGames.MemorySeconds;
            faceUp.Clear();
            resolving = ending = running = false;
            nextIn = 0f;
            Deal();
            UpdateHud();
            counting = true;
            countdown = 3f;
            endBtn.gameObject.SetActive(false);
            ShowBig("3");
        }

        public override void Stop()
        {
            running = counting = ending = resolving = false;
            nextIn = 0f;
            bigText.text = "";
        }

        void Deal()
        {
            foreach (var c in cards) if (c.rt) Destroy(c.rt.gameObject);
            cards.Clear();
            faceUp.Clear();
            int pairs = MiniGames.MemoryPairs[Mathf.Min(round, MiniGames.MemoryPairs.Length - 1)];
            pairsLeft = pairs;
            previewIn = 1.2f + pairs * 0.1f;   // bigger tables are shown a little longer

            // pick different faces; sometimes one pair is the golden hourglass
            var pool = new List<int>();
            for (int i = 0; i < faces.Count; i++) pool.Add(i);
            var kinds = new List<int>();
            bool hourglass = Random.value < 0.45f;
            for (int p = 0; p < pairs; p++)
            {
                int k;
                if (hourglass && p == 0) k = Hourglass;
                else { int j = Random.Range(0, pool.Count); k = pool[j]; pool.RemoveAt(j); }
                kinds.Add(k); kinds.Add(k);
            }
            for (int i = kinds.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (kinds[i], kinds[j]) = (kinds[j], kinds[i]); }

            int rows = Mathf.CeilToInt(kinds.Count / (float)Cols);
            float h = rows * CardH + (rows - 1) * Gap;
            for (int i = 0; i < kinds.Count; i++)
            {
                int cx = i % Cols, cy = i / Cols;
                var pos = new Vector2(-((Cols - 1) * (CardW + Gap)) * 0.5f + cx * (CardW + Gap), h * 0.5f - CardH * 0.5f - cy * (CardH + Gap));
                cards.Add(MakeCard(kinds[i], pos, i));
            }
        }

        Card MakeCard(int kind, Vector2 pos, int index)
        {
            var c = new Card { kind = kind };
            var bg = UIKit.Img("Card" + index, table, ProcArt.Rounded, new Color(1f, 0.95f, 0.85f), true);
            bg.raycastTarget = true;
            c.rt = bg.rectTransform;
            c.rt.At(pos, new Vector2(CardW, CardH));
            c.face = UIKit.Img("Face", c.rt, kind == Hourglass ? ProcArt.Hourglass : ProcArt.Item(faces[kind].chain, faces[kind].level), Color.white);
            c.face.rectTransform.sizeDelta = new Vector2(CardW * 0.8f, CardW * 0.8f);
            // the back: purple with Kara Pati's paw print
            c.back = UIKit.Img("Back", c.rt, ProcArt.Rounded, new Color(0.45f, 0.3f, 0.62f), true);
            UIKit.Stretch(c.back.rectTransform, 6f, 6f, 6f, 6f);
            var paw = UIKit.Img("Paw", c.back.transform, ProcArt.PawPrint, new Color(1f, 0.85f, 0.95f, 0.45f));
            paw.rectTransform.sizeDelta = new Vector2(CardW * 0.5f, CardW * 0.5f);
            c.back.gameObject.SetActive(false);   // dealt face up: the player gets a look first
            bg.gameObject.AddComponent<TapDown>().onDown = () => OnTap(c);
            Tween.Pop(c.rt, 0.25f + index * 0.012f);
            return c;
        }

        void ShowBig(string s)
        {
            bigText.text = s;
            bigTimer = 0.9f;
            Tween.Pop(bigRt, 0.3f);
        }

        void Flip(Card c, bool open)
        {
            c.open = open;
            var rt = c.rt;
            bool swapped = false;
            Tween.Run(FlipTime, t =>
            {
                if (!rt) return;
                // squeeze to nothing, swap sides, open up again
                float sx = t < 0.5f ? 1f - t * 2f : (t - 0.5f) * 2f;
                rt.localScale = new Vector3(Mathf.Max(0.02f, sx), 1f, 1f);
                if (t >= 0.5f && !swapped)
                {
                    swapped = true;
                    c.face.gameObject.SetActive(open);
                    c.back.gameObject.SetActive(!open);
                }
            }, () => { if (rt) rt.localScale = Vector3.one; }, Tween.Key(rt, "flip"));
        }

        // ------------------------------------------------------------------ input
        void OnTap(Card c)
        {
            if (!running || resolving || nextIn > 0f || previewIn > 0f || c.open || c.matched) return;
            Flip(c, true);
            Sfx.Play("page", Random.Range(1.1f, 1.3f), 0.8f);
            faceUp.Add(c);
            if (faceUp.Count < 2) return;
            resolving = true;
            bool match = faceUp[0].kind == faceUp[1].kind;
            resolveIn = match ? FlipTime + 0.12f : ShowWrong;
        }

        void Resolve()
        {
            resolving = false;
            var a = faceUp[0];
            var b = faceUp[1];
            faceUp.Clear();
            if (a.kind == b.kind)
            {
                a.matched = b.matched = true;
                combo++;
                int gain = 1 + (combo >= 2 ? 1 : 0);
                score += gain;
                pairsLeft--;
                foreach (var c in new[] { a, b })
                {
                    c.rt.GetComponent<Image>().color = new Color(0.75f, 0.95f, 0.65f);
                    Tween.Punch(c.rt, 0.15f, 0.3f);
                }
                if (a.kind == Hourglass)
                {
                    timeLeft += MiniGames.HourglassBonus;
                    ShowBig(L.F("+{0} sn", (int)MiniGames.HourglassBonus));
                    Sfx.Play("gift");
                }
                else Sfx.Play(combo >= 2 ? "coin" : "pop", 1f + Mathf.Min(combo, 6) * 0.05f);
                comboText.text = combo >= 2 ? L.F("Seri x{0}! +1", combo) : "";
                Tween.Punch(scoreText.transform, 0.25f, 0.25f);
                if (pairsLeft <= 0)
                {
                    score += 3;
                    ShowBig(L.T("Masa temiz! +3"));
                    Sfx.Play("levelup", 1.1f);
                    round++;
                    nextIn = 0.9f;
                }
            }
            else
            {
                combo = 0;
                comboText.text = "";
                Flip(a, false);
                Flip(b, false);
                Sfx.Play("error", 1.2f, 0.5f);
            }
            UpdateHud();
        }

        void HideAll()
        {
            previewIn = 0f;
            bigText.text = "";
            foreach (var c in cards) Flip(c, false);
            Sfx.Play("whoosh", 1.4f, 0.6f);
        }

        void Finish()
        {
            running = false;
            ending = true;
            endWait = 1.2f;
            resolving = false;
            endBtn.gameObject.SetActive(false);
            ShowBig(L.T("Bitti!"));
            Sfx.Play("ready");
        }

        // ------------------------------------------------------------------ loop
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
                    endBtn.gameObject.SetActive(true);
                    HideAll();
                    ShowBig(L.T("Başla!"));
                    Sfx.Play("whoosh", 1.2f);
                }
                else if (now != before) { ShowBig(now.ToString()); Sfx.Play("click", 1.2f); }
                return;
            }

            if (running)
            {
                if (previewIn > 0f)
                {
                    // a new table is on show: the clock waits
                    previewIn -= dt;
                    if (previewIn <= 0f) HideAll();
                    UpdateHud();
                    return;
                }
                timeLeft -= dt;
                if (bigTimer > 0f) { bigTimer -= dt; if (bigTimer <= 0f && nextIn <= 0f) bigText.text = ""; }
                if (resolving) { resolveIn -= dt; if (resolveIn <= 0f) Resolve(); }
                if (nextIn > 0f)
                {
                    nextIn -= dt;
                    if (nextIn <= 0f) { bigText.text = ""; Deal(); ShowBig(L.T("Ezberle!")); }
                }
                if (timeLeft <= 0f) { timeLeft = 0f; Finish(); }
                UpdateHud();
            }

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

        void UpdateHud()
        {
            scoreText.text = score.ToString();
            float frac = Mathf.Clamp01(timeLeft / MiniGames.MemorySeconds);
            timeFill.sizeDelta = new Vector2(Mathf.Max(32f, BarW * frac), 32f);
            timeFillImg.color = timeLeft > 8f ? new Color(0.55f, 0.85f, 0.45f) : new Color(1f, 0.45f, 0.4f);
            timeText.text = L.F("{0} sn", Mathf.CeilToInt(timeLeft));
        }
    }
}
