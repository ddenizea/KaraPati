using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Boots the whole prototype from code: builds the UI, owns gold/energy, saving and toasts.
    /// Drop-in: it spawns itself in any scene, so pressing Play is enough.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("KaraPati Game").AddComponent<GameManager>();
        }

        public const int MaxEnergy = 30;
        public const float EnergyRegenSec = 8f;
        public const int EnergyPackCost = 20, EnergyPackAmount = 10;
        const string SaveKey = "karapati_save_v1";

        public int Gold { get; private set; }
        public int Energy { get; private set; }
        public int Served { get; private set; }
        public bool KittenHintShown { get; set; }

        public RectTransform Root { get; private set; }
        public BoardController Board { get; private set; }
        public OrderPanel Orders { get; private set; }
        public KittenChaos Chaos { get; private set; }

        CanvasScaler scaler;
        Text goldText, energyText, energyTimerText, toastText, infoTitle, infoDesc;
        CanvasGroup toastGroup;
        Button actionBtn;
        RectTransform goldIcon;
        float energyTimer;
        bool dirty;
        float saveTimer;
        Coroutine toastCo;
        float resetArmedUntil;
        int tipIndex;

        static readonly string[] tips =
        {
            "Aynı iki eşyayı üst üste sürükle, birleşsinler.",
            "Kazana ve sandığa dokunarak malzeme üret (1 enerji).",
            "Hayaletlerin istediklerini tahtada topla, sonra 'Ver'e bas.",
            "Yavru kediler kaos çıkarır. Birleştir, yardımcıya dönüşsünler!",
            "Enerjin bitince altınla alabilirsin (+ düğmesi)."
        };

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            SetupCamera();
            EnsureEventSystem();
            BuildUI();
            Load();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void Update()
        {
            UpdateScalerMatch();

            if (Energy < MaxEnergy)
            {
                energyTimer += Time.deltaTime;
                if (energyTimer >= EnergyRegenSec)
                {
                    energyTimer -= EnergyRegenSec;
                    Energy++;
                    RefreshHud();
                    MarkDirty();
                }
                energyTimerText.text = Energy < MaxEnergy ? Mathf.CeilToInt(EnergyRegenSec - energyTimer) + " sn" : "dolu";
            }
            else
            {
                energyTimer = 0f;
                energyTimerText.text = "dolu";
            }

            if (resetArmedUntil > 0f && Time.unscaledTime > resetArmedUntil)
            {
                resetArmedUntil = 0f;
                if (Board.Get(Board.Selected) == null) ShowInfo(null);
            }

            if (dirty)
            {
                saveTimer -= Time.unscaledDeltaTime;
                if (saveTimer <= 0f) Save();
            }
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();

        // ------------------------------------------------------------------ setup
        void SetupCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.06f, 0.14f);
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void UpdateScalerMatch()
        {
            if (!scaler) return;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0.5625f;
            scaler.matchWidthOrHeight = aspect > 1080f / 1920f ? 1f : 0f;
        }

        RectTransform Section(string name, float top, float height)
        {
            var rt = UIKit.Rect(name, Root);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
            return rt;
        }

        void BuildUI()
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateScalerMatch();
            var canvasRt = (RectTransform)cgo.transform;

            var bg = UIKit.Img("Background", canvasRt, ProcArt.Gradient, Color.white);
            UIKit.Stretch(bg.rectTransform);

            // a few twinkling stars in the background
            for (int i = 0; i < 14; i++)
            {
                var s = UIKit.Img("Star", canvasRt, ProcArt.Sparkle, new Color(1f, 0.95f, 0.8f, UnityEngine.Random.Range(0.15f, 0.45f)));
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
                float sz = UnityEngine.Random.Range(12f, 30f);
                s.rectTransform.sizeDelta = new Vector2(sz, sz);
            }

            Root = UIKit.Rect("Root", canvasRt);
            Root.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(Vector2.zero, new Vector2(1080f, 1920f));

            BuildTopBar(Section("TopBar", 12f, 115f));

            var ordersArea = Section("Orders", 138f, 400f);
            Orders = ordersArea.gameObject.AddComponent<OrderPanel>();
            Orders.Build(ordersArea);

            var toastArea = Section("Toast", 542f, 70f);
            toastGroup = toastArea.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastText = UIKit.Txt("Text", toastArea, "", 30, UIKit.Cream);
            UIKit.Stretch(toastText.rectTransform, 30f, 30f);

            var boardArea = Section("Board", 618f, 1150f);
            Board = boardArea.gameObject.AddComponent<BoardController>();
            Board.Build(boardArea);

            BuildInfoBar(Section("InfoBar", 1776f, 136f));

            Chaos = gameObject.AddComponent<KittenChaos>();
        }

        void BuildTopBar(RectTransform bar)
        {
            // gold
            var gold = UIKit.Img("Gold", bar, ProcArt.Rounded, UIKit.PanelDark, true);
            gold.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(250f, 92f));
            var coin = UIKit.Img("Coin", gold.transform, ProcArt.Coin, Color.white);
            goldIcon = coin.rectTransform;
            goldIcon.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(10f, 0f), new Vector2(72f, 72f));
            goldText = UIKit.Txt("Value", gold.transform, "0", 42, UIKit.Cream, TextAnchor.MiddleLeft);
            UIKit.Stretch(goldText.rectTransform, 92f, 10f);

            // title
            var title = UIKit.Txt("Title", bar, "Kara Pati'nin\nİksir Dükkânı", 34, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(-15f, 0f), new Vector2(380f, 110f));

            // energy
            var en = UIKit.Img("Energy", bar, ProcArt.Rounded, UIKit.PanelDark, true);
            en.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(330f, 92f));
            var bolt = UIKit.Img("Bolt", en.transform, ProcArt.Bolt, Color.white);
            bolt.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(8f, 0f), new Vector2(70f, 70f));
            energyText = UIKit.Txt("Value", en.transform, "30/30", 38, UIKit.Cream, TextAnchor.MiddleLeft);
            energyText.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(82f, 10f), new Vector2(150f, 50f));
            energyTimerText = UIKit.Txt("Timer", en.transform, "", 22, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleLeft);
            energyTimerText.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(84f, -24f), new Vector2(150f, 30f));
            var plus = UIKit.Btn("Buy", en.transform, "+", UIKit.Green, BuyEnergy, 48);
            ((RectTransform)plus.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-10f, 0f), new Vector2(76f, 76f));
        }

        void BuildInfoBar(RectTransform area)
        {
            var panel = UIKit.Img("Panel", area, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 35f, 35f, 0f, 0f);
            infoTitle = UIKit.Txt("Title", panel.transform, "", 34, UIKit.Orange, TextAnchor.UpperLeft);
            infoTitle.fontStyle = FontStyle.Bold;
            infoTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            infoTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            infoTitle.rectTransform.pivot = new Vector2(0f, 1f);
            infoTitle.rectTransform.offsetMin = new Vector2(30f, -60f);
            infoTitle.rectTransform.offsetMax = new Vector2(-260f, -16f);
            infoDesc = UIKit.Txt("Desc", panel.transform, "", 25, UIKit.Cream, TextAnchor.UpperLeft);
            infoDesc.rectTransform.anchorMin = new Vector2(0f, 0f);
            infoDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            infoDesc.rectTransform.offsetMin = new Vector2(30f, 8f);
            infoDesc.rectTransform.offsetMax = new Vector2(-260f, -60f);
            actionBtn = UIKit.Btn("Action", panel.transform, "", UIKit.Orange, OnAction, 30);
            ((RectTransform)actionBtn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(220f, 92f));
        }

        // ------------------------------------------------------------------ info bar
        public void ShowInfo(ItemData d)
        {
            if (infoTitle == null) return;
            var btnImg = actionBtn.GetComponent<Image>();
            if (d == null)
            {
                infoTitle.text = "İpucu";
                infoDesc.text = tips[tipIndex % tips.Length];
                bool armed = resetArmedUntil > Time.unscaledTime;
                actionBtn.gameObject.SetActive(true);
                actionBtn.Label().text = armed ? "Emin misin?" : "Yeni Oyun";
                btnImg.color = armed ? new Color(0.85f, 0.3f, 0.3f) : UIKit.Disabled;
                return;
            }

            infoTitle.text = d.Name + (Chains.IsGenerator(d.chain) ? "" : "  <size=26>(Sv. " + d.level + ")</size>");
            actionBtn.gameObject.SetActive(false);
            btnImg.color = UIKit.Orange;

            if (d.nap)
            {
                infoDesc.text = "Üstünde bir yavru uyuyor! Dokunup okşarsan kalkar.";
                return;
            }
            if (Chains.IsGenerator(d.chain))
            {
                infoDesc.text = (d.chain == Chains.Kazan ? "Ot ve mantar üretir." : "Kristal ve örümcek ağı üretir.") +
                                " Dokun: 1 enerji.";
                return;
            }
            if (d.IsKitten)
            {
                switch (d.level)
                {
                    case 1:
                        infoDesc.text = "Eşyaları iter, üstlerine yatar! İki yavru birleşirse Çırak Kedi olur.";
                        actionBtn.gameObject.SetActive(true);
                        actionBtn.Label().text = "Sahiplendir +3";
                        break;
                    case 2:
                        infoDesc.text = "Dokun: bedava malzeme getirir (" + (int)KittenChaos.ApprenticeCooldown + " sn bekler). İki çırak = Usta Kedi.";
                        break;
                    default:
                        infoDesc.text = "Her " + (int)KittenChaos.MasterInterval + " sn'de kendiliğinden malzeme getirir, yavruları sakinleştirir.";
                        break;
                }
                return;
            }
            int max = Chains.MaxLevel(d.chain);
            infoDesc.text = d.level < max
                ? "İki tanesini birleştir: " + Chains.Name(d.chain, d.level + 1)
                : "En üst seviye! Hayaletler buna bayılır.";
            actionBtn.gameObject.SetActive(true);
            actionBtn.Label().text = "Sat  +" + d.level;
        }

        void OnAction()
        {
            var d = Board.Get(Board.Selected);
            if (d != null) { Board.SellSelected(); return; }
            if (resetArmedUntil > Time.unscaledTime)
            {
                resetArmedUntil = 0f;
                NewGame();
                Toast("Yeni oyun başladı. Dükkân senin, Kara Pati!", UIKit.Orange);
                return;
            }
            resetArmedUntil = Time.unscaledTime + 3f;
            ShowInfo(null);
        }

        // ------------------------------------------------------------------ economy
        public void AddGold(int amount)
        {
            Gold += amount;
            RefreshHud();
            if (goldIcon) Tween.Punch(goldIcon, 0.25f, 0.3f);
            MarkDirty();
        }

        public bool TrySpendEnergy(int amount)
        {
            if (Energy < amount)
            {
                Toast("Enerjin bitti! Biraz bekle ya da altınla al (+).", new Color(1f, 0.6f, 0.5f));
                return false;
            }
            Energy -= amount;
            RefreshHud();
            MarkDirty();
            return true;
        }

        void BuyEnergy()
        {
            if (Gold < EnergyPackCost)
            {
                Toast(EnergyPackAmount + " enerji için " + EnergyPackCost + " altın gerekiyor.", new Color(1f, 0.6f, 0.5f));
                return;
            }
            Gold -= EnergyPackCost;
            Energy += EnergyPackAmount;
            RefreshHud();
            Toast("+" + EnergyPackAmount + " enerji! Kazan fokurduyor.");
            MarkDirty();
        }

        public void RegisterServed()
        {
            Served++;
            tipIndex++;
            MarkDirty();
        }

        void RefreshHud()
        {
            goldText.text = Gold.ToString();
            energyText.text = Energy + "/" + MaxEnergy;
        }

        public void OnBoardChanged()
        {
            Orders.Refresh();
            MarkDirty();
        }

        public void MarkDirty()
        {
            if (!dirty) saveTimer = 1.5f;
            dirty = true;
        }

        // ------------------------------------------------------------------ toast
        public void Toast(string msg, Color? color = null, float duration = 3f)
        {
            if (!toastText) return;
            toastText.text = msg;
            toastText.color = color ?? UIKit.Cream;
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo(duration));
        }

        IEnumerator ToastCo(float duration)
        {
            float t = 0f;
            toastGroup.alpha = 1f;
            Tween.Pop(toastText.transform, 0.25f);
            while (t < duration) { t += Time.unscaledDeltaTime; yield return null; }
            t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / 0.4f; yield return null; }
            toastGroup.alpha = 0f;
        }

        // ------------------------------------------------------------------ save / load
        void NewGame()
        {
            Gold = 20;
            Energy = MaxEnergy;
            energyTimer = 0f;
            Served = 0;
            KittenHintShown = false;
            tipIndex = 0;

            Board.ClearAll();
            Board.Set(new Vector2Int(1, 3), new ItemData(Chains.Kazan, 1), true);
            Board.Set(new Vector2Int(5, 3), new ItemData(Chains.Sandik, 1), true);
            Board.Set(new Vector2Int(2, 5), new ItemData(Chains.Ot, 1), true);
            Board.Set(new Vector2Int(3, 5), new ItemData(Chains.Ot, 1), true);
            Board.Set(new Vector2Int(4, 5), new ItemData(Chains.Mantar, 1), true);
            Board.Set(new Vector2Int(3, 6), new ItemData(Chains.Kristal, 1), true);
            Board.Set(new Vector2Int(2, 6), new ItemData(Chains.Ag, 1), true);

            Orders.ClearAll();
            Orders.FillEmpty();
            RefreshHud();
            ShowInfo(null);
            Save();
        }

        void Save()
        {
            dirty = false;
            if (Board == null || Orders == null) return;
            var s = new SaveData
            {
                gold = Gold,
                energy = Energy,
                energyTimer = energyTimer,
                stamp = DateTime.UtcNow.Ticks,
                served = Served,
                kittenHint = KittenHintShown
            };
            Board.WriteSave(s.cells);
            Orders.WriteSave(s.orders);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json))
            {
                NewGame();
                Toast("Cadı ortadan kayboldu, dükkân artık senin Kara Pati! Kazana dokun.", UIKit.Orange, 5f);
                return;
            }
            try
            {
                var s = JsonUtility.FromJson<SaveData>(json);
                if (s == null || s.cells == null || s.cells.Count == 0) { NewGame(); return; }
                Gold = s.gold;
                Served = s.served;
                KittenHintShown = s.kittenHint;
                Energy = s.energy;
                energyTimer = s.energyTimer;
                if (Energy < MaxEnergy && s.stamp > 0)
                {
                    float elapsed = (float)TimeSpan.FromTicks(Math.Max(0, DateTime.UtcNow.Ticks - s.stamp)).TotalSeconds + energyTimer;
                    int gain = Mathf.FloorToInt(elapsed / EnergyRegenSec);
                    Energy = Mathf.Min(MaxEnergy, Energy + gain);
                    energyTimer = Energy < MaxEnergy ? elapsed % EnergyRegenSec : 0f;
                }
                Board.ReadSave(s.cells);
                Orders.ReadSave(s.orders);
                RefreshHud();
                ShowInfo(null);
                Toast("Tekrar hoş geldin, Kara Pati! Hayaletler seni bekliyordu.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("KaraPati: kayıt okunamadı, yeni oyun başlıyor. " + e.Message);
                NewGame();
            }
        }
    }
}
