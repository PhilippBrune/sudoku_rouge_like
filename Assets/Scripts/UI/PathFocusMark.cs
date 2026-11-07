using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-PATH-FOCUS-001] Playtest 0.4.1 on a Legion Go (owner's pick A of docs/design/layout-proposals/path_focus):
    /// the path tile the pad or the keys have chosen gets a vermilion brush ring with a dot and the class's marker above it;
    /// the faint gold glow alone was hard to see on a handheld.
    /// </summary>
    public sealed class PathFocusMark : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private GameObject _ring, _marker;

        public bool Shown => _ring != null && _ring.activeSelf;

        public static PathFocusMark Attach(Button tile, ClassId classId, float size)
        {
            var mark = tile.gameObject.AddComponent<PathFocusMark>();
            var ring = ScrollUiKit.Fill(tile.transform, "FocusRing", new Vector2(-0.14f, -0.14f), new Vector2(1.14f, 1.14f),
                ScrollUiKit.Vermilion, ScrollUiKit.Enso());
            ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            ring.raycastTarget = false;
            var dot = ScrollUiKit.Spot(ring.transform, "Dot", new Vector2(0.88f, 0.86f), new Vector2(size * 0.16f, size * 0.16f),
                ScrollUiKit.Vermilion, ScrollUiKit.Disc());
            dot.raycastTarget = false;
            mark._ring = ring.gameObject;

            var marker = ScrollUiKit.Spot(tile.transform, "FocusMarker", new Vector2(0.5f, 1f), new Vector2(46f, 46f),
                ScrollUiKit.Paper, ScrollUiKit.Disc());
            marker.rectTransform.pivot = new Vector2(0.5f, 0f);
            marker.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            marker.raycastTarget = false;
            ScrollUiKit.InkFrame(marker.transform, ScrollUiKit.Vermilion, 3f);
            ScrollUiKit.Icon(marker.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(classId),
                new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Color.white);
            mark._marker = marker.gameObject;
            mark.Show(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == tile.gameObject);
            return mark;
        }

        public void OnSelect(BaseEventData eventData) => Show(true);

        public void OnDeselect(BaseEventData eventData) => Show(false);

        private void OnDisable() => Show(false);   // a hidden tile gets no OnDeselect

        private void OnEnable() => Show(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);

        private void Show(bool on)
        {
            if (_ring != null) _ring.SetActive(on);
            if (_marker != null) _marker.SetActive(on);
        }
    }
}
