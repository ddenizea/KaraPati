using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>One mini game inside the Game Room. It builds its own playfield once and reports the score when a round ends.</summary>
    public abstract class MiniGame : MonoBehaviour
    {
        public abstract void Build(RectTransform area);
        /// <summary>Start a round; done(score) is called when it is over.</summary>
        public abstract void Play(Action<int> done);
        /// <summary>Stop at once without reporting (the room was closed).</summary>
        public abstract void Stop();
        /// <summary>This round was paid with energy (some bonuses are for free plays only).</summary>
        public bool Paid { get; set; }
        /// <summary>The round earned the game's extra prize (e.g. Yumak Atma's gift chest).</summary>
        public virtual bool BonusEarned => false;
    }

    /// <summary>Reacts the moment a finger touches (not on release), so fast taps in mini games count at once.</summary>
    public class TapDown : MonoBehaviour, IPointerDownHandler
    {
        public Action onDown;
        public void OnPointerDown(PointerEventData e) => onDown?.Invoke();
    }

    /// <summary>
    /// L.T("Oyun Odası"): one screen with a card per mini game. Each game has 2 free plays a day, after that a play costs
    /// 10 energy. Tapping a card shows the rules and the price, the game runs on the same screen, and the result
    /// shows the reward.
    /// </summary>
    public class GameRoomPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform content, hub, playArea, popup, popCard;
        Text energyText, popTitle, popBody;
        Image popIcon;
        Button popMain, popSecond;
        Action popMainAct, popSecondAct;
        readonly Card[] cards = new Card[MiniGames.Count];
        readonly MiniGame[] games = new MiniGame[MiniGames.Count];
        int current = -1;
        bool playing, paid;
        float tick;

        class Card
        {
            public Text best, free;
            public Button play;
        }

        public bool IsOpen => gameObject.activeSelf;

        static Sprite Icon(int g) =>
            g == MiniGames.Fare ? ProcArt.Mouse(false) : g == MiniGames.Yumak ? ProcArt.YarnBall : ProcArt.Item(Chains.Kristal, 2);

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = ProcArt.Gradient;
            bg.color = new Color(0.85f, 0.8f, 0.95f);
            bg.raycastTarget = true;   // nothing behind the room can be tapped

            // same 1080 x 2060 frame as the rest of the game, centred
            content = UIKit.Rect("Content", root);
            content.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(Vector2.zero, new Vector2(1080f, 2060f));

            hub = UIKit.Fill(content, "Hub");
            BuildHub();

            playArea = UIKit.Fill(content, "Play");
            var fare = UIKit.Fill(playArea, "FareAvi");
            games[MiniGames.Fare] = fare.gameObject.AddComponent<MouseHunt>();
            games[MiniGames.Fare].Build(fare);
            fare.gameObject.SetActive(false);
            var yumak = UIKit.Fill(playArea, "YumakAtma");
            games[MiniGames.Yumak] = yumak.gameObject.AddComponent<YarnToss>();
            games[MiniGames.Yumak].Build(yumak);
            yumak.gameObject.SetActive(false);
            playArea.gameObject.SetActive(false);

            BuildPopup();
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ hub
        void BuildHub()
        {
            var title = UIKit.Txt("Title", hub, L.T("Oyun Odası"), 66, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -60f), new Vector2(600f, 90f));
            var sub = UIKit.Txt("Sub", hub, L.F("Her oyunun günde {0} ücretsiz hakkı var.\nSonrası bir oyun {1} enerji.", MiniGames.FreePerDay, MiniGames.EnergyCost),
                30, new Color(1f, 1f, 1f, 0.8f));
            sub.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -160f), new Vector2(900f, 90f));

            // energy pill (the top bar is under this screen)
            var pill = UIKit.Img("Energy", hub, ProcArt.Rounded, UIKit.PanelDark, true);
            pill.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -40f), new Vector2(190f, 84f));
            var bolt = UIKit.Img("Bolt", pill.transform, ProcArt.Bolt, Color.white);
            bolt.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f, 0f), new Vector2(66f, 66f));
            energyText = UIKit.Txt("Value", pill.transform, "0", 38, UIKit.Cream, TextAnchor.MiddleLeft);
            UIKit.Stretch(energyText.rectTransform, 78f, 8f);

            var close = UIKit.Btn("Close", hub, "X", new Color(0.8f, 0.3f, 0.35f), Close, 48);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-30f, -36f), new Vector2(100f, 100f));

            for (int g = 0; g < MiniGames.Count; g++) cards[g] = BuildCard(g, 290f + g * 440f);

            // Kara Pati at the bottom with a reminder of the rewards
            var head = UIKit.Img("KaraPati", hub, ProcArt.KaraPatiHead, Color.white);
            head.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0.5f, 0.5f)).At(new Vector2(150f, 230f), new Vector2(170f, 170f));
            var say = UIKit.Img("Say", hub, ProcArt.Rounded, new Color(0.98f, 0.94f, 0.86f, 0.95f), true);
            say.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0.5f)).At(new Vector2(250f, 230f), new Vector2(790f, 130f));
            var sayText = UIKit.Txt("Text", say.transform, L.T("Ücretsiz oyunların ödülü daha büyük.\nHaklar her gece yenilenir!"), 30,
                new Color(0.25f, 0.15f, 0.3f), TextAnchor.MiddleLeft, false);
            UIKit.Stretch(sayText.rectTransform, 28f, 20f, 8f, 8f);
        }

        Card BuildCard(int g, float top)
        {
            bool ready = MiniGames.Ready(g);
            var c = new Card();
            var bg = UIKit.Img("Card" + g, hub, ProcArt.Rounded, ready ? new Color(0.26f, 0.18f, 0.38f, 0.98f) : new Color(0.2f, 0.15f, 0.27f, 0.9f), true);
            var rt = bg.rectTransform;
            rt.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -top), new Vector2(980f, 400f));

            var disc = UIKit.Img("Disc", rt, ProcArt.Circle, ready ? new Color(1f, 0.93f, 0.8f) : new Color(0.6f, 0.55f, 0.65f));
            disc.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(170f, 0f), new Vector2(270f, 270f));
            var icon = UIKit.Img("Icon", disc.transform, Icon(g), ready ? Color.white : new Color(1f, 1f, 1f, 0.45f));
            icon.rectTransform.sizeDelta = new Vector2(210f, 210f);

            var name = UIKit.Txt("Name", rt, MiniGames.Names[g], 48, ready ? UIKit.Orange : new Color(0.8f, 0.72f, 0.85f), TextAnchor.MiddleLeft);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(330f, -26f), new Vector2(620f, 64f));
            var blurb = UIKit.Txt("Blurb", rt, MiniGames.Blurbs[g], 30, new Color(1f, 1f, 1f, ready ? 0.85f : 0.5f), TextAnchor.UpperLeft, false);
            blurb.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(330f, -96f), new Vector2(610f, 90f));

            c.best = UIKit.Txt("Best", rt, "", 28, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleLeft, false);
            c.best.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0f)).At(new Vector2(330f, 92f), new Vector2(340f, 40f));
            c.free = UIKit.Txt("Free", rt, "", 28, UIKit.Cream, TextAnchor.MiddleLeft, false);
            c.free.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(0f, 0f)).At(new Vector2(330f, 40f), new Vector2(360f, 40f));

            int gi = g;
            c.play = UIKit.Btn("Play", rt, ready ? L.T("Oyna") : L.T("Yakında"), ready ? UIKit.Green : UIKit.Disabled, () => ShowIntro(gi), 40);
            ((RectTransform)c.play.transform).Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f)).At(new Vector2(-30f, 30f), new Vector2(250f, 104f));
            c.play.interactable = ready;
            return c;
        }

        void RefreshHub()
        {
            energyText.text = GM.Energy.ToString();
            for (int g = 0; g < MiniGames.Count; g++)
            {
                var c = cards[g];
                if (!MiniGames.Ready(g)) { c.best.text = ""; c.free.text = L.T("Çok yakında burada!"); continue; }
                int best = GM.MiniBest(g);
                c.best.text = best > 0 ? L.F("En iyi skor: {0}", best) : L.T("Henüz oynanmadı");
                int left = GM.MiniFreeLeft(g);
                c.free.text = left > 0 ? L.F("Bugün {0} ücretsiz hak", left) : L.F("Ücretsiz hak bitti: {0} enerji", MiniGames.EnergyCost);
                c.free.color = left > 0 ? new Color(0.6f, 0.95f, 0.55f) : UIKit.Cream;
            }
        }

        // ------------------------------------------------------------------ popup (rules and results)
        void BuildPopup()
        {
            popup = UIKit.Fill(content, "Popup");
            var dim = popup.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.01f, 0.06f, 0.6f);
            dim.raycastTarget = true;

            var card = UIKit.Img("Card", popup, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.99f), true);
            popCard = card.rectTransform;
            popCard.At(new Vector2(0f, 40f), new Vector2(880f, 820f));

            var disc = UIKit.Img("Disc", popCard, ProcArt.Circle, new Color(1f, 0.93f, 0.8f));
            disc.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -40f), new Vector2(220f, 220f));
            popIcon = UIKit.Img("Icon", disc.transform, ProcArt.Mouse(false), Color.white);
            popIcon.rectTransform.sizeDelta = new Vector2(170f, 170f);

            popTitle = UIKit.Txt("Title", popCard, "", 54, UIKit.Orange);
            popTitle.fontStyle = FontStyle.Bold;
            popTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -160f), new Vector2(800f, 80f));
            popBody = UIKit.Txt("Body", popCard, "", 32, UIKit.Cream, TextAnchor.UpperCenter, false);
            popBody.lineSpacing = 1.15f;
            popBody.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -250f), new Vector2(780f, 360f));

            popSecond = UIKit.Btn("Second", popCard, "", new Color(0.45f, 0.36f, 0.55f), () => popSecondAct?.Invoke(), 38);
            popMain = UIKit.Btn("Main", popCard, "", UIKit.Green, () => popMainAct?.Invoke(), 40);
            popup.gameObject.SetActive(false);
        }

        void ShowPopup(int g, string title, string body, string main, Action mainAct, string second, Action secondAct, bool mainEnabled = true)
        {
            popIcon.sprite = Icon(g);
            popTitle.text = title;
            popBody.text = body;
            popMainAct = mainAct;
            popSecondAct = secondAct;
            popMain.Label().text = main;
            popMain.interactable = mainEnabled;
            popMain.GetComponent<Image>().color = mainEnabled ? UIKit.Green : UIKit.Disabled;
            bool two = second != null;
            popSecond.gameObject.SetActive(two);
            if (two) popSecond.Label().text = second;
            ((RectTransform)popMain.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(two ? 200f : 0f, 44f), new Vector2(360f, 116f));
            ((RectTransform)popSecond.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(-200f, 44f), new Vector2(360f, 116f));
            popup.gameObject.SetActive(true);
            popup.SetAsLastSibling();
            Tween.Pop(popCard, 0.3f);
        }

        void HidePopup() => popup.gameObject.SetActive(false);

        // ------------------------------------------------------------------ flow
        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowHub();
            Tween.Pop(hub, 0.25f);
        }

        public void Close()
        {
            if (playing) { games[current].Stop(); playing = false; }
            HidePopup();
            gameObject.SetActive(false);
            GM.OnGameRoomClosed();
        }

        void ShowHub()
        {
            if (current >= 0 && games[current]) games[current].gameObject.SetActive(false);
            current = -1;
            HidePopup();
            playArea.gameObject.SetActive(false);
            hub.gameObject.SetActive(true);
            RefreshHub();
        }

        void ShowIntro(int g)
        {
            if (!MiniGames.Ready(g)) return;
            current = g;
            int left = GM.MiniFreeLeft(g);
            bool can = left > 0 || GM.Energy >= MiniGames.EnergyCost;
            string price = left > 0
                ? L.F("<color=#9BF28C>Ücretsiz oyun</color> (bugün {0} hakkın var)", left) + "\n" + RewardLine(g, false)
                : can ? L.F("Bu oyun <color=#FFD24D>{0} enerji</color> (sende {1} var)", MiniGames.EnergyCost, GM.Energy) + "\n" + RewardLine(g, true)
                      : L.F("<color=#FF9A8A>Enerjin yetmiyor.</color> Bir oyun {0} enerji, sende {1} var.\nBiraz bekle, enerji kendiliğinden dolar.", MiniGames.EnergyCost, GM.Energy);
            ShowPopup(g, MiniGames.Names[g], Rules(g) + "\n\n" + price, L.T("Başla"), () => StartGame(g), L.T("Vazgeç"), ShowHub, can);
        }

        static string Rules(int g)
        {
            switch (g)
            {
                case MiniGames.Fare:
                    return L.F("Fıçılardan fareler çıkıyor: {0} saniyede yakalayabildiğin kadar yakala!\n<color=#FFD24D>Altın fare</color> 3 puan. Hayalet müşteriyi korkutursan 2 puan gider.", (int)MiniGames.FareSeconds);
                case MiniGames.Yumak:
                    return L.F("Usta ve Çırak sırayla zıplar. Kedi tepedeyken, halka parladığında ekrana dokun: yumak ona uçar!\nTam tepede <color=#FFD24D>Mükemmel</color> 2 puan. {0} kaçırışta oyun biter. Ücretsiz oyunda {1} seri yakalarsan hediye sandığı!",
                        MiniGames.YarnLives, MiniGames.YarnChestStreak);
                default: return MiniGames.Blurbs[g];
            }
        }

        static string RewardLine(int g, bool paid)
        {
            if (g == MiniGames.Fare) return paid ? L.T("Ödül: altın") : L.T("Ödül: enerji");
            if (g == MiniGames.Yumak) return paid ? L.T("Ödül: altın (sandık sadece ücretsiz oyunda)") : L.T("Ödül: altın + hediye sandığı şansı");
            return L.T("Ödül: altın");
        }

        void StartGame(int g)
        {
            if (!GM.PayMini(g, out paid)) { Sfx.Play("error"); ShowIntro(g); return; }
            HidePopup();
            hub.gameObject.SetActive(false);
            playArea.gameObject.SetActive(true);
            games[g].gameObject.SetActive(true);
            games[g].Paid = paid;
            playing = true;
            Sfx.Play("whoosh");
            games[g].Play(score => OnRoundOver(g, score));
        }

        void OnRoundOver(int g, int score)
        {
            playing = false;
            int reward = 0;
            string rewardText;
            if (g == MiniGames.Fare)
            {
                reward = paid ? MiniGames.FareGold(score) : MiniGames.FareEnergy(score);
                if (paid) GM.AddGold(reward); else GM.AddEnergy(reward);
                rewardText = reward > 0 ? (paid ? L.F("Ödül: <color=#FFD24D>+{0} altın</color>", reward) : L.F("Ödül: <color=#FFD24D>+{0} enerji</color>", reward)) : L.T("Bu sefer fareler kaçtı, ödül yok.");
            }
            else if (g == MiniGames.Yumak)
            {
                reward = paid ? MiniGames.YarnGoldPaid(score) : MiniGames.YarnGold(score);
                GM.AddGold(reward);
                rewardText = reward > 0 ? L.F("Ödül: <color=#FFD24D>+{0} altın</color>", reward) : L.T("Yumak hep yere düştü, bu sefer ödül yok.");
                if (!paid && games[g].BonusEarned)
                    rewardText += "\n" + L.T("<color=#9BF28C>Hediye sandığı!</color>") + " " + GM.GiveMiniChest();
            }
            else rewardText = "";
            bool record = GM.MiniRecord(g, score);
            Sfx.Play(record && score > 0 ? "levelup" : reward > 0 ? "gift" : "pop");

            string body = L.F("Skor: <size=48><b>{0}</b></size>", score) + "\n" +
                          (record && score > 0 ? L.T("<color=#9BF28C>Yeni rekor!</color>") : L.F("En iyi skor: {0}", GM.MiniBest(g))) + "\n\n" + rewardText;
            int left = GM.MiniFreeLeft(g);
            string again = left > 0 ? L.T("Tekrar (ücretsiz)") : L.F("Tekrar ({0} enerji)", MiniGames.EnergyCost);
            ShowPopup(g, g == MiniGames.Fare ? L.T("Süre doldu!") : L.T("Oyun bitti!"), body, again, () => ShowIntro(g), L.T("Odaya Dön"), ShowHub, true);
        }

        void Update()
        {
            if (!IsOpen) return;
            tick -= Time.unscaledDeltaTime;
            if (tick > 0f) return;
            tick = 0.5f;
            energyText.text = GM.Energy.ToString();
        }
    }
}
