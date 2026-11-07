using System;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-039] Quit · A · The garden at dusk (owner's pick, 0.3.1 playtest, RM-224): when the game closes, the
    /// main menu's painting dims to evening and a hanging scroll says goodbye with what waits tomorrow (the day's walk and
    /// its streak, the saved run, the Monthly Walk's days), the red Kratossos seal at its foot. It shows for 3 s, then the
    /// game closes; any key, click or button closes at once. The window's own close (Alt+F4, X) shows it too.
    /// </summary>
    public sealed class QuitScreen : MonoBehaviour
    {
        public const float Seconds = 3f;
        private static bool _done;
        private static QuitScreen _shown;
        private static DateTime _deadlineUtc = DateTime.MaxValue;

        private float _startedAt;
        private Image _bar, _lanternGlow;
        private Action _quit;

        /// <summary>For Application.wantsToQuit: the first request shows the screen and waits; the screen's own quit passes.</summary>
        public static bool AllowQuit()
        {
#if UNITY_EDITOR
            return true;   // the Editor's Stop must never wait; the menu's Quit shows the screen there
#else
            // A second request after the screen's time (the OS logging off, Update not ticking) always closes.
            if (_done || Application.isBatchMode || DateTime.UtcNow > _deadlineUtc) return true;
#endif
#if !UNITY_EDITOR
            Show();
            return false;
#endif
        }

        /// <summary>Shows the screen (once); <paramref name="quit"/> runs when it ends (the game closes by default).</summary>
        public static QuitScreen Show(Action quit = null)
        {
            if (_shown != null) return _shown;
            var root = new GameObject("QuitScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;   // above every menu, run screen and modal
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _shown = root.AddComponent<QuitScreen>();
            _shown._quit = quit ?? CloseGame;
            _shown._startedAt = Time.unscaledTime;
            _deadlineUtc = DateTime.UtcNow.AddSeconds(Seconds + 1f);
            Application.runInBackground = true;   // it must end even when the window has lost the focus
            try { _shown.Build((RectTransform)root.transform, new SaveFileService(SaveProfileService.ActiveSlot), DateTime.Today); }
            catch (Exception ex) { Debug.LogWarning("[Quit] The goodbye could not read everything: " + ex.Message); }
            return _shown;
        }

        private static void CloseGame()
        {
            _done = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Seconds);
            if (_bar != null) _bar.rectTransform.anchorMax = new Vector2(t, 1f);
            if (_lanternGlow != null && !AnimationHelper.MotionReduced)   // the lantern goes out last
                _lanternGlow.color = new Color(1f, 0.64f, 0.35f, 0.55f * (1f - t * t));
            var skip = Time.unscaledTime - _startedAt > 0.25f && (Input.anyKeyDown || PadButtons.AnyPressed());
            if (t < 1f && !skip) return;
            var quit = _quit;
            _quit = null;
            quit?.Invoke();
        }

        private void OnDestroy()
        {
            if (_shown == this) _shown = null;
        }

        private void Build(RectTransform root, SaveFileService save, DateTime today)
        {
            var screen = ScrollUiKit.Area(root, "QuitDusk", Vector2.zero, Vector2.one);
            ScrollUiKit.Fill(screen, "Dark", Vector2.zero, Vector2.one, Color.black).raycastTarget = true;   // nothing under it reacts
            ScrollUiKit.Painting(screen, "QuitPainting", "bg_main_menu", new Color(0.40f, 0.34f, 0.40f, 1f));
            ScrollUiKit.Fill(screen, "DuskTop", new Vector2(0f, 0.45f), Vector2.one, new Color(0.16f, 0.09f, 0.18f, 0.45f));
            ScrollUiKit.Fill(screen, "DuskFoot", Vector2.zero, new Vector2(1f, 0.30f), new Color(0.03f, 0.02f, 0.02f, 0.55f));
            _lanternGlow = ScrollUiKit.Spot(screen, "LanternGlow", new Vector2(0.80f, 0.53f), new Vector2(170f, 170f),
                new Color(1f, 0.64f, 0.35f, 0.55f), ScrollUiKit.Glow());
            ScrollUiKit.Icon(screen, "Lantern", "ui/icon_modes_lantern", new Vector2(0.7815f, 0.49f), new Vector2(0.8185f, 0.575f), Color.white);

            var paper = ScrollUiKit.HangingScroll(screen, "QuitScroll", new Vector2(0.344f, 0.165f), new Vector2(0.656f, 0.90f));
            ScrollUiKit.Label(paper, "QuitEyebrow", T("Quit.Eyebrow", "The gate closes for tonight").ToUpperInvariant(), 18,
                TextAnchor.MiddleCenter, ScrollUiKit.InkSoft, new Vector2(0.06f, 0.885f), new Vector2(0.94f, 0.925f), 12);
            ScrollUiKit.Label(paper, "QuitTitle", T("Quit.Title", "The garden\nwaits for you"), 56, TextAnchor.MiddleCenter,
                ScrollUiKit.Ink, new Vector2(0.06f, 0.71f), new Vector2(0.94f, 0.88f), 30).lineSpacing = 0.95f;
            ScrollUiKit.Line(paper, "Brush", new Vector2(0.30f, 0.685f), new Vector2(0.70f, 0.685f), ScrollUiKit.Vermilion, 6f);
            ScrollUiKit.Label(paper, "QuitPoem", T("Quit.Poem", "The sand is raked, the koi are still.\nNine digits rest until you return."),
                23, TextAnchor.MiddleCenter, ScrollUiKit.InkSoft, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.665f), 14,
                FontStyle.Italic).lineSpacing = 1.2f;

            ScrollUiKit.Label(paper, "TomorrowHeader", T("Quit.Tomorrow", "Tomorrow in the garden").ToUpperInvariant(), 15,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.10f, 0.495f), new Vector2(0.90f, 0.53f), 10);
            var row = 0;
            foreach (var (icon, text) in Tomorrow(save, today))
            {
                var top = 0.49f - row * 0.075f;
                ScrollUiKit.Line(paper, $"TomorrowRule_{row}", new Vector2(0.10f, top), new Vector2(0.90f, top),
                    new Color(0.24f, 0.18f, 0.12f, 0.18f), 1.5f);
                ScrollUiKit.Icon(paper, $"TomorrowIcon_{row}", icon, new Vector2(0.10f, top - 0.065f), new Vector2(0.17f, top - 0.01f), Color.white);
                var label = ScrollUiKit.Label(paper, $"TomorrowLine_{row}", text, 20, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                    new Vector2(0.20f, top - 0.07f), new Vector2(0.92f, top - 0.005f), 12);
                label.supportRichText = true;
                row++;
            }

            var seal = ScrollUiKit.Spot(paper, "KratossosSeal", new Vector2(0.5f, 0.115f), new Vector2(112f, 112f), ScrollUiKit.Vermilion,
                ScrollUiKit.Rounded(10f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var inner = seal.gameObject.AddComponent<Outline>();
            inner.effectColor = new Color(0.98f, 0.93f, 0.88f, 0.9f);
            inner.effectDistance = new Vector2(2f, -2f);
            ScrollUiKit.Label(seal.transform, "Mark", "K", 46, TextAnchor.MiddleCenter, new Color(0.98f, 0.93f, 0.88f, 1f),
                new Vector2(0f, 0.32f), new Vector2(1f, 0.95f));
            ScrollUiKit.Label(seal.transform, "Studio", T("Quit.Studio", "KRATOSSOS"), 13, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.93f, 0.88f, 1f), new Vector2(0f, 0.10f), new Vector2(1f, 0.34f), 9);

            var track = ScrollUiKit.Fill(screen, "QuitBarTrack", new Vector2(0.344f, 0.112f), new Vector2(0.656f, 0.118f),
                new Color(0.95f, 0.91f, 0.81f, 0.18f), ScrollUiKit.Rounded(3f));
            _bar = ScrollUiKit.Fill(track.transform, "QuitBar", Vector2.zero, new Vector2(0f, 1f), ScrollUiKit.Gold, ScrollUiKit.Rounded(3f));
            var hint = ScrollUiKit.Label(screen, "QuitHint", string.Empty, 19, TextAnchor.MiddleCenter, new Color(0.85f, 0.79f, 0.66f, 1f),
                new Vector2(0.30f, 0.07f), new Vector2(0.70f, 0.10f), 12);
            DeviceHint.Attach(hint, "Quit.Hint");   // INPUT-HINT-001
        }

        /// <summary>What waits tomorrow, up to three lines: the day's walk and its streak, the saved run (or today's walk),
        /// the Monthly Walk and its days.</summary>
        internal static System.Collections.Generic.List<(string Icon, string Text)> Tomorrow(SaveFileService save, DateTime today)
        {
            var lines = new System.Collections.Generic.List<(string, string)>();
            SaveFileEnvelope envelope;
            try { envelope = save != null && save.HasSaveFile() ? save.Load() : new SaveFileEnvelope(); }
            catch (Exception) { envelope = new SaveFileEnvelope(); }   // a broken save only loses the "tomorrow" lines
            var streak = envelope.DailyGoals?.CurrentStreak ?? 0;
            lines.Add(("ui/icon_golden_bloom", streak > 0
                ? LocalizationService.Format("Quit.Daily.Streak", "A new Daily Walk · <color=#9e2a2b>streak day {0}</color> if you come", streak + 1)
                : T("Quit.Daily", "A new Daily Walk waits")));
            // [REQ: SAVE-DAILY-001] The saved Garden Run and today's walk each get their line.
            var run = envelope.ActiveRunState;
            if (SaveFileService.IsResumableRunState(run))
                lines.Add((ClassCatalog.IsClass(run.ClassId) ? "class/icon_" + ClassCatalog.GetIconName(run.ClassId) : "ui/icon_golden_bloom",
                    LocalizationService.Format("Quit.Run", "Your run waits on <color=#9e2a2b>Floor {0}</color>, {1}",
                        run.WalkFloor, FloorThemeData.GetFloorName(run.CurrentFloor))));
            var walk = envelope.ActiveDailyRunState;
            if (SaveFileService.IsResumableRunState(walk) && !SaveFileService.IsWalkOfAnotherDay(walk, today))
                lines.Add(("ui/icon_golden_bloom", T("Quit.Walk", "Today's walk waits until midnight")));
            lines.Add(("ui/icon_monthly_lantern", LocalizationService.Format("Quit.Monthly", "{0} · {1}",
                SeasonalChallengeService.GetThemeName(today.Year, today.Month), SeasonalChallengeService.GetCountdownLabel(today))));
            return lines;
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);
    }
}
