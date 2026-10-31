using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>"Cadı Dosyası": the notebook where every clue found in the scenes is kept.</summary>
    public class CaseFilePanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;
        RectTransform panel, list;
        Text count;

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
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1640f));

            var icon = UIKit.Img("Icon", panel, ProcArt.CaseFile, Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -24f), new Vector2(100f, 100f));
            var title = UIKit.Txt("Title", panel, "Cadı Dosyası", 52, new Color(0.35f, 0.2f, 0.12f), TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(150f, -30f), new Vector2(600f, 70f));
            count = UIKit.Txt("Count", panel, "", 30, new Color(0.45f, 0.3f, 0.2f), TextAnchor.MiddleLeft, false);
            count.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(152f, -92f), new Vector2(600f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.75f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            list = UIKit.Rect("List", panel);
            UIKit.Stretch(list, 30f, 30f, 150f, 30f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            Tween.Pop(panel, 0.3f);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void Fill()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            int found = 0, row = 0;
            var ink = new Color(0.3f, 0.18f, 0.1f);
            for (int ch = 0; ch < Story.Chapters.Length; ch++)
            {
                var head = UIKit.Txt("Chapter", list, Story.Chapters[ch].name, 32, new Color(0.65f, 0.3f, 0.15f), TextAnchor.MiddleLeft, false);
                head.fontStyle = FontStyle.Bold;
                head.rectTransform.anchorMin = new Vector2(0f, 1f);
                head.rectTransform.anchorMax = new Vector2(1f, 1f);
                head.rectTransform.pivot = new Vector2(0.5f, 1f);
                head.rectTransform.anchoredPosition = new Vector2(0f, -Y(row, ch));
                head.rectTransform.sizeDelta = new Vector2(-20f, 46f);

                foreach (var c in Story.Clues)
                {
                    if (c.chapter != ch) continue;
                    bool has = GM.HasClue(c.id);
                    if (has) found++;
                    var bg = UIKit.Img("Clue", list, ProcArt.Rounded, has ? new Color(1f, 0.97f, 0.9f) : new Color(0.85f, 0.78f, 0.64f), true);
                    var rt = bg.rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -(Y(row, ch) + 52f));
                    rt.sizeDelta = new Vector2(0f, 150f);

                    var ic = UIKit.Img("Icon", bg.transform, has ? ProcArt.ClueIcon(c.id) : ProcArt.Sparkle,
                        has ? (c.id.EndsWith("_iz") ? new Color(0.45f, 0.65f, 0.85f) : Color.white) : new Color(0.55f, 0.45f, 0.35f, 0.5f));
                    ic.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(20f, 0f), new Vector2(100f, 100f));
                    var t = UIKit.Txt("Title", bg.transform, has ? c.title : "???", 32, ink, TextAnchor.UpperLeft, false);
                    t.fontStyle = FontStyle.Bold;
                    t.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(140f, -14f), new Vector2(770f, 40f));
                    t.rectTransform.pivot = new Vector2(0f, 1f);
                    var d = UIKit.Txt("Text", bg.transform, has ? c.text : "Henüz bulunmadı. Görevleri ilerlet ve göz düğmesini kullan.", 24,
                        has ? ink : new Color(0.45f, 0.35f, 0.25f), TextAnchor.UpperLeft, false);
                    d.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(140f, -56f), new Vector2(770f, 90f));
                    d.rectTransform.pivot = new Vector2(0f, 1f);
                    row++;
                }
            }
            count.text = found + "/" + Story.Clues.Length + " ipucu bulundu";
        }

        // 160 px per clue row plus a 60 px heading per chapter
        static float Y(int row, int chapter) => row * 160f + chapter * 60f;
    }
}
