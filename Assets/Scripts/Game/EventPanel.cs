using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>The pumpkin event: timer, pumpkin count and the reward path.</summary>
    public class EventPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, list;
        Text timer, count;
        RectTransform barFill;
        const float BarW = 640f;
        float refresh;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.75f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.3f, 0.16f, 0.36f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1660f));

            var moon = UIKit.Img("Moon", panel, ProcArt.Moon, new Color(1f, 1f, 1f, 0.3f));
            moon.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-180f, -100f), new Vector2(180f, 180f));
            var big = UIKit.Img("Pumpkin", panel, ProcArt.Pumpkin, Color.white);
            big.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(110f, -100f), new Vector2(150f, 150f));

            var title = UIKit.Txt("Title", panel, PumpkinEvent.Name, 54, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -40f), new Vector2(640f, 70f));
            timer = UIKit.Txt("Timer", panel, "", 30, new Color(1f, 0.9f, 0.6f));
            timer.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(20f, -110f), new Vector2(640f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            var info = UIKit.Txt("Info", panel, "Siparişler ve dükkân görevleri balkabağı verir. Topla, yoldaki ödülleri aç!", 26, new Color(1f, 1f, 1f, 0.75f));
            info.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -170f), new Vector2(900f, 40f));

            var barBg = UIKit.Img("Bar", panel, ProcArt.Rounded, new Color(0.18f, 0.1f, 0.22f), true);
            barBg.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(30f, -225f), new Vector2(BarW + 12f, 54f));
            var fill = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, UIKit.Orange, true);
            barFill = fill.rectTransform;
            barFill.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(6f, 0f), new Vector2(40f, 42f));
            count = UIKit.Txt("Count", barBg.transform, "", 30, Color.white);
            UIKit.Stretch(count.rectTransform);
            var pi = UIKit.Img("Icon", barBg.transform, ProcArt.Pumpkin, Color.white);
            pi.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(90f, 90f));

            list = UIKit.Rect("Path", panel);
            UIKit.Stretch(list, 40f, 40f, 300f, 30f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Sfx.Play("page");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);

        void Update()
        {
            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0f) return;
            refresh = 1f;
            timer.text = GM.EventActive ? "Bitmesine: " + GM.EventTimeLeftText : "Etkinlik sona erdi. Toplanan ödüller senin!";
        }

        void Fill()
        {
            refresh = 0f;
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            var path = PumpkinEvent.Path;
            int have = GM.Pumpkins;
            int next = 0;
            while (next < path.Length && have >= path[next].need) next++;
            int target = next < path.Length ? path[next].need : path[path.Length - 1].need;
            count.text = have + " / " + target;
            barFill.sizeDelta = new Vector2(Mathf.Max(40f, BarW * Mathf.Clamp01(have / (float)target)), 42f);

            for (int i = 0; i < path.Length; i++)
            {
                int idx = i;
                var m = path[i];
                bool reached = have >= m.need;
                bool claimed = i < GM.EventClaimed;
                bool claimable = reached && !claimed && i == GM.EventClaimed;

                var row = UIKit.Img("Step", list, ProcArt.Rounded,
                    claimed ? new Color(0.22f, 0.13f, 0.27f, 0.8f) : reached ? new Color(0.5f, 0.3f, 0.2f) : new Color(0.2f, 0.12f, 0.25f), true);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 132f);
                rt.sizeDelta = new Vector2(0f, 120f);

                var num = UIKit.Img("Num", rt, ProcArt.Circle, reached ? UIKit.Orange : new Color(0.35f, 0.25f, 0.4f));
                num.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(16f, 0f), new Vector2(80f, 80f));
                var nt = UIKit.Txt("N", num.transform, (i + 1).ToString(), 34, Color.white);
                UIKit.Stretch(nt.rectTransform);

                var icon = UIKit.Img("Reward", rt, RewardIcon(m), claimed ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
                icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(116f, 0f), new Vector2(92f, 92f));
                var rtext = UIKit.Txt("Text", rt, PumpkinEvent.RewardText(m), 32, claimed ? new Color(1f, 1f, 1f, 0.5f) : UIKit.Cream, TextAnchor.MiddleLeft);
                rtext.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(226f, 14f), new Vector2(380f, 44f));
                var need = UIKit.Txt("Need", rt, m.need + " balkabağı", 24, new Color(1f, 0.8f, 0.5f, claimed ? 0.5f : 0.9f), TextAnchor.MiddleLeft);
                need.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(228f, -26f), new Vector2(380f, 34f));

                if (claimed)
                {
                    var ok = UIKit.Img("Done", rt, ProcArt.Check, Color.white);
                    ok.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-60f, 0f), new Vector2(70f, 70f));
                }
                else if (claimable)
                {
                    var b = UIKit.Btn("Claim", rt, "Al!", UIKit.Green, () => { if (GM.ClaimEventStep(idx)) Fill(); }, 38);
                    ((RectTransform)b.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-16f, 0f), new Vector2(180f, 88f));
                    Tween.Punch(b.transform, 0.1f, 0.4f);
                }
                else
                {
                    var lk = UIKit.Img("Lock", rt, ProcArt.Lock, new Color(1f, 1f, 1f, reached ? 0.8f : 0.4f));
                    lk.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-66f, 0f), new Vector2(60f, 60f));
                }
            }
        }

        static Sprite RewardIcon(EventMilestone m)
        {
            switch (m.type)
            {
                case "gold": return ProcArt.Coin;
                case "energy": return ProcArt.Bolt;
                case "stamps": return ProcArt.Stamp;
                default: return ProcArt.AlbumIcon;
            }
        }
    }
}
