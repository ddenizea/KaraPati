using UnityEngine;
using UnityEngine.EventSystems;

namespace KaraPati
{
    /// <summary>A placed tower ornament: tap to select it, drag to move it around its floor.</summary>
    public class DecorPiece : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PlacedSave data;
        public TowerScreen tower;
        RectTransform rt, layer;
        Vector2 grabOffset;
        bool dragging;

        public void Init(TowerScreen t, PlacedSave p)
        {
            tower = t;
            data = p;
            rt = (RectTransform)transform;
            layer = (RectTransform)rt.parent;
        }

        public void OnPointerDown(PointerEventData e) => tower.Select(this);

        public void OnBeginDrag(PointerEventData e)
        {
            dragging = true;
            tower.Select(this);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, e.position, e.pressEventCamera, out var lp);
            grabOffset = rt.anchoredPosition - lp;
            rt.localScale = new Vector3(data.flip ? -1.08f : 1.08f, 1.08f, 1f);   // lifted
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, e.position, e.pressEventCamera, out var lp);
            var pos = TowerScreen.ClampOnFloor(lp + grabOffset);
            rt.anchoredPosition = pos;
            data.x = pos.x;
            data.y = pos.y;
            tower.FollowSelection();
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            rt.localScale = new Vector3(data.flip ? -1f : 1f, 1f, 1f);
            Sfx.Play("spawn", 1.3f, 0.6f);
            GameManager.I.MarkDirty();
        }
    }
}
