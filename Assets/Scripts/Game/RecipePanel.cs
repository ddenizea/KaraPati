using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>"Tarif Defteri": every alchemy recipe, what goes in, how long it brews and what ghosts pay for it.</summary>
    public class RecipePanel : MonoBehaviour
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

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.24f, 0.17f, 0.36f, 0.99f), true);
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1500f));

            var icon = UIKit.Img("Icon", panel, ProcArt.Item(Chains.Simya, 1), Color.white);
            icon.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -20f), new Vector2(110f, 110f));
            var title = UIKit.Txt("Title", panel, "Tarif Defteri", 52, UIKit.Orange, TextAnchor.MiddleLeft, false);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(160f, -28f), new Vector2(600f, 70f));
            count = UIKit.Txt("Count", panel, "", 28, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, false);
            count.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(162f, -92f), new Vector2(720f, 40f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.8f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            list = UIKit.Rect("List", panel);
            UIKit.Stretch(list, 30f, 30f, 160f, 30f);
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

        void Fill()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            int done = GM.TasksDone;
            count.text = Recipes.UnlockedCount(done) + "/" + Recipes.All.Length + " tarif açık. İki malzemeyi kazana sürükle!";

            for (int i = 0; i < Recipes.All.Length; i++)
            {
                var r = Recipes.All[i];
                bool open = Recipes.Unlocked(r, done);
                var row = UIKit.Img("Recipe", list, ProcArt.Rounded, open ? new Color(0.33f, 0.24f, 0.47f) : new Color(0.18f, 0.13f, 0.26f), true);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 212f);
                rt.sizeDelta = new Vector2(0f, 196f);

                var pot = UIKit.Img("Potion", rt, ProcArt.Item(Chains.Iksir, r.id), open ? Color.white : new Color(0.2f, 0.15f, 0.3f, 0.9f));
                pot.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 0f), new Vector2(150f, 150f));

                var name = UIKit.Txt("Name", rt, open ? r.Name : "???", 36, open ? UIKit.Cream : new Color(1f, 1f, 1f, 0.4f), TextAnchor.MiddleLeft, false);
                name.fontStyle = FontStyle.Bold;
                name.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(180f, -12f), new Vector2(500f, 50f));
                name.rectTransform.pivot = new Vector2(0f, 1f);

                if (!open)
                {
                    var lk = UIKit.Img("Lock", rt, ProcArt.Lock, new Color(1f, 1f, 1f, 0.7f));
                    lk.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(59f, 0f), new Vector2(60f, 60f));
                    var info = UIKit.Txt("Info", rt, (r.chapter + 1) + ". bölümde açılır", 30, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft, false);
                    info.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(180f, -20f), new Vector2(600f, 50f));
                    info.rectTransform.pivot = new Vector2(0f, 0.5f);
                    continue;
                }

                // A + B
                Ingredient(rt, r.aChain, r.aLevel, 180f);
                var plus = UIKit.Txt("Plus", rt, "+", 44, UIKit.Orange, TextAnchor.MiddleCenter, false);
                plus.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(290f, -26f), new Vector2(50f, 60f));
                plus.rectTransform.pivot = new Vector2(0f, 0.5f);
                Ingredient(rt, r.bChain, r.bLevel, 340f);

                var time = UIKit.Txt("Time", rt, "Süre: " + (r.seconds >= 60 ? (r.seconds / 60f).ToString("0.#") + " dk" : r.seconds + " sn"), 26, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, false);
                time.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(470f, -2f), new Vector2(300f, 40f));
                time.rectTransform.pivot = new Vector2(0f, 0.5f);

                var coin = UIKit.Img("Coin", rt, ProcArt.Coin, Color.white);
                coin.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-180f, -50f), new Vector2(44f, 44f));
                var gold = UIKit.Txt("Gold", rt, r.Gold.ToString(), 32, UIKit.Cream, TextAnchor.MiddleLeft, false);
                gold.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-104f, -50f), new Vector2(90f, 44f));
                var stamp = UIKit.Img("Stamp", rt, ProcArt.Stamp, Color.white);
                stamp.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-66f, -50f), new Vector2(44f, 44f));
                var st = UIKit.Txt("Stamps", rt, Recipes.PotionStamps.ToString(), 32, UIKit.Cream, TextAnchor.MiddleLeft, false);
                st.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-10f, -50f), new Vector2(50f, 44f));
            }
        }

        static void Ingredient(RectTransform row, string chain, int level, float x)
        {
            var bg = UIKit.Img("Slot", row, ProcArt.Rounded, new Color(0.14f, 0.09f, 0.2f, 0.9f), true);
            bg.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(x, -26f), new Vector2(104f, 104f));
            bg.rectTransform.pivot = new Vector2(0f, 0.5f);
            bg.rectTransform.anchoredPosition = new Vector2(x, -26f);
            var ic = UIKit.Img("Icon", bg.transform, ProcArt.Item(chain, level), Color.white);
            ic.rectTransform.sizeDelta = new Vector2(90f, 90f);
            var lv = UIKit.Img("Lv", bg.transform, ProcArt.Circle, new Color(0.14f, 0.09f, 0.2f, 0.95f));
            lv.rectTransform.At(new Vector2(36f, -36f), new Vector2(36f, 36f));
            var t = UIKit.Txt("N", lv.transform, level.ToString(), 22, UIKit.Cream, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(t.rectTransform);
        }
    }
}
