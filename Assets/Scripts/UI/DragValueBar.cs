using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: OPT-DRAG-001] An Options value row's bar follows the mouse: a press or a drag on it sets the value at that point,
    /// in the row's steps (RM-210). The arrows and the pad keep working as before; the bar takes no focus.
    /// </summary>
    public sealed class DragValueBar : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        /// <summary>Gets the point along the bar, 0 (left) to 1 (right).</summary>
        public Action<float> OnPoint;

        public void OnPointerDown(PointerEventData eventData) => Report(eventData);

        public void OnDrag(PointerEventData eventData) => Report(eventData);

        private void Report(PointerEventData eventData)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out var local))
                return;
            var rect = rt.rect;
            OnPoint?.Invoke(rect.width <= 0f ? 0f : Mathf.Clamp01((local.x - rect.xMin) / rect.width));
        }

        /// <summary>The value at a point of the bar, on the row's steps between its limits.</summary>
        public static float ValueAt(float t, float min, float max, float step)
        {
            var raw = Mathf.Lerp(min, max, Mathf.Clamp01(t));
            var stepped = step > 0f ? Mathf.Round(raw / step) * step : raw;
            return Mathf.Clamp(stepped, min, max);
        }
    }
}
