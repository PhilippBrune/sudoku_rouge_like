using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-016] The end of a Garden Run, victory and defeat in one layout (layout-proposals.md, "Run end"):
    // the painting, a banner, one seal per floor (the boss's mask up to the floor reached, the lost floor crossed out),
    // the run on a tally scroll with the class's closing line and one grade stamp per puzzle (SCORE-GRADE-003), the class
    // XP as a bar with a level-up stamp, the run score as a short ledger and new unlocks as ema plaques ("Nothing new this
    // time" and the next goal otherwise).
    // The scroll is built inside GameOverPanel, so the end-screen save rules and shortcuts keep working.
    public sealed partial class EndScreenViewController
    {
        private sealed class RunEndSummary
        {
            public bool Victory;
            public RunState State;
            public RunResult Result;
            public int PreLevel, PostLevel, XpInto, XpToNext;
            public List<ClassId> NewUnlocks;
            public List<ClassId> Unlocked;
            public List<string> SoClose;   // RM-111
            public List<string> Moved;     // RM-112
        }

        private const int MaxClassLevel = 40;
        // The text layout of the other modes; hidden while the scroll is shown.
        private static readonly string[] TextLayoutParts =
            { "PanelBackground", "GameOverTitle", "GameOverDetailsScrim", "GameOverDetails", "BtnGameOverBack" };
        private static readonly Color Green = new Color(0.20f, 0.42f, 0.22f, 1f);

        private GameObject _runEndScroll;
        private Vector2 _textLayoutMin, _textLayoutMax;
        private bool _textLayoutSaved;
        // Which text-layout parts were showing when the scroll took over, so switching back restores them as they were.
        private readonly Dictionary<string, bool> _textLayoutShown = new Dictionary<string, bool>();

        /// <summary>Switches GameOverPanel between the full-screen run-end scroll and the text layout.</summary>
        private void UseRunEndScroll(bool on)
        {
            var wasOn = _runEndScroll != null;
            ScrollUiKit.DestroyNow(_runEndScroll);
            _runEndScroll = null;
            if (_gameOverPanel == null) return;
            var rt = (RectTransform)_gameOverPanel.transform;
            if (!_textLayoutSaved)
            {
                _textLayoutMin = rt.anchorMin;
                _textLayoutMax = rt.anchorMax;
                _textLayoutSaved = true;
            }
            rt.anchorMin = on ? Vector2.zero : _textLayoutMin;
            rt.anchorMax = on ? Vector2.one : _textLayoutMax;
            var outline = _gameOverPanel.GetComponent<Outline>();
            if (outline != null) outline.enabled = !on;
            foreach (var part in TextLayoutParts)
            {
                var go = _gameOverPanel.transform.Find(part)?.gameObject;
                if (go == null) continue;
                if (on)
                {
                    if (!wasOn) _textLayoutShown[part] = go.activeSelf;
                    go.SetActive(false);
                }
                else if (wasOn)
                    go.SetActive(!_textLayoutShown.TryGetValue(part, out var shown) || shown);
            }
        }

        private void ShowRunEndScroll(RunEndSummary run)
        {
            UseRunEndScroll(true);
            var screen = ScrollUiKit.Area(_gameOverPanel.transform, "RunEndScroll", Vector2.zero, Vector2.one);
            _runEndScroll = screen.gameObject;
            ScrollUiKit.Painting(screen, "RunEndPainting", run.Victory ? "bg_victory" : "bg_defeat",
                run.Victory ? new Color(0.80f, 0.78f, 0.78f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f));

            var state = run.State;
            var floorReached = run.Victory ? state.TotalFloors : state.CurrentFloor + 1;
            // [REQ: RUN-LENGTH-001] The eyebrow names the run's length too ("· 41 min"), from one minute on.
            var eyebrow = F("InRun.End.Scroll.Eyebrow", ClassLabel(state.ClassId));
            if (state.ActiveSeconds >= 60f) eyebrow += " · " + RunLengthLog.FormatDuration(state.ActiveSeconds);
            ScrollUiKit.Banner(screen, "RunEndBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "RunEndEyebrow", eyebrow,
                "RunEndTitle", run.Victory ? T("InRun.End.Scroll.Victory") : F("InRun.End.Scroll.Defeat", floorReached));
            BuildFloorSeals(screen, state, run.Victory);
            BuildRunTally(screen, run, floorReached);
            BuildXpPanel(screen, run);
            BuildScoreLedger(screen, run.Result);
            BuildUnlocks(screen, run);
            BuildThisRun(screen, run);
            BuildEndButtons(screen, "BtnRunEndBack", new Vector2(0.7135f, 0.1407f), new Vector2(0.9219f, 0.1944f), RunAgain.KindOf(state));
        }

        // [REQ: RUN-AGAIN-001] Walk again first (it has the focus), Back under it; a run without Walk again has Back only.
        private void BuildEndButtons(Transform screen, string backName, Vector2 backMin, Vector2 backMax, RunAgain.Kind again)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            var back = ScrollUiKit.PillButton(screen, backName, backMin, backMax, T("InRun.End.Scroll.Back"), 24,
                new Color(0.24f, 0.20f, 0.16f, 1f), ScrollUiKit.PaperText, 30f);
            back.onClick.AddListener(() => OnReturnToMenuPressed?.Invoke());
            if (again == RunAgain.Kind.None)
            {
                if (events != null) events.SetSelectedGameObject(back.gameObject);
                return;
            }
            var step = backMax.y - backMin.y + 0.012f;
            var walk = ScrollUiKit.PillButton(screen, "BtnWalkAgain", new Vector2(backMin.x, backMin.y + step),
                new Vector2(backMax.x, backMax.y + step), T("InRun.End.WalkAgain"), 26, ScrollUiKit.Vermilion, Color.white, 30f);
            walk.onClick.AddListener(() => OnWalkAgainPressed?.Invoke());
            walk.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = back };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = walk };
            if (events != null) events.SetSelectedGameObject(walk.gameObject);
        }

        // [REQ: RUN-END-NEAR-001] [REQ: RUN-END-MOVED-001] "This run": how close a lost run came, then what it moved.
        private static void BuildThisRun(Transform screen, RunEndSummary run)
        {
            var near = run.SoClose ?? new List<string>();
            var moved = run.Moved ?? new List<string>();
            if (near.Count == 0 && moved.Count == 0) return;
            var header = ScrollUiKit.Label(screen, "ThisRunHeader", T("InRun.End.ThisRun").ToUpperInvariant(), 18, TextAnchor.MiddleLeft,
                ScrollUiKit.Gold, new Vector2(0.7135f, 0.405f), new Vector2(0.96f, 0.43f), 12);
            Shade(header);
            var lines = new List<string>();
            foreach (var line in near) lines.Add("<color=#E2B866>" + line + "</color>");
            lines.AddRange(moved);
            var text = ScrollUiKit.Label(screen, "ThisRunLines", string.Join("\n", lines), 18, TextAnchor.UpperLeft,
                new Color(0.88f, 0.84f, 0.74f, 1f), new Vector2(0.7135f, 0.272f), new Vector2(0.97f, 0.40f), 11);
            text.supportRichText = true;
            Shade(text);
        }

        // One seal per floor: the boss's mask on vermilion up to the floor reached, the lost floor crossed out, the rest
        // an empty frame.
        private static void BuildFloorSeals(Transform screen, RunState state, bool victory)
        {
            var floors = Mathf.Max(1, state.TotalFloors);
            const float step = 0.0802f, size = 84f, y = 0.789f;
            for (var f = 0; f < floors; f++)
            {
                var x = 0.5f + (f - (floors - 1) * 0.5f) * step;
                var cleared = victory || f < state.CurrentFloor;
                var lost = !victory && f == state.CurrentFloor;
                if (cleared)
                {
                    // [REQ: DAILY-WALK-007] The path seal worn from the stamp shop: its colour, round or square.
                    var round = InkStampShop.WornPathSeal.Round;
                    var seal = ScrollUiKit.Spot(screen, $"FloorSeal_{f}", new Vector2(x, y), new Vector2(size, size),
                        InkStampShop.PathSealColour(ScrollUiKit.Vermilion), round ? ScrollUiKit.Disc() : ScrollUiKit.Rounded(12f));
                    seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, round ? 0f : f % 2 == 0 ? -4f : 3f);
                    var shadow = seal.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                    shadow.effectDistance = new Vector2(2f, -4f);
                    ScrollUiKit.Icon(seal.transform, "Mask", "boss/icon_" + FloorThemeData.GetBossKey(f),
                        new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Color.white);
                }
                else
                {
                    var frame = ScrollUiKit.Spot(screen, $"FloorSeal_{f}", new Vector2(x, y), new Vector2(size, size),
                        new Color(0f, 0f, 0f, lost ? 0.25f : 0.12f));
                    ScrollUiKit.InkFrame(frame.transform, lost ? ScrollUiKit.Vermilion : new Color(0.92f, 0.88f, 0.79f, 0.55f), lost ? 3f : 2f);
                    if (lost)
                        foreach (var angle in new[] { 45f, -45f })
                        {
                            var bar = ScrollUiKit.Fill(frame.transform, angle > 0 ? "CrossA" : "CrossB", new Vector2(0.28f, 0.47f),
                                new Vector2(0.72f, 0.53f), new Color(0.88f, 0.30f, 0.24f, 1f));
                            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                        }
                }
                var name = ScrollUiKit.Label(screen, $"FloorName_{f}", FloorThemeData.GetFloorName(f), 17, TextAnchor.UpperCenter,
                    ScrollUiKit.PaperText, new Vector2(x - 0.04f, 0.70f), new Vector2(x + 0.04f, 0.745f), 11);
                Shade(name);
            }
        }

        // The tally scroll: the class, the run's numbers and the class's closing line.
        private static void BuildRunTally(Transform screen, RunEndSummary run, int floorReached)
        {
            var state = run.State;
            var paper = ScrollUiKit.Fill(screen, "RunEndTally", new Vector2(0.0781f, 0.1204f), new Vector2(0.3073f, 0.6944f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var t = paper.rectTransform;
            ScrollUiKit.Fill(t, "PaperLight", new Vector2(0f, 0.45f), new Vector2(0.7f, 1f), new Color(1f, 1f, 1f, 0.22f), ScrollUiKit.Glow());
            ScrollUiKit.BrocadeBand(t, "BrocadeTop", new Vector2(0f, 0.968f), Vector2.one, true);
            ScrollUiKit.BrocadeBand(t, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.032f), true);

            ScrollUiKit.Icon(t, "ClassIcon", "class/icon_" + ClassCatalog.GetIconName(state.ClassId),
                new Vector2(0.114f, 0.823f), new Vector2(0.261f, 0.927f), Color.white);
            ScrollUiKit.Label(t, "ClassHeader", T("InRun.End.Scroll.Class").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.30f, 0.885f), new Vector2(0.95f, 0.92f), 11);
            ScrollUiKit.Label(t, "ClassName", ClassLabel(state.ClassId), 30, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.30f, 0.825f), new Vector2(0.95f, 0.888f), 18);

            var rows = new (string label, string value)[]
            {
                (T("InRun.End.Scroll.FloorReached"), F("InRun.End.Scroll.FloorOf", floorReached, state.TotalFloors)),
                (T("InRun.Reward.Row.Hp"), $"{state.CurrentHP} / {state.MaxHP}"),
                (T("InRun.End.Scroll.Gold"), state.CurrentGold.ToString()),
                (T("InRun.End.Scroll.PuzzlesSolved"), (run.Result?.TileXpEntries?.Count ?? 0).ToString())
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var centre = 0.739f - i * 0.068f;
                ScrollUiKit.ValueRow(t, $"Tally_{i}", new Vector2(0.10f, centre - 0.03f), new Vector2(0.90f, centre + 0.03f),
                    rows[i].label, rows[i].value, 22, ScrollUiKit.Ink);
            }
            BuildGradeStamps(t, run.Result);
            var closing = ScrollUiKit.Label(t, "ClosingLine", ParkNarrativeService.GetClassRunEndLine(run.Victory, state.ClassId), 18,
                TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.205f), 12);
            closing.fontStyle = FontStyle.Italic;
        }

        // Stamp row geometry on the tally paper (440 × 620 px): 20 px stamps, a wider gap between floors, three lines; a
        // very long run (Ascension, many side tiles) gets smaller stamps so they still fit.
        private const float TallyWidthPx = 440f, TallyHeightPx = 620f;
        private const float StampLeftPx = 0.10f * TallyWidthPx, StampRightPx = 0.90f * TallyWidthPx;
        private const int MaxStampLines = 3;
        private static readonly float[] StampSizes = { 20f, 16f, 12f, 9f };

        // How many lines the stamps take at one size (gap and floor gap scale with it).
        private static int StampLines(List<(PuzzleGrade Grade, int Floor)> entries, float size)
        {
            float gap = size * 0.2f, floorGap = size * 0.6f, x = StampLeftPx;
            var lines = 1;
            for (var i = 0; i < entries.Count; i++)
            {
                if (RunGradeStamps.NewFloor(entries, i)) x += floorGap - gap;
                if (x + size > StampRightPx) { x = StampLeftPx; lines++; }
                x += size + gap;
            }
            return lines;
        }

        // [REQ: SCORE-GRADE-003] One stamp per puzzle in the order played (Perfect, Clean, Rough, Messy; the puzzle that
        // ended the run is Messy), floors apart, with a legend; so the player sees when the run went well or wrong.
        private static void BuildGradeStamps(RectTransform tally, RunResult result)
        {
            var entries = RunGradeStamps.EntriesOf(result);
            if (entries.Count == 0) return;
            ScrollUiKit.Label(tally, "GradesHeader", T("InRun.End.Scroll.Grades").ToUpperInvariant(), 15, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.10f, 0.455f), new Vector2(0.90f, 0.49f), 10);
            var size = StampSizes[StampSizes.Length - 1];
            foreach (var candidate in StampSizes)
                if (StampLines(entries, candidate) <= MaxStampLines) { size = candidate; break; }
            float gap = size * 0.2f, floorGap = size * 0.6f, lineStep = size * 1.3f, top = 0.445f * TallyHeightPx;
            float x = StampLeftPx, line = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                if (RunGradeStamps.NewFloor(entries, i)) x += floorGap - gap;
                if (x + size > StampRightPx) { x = StampLeftPx; line++; }
                var centre = new Vector2((x + size * 0.5f) / TallyWidthPx, (top - line * lineStep - size * 0.5f) / TallyHeightPx);
                var grade = entries[i].Grade;
                var stamp = ScrollUiKit.Spot(tally, $"GradeStamp_{i}", centre, new Vector2(size, size),
                    RunGradeStamps.ColorOf(grade), ScrollUiKit.Rounded(4f));
                stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i % 3 == 0 ? -4f : i % 3 == 1 ? 3f : 0f);
                ScrollUiKit.Label(stamp.transform, "Mark", RunGradeStamps.MarkOf(grade), Mathf.RoundToInt(size * 0.65f),
                    TextAnchor.MiddleCenter, ScrollUiKit.PaperText, Vector2.zero, Vector2.one, 6, FontStyle.Bold);
                x += size + gap;
            }
            var legendTop = (top - (line + 1) * lineStep - 2f) / TallyHeightPx;
            var legend = ScrollUiKit.Label(tally, "GradeLegend", RunGradeStamps.Legend(), 14, TextAnchor.UpperLeft,
                ScrollUiKit.InkSoft, new Vector2(0.10f, legendTop - 0.045f), new Vector2(0.90f, legendTop), 9);
            legend.supportRichText = true;
        }

        // The class XP of this run: the gain, a bar to the next level and a stamp when the class levelled up.
        private static void BuildXpPanel(Transform screen, RunEndSummary run)
        {
            var panel = ScrollUiKit.Fill(screen, "RunEndXp", new Vector2(0.3438f, 0.4278f), new Vector2(0.6771f, 0.6944f),
                new Color(0.10f, 0.08f, 0.06f, 0.80f), ScrollUiKit.Rounded(14f));
            var rim = panel.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.86f, 0.71f, 0.38f, 0.35f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);
            var t = panel.transform;
            var className = ClassLabel(run.State.ClassId);
            ScrollUiKit.Label(t, "XpHeader", T("InRun.End.Scroll.XpHeader").ToUpperInvariant(), 17, TextAnchor.MiddleLeft,
                new Color(0.86f, 0.82f, 0.72f, 1f), new Vector2(0.0625f, 0.833f), new Vector2(0.70f, 0.889f), 11);
            ScrollUiKit.Label(t, "XpGain", F("InRun.End.Scroll.XpGain", run.Result?.XpEarned ?? 0), 44, TextAnchor.MiddleLeft,
                ScrollUiKit.Gold, new Vector2(0.0625f, 0.65f), new Vector2(0.70f, 0.79f), 24);

            var maxed = run.PostLevel >= MaxClassLevel || run.XpToNext <= 0;
            var fill = maxed ? 1f : Mathf.Clamp01(run.XpInto / (float)run.XpToNext);
            var track = ScrollUiKit.Fill(t, "XpTrack", new Vector2(0.0625f, 0.444f), new Vector2(0.934f, 0.559f),
                new Color(1f, 1f, 1f, 0.12f), ScrollUiKit.Rounded(16f));
            if (fill > 0f)
                ScrollUiKit.Fill(track.transform, "XpFill", Vector2.zero, new Vector2(Mathf.Max(fill, 0.06f), 1f), ScrollUiKit.Gold,
                    ScrollUiKit.Rounded(16f));
            ScrollUiKit.Label(t, "XpLevel", F("InRun.End.Scroll.ClassLevel", className, run.PostLevel), 20, TextAnchor.MiddleLeft,
                ScrollUiKit.PaperText, new Vector2(0.0625f, 0.33f), new Vector2(0.55f, 0.40f), 12);
            ScrollUiKit.Label(t, "XpToNext", maxed ? T("InRun.End.Scroll.MaxLevel") : F("InRun.End.Scroll.XpToNext", run.XpInto, run.XpToNext,
                    run.PostLevel + 1), 20, TextAnchor.MiddleRight, ScrollUiKit.PaperText, new Vector2(0.45f, 0.33f), new Vector2(0.934f, 0.40f), 12);

            if (run.PostLevel <= run.PreLevel) return;
            var stamp = ScrollUiKit.Spot(t, "LevelUpStamp", new Vector2(0.852f, 0.712f), new Vector2(122f, 122f), ScrollUiKit.Gold,
                ScrollUiKit.Enso());
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            ScrollUiKit.Label(stamp.transform, "Text", F("InRun.End.Scroll.LevelUp", run.PreLevel, run.PostLevel), 22, TextAnchor.MiddleCenter,
                ScrollUiKit.Gold, new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.85f), 12);
        }

        // The run score as a ledger: base XP, each bonus and the mistake penalty, then the final score.
        private static void BuildScoreLedger(Transform screen, RunResult result)
        {
            var paper = ScrollUiKit.Fill(screen, "RunEndLedger", new Vector2(0.3438f, 0.1204f), new Vector2(0.6771f, 0.3981f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);
            var t = paper.transform;
            ScrollUiKit.Label(t, "Header", T("InRun.End.Scroll.ScoreHeader").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.0625f, 0.86f), new Vector2(0.94f, 0.907f), 11);
            var score = RunScoreBreakdown.For(result);
            var rows = LedgerRows(score);
            // Up to seven rows (base, three grades, cursed, victory, gold) share the paper above the score line.
            var step = rows.Count > 1 ? Mathf.Min(0.13f, 0.52f / (rows.Count - 1)) : 0.13f;
            var size = step < 0.11f ? 18 : 21;
            var centre = 0.79f;
            for (var i = 0; i < rows.Count; i++, centre -= step)
                ScrollUiKit.ValueRow(t, $"Ledger_{i}", new Vector2(0.0625f, centre - step * 0.42f), new Vector2(0.934f, centre + step * 0.42f),
                    rows[i].label, rows[i].value, size, rows[i].color);
            var lineY = centre + step - 0.095f;
            ScrollUiKit.Line(t, "Rule", new Vector2(0.0625f, lineY), new Vector2(0.934f, lineY), new Color(0.70f, 0.20f, 0.15f, 0.6f), 2f);
            ScrollUiKit.ValueRow(t, "LedgerScore", new Vector2(0.0625f, lineY - 0.16f), new Vector2(0.934f, lineY - 0.04f),
                T("InRun.End.Scroll.Score"), score.Total.ToString(), 28, ScrollUiKit.Ink);
        }

        // GameDesignSpec §9c (GAP-053): the base XP, then each grade, bonus and the kept gold with its share of the base in
        // the label and its points as the value, so the rows add up to the score below them.
        private static List<(string label, string value, Color color)> LedgerRows(RunScoreBreakdown score)
        {
            var rows = new List<(string, string, Color)> { (T("InRun.End.Scroll.BaseXp"), score.BaseXp.ToString(), ScrollUiKit.Ink) };
            foreach (var line in score.Lines)
            {
                var percent = Math.Abs(line.Percent);
                var label = line.Kind switch
                {
                    RunScoreLineKind.Perfect => F("InRun.End.Scroll.Perfect", line.Count, percent),
                    RunScoreLineKind.Rough => F("InRun.End.Scroll.Rough", line.Count, percent),
                    RunScoreLineKind.Messy => F("InRun.End.Scroll.Messy", line.Count, percent),
                    RunScoreLineKind.Cursed => F("InRun.End.Scroll.Cursed", line.Count, percent),
                    RunScoreLineKind.Victory => F("InRun.End.Scroll.VictoryBonus", percent),
                    RunScoreLineKind.Time => F("InRun.End.Scroll.TimeBonus", line.Count, percent),
                    _ => F("InRun.End.Scroll.GoldKept", line.Count, percent)
                };
                rows.Add((label, Points(line.Points), line.Points < 0 ? ScrollUiKit.Vermilion : Green));
            }
            return rows;
        }

        private static string Points(int points) => points < 0 ? $"\u2212{-points}" : $"+{points}";

        // New classes as ema plaques; without one, "Nothing new this time" and the next class to unlock.
        private static void BuildUnlocks(Transform screen, RunEndSummary run)
        {
            var header = ScrollUiKit.Label(screen, "UnlocksHeader", T("InRun.End.Scroll.New").ToUpperInvariant(), 18, TextAnchor.MiddleLeft,
                ScrollUiKit.Gold, new Vector2(0.7135f, 0.672f), new Vector2(0.80f, 0.698f), 12);
            Shade(header);
            if (run.NewUnlocks != null && run.NewUnlocks.Count > 0)
            {
                for (var i = 0; i < run.NewUnlocks.Count && i < 2; i++)
                    BuildUnlockEma(screen, i, run.NewUnlocks[i]);
                return;
            }
            // Without saved progress there is no next goal to name.
            var text = T("InRun.End.Scroll.NothingNew");
            if (run.Unlocked != null)
            {
                var next = NextLockedClass(run.Unlocked);
                text += "\n" + (next.HasValue
                    ? F("InRun.End.Scroll.NextGoal", LocalizationService.T("Class.UnlockCondition." + next.Value,
                        ClassCatalog.GetDefinition(next.Value).UnlockCondition), ClassLabel(next.Value))
                    : T("InRun.End.Scroll.AllUnlocked"));
            }
            var line = ScrollUiKit.Label(screen, "NothingNew", text, 19, TextAnchor.UpperLeft, new Color(0.88f, 0.84f, 0.74f, 1f),
                new Vector2(0.7135f, 0.56f), new Vector2(0.96f, 0.665f), 12);
            Shade(line);
        }

        private static void BuildUnlockEma(Transform screen, int index, ClassId classId)
        {
            var x = 0.7708f + index * 0.105f;
            var ema = ScrollUiKit.Fill(screen, $"UnlockEma_{index}", new Vector2(x, 0.45f), new Vector2(x + 0.0938f, 0.662f),
                new Color(0.86f, 0.66f, 0.42f, 1f), ScrollUiKit.Ema());
            var shadow = ema.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -8f);
            ScrollUiKit.Icon(ema.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(classId),
                new Vector2(0.27f, 0.36f), new Vector2(0.73f, 0.73f), Color.white);
            ScrollUiKit.Label(ema.transform, "Name", ClassLabel(classId), 22, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.30f), 13);
            ScrollUiKit.Label(ema.transform, "What", T("InRun.End.Scroll.ClassUnlocked"), 15, TextAnchor.MiddleCenter, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.15f), 10);
        }

        // The next class in catalog order (the order the class select shows) that is still locked.
        private static ClassId? NextLockedClass(List<ClassId> unlocked)
        {
            foreach (var def in ClassCatalog.GetAll())
                if (!unlocked.Contains(def.Id)) return def.Id;
            return null;
        }

        private static void Shade(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -2f);
        }
    }
}
