using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Sticker collection album: three sets, each pays out once when completed.</summary>
    public class AlbumPanel : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        RectTransform panel, grid;
        Text progress;
        Button claim;
        Image claimImg;
        readonly Button[] tabs = new Button[3];
        int setIndex;

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

            var bg = UIKit.Img("Panel", root, ProcArt.Rounded, new Color(0.22f, 0.15f, 0.31f, 0.99f), true);
            bg.raycastTarget = true;
            // swallow clicks on the panel itself so they don't bubble up to the dim (which closes)
            bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            panel = bg.rectTransform;
            panel.At(Vector2.zero, new Vector2(1000f, 1560f));

            var title = UIKit.Txt("Title", panel, "Etiket Albümü", 52, UIKit.Orange);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -30f), new Vector2(800f, 70f));

            var close = UIKit.Btn("Close", panel, "X", new Color(0.75f, 0.3f, 0.35f), Close, 44);
            ((RectTransform)close.transform).Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f)).At(new Vector2(-24f, -24f), new Vector2(90f, 90f));

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                tabs[i] = UIKit.Btn("Tab" + i, panel, "", UIKit.PanelDark, () => Show(idx), 26);
                ((RectTransform)tabs[i].transform).Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f))
                    .At(new Vector2((i - 1) * 315f, -125f), new Vector2(300f, 100f));
            }

            grid = UIKit.Rect("Grid", panel);
            grid.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -250f), new Vector2(900f, 1040f));

            progress = UIKit.Txt("Progress", panel, "", 34, UIKit.Cream);
            progress.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 150f), new Vector2(900f, 50f));

            claim = UIKit.Btn("Claim", panel, "", UIKit.Disabled, Claim, 32);
            claimImg = claim.GetComponent<Image>();
            ((RectTransform)claim.transform).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)).At(new Vector2(0f, 40f), new Vector2(760f, 100f));

            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            // jump to the first set that has a reward waiting
            for (int i = 0; i < Stickers.Sets.Length; i++)
                if (GM.IsSetClaimable(Stickers.Sets[i])) { setIndex = i; break; }
            Show(setIndex);
            Tween.Pop(panel, 0.3f);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GM.OnAlbumClosed();
        }

        void Show(int i)
        {
            setIndex = i;
            var set = Stickers.Sets[i];

            for (int k = 0; k < tabs.Length; k++)
            {
                var s = Stickers.Sets[k];
                tabs[k].Label().text = s.name + "\n" + GM.StickerCount(s) + "/" + s.ids.Length;
                tabs[k].GetComponent<Image>().color = k == i ? UIKit.Orange : UIKit.PanelDark;
            }

            for (int c = grid.childCount - 1; c >= 0; c--) Destroy(grid.GetChild(c).gameObject);
            for (int n = 0; n < set.ids.Length; n++)
            {
                string id = set.ids[n];
                bool owned = GM.HasSticker(id);
                var slot = UIKit.Rect("Slot", grid);
                slot.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(((n % 3) - 1) * 300f, -(n / 3) * 345f), new Vector2(280f, 330f));

                var edge = UIKit.Img("Edge", slot, ProcArt.Circle, owned ? new Color(0.12f, 0.07f, 0.18f) : new Color(0.1f, 0.06f, 0.14f, 0.6f));
                edge.rectTransform.At(new Vector2(0f, 40f), new Vector2(236f, 236f));
                var paper = UIKit.Img("Paper", slot, ProcArt.Circle, owned ? new Color(1f, 0.97f, 0.92f) : new Color(0.17f, 0.11f, 0.24f));
                paper.rectTransform.At(new Vector2(0f, 40f), new Vector2(220f, 220f));

                if (owned)
                {
                    var icon = UIKit.Img("Icon", slot, ProcArt.StickerIcon(id), Color.white);
                    icon.rectTransform.At(new Vector2(0f, 40f), new Vector2(165f, 165f));
                    icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, ((n * 37) % 11) - 5f);
                }
                else
                {
                    var q = UIKit.Txt("?", slot, "?", 110, new Color(1f, 1f, 1f, 0.25f), TextAnchor.MiddleCenter, false);
                    q.rectTransform.At(new Vector2(0f, 40f), new Vector2(200f, 200f));
                }

                var name = UIKit.Txt("Name", slot, owned ? Stickers.Name(id) : "???", 28, owned ? UIKit.Cream : new Color(1f, 1f, 1f, 0.4f));
                name.rectTransform.At(new Vector2(0f, -120f), new Vector2(290f, 40f));
            }

            int have = GM.StickerCount(set);
            progress.text = set.name + ": " + have + "/" + set.ids.Length;
            string reward = Stickers.SetRewardGold + " altın + " + Stickers.SetRewardStamps + " mühür";
            if (GM.IsSetClaimed(set))
            {
                claim.Label().text = "Ödül alındı";
                claimImg.color = UIKit.Disabled;
                claim.interactable = false;
            }
            else if (have >= set.ids.Length)
            {
                claim.Label().text = "Ödülü Al: " + reward;
                claimImg.color = UIKit.Orange;
                claim.interactable = true;
            }
            else
            {
                claim.Label().text = "Seti tamamla: " + reward;
                claimImg.color = UIKit.Disabled;
                claim.interactable = false;
            }
        }

        void Claim()
        {
            if (GM.ClaimSet(Stickers.Sets[setIndex]))
            {
                Show(setIndex);
                Tween.Punch(claim.transform, 0.12f, 0.3f);
            }
        }
    }
}
