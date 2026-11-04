using System.Collections.Generic;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-026] The results of Endless Zen, Spirit Trials and the Monthly Walk in the run-end family (owner
    // 2026-10-21: "the result texts of the Monthly Walk, Spirit Trials and Endless Zen are not [in the scroll style]"):
    // the mode's painting, a banner with the mode and the outcome, the numbers on a tally paper with a closing line, the
    // record (depth, score or grade) large on a dark panel with the personal best and a "New best" stamp, notes on a paper
    // slip (ink stamps, the month's countdown) and the way back. Built inside GameOverPanel like the run-end scroll.
    public sealed partial class EndScreenViewController
    {
        private sealed class ModeEndSummary
        {
            public string Painting;
            public bool Good;
            public string Eyebrow, Title;
            public readonly List<(string Label, string Value)> Rows = new List<(string, string)>();
            public string Closing;
            public string RecordHeader, RecordValue, RecordLine;
            public bool NewBest;
            public readonly List<string> Notes = new List<string>();
            public RunAgain.Kind Again;
            public string BoardHeaderKey;   // [REQ: SEED-BOARD-003] set: a board slip in the notes' place, the notes go to the record
        }

        private void ShowModeEndScroll(ModeEndSummary end)
        {
            if (_gameOverPanel == null) return;
            UseRunEndScroll(true);
            var screen = ScrollUiKit.Area(_gameOverPanel.transform, "ModeEndScroll", Vector2.zero, Vector2.one);
            _runEndScroll = screen.gameObject;
            ScrollUiKit.Painting(screen, "ModeEndPainting", end.Painting,
                end.Good ? new Color(0.78f, 0.76f, 0.76f, 1f) : new Color(0.60f, 0.60f, 0.60f, 1f));
            ScrollUiKit.Banner(screen, "ModeEndBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "ModeEndEyebrow", end.Eyebrow, "ModeEndTitle", end.Title);
            BuildModeTally(screen, end);
            if (end.BoardHeaderKey != null && end.Notes.Count > 0)
            {
                end.RecordLine += "\n" + string.Join("\n", end.Notes);
                end.Notes.Clear();
            }
            BuildModeRecord(screen, end);
            _modeBoard = null;
            if (end.BoardHeaderKey != null)
            {
                // The record box gets shorter, so the board and its bookmarks fit above the buttons.
                if (screen.Find("ModeEndRecord") is RectTransform record) record.anchorMin = new Vector2(record.anchorMin.x, 0.555f);
                _modeBoard = BoardSlip.Build(screen, "ModeEndBoard", new Vector2(0.53f, 0.285f), new Vector2(0.84f, 0.51f), end.BoardHeaderKey, 2);
            }
            if (end.Notes.Count > 0)
            {
                var slip = ScrollUiKit.Fill(screen, "ModeEndNotes", new Vector2(0.53f, 0.27f), new Vector2(0.84f, 0.435f), ScrollUiKit.Paper);
                var shadow = slip.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shadow.effectDistance = new Vector2(0f, -10f);
                ScrollUiKit.Label(slip.transform, "Text", string.Join("\n", end.Notes), 20, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                    new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), 12);
            }
            BuildEndButtons(screen, "BtnModeEndBack", new Vector2(0.62f, 0.135f), new Vector2(0.84f, 0.195f), end.Again);
        }

        // The numbers of this attempt on a tally paper, the closing line at its foot.
        private static void BuildModeTally(Transform screen, ModeEndSummary end)
        {
            var paper = ScrollUiKit.Fill(screen, "ModeEndTally", new Vector2(0.16f, 0.135f), new Vector2(0.47f, 0.78f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var t = paper.rectTransform;
            ScrollUiKit.Fill(t, "PaperLight", new Vector2(0f, 0.45f), new Vector2(0.7f, 1f), new Color(1f, 1f, 1f, 0.22f), ScrollUiKit.Glow());
            ScrollUiKit.BrocadeBand(t, "BrocadeTop", new Vector2(0f, 0.968f), Vector2.one, true);
            ScrollUiKit.BrocadeBand(t, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.032f), true);
            ScrollUiKit.Label(t, "TallyHeader", T("InRun.End.Mode.Tally").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.10f, 0.875f), new Vector2(0.90f, 0.915f), 11);
            for (var i = 0; i < end.Rows.Count; i++)
            {
                var centre = 0.81f - i * 0.085f;
                ScrollUiKit.ValueRow(t, $"Tally_{i}", new Vector2(0.10f, centre - 0.035f), new Vector2(0.90f, centre + 0.035f),
                    end.Rows[i].Label, end.Rows[i].Value, 24, ScrollUiKit.Ink);
            }
            if (string.IsNullOrEmpty(end.Closing)) return;
            ScrollUiKit.Line(t, "Rule", new Vector2(0.10f, 0.235f), new Vector2(0.90f, 0.235f), new Color(0.70f, 0.20f, 0.15f, 0.5f), 2f);
            var closing = ScrollUiKit.Label(t, "ClosingLine", end.Closing, 20, TextAnchor.UpperLeft, ScrollUiKit.InkSoft,
                new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.215f), 12);
            closing.fontStyle = FontStyle.Italic;
        }

        // The record of the mode, large, with the personal best below; a gold ensō stamp when it is a new best.
        private static void BuildModeRecord(Transform screen, ModeEndSummary end)
        {
            var panel = ScrollUiKit.Fill(screen, "ModeEndRecord", new Vector2(0.53f, 0.47f), new Vector2(0.84f, 0.78f),
                new Color(0.10f, 0.08f, 0.06f, 0.82f), ScrollUiKit.Rounded(14f));
            var rim = panel.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.86f, 0.71f, 0.38f, 0.35f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);
            var t = panel.transform;
            ScrollUiKit.Label(t, "RecordHeader", end.RecordHeader.ToUpperInvariant(), 17, TextAnchor.MiddleLeft,
                new Color(0.86f, 0.82f, 0.72f, 1f), new Vector2(0.07f, 0.80f), new Vector2(0.70f, 0.90f), 11);
            ScrollUiKit.Label(t, "RecordValue", end.RecordValue, 84, TextAnchor.MiddleLeft, ScrollUiKit.Gold,
                new Vector2(0.07f, 0.36f), new Vector2(0.70f, 0.80f), 36);
            ScrollUiKit.Label(t, "RecordLine", end.RecordLine, 21, TextAnchor.UpperLeft, ScrollUiKit.PaperText,
                new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.32f), 12);
            if (!end.NewBest) return;
            var stamp = ScrollUiKit.Spot(t, "NewBestStamp", new Vector2(0.80f, 0.60f), new Vector2(150f, 150f), ScrollUiKit.Gold,
                ScrollUiKit.Enso());
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            ScrollUiKit.Label(stamp.transform, "Text", T("InRun.End.Mode.NewBest"), 24, TextAnchor.MiddleCenter, ScrollUiKit.Gold,
                new Vector2(0.14f, 0.18f), new Vector2(0.86f, 0.82f), 12);
        }

        /// <summary>[REQ: CUSTOM-FINISH-001] A Custom Puzzle solved or lost (0.3.1 playtest): its numbers on the tally, the
        /// grade large with the board and the seed, Back to the menu.</summary>
        private void ShowCustomPuzzleResult(RunDirector run, bool solved)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
            var state = run.State;
            var level = run.CurrentLevelState;
            var config = run.CurrentLevelConfig;
            var mistakes = level?.Mistakes ?? 0;
            var size = config?.BoardSize ?? 0;
            var rules = config?.ActiveModifiers?.Count ?? 0;
            var end = new ModeEndSummary
            {
                Painting = "bg_tutorial_setup",
                Good = solved,
                Eyebrow = F("InRun.End.Custom.Eyebrow", size, new string('\u2605', System.Math.Max(1, config?.Stars ?? 1))),
                Title = solved ? T("InRun.End.Custom.Solved") : T("InRun.End.Custom.Lost"),
                Closing = T("InRun.End.Custom.Closing"),
                RecordHeader = T("InRun.End.Custom.Grade"),
                RecordValue = solved ? T("InRun.End.Grade." + RunScoreBreakdown.GradeFor(mistakes)) : "\u2013",
                RecordLine = F("InRun.End.Custom.Board", size, state.Seed),
                Again = RunAgain.KindOf(state),
            };
            end.Rows.Add((T("InRun.End.Mode.Time"), Clock(state.TotalRunSeconds)));
            end.Rows.Add((T("InRun.End.Mode.Mistakes"), Number(mistakes)));
            end.Rows.Add((T("InRun.End.Mode.PencilMarks"), Number(level?.PencilMarksUsed ?? 0)));
            end.Rows.Add((T("InRun.End.Mode.CellsFilled"), Number(level?.CorrectPlacements ?? 0)));
            if (rules > 0) end.Rows.Add((T("InRun.End.Mode.Modifiers"), Number(rules)));
            ShowModeEndScroll(end);
        }

        private static string Clock(float seconds) => $"{(int)(seconds / 60)}:{(int)(seconds % 60):00}";

        private static string Number(int value) => value.ToString("N0", Core.LocalizationService.Culture);
        private BoardSlip _modeBoard;
    }
}
