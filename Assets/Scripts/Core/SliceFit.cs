using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>
    /// Keeps the hand-drawn line of a stretched panel the same thickness at every size. Big panels use the normal
    /// rounded shape; thin ones (progress bars, tags, small buttons) would squash its corners, so they switch to the
    /// bold-line version drawn at a smaller scale, which keeps round ends and a line of about the same weight.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SliceFit : MonoBehaviour
    {
        const float Corners = 80f;     // both corners of the normal shape, in canvas units at scale 1
        const float BoldScale = 2.4f;  // the bold line drawn this much smaller is as thick as the normal one

        Image img;

        void Awake() => Fit();
        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (!img) img = GetComponent<Image>();
            if (!img) return;
            // only manage our own two shapes; leave it alone if some code put another picture on it
            if (img.sprite != ProcArt.Rounded && img.sprite != ProcArt.RoundedBold) return;
            var r = ((RectTransform)transform).rect;
            float m = Mathf.Min(r.width, r.height);
            if (m <= 0f) return;
            if (m >= Corners)
            {
                if (img.sprite != ProcArt.Rounded) img.sprite = ProcArt.Rounded;
                img.pixelsPerUnitMultiplier = 1f;
            }
            else
            {
                if (img.sprite != ProcArt.RoundedBold) img.sprite = ProcArt.RoundedBold;
                img.pixelsPerUnitMultiplier = Mathf.Max(BoldScale, Corners / m);
            }
        }
    }
}
