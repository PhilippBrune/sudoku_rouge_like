using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-030] The Incense Walk's puzzle screen (owner 2026-10-26): where the HP and Pencil lanterns hang, an
    // incense stick burns down in a bowl (its length is the time left), the time large beside it, the bonus a solved
    // puzzle floats up; under it a slip with what this puzzle adds (cut by each mistake) and one with the walk so far.
    // In the last 30 seconds the ember and the time turn red. No smoke and no flicker, so Reduce Motion changes nothing.
    public sealed partial class HudViewController
    {
        private const float StickFullSeconds = 600f, LateSeconds = 30f, FloatSeconds = 1.6f;
        private static readonly string[] LanternParts =
            { "HpPencilScrim", "HpBarBg", "PencilBarBg", "SudokuGameplayHp", "SudokuGameplayPencil", "BtnBuyPencil", "PuzzleTimer" };
        private static readonly Color Ember = new Color(1f, 0.70f, 0.33f, 1f);
        private static readonly Color EmberLate = new Color(0.86f, 0.22f, 0.17f, 1f);
        private static readonly Color TimeCalm = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color TimeLate = new Color(1f, 0.56f, 0.48f, 1f);
        private static readonly Color Green = new Color(0.18f, 0.42f, 0.23f, 1f);

        private RectTransform _incense, _stick;
        private Image _ember, _ash;
        private Text _incenseTime, _incenseFloat, _adds, _addsLines, _walkLines;
        private float _floatUntil;
        private bool _incenseShown;

        /// <summary>The walk's HUD, refreshed every frame; outside the walk it hides itself and leaves the lanterns.</summary>
        public void ShowIncense(IncenseWalkService walk, LevelConfig config, LevelState level, int best, ClassId classId)
        {
            var on = walk != null && _sudokuPanel != null;
            if (on != _incenseShown && _sudokuPanel != null)
            {
                _incenseShown = on;
                foreach (var part in LanternParts)
                {
                    var t = _sudokuPanel.transform.Find(part);
                    if (t != null && (on || part != "BtnBuyPencil")) t.gameObject.SetActive(!on);   // its refresh shows it
                }
                if (on && _incense == null) BuildIncense();
                if (_incense != null) _incense.gameObject.SetActive(on);
                if (!on)
                {
                    if (_timerText != null) _timerText.gameObject.SetActive(true);
                    if (_runTimerText != null) _runTimerText.gameObject.SetActive(true);
                    if (_passiveText != null) _passiveText.gameObject.SetActive(true);
                }
            }
            if (!on) return;
            // The puzzle and run timers and the HP passive mean nothing here; both are made later than the walk starts.
            if (_timerText != null && _timerText.gameObject.activeSelf) _timerText.gameObject.SetActive(false);
            if (_runTimerText != null && _runTimerText.gameObject.activeSelf) _runTimerText.gameObject.SetActive(false);
            if (_passiveText != null && _passiveText.gameObject.activeSelf) _passiveText.gameObject.SetActive(false);

            var late = walk.SecondsLeft <= LateSeconds;
            var length = Mathf.Clamp01(walk.SecondsLeft / StickFullSeconds);
            _stick.anchorMax = new Vector2(_stick.anchorMax.x, Mathf.Lerp(0.04f, 1f, length));
            _ember.color = late ? EmberLate : Ember;
            _incenseTime.text = Clock(walk.SecondsLeft);
            _incenseTime.color = late ? TimeLate : TimeCalm;
            _incenseFloat.gameObject.SetActive(Time.unscaledTime < _floatUntil);

            var mistakes = level?.Mistakes ?? 0;
            var size = config?.BoardSize ?? 6;
            var stars = config?.Stars ?? 1;
            var boardSeconds = IncenseWalkService.SolveSeconds(size, 1, 0) - IncenseWalkService.PerfectSeconds;
            _adds.text = "+" + Clock(IncenseWalkService.SolveSeconds(size, stars, mistakes));
            var lines = Tf("Incense.Hud.Board", size, Clock(boardSeconds), new string('★', Mathf.Max(1, stars)),
                Clock(IncenseWalkService.SecondsPerStar * (stars - 1)));
            if (mistakes > 0)
                lines += "\n<color=#9E2A2B>" + Tf("Incense.Hud.Mistakes", mistakes,
                    Mathf.Min(IncenseWalkService.MaxBonusCutPercent, IncenseWalkService.BonusCutPerMistakePercent * mistakes)) + "</color>";
            lines += "\n" + (mistakes == 0
                ? Tf("Incense.Hud.Perfect", Clock(IncenseWalkService.PerfectSeconds))
                : "<color=#9A8F7A>" + Tf("Incense.Hud.PerfectLost", Clock(IncenseWalkService.PerfectSeconds)) + "</color>");
            _addsLines.text = lines;
            _walkLines.text = Tf("Incense.Hud.Walk", walk.PuzzlesSolved, Number(walk.Score), Number(System.Math.Max(best, walk.Score)))
                              + "\n" + Classes.ClassCatalog.DisplayName(classId) + ": " + LocalizationService.T("Incense.Class." + classId);
        }

        /// <summary>The seconds a solved puzzle added, floating up beside the time for a moment.</summary>
        public void FloatIncenseBonus(int seconds)
        {
            if (_incenseFloat == null) return;
            _incenseFloat.text = "+" + Clock(seconds);
            _floatUntil = Time.unscaledTime + FloatSeconds;
        }

        private void BuildIncense()
        {
            _incense = ScrollUiKit.Area(_sudokuPanel.transform, "IncenseHud", new Vector2(0.025f, 0.10f), new Vector2(0.255f, 0.86f));
            // The bowl with the stick standing in it, on the left of the column.
            var holder = ScrollUiKit.Area(_incense, "StickHolder", new Vector2(0.10f, 0.50f), new Vector2(0.36f, 0.99f));
            ScrollUiKit.Fill(holder, "Bowl", new Vector2(0.02f, 0f), new Vector2(0.98f, 0.10f), new Color(0.24f, 0.25f, 0.27f, 1f),
                ScrollUiKit.Rounded(18f));
            var column = ScrollUiKit.Area(holder, "StickColumn", new Vector2(0.45f, 0.08f), new Vector2(0.55f, 0.94f));
            _stick = ScrollUiKit.Fill(column, "Stick", Vector2.zero, Vector2.one, new Color(0.47f, 0.30f, 0.19f, 1f), ScrollUiKit.Rounded(3f))
                .rectTransform;
            _ash = ScrollUiKit.Fill(_stick, "Ash", new Vector2(0f, 0.93f), Vector2.one, new Color(0.58f, 0.56f, 0.52f, 1f));
            _ember = ScrollUiKit.Spot(_stick, "Ember", new Vector2(0.5f, 1f), new Vector2(26f, 26f), Ember, ScrollUiKit.Disc());
            var glow = ScrollUiKit.Spot(_ember.transform, "Glow", new Vector2(0.5f, 0.5f), new Vector2(64f, 64f),
                new Color(1f, 0.63f, 0.24f, 0.45f), ScrollUiKit.Glow());
            glow.transform.SetAsFirstSibling();

            // The time beside the stick, the solved puzzle's bonus above it.
            var header = ScrollUiKit.Label(_incense, "TimeHeader", LocalizationService.T("Incense.Hud.TimeLeft").ToUpperInvariant(), 16,
                TextAnchor.MiddleLeft, new Color(0.85f, 0.79f, 0.66f, 1f), new Vector2(0.42f, 0.70f), new Vector2(1f, 0.74f), 11);
            Shade(header);
            _incenseTime = ScrollUiKit.Label(_incense, "IncenseTime", string.Empty, 64, TextAnchor.MiddleLeft, TimeCalm,
                new Vector2(0.42f, 0.58f), new Vector2(1f, 0.70f), 30);
            Shade(_incenseTime);
            _incenseFloat = ScrollUiKit.Label(_incense, "IncenseFloat", string.Empty, 32, TextAnchor.MiddleLeft,
                new Color(0.62f, 0.83f, 0.66f, 1f), new Vector2(0.42f, 0.76f), new Vector2(1f, 0.84f), 18);
            Shade(_incenseFloat);

            // What this puzzle adds, and the walk so far, on two paper slips.
            var adds = ScrollUiKit.PaperTag(_incense, "AddsSlip", new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.48f), -1f);
            ScrollUiKit.Label(adds.transform, "Header", LocalizationService.T("Incense.Hud.Adds").ToUpperInvariant(), 14, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.95f), 10);
            _adds = ScrollUiKit.Label(adds.transform, "Seconds", string.Empty, 36, TextAnchor.MiddleLeft, Green,
                new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.82f), 18);
            _addsLines = ScrollUiKit.Label(adds.transform, "Lines", string.Empty, 16, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.56f), 10);
            _addsLines.supportRichText = true;
            var walk = ScrollUiKit.PaperTag(_incense, "WalkSlip", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.22f), 1f);
            ScrollUiKit.Label(walk.transform, "Header", LocalizationService.T("Incense.Hud.WalkHeader").ToUpperInvariant(), 14,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.95f), 10);
            _walkLines = ScrollUiKit.Label(walk.transform, "Lines", string.Empty, 16, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.78f), 10);
        }

        private static void Shade(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -2f);
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        private static string Number(int value) => value.ToString("N0", LocalizationService.Culture);

        private static string Tf(string key, params object[] args) => LocalizationService.Format(key, key, args);
    }
}
