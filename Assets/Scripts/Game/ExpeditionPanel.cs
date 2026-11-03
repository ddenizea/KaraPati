using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>"Usta'yı Keşfe Gönder": pick one of three trips. Longer trips bring better finds.</summary>
    public class ExpeditionPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel;
        readonly Image[] goImgs = new Image[3];

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.03f, 0.01f, 0.06f, 0.7f);
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.93f, 0.86f, 0.72f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the panel don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(960f, 1080f));

            var ink = new Color(0.3f, 0.18f, 0.1f);
            var cat = UIKit.Img("Usta", panel, ProcArt.SideCat(3, 4), Color.white);
            cat.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -20f), new Vector2(120f, 120f));
            var title = UIKit.Txt("Title", panel, "Usta'yı Keşfe Gönder", 50, ink, TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(160f, -30f), new Vector2(640f, 64f));
            var sub = UIKit.Txt("Sub", panel, "Enerji harcar. Keşifteyken malzeme getirmez, yavrulara tıslamaz.", 26, new Color(0.4f, 0.28f, 0.2f), TextAnchor.MiddleLeft, false);
            sub.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(162f, -94f), new Vector2(700f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.75f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            for (int k = 0; k < Expeditions.Names.Length; k++) Row(k, ink);
            gameObject.SetActive(false);
        }

        void Row(int k, Color ink)
        {
            var card = UIKit.Img("Trip" + k, panel, ProcArt.Rounded, new Color(1f, 0.95f, 0.85f), true);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(30f, 0f);
            rt.offsetMax = new Vector2(-30f, 0f);
            rt.anchoredPosition = new Vector2(0f, -160f - k * 300f);
            rt.sizeDelta = new Vector2(-60f, 280f);

            // one, two or three paw prints show how far the trip goes
            for (int i = 0; i <= k; i++)
            {
                var paw = UIKit.Img("Paw", rt, ProcArt.PawPrint, new Color(0.85f, 0.45f, 0.3f));
                paw.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(60f + i * 52f, -56f), new Vector2(46f, 46f));
            }
            var name = UIKit.Txt("Name", rt, Expeditions.Names[k], 40, ink, TextAnchor.MiddleLeft, false);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(200f, -24f), new Vector2(420f, 60f));
            var dur = UIKit.Txt("Dur", rt, Expeditions.Duration(k), 34, UIKit.Orange, TextAnchor.MiddleRight, false);
            dur.fontStyle = FontStyle.Bold;
            dur.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-30f, -24f), new Vector2(200f, 60f));
            var reward = UIKit.Txt("Reward", rt, "Getirir: " + Expeditions.Rewards(k), 28, new Color(0.35f, 0.25f, 0.2f), TextAnchor.UpperLeft, false);
            reward.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(36f, -100f), new Vector2(560f, 140f));

            int kk = k;
            var go = UIKit.Btn("Go", rt, "Gönder", UIKit.Green, () =>
            {
                if (GM.Energy < Expeditions.Energy[kk]) { GM.Toast("Bu keşif için " + Expeditions.Energy[kk] + " enerji gerekiyor."); Sfx.Play("error"); return; }
                Close();
                GM.Cats.SendOnTrip(kk);
            }, 34);
            ((RectTransform)go.transform).Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f)).At(new Vector2(-24f, 24f), new Vector2(260f, 100f));
            // the energy price on the button: bolt + number
            var lbl = go.Label();
            lbl.alignment = TextAnchor.MiddleLeft;
            UIKit.Stretch(lbl.rectTransform, 24f, 0f, 0f, 0f);
            var bolt = UIKit.Img("Bolt", go.transform, ProcArt.Bolt, Color.white);
            bolt.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(-82f, 0f), new Vector2(50f, 50f));
            bolt.raycastTarget = false;
            var cost = UIKit.Txt("Cost", go.transform, Expeditions.Energy[k].ToString(), 36, Color.white, TextAnchor.MiddleLeft, false);
            cost.fontStyle = FontStyle.Bold;
            cost.raycastTarget = false;
            cost.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(60f, 50f));
            goImgs[k] = go.GetComponent<Image>();
        }

        public void Open()
        {
            Sfx.Play("page");
            for (int k = 0; k < goImgs.Length; k++)
                if (goImgs[k]) goImgs[k].color = GM.Energy >= Expeditions.Energy[k] ? UIKit.Green : UIKit.Disabled;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Tween.Pop(panel, 0.3f);
        }

        public void Close() => gameObject.SetActive(false);
    }
}
