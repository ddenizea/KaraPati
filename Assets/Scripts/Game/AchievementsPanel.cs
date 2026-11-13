using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// "Başarımlar": 12 goals, each with bronze / silver / gold steps. One row per goal: the medal of the best step
    /// taken, its name and next target, a progress bar, and an "Al" button when a step's reward is waiting.
    /// </summary>
    public class AchievementsPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, list;
        Text summary;

        static readonly Color Ink = new Color(0.32f, 0.2f, 0.12f);
        static readonly Color Soft = new Color(0.5f, 0.36f, 0.26f);
        static readonly Color RowColor = new Color(1f, 0.96f, 0.88f);
        const float ListW = 920f, ListH = 1610f;

        public bool IsOpen => gameObject.activeSelf;

        public void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            var dimImg = root.gameObject.AddComponent<Image>();
            dimImg.color = Theme.Ui(new Color(0.03f, 0.01f, 0.06f, 0.7f));
            var dimBtn = root.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.93f, 0.86f, 0.72f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // taps on the panel don't close it
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(980f, 1820f));

            var head = UIKit.Img("Header", panel, ProcArt.Rounded, new Color(0.82f, 0.55f, 0.18f), true);
            head.rectTransform.anchorMin = new Vector2(0f, 1f);
            head.rectTransform.anchorMax = new Vector2(1f, 1f);
            head.rectTransform.pivot = new Vector2(0.5f, 1f);
            head.rectTransform.anchoredPosition = Vector2.zero;
            head.rectTransform.sizeDelta = new Vector2(0f, 120f);
            var title = UIKit.Txt("Title", head.transform, L.T("Başarımlar"), 52, Color.white, TextAnchor.MiddleCenter, true);
            title.fontStyle = FontStyle.Bold;
            UIKit.Stretch(title.rectTransform, 30f, 120f, 10f, 10f);
            var close = UIKit.Btn("Close", head.transform, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-20f, 0f), new Vector2(90f, 90f));

            summary = UIKit.Txt("Summary", panel, "", 30, Soft, TextAnchor.MiddleCenter, false);
            summary.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, -150f), new Vector2(900f, 44f));

            list = UIKit.Rect("List", panel);
            list.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -185f), new Vector2(ListW, ListH));
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = new Vector2(0f, -185f);

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

        public void Close()
        {
            gameObject.SetActive(false);
            GM.OnAchievementsClosed();
        }

        void Fill()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            var all = Achievements.All;
            float gap = 10f, h = Mathf.Min(128f, (ListH - gap * (all.Length - 1)) / all.Length);
            int medals = 0;
            for (int i = 0; i < all.Length; i++)
            {
                medals += GM.AchClaimed(all[i]);
                BuildRow(all[i], -h * 0.5f - i * (h + gap), h);
            }
            summary.text = L.F("{0}/{1} rozet", medals, all.Length * 3);
        }

        void BuildRow(Achievements.Def d, float y, float h)
        {
            int taken = GM.AchClaimed(d);
            int progress = GM.AchProgress(d);
            bool allDone = taken >= d.steps.Length;
            int target = allDone ? d.steps[d.steps.Length - 1] : d.steps[taken];
            bool ready = !allDone && progress >= target;

            var row = UIKit.Img("Row", list, ProcArt.Rounded, ready ? new Color(1f, 0.92f, 0.7f) : RowColor, true);
            row.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(0f, y), new Vector2(ListW, h));
            var rt = row.transform;

            // medal of the best step taken so far
            var medal = UIKit.Img("Medal", rt, ProcArt.Medal(taken), Color.white);
            medal.preserveAspect = true;
            medal.rectTransform.At(new Vector2(-ListW / 2f + h / 2f + 6f, 0f), new Vector2(h - 14f, h - 14f));

            float textX = -ListW / 2f + h + 20f;   // left edge of the text column
            const float textW = 520f;

            var name = UIKit.Txt("Name", rt, d.Name, 32, Ink, TextAnchor.MiddleLeft, false);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.At(new Vector2(textX + textW / 2f, h * 0.24f), new Vector2(textW, 40f));

            var goal = UIKit.Txt("Goal", rt, allDone ? L.T("Bütün rozetler alındı!") : d.Goal(target), 24, Soft, TextAnchor.MiddleLeft, false);
            goal.rectTransform.At(new Vector2(textX + textW / 2f, -2f), new Vector2(textW, 32f));

            // progress bar + numbers
            float barW = 380f;
            var bar = UIKit.Img("Bar", rt, ProcArt.Rounded, new Color(0.84f, 0.76f, 0.64f), true);
            bar.rectTransform.At(new Vector2(textX + barW / 2f, -h * 0.29f), new Vector2(barW, 20f));
            float f = Mathf.Clamp01(target > 0 ? progress / (float)target : 1f);
            if (f > 0.04f)
            {
                var fill = UIKit.Img("Fill", bar.transform, ProcArt.Rounded, ready || allDone ? UIKit.Green : UIKit.Orange, true);
                fill.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(Vector2.zero, new Vector2(barW * f, 20f));
            }
            var num = UIKit.Txt("Num", rt, Mathf.Min(progress, target) + " / " + target, 22, Soft, TextAnchor.MiddleLeft, false);
            num.rectTransform.At(new Vector2(textX + barW + 14f + 70f, -h * 0.29f), new Vector2(140f, 30f));

            // right: the reward waiting, or which step comes next and what it gives
            float rightX = ListW / 2f - 100f;
            if (ready)
            {
                var get = UIKit.Btn("Get", rt, L.T("Al"), UIKit.Green, () => { GM.ClaimAchievement(d); Fill(); }, 34);
                ((RectTransform)get.transform).At(new Vector2(rightX, 10f), new Vector2(160f, 70f));
                Tween.Pop((RectTransform)get.transform, 0.3f);
                var rw = UIKit.Txt("Reward", rt, Achievements.RewardText(taken), 18, Soft, TextAnchor.MiddleCenter, false);
                rw.rectTransform.At(new Vector2(rightX, -h * 0.34f), new Vector2(190f, 26f));
            }
            else if (!allDone)
            {
                var next = UIKit.Txt("Next", rt, Achievements.StepName(taken) + "\n<size=18>" + Achievements.RewardText(taken) + "</size>", 24, Soft, TextAnchor.MiddleCenter, false);
                next.rectTransform.At(new Vector2(rightX, 0f), new Vector2(190f, h - 10f));
            }
        }
    }
}
