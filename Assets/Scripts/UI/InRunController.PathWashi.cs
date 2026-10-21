using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-022] Garden flow A · Washi window (owner pick 2026-10-21, with D · Entering a floor): the floor's
    // painting stays sharp and bright round the path scroll and shows through its thin paper; the floor's title, the route
    // names, the floor marks, the legend and the node labels sit on paper slips so they stay readable. High Contrast keeps
    // the opaque paper and the dark, blurred room of before.
    public sealed partial class InRunController
    {
        private const float WashiPaperAlpha = 0.48f;
        private const float WashiRoomScrim = 0.22f;
        private const float SolidRoomScrim = 0.55f;
        private const float SlipAlpha = 0.86f;
        private static readonly Color SlipPaper = new Color(0.94f, 0.90f, 0.81f, SlipAlpha);

        private Image _pathTitleSlip;
        private Image _pathRouteSlip;
        private Image _firstPickSlip;

        /// <summary>The washi window is on unless High Contrast asks for the opaque paper.</summary>
        internal static bool PathWashiOn => !(OptionsController.LiveAccessibility?.HighContrastMode ?? false);

        // The paper, the room round it and the slips, for the current setting (on every rebuild of the path).
        private void ApplyPathWashi()
        {
            if (_pathPaper == null) return;
            var washi = PathWashiOn;
            var paper = _pathPaper.GetComponent<Image>();
            if (paper != null)
                paper.color = new Color(ScrollUiKit.Paper.r, ScrollUiKit.Paper.g, ScrollUiKit.Paper.b, washi ? WashiPaperAlpha : 1f);
            if (_pathPaperWash != null) _pathPaperWash.enabled = !washi;   // the painting itself shows through instead
            if (_bgScrimImg != null) _bgScrimImg.color = new Color(0f, 0f, 0f, washi ? WashiRoomScrim : SolidRoomScrim);

            _pathTitleSlip = FitSlip(_pathTitleSlip, "PathTitleSlip", washi, _pathEyebrow, _pathTitle);
            _pathRouteSlip = FitSlip(_pathRouteSlip, "PathRouteSlip", washi, _pathRouteLine);
            SlipBehind(_floorProgressStrip, "Slip", washi);
            SlipBehind(_pathLegend != null ? (RectTransform)_pathLegend.transform : null, "Slip", washi);
        }

        // A paper slip behind the given labels on the scroll, as wide as their text.
        private Image FitSlip(Image slip, string name, bool show, params Text[] labels)
        {
            if (slip == null)
            {
                slip = ScrollUiKit.Fill(_pathPaper, name, Vector2.zero, Vector2.zero, SlipPaper, ScrollUiKit.Rounded(6f));
                slip.raycastTarget = false;
            }
            var first = labels.Length > 0 ? labels[0] : null;
            slip.gameObject.SetActive(show && first != null);
            if (!show || first == null) return slip;
            if (_pathPaper.rect.width <= 1f) Canvas.ForceUpdateCanvases();
            var paperWidth = Mathf.Max(1f, _pathPaper.rect.width);
            float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
            foreach (var label in labels)
            {
                if (label == null || string.IsNullOrEmpty(label.text)) continue;
                var rt = label.rectTransform;
                var width = Mathf.Min(label.preferredWidth, rt.rect.width > 1f ? rt.rect.width : label.preferredWidth);
                minX = Mathf.Min(minX, rt.anchorMin.x);
                maxX = Mathf.Max(maxX, rt.anchorMin.x + width / paperWidth);
                minY = Mathf.Min(minY, rt.anchorMin.y);
                maxY = Mathf.Max(maxY, rt.anchorMax.y);
            }
            if (maxX <= minX) { slip.gameObject.SetActive(false); return slip; }
            const float padX = 0.010f, padY = 0.008f;
            var r = slip.rectTransform;
            r.anchorMin = new Vector2(minX - padX, minY - padY);
            r.anchorMax = new Vector2(maxX + padX, maxY + padY);
            r.offsetMin = r.offsetMax = Vector2.zero;
            // Under the labels: moved only when it is above the first one (moving it to the label's index when it is already
            // below would put it on top).
            var labelIndex = first.transform.GetSiblingIndex();
            if (r.GetSiblingIndex() > labelIndex) r.SetSiblingIndex(labelIndex);
            return slip;
        }

        // A slip filling an area (floor marks, legend), as its first child so it draws under the area's contents.
        private static void SlipBehind(RectTransform area, string name, bool show)
        {
            if (area == null) return;
            var existing = area.Find(name);
            if (existing == null && !show) return;
            var slip = existing != null ? existing.GetComponent<Image>()
                : ScrollUiKit.Fill(area, name, new Vector2(-0.02f, -0.06f), new Vector2(1.02f, 1.06f), SlipPaper, ScrollUiKit.Rounded(6f));
            slip.raycastTarget = false;
            slip.transform.SetAsFirstSibling();
            slip.gameObject.SetActive(show);
        }

        // The first-pick note gets its own slip (it is rebuilt with every refresh of the path).
        private void SlipFirstPickNote(Text note)
        {
            if (_firstPickSlip != null) DestroyNow(_firstPickSlip.gameObject);
            _firstPickSlip = null;
            if (note == null || !PathWashiOn) return;
            _firstPickSlip = FitSlip(null, "FirstPickSlip", true, note);
        }

        // A node's label on a small paper pill, the size of its text (only with the washi window).
        private static void PillUnderLabel(Text label, bool above)
        {
            if (label == null || !PathWashiOn || string.IsNullOrEmpty(label.text)) return;
            var parent = label.transform.parent;
            var pill = ScrollUiKit.Fill(parent, "LabelPill", Vector2.zero, Vector2.zero, SlipPaper, ScrollUiKit.Rounded(8f));
            pill.raycastTarget = false;
            var lrt = label.rectTransform;
            var r = pill.rectTransform;
            r.anchorMin = r.anchorMax = lrt.anchorMin;
            r.pivot = lrt.pivot;
            r.sizeDelta = new Vector2(Mathf.Round(Mathf.Min(label.preferredWidth, lrt.sizeDelta.x) + 14f),
                Mathf.Round(label.preferredHeight + 4f));
            r.anchoredPosition = lrt.anchoredPosition + new Vector2(0f, above ? -2f : 2f);
            pill.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        }
    }
}
