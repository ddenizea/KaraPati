using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// First-time guide on the merge table. A bouncing paw points at what to do next:
    /// 1) tap the cauldron to make ingredients, 2) merge two equal items, 3) deliver an order, 4) try the alchemy pot.
    /// Each step advances when the player actually does it. Can be skipped at any time.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public const int Done = 99;
        const int LastStep = 4;

        public int Step { get; set; }
        public bool Active => Step > 0 && Step < Done;

        GameManager GM => GameManager.I;
        RectTransform layer, paw, ring, bubble;
        Image pawImg, ringImg;
        Text text, stepText;
        float t;

        public void Build(RectTransform root)
        {
            layer = UIKit.Rect("Tutorial", root);
            UIKit.Stretch(layer);

            ringImg = UIKit.Img("Ring", layer, ProcArt.Circle, new Color(1f, 0.85f, 0.4f, 0.35f));
            ringImg.raycastTarget = false;
            ring = ringImg.rectTransform;
            ring.sizeDelta = new Vector2(150f, 150f);

            pawImg = UIKit.Img("Paw", layer, ProcArt.PawPrint, new Color(1f, 0.92f, 0.75f));
            pawImg.raycastTarget = false;
            paw = pawImg.rectTransform;
            paw.sizeDelta = new Vector2(110f, 110f);
            var shadow = pawImg.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0.15f, 0.08f, 0.2f, 0.9f);
            shadow.effectDistance = new Vector2(4f, -4f);

            var bg = UIKit.Img("Bubble", layer, ProcArt.Rounded, new Color(0.98f, 0.94f, 0.86f, 0.97f), true);
            bg.raycastTarget = false;
            bubble = bg.rectTransform;
            bubble.sizeDelta = new Vector2(720f, 150f);

            var head = UIKit.Img("Head", bubble, ProcArt.KaraPatiHead, Color.white);
            head.raycastTarget = false;
            head.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(12f, 0f), new Vector2(110f, 110f));

            text = UIKit.Txt("Text", bubble, "", 30, new Color(0.25f, 0.15f, 0.3f), TextAnchor.MiddleLeft, false);
            text.raycastTarget = false;
            UIKit.Stretch(text.rectTransform, 132f, 120f, 34f, 10f);

            stepText = UIKit.Txt("Step", bubble, "", 22, new Color(0.55f, 0.4f, 0.55f), TextAnchor.UpperLeft, false);
            stepText.raycastTarget = false;
            stepText.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(134f, -8f), new Vector2(300f, 30f));
            stepText.rectTransform.pivot = new Vector2(0f, 1f);
            stepText.rectTransform.anchoredPosition = new Vector2(134f, -8f);

            var skip = UIKit.Btn("Skip", bubble, "Geç", new Color(0.75f, 0.6f, 0.7f), Skip, 26);
            ((RectTransform)skip.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-12f, 0f), new Vector2(96f, 70f));

            layer.gameObject.SetActive(false);
        }

        /// <summary>Called the first time the player opens the table.</summary>
        public void Begin()
        {
            if (Step != 0) return;
            Step = 1;
            GM.MarkDirty();
        }

        public void Notify(string e)
        {
            if (!Active) return;
            bool ok = (Step == 1 && e == "spawn") || (Step == 2 && e == "merge") || (Step == 3 && e == "deliver") ||
                      (Step == 4 && (e == "pot" || e == "recipes"));
            if (!ok) return;
            Step++;
            if (Step > LastStep)
            {
                Step = Done;
                GM.Toast("Harika! Artık dükkânı yönetmeye hazırsın. Bol iksirli günler!", UIKit.Orange, 4.5f);
            }
            else if (paw) Tween.Pop(bubble, 0.3f);
            GM.MarkDirty();
        }

        void Skip()
        {
            Step = Done;
            GM.MarkDirty();
            GM.Toast("Rehber kapandı. İpuçları alttaki panelde.");
        }

        void LateUpdate()
        {
            if (!layer) return;
            bool show = Active && GM != null && GM.OnBoard && !GM.ModalOpen && GM.Board != null && GM.Board.Ready;
            if (layer.gameObject.activeSelf != show)
            {
                layer.gameObject.SetActive(show);
                if (show) layer.SetAsLastSibling();
            }
            if (!show) return;
            t += Time.unscaledDeltaTime;

            RectTransform target = null, dragFrom = null;
            string msg = "";
            var B = GM.Board;
            switch (Step)
            {
                case 1:
                    if (B.FindFirst(d => d.chain == Chains.Kazan, out var k)) target = B.ViewRect(k);
                    msg = "Kazana dokun: ot ve mantar üretir. Her dokunuş 1 enerji.";
                    break;
                case 2:
                    if (B.FindPair(out var a, out var b)) { dragFrom = B.ViewRect(a); target = B.ViewRect(b); msg = "Aynı iki malzemeyi üst üste sürükle, birleşsinler!"; }
                    else { if (B.FindFirst(d => d.chain == Chains.Kazan, out var k2)) target = B.ViewRect(k2); msg = "Aynı iki malzeme lazım. Kazana dokunmaya devam et."; }
                    break;
                case 3:
                    target = GM.Orders.ReadyGiveButton();
                    if (target) msg = "Hayaletin istediği masada! 'Ver'e bas, altın ve Pati Mührü kazan.";
                    else { target = GM.Orders.FirstReqTile(); msg = "Hayaletlerin istediklerini birleştirerek yap. Hazır olunca 'Ver' yeşil yanar."; }
                    break;
                case 4:
                    if (B.FindFirst(d => Chains.IsStation(d.chain), out var p))
                    {
                        target = B.ViewRect(p);
                        var pot = B.Get(p);
                        if (B.FindPotIngredient(pot, out var src)) dragFrom = B.ViewRect(src);
                    }
                    msg = dragFrom ? "Bu malzemeyi mor Simya Kazanı'na sürükle: iksir kaynar!"
                                   : "Bu mor Simya Kazanı. Seçip 'Tarifler'e bak: iki malzeme = bir iksir!";
                    break;
            }
            stepText.text = "Rehber " + Step + "/" + LastStep;
            text.text = msg;

            if (!target) { paw.gameObject.SetActive(false); ring.gameObject.SetActive(false); PlaceBubble(new Vector2(0f, -200f)); return; }
            paw.gameObject.SetActive(true);
            ring.gameObject.SetActive(true);
            Vector2 tp = layer.InverseTransformPoint(target.position);
            ring.anchoredPosition = tp;
            float pulse = 1f + Mathf.Sin(t * 5f) * 0.12f;
            ring.localScale = Vector3.one * pulse;
            ringImg.color = new Color(1f, 0.85f, 0.4f, 0.25f + Mathf.Sin(t * 5f) * 0.1f);

            if (dragFrom)
            {
                // the paw slides from the source to the target, again and again
                Vector2 fp = layer.InverseTransformPoint(dragFrom.position);
                float cyc = Mathf.Repeat(t, 1.8f);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cyc - 0.3f) / 1.0f));
                paw.anchoredPosition = Vector2.Lerp(fp, tp, e) + new Vector2(30f, -40f);
                pawImg.color = new Color(1f, 0.92f, 0.75f, cyc > 1.5f ? Mathf.Lerp(1f, 0f, (cyc - 1.5f) / 0.3f) : 1f);
                paw.localScale = Vector3.one * (cyc < 0.3f ? 0.9f : 1f);
                paw.localEulerAngles = new Vector3(0f, 0f, 25f);
                PlaceBubble(tp, fp);
                return;
            }
            else
            {
                pawImg.color = new Color(1f, 0.92f, 0.75f);
                paw.anchoredPosition = tp + new Vector2(40f, -50f) + new Vector2(1f, -1f) * (Mathf.Abs(Mathf.Sin(t * 4f)) * 22f);
                paw.localScale = Vector3.one;
            }
            paw.localEulerAngles = new Vector3(0f, 0f, 25f);
            PlaceBubble(tp);
        }

        void PlaceBubble(Vector2 tp) => PlaceBubble(tp, tp);

        void PlaceBubble(Vector2 a, Vector2 b)
        {
            // keep clear of both points: above them in the lower half of the screen, below them otherwise
            float hi = Mathf.Max(a.y, b.y), lo = Mathf.Min(a.y, b.y);
            float y = lo < -200f ? hi + 190f : lo - 210f;
            float x = Mathf.Clamp((a.x + b.x) * 0.5f, -170f, 170f);
            bubble.anchoredPosition = new Vector2(x, y);
        }
    }
}
