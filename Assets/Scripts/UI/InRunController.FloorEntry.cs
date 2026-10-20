using System.Collections;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-021] Garden flow D (layout-proposals.md 2.0, owner pick after the 0.2.0 playtest): entering a floor
    // of a Garden Run shows the floor's painting at full brightness for two seconds, with the floor's name on a hanging
    // banner and the path still rolled up along the bottom; then the hand scroll unrolls. A click or any key skips it.
    // With Reduce Motion the scroll appears without the unroll.
    public sealed partial class InRunController
    {
        public const float FloorEntrySeconds = 2f;
        private const float FloorEntryFadeSeconds = 0.3f;
        private const float PathUnrollSeconds = 0.45f;

        private int _floorEntryShownFor = -1;
        private GameObject _floorEntry;
        private Coroutine _floorEntryRoutine;
        // The entry that is fading out while the path unrolls, and the coroutine that does it.
        private GameObject _floorEntryFading;
        private Coroutine _floorEntryUnroll;
        private int _floorEntryBuiltFrame = -1;

        /// <summary>True while the floor's painting and name are shown before the path.</summary>
        public bool IsFloorEntryShowing => _floorEntry != null;

        // A floor whose puzzle is on screen has been entered (a run resumed inside a puzzle never showed its path).
        private void MarkFloorEntered(RunState state)
        {
            if (state != null && state.Mode == GameMode.GardenRun) _floorEntryShownFor = state.CurrentFloor;
        }

        // The click or key that closed the class card must not also skip the floor's painting in the same frame.
        private bool FloorEntrySkipPressed() => Input.anyKeyDown && Time.frameCount > _floorEntryBuiltFrame;

        // Each floor of a Garden Run is entered once; the tutorial, the Monthly Walk and the other modes have no path.
        private void ShowFloorEntryIfNew(RunState state)
        {
            if (state == null || state.Mode != GameMode.GardenRun || state.TutorialMode || state.IsSeasonalChallenge) return;
            if (_pathPanel == null || !_pathPanel.activeInHierarchy || state.CurrentFloor == _floorEntryShownFor) return;
            _floorEntryShownFor = state.CurrentFloor;
            BuildFloorEntry(state);
            _floorEntryBuiltFrame = Time.frameCount;
            _floorEntryRoutine = StartCoroutine(FloorEntryTimer());
        }

        private void BuildFloorEntry(RunState state)
        {
            DismissFloorEntry(false);
            var floor = state.CurrentFloor;
            var screen = ScrollUiKit.Scene(_pathPanel, "FloorEntry", PathBackgroundName(floor), Color.white);
            _floorEntry = screen.gameObject;
            _floorEntry.AddComponent<CanvasGroup>();
            var skip = _floorEntry.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(() => DismissFloorEntry(true));

            // The banner hangs on the right (mockup 1480–1630 × 120–760 px) with the name written down the paper.
            var banner = ScrollUiKit.Fill(screen.transform, "FloorEntryBanner", new Vector2(0.771f, 0.296f),
                new Vector2(0.849f, 0.889f), ScrollUiKit.Paper);
            var shadow = banner.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(6f, -10f);
            ScrollUiKit.Fill(banner.transform, "RodTop", new Vector2(-0.07f, 0.985f), new Vector2(1.07f, 1.03f),
                ScrollUiKit.Wood, ScrollUiKit.Rounded(11f));
            ScrollUiKit.Fill(banner.transform, "RodBottom", new Vector2(-0.07f, -0.03f), new Vector2(1.07f, 0.015f),
                ScrollUiKit.Wood, ScrollUiKit.Rounded(11f));
            var column = ScrollUiKit.Area(banner.transform, "NameColumn", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            column.sizeDelta = new Vector2(580f, 130f);
            column.localRotation = Quaternion.Euler(0f, 0f, -90f);
            ScrollUiKit.Label(column, "FloorEntryName", FloorThemeData.GetFloorName(floor), 70, TextAnchor.MiddleCenter,
                ScrollUiKit.Ink, Vector2.zero, Vector2.one, 30);

            var eyebrow = ScrollUiKit.Label(screen.transform, "FloorEntryEyebrow",
                LocalizationService.Format("InRun.Path.Eyebrow", "Floor {0} of {1}", floor + 1, state.TotalFloors)
                    .ToUpperInvariant(), 24, TextAnchor.UpperCenter, ScrollUiKit.PaperText,
                new Vector2(0.735f, 0.205f), new Vector2(0.885f, 0.27f), 14);
            var eyebrowShadow = eyebrow.gameObject.AddComponent<Shadow>();
            eyebrowShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            eyebrowShadow.effectDistance = new Vector2(2f, -2f);

            // [REQ: ECON-BAL-003] From Floor 4 the painting also says that a mistake costs more here (only when the floor
            // raises it; at a Harmony level that already costs 2 or 3 nothing changes).
            if (RunDirector.LateFloorMistakeHpCost(state) > state.MistakeHpCost)
            {
                var cost = RunDirector.LateFloorMistakeHpCost(state);
                var warning = ScrollUiKit.Label(screen.transform, "FloorEntryMistakeCost",
                    LocalizationService.Format("InRun.FloorEntry.MistakeCost", "Here a mistake costs {0} HP", cost), 22,
                    TextAnchor.UpperCenter, new Color(1f, 0.78f, 0.70f, 1f), new Vector2(0.70f, 0.15f), new Vector2(0.92f, 0.205f), 13);
                var warningShadow = warning.gameObject.AddComponent<Shadow>();
                warningShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                warningShadow.effectDistance = new Vector2(2f, -2f);
            }

            // The path, still rolled up, lies along the bottom.
            var rolled = ScrollUiKit.Fill(screen.transform, "RolledPath", new Vector2(0.1875f, 0.065f),
                new Vector2(0.8125f, 0.124f), ScrollUiKit.Paper, ScrollUiKit.Rounded(30f));
            var rolledShadow = rolled.gameObject.AddComponent<Shadow>();
            rolledShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            rolledShadow.effectDistance = new Vector2(0f, -8f);
            ScrollUiKit.Fill(rolled.transform, "Light", new Vector2(0.01f, 0.55f), new Vector2(0.99f, 0.85f),
                new Color(1f, 1f, 1f, 0.35f), ScrollUiKit.Rounded(10f));
            ScrollUiKit.Fill(rolled.transform, "CapLeft", new Vector2(-0.018f, -0.16f), new Vector2(0.01f, 1.16f),
                ScrollUiKit.Wood, ScrollUiKit.Rounded(14f));
            ScrollUiKit.Fill(rolled.transform, "CapRight", new Vector2(0.99f, -0.16f), new Vector2(1.018f, 1.16f),
                ScrollUiKit.Wood, ScrollUiKit.Rounded(14f));

            // A controller's Submit lands on the skip button, not on a path tile under the painting.
            EventSystem.current?.SetSelectedGameObject(_floorEntry);
        }

        private IEnumerator FloorEntryTimer()
        {
            yield return new WaitForSecondsRealtime(FloorEntrySeconds);
            _floorEntryRoutine = null;
            DismissFloorEntry(true);
        }

        /// <summary>
        /// Ends the floor entry; <paramref name="unroll"/> plays the fade and the unrolling scroll. Without it (a puzzle
        /// opens, a new run starts) an unroll still under way also ends at once, so nothing is left over the next screen.
        /// </summary>
        public void DismissFloorEntry(bool unroll)
        {
            if (_floorEntryRoutine != null)
            {
                StopCoroutine(_floorEntryRoutine);
                _floorEntryRoutine = null;
            }
            if (!unroll) FinishFloorEntryUnroll();
            if (_floorEntry == null) return;
            var entry = _floorEntry;
            _floorEntry = null;
            RestorePathFocus(entry);
            if (!unroll || _accessibility.IsReduceMotion || !isActiveAndEnabled)
            {
                Destroy(entry);
                return;
            }
            FinishFloorEntryUnroll();
            entry.GetComponent<Image>().raycastTarget = false;
            _floorEntryFading = entry;
            _floorEntryUnroll = StartCoroutine(UnrollPath(entry));
        }

        // The entry held the controller focus (so Submit could not open a tile under it); give it back to the path.
        private void RestorePathFocus(GameObject entry)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject != entry) return;
            var node = _gbPathCursor >= 0 && _gbPathCursor < _gbReachableNodeButtons.Count
                ? _gbReachableNodeButtons[_gbPathCursor] : null;
            eventSystem.SetSelectedGameObject(node != null ? node.gameObject : null);
        }

        // Ends an unroll under way: the painting goes and the path scroll is at full width.
        private void FinishFloorEntryUnroll()
        {
            if (_floorEntryUnroll != null)
            {
                StopCoroutine(_floorEntryUnroll);
                _floorEntryUnroll = null;
            }
            if (_floorEntryFading != null) Destroy(_floorEntryFading);
            _floorEntryFading = null;
            if (_pathPaper != null) _pathPaper.localScale = Vector3.one;
        }

        // Coroutines stop with the object; leave neither the painting nor a half-open scroll behind.
        private void OnDisable()
        {
            DismissFloorEntry(false);
        }

        // The painting fades while the path scroll opens from its middle to full width.
        private IEnumerator UnrollPath(GameObject entry)
        {
            var group = entry.GetComponent<CanvasGroup>();
            var elapsed = 0f;
            var total = Mathf.Max(FloorEntryFadeSeconds, PathUnrollSeconds);
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                if (group != null) group.alpha = 1f - Mathf.Clamp01(elapsed / FloorEntryFadeSeconds);
                if (_pathPaper != null)
                {
                    var t = Mathf.Clamp01(elapsed / PathUnrollSeconds);
                    _pathPaper.localScale = new Vector3(Mathf.Lerp(0.04f, 1f, 1f - (1f - t) * (1f - t)), 1f, 1f);
                }
                yield return null;
            }
            _floorEntryUnroll = null;
            FinishFloorEntryUnroll();
        }
    }
}
