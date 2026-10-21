using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-019] The chosen class on the hanging scroll: name, level, runs and best floor (META-STATS-001), the
    // XP stroke (for a locked class the way to its unlock), the passive, the level path with every milestone of
    // ClassCatalog plus the exclusive item (L15), relic (L30) and prestige (L40), ticked off up to the class's level,
    // and the prestige stars with their gold and, at Level 40, the prestige button or what it still needs.
    public sealed partial class MetaProgressionPanelController
    {
        private static readonly Color StrokeTrack = new Color(0.24f, 0.18f, 0.12f, 0.16f);
        private static readonly Color StrokeGold = new Color(0.80f, 0.62f, 0.25f, 1f);
        private static readonly Color PassiveSlip = new Color(0.94f, 0.89f, 0.78f, 1f);

        private static readonly string ReachedColor = "#B23A2E";

        private Button _prestigeButton;
        private Text _prestigeLabel;
        private ClassId? _pendingPrestige;

        private void ShowClass(ClassId id)
        {
            if (_scroll == null) return;
            ScrollUiKit.DestroyChildrenNow(_scroll, "ClassPage");
            _prestigeButton = null;
            _prestigeLabel = null;
            var page = ScrollUiKit.Area(_scroll, "ClassPage", new Vector2(0.07f, 0.03f), new Vector2(0.93f, 0.955f));
            var def = ClassCatalog.GetDefinition(id);
            var progress = ClassUnlockService.GetProgress(_meta, id);
            var level = progress.Unlocked ? _progression.GetLevel(_meta, id) : 0;

            ScrollUiKit.Label(page, "ClassName", T(def.Name, def.Name), 44, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0f, 0.905f), new Vector2(1f, 1f), 24);
            ScrollUiKit.Label(page, "ClassSub", progress.Unlocked ? SubLine(id, level) : T("MetaProgression.Locked", "Locked"), 19,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0f, 0.86f), new Vector2(1f, 0.905f), 12);
            BuildStroke(page, id, level, progress);

            var slip = ScrollUiKit.Fill(page, "ClassPassive", new Vector2(0f, 0.615f), new Vector2(1f, 0.75f), PassiveSlip);
            ScrollUiKit.Label(slip.transform, "Caption", T("MetaProgression.Passive", "PASSIVE"), 14, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.95f));
            ScrollUiKit.Label(slip.transform, "Text", T($"Class.Passive.{def.Id}", def.PassiveDescription), 19, TextAnchor.UpperLeft,
                ScrollUiKit.Ink, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.70f), 12);

            BuildPath(page, id, level);
            if (progress.Unlocked) BuildPrestige(page, id, level);
            WireNavigation();
        }

        private string SubLine(ClassId id, int level)
        {
            var line = F("MetaProgression.Sub.Level", "Level {0} of {1}", level, XpTable.MaxLevel);
            var entry = _progression.GetEntry(_meta, id);
            if (entry != null && entry.GardenRuns > 0)
                line += " · " + (entry.GardenRuns == 1
                    ? T("MetaProgression.Sub.OneRun", "1 run")
                    : F("MetaProgression.Sub.Runs", "{0} runs", entry.GardenRuns));
            if (entry != null && entry.BestFloor > 0)
                line += " · " + F("MetaProgression.Sub.BestFloor", "best floor {0}", entry.BestFloor);
            return line;
        }

        // The XP stroke; for a locked class the same stroke shows the way to its unlock.
        private void BuildStroke(RectTransform page, ClassId id, int level, ClassUnlockService.UnlockProgress progress)
        {
            float fraction;
            string left, right;
            if (!progress.Unlocked)
            {
                fraction = progress.Fraction;
                left = GoalText(id, progress);
                right = T("MetaProgression.ToUnlock", "to unlock");
            }
            else if (level >= XpTable.MaxLevel)
            {
                fraction = 1f;
                left = F("MetaProgression.MaxLevel", "Level {0} reached", XpTable.MaxLevel);
                right = string.Empty;
            }
            else
            {
                var xp = _progression.GetTotalXp(_meta, id);
                var into = Mathf.Max(0, xp - XpTable.CumulativeXpForLevel(level));
                var need = Mathf.Max(1, XpTable.XpToNextLevel(level));
                fraction = Mathf.Clamp01((float)into / need);
                left = F("MetaProgression.Xp", "{0:N0} / {1:N0} XP", into, need);
                right = F("MetaProgression.ToLevel", "to Level {0}", level + 1);
            }
            var track = ScrollUiKit.Fill(page, "ClassStroke", new Vector2(0f, 0.815f), new Vector2(1f, 0.845f), StrokeTrack,
                ScrollUiKit.Rounded(12f));
            if (fraction > 0f)
                ScrollUiKit.Fill(track.transform, "Fill", Vector2.zero, new Vector2(Mathf.Clamp01(fraction), 1f), StrokeGold,
                    ScrollUiKit.Rounded(12f));
            ScrollUiKit.Label(page, "ClassStrokeLeft", left, 19, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0f, 0.77f), new Vector2(0.68f, 0.81f), 12);
            ScrollUiKit.Label(page, "ClassStrokeRight", right, 19, TextAnchor.MiddleRight, ScrollUiKit.Ink,
                new Vector2(0.62f, 0.77f), new Vector2(1f, 0.81f), 12);
        }

        // Every milestone of the class, one row each, ticked off up to its level.
        private void BuildPath(RectTransform page, ClassId id, int level)
        {
            var rows = PathRows(id, level);
            var path = ScrollUiKit.Area(page, "ClassPath", new Vector2(0f, 0.15f), new Vector2(1f, 0.595f));
            if (rows.Count == 0) return;
            var step = 1f / rows.Count;
            var line = ScrollUiKit.Fill(path, "Line", new Vector2(0.025f, step * 0.5f), new Vector2(0.025f, 1f - step * 0.5f), ScrollUiKit.Ink);
            line.rectTransform.offsetMin = new Vector2(-1.5f, 0f);
            line.rectTransform.offsetMax = new Vector2(1.5f, 0f);
            for (var i = 0; i < rows.Count; i++)
            {
                var (rowLevel, parts) = rows[i];
                var done = rowLevel <= level;
                var top = 1f - i * step;
                var center = top - step * 0.5f;
                var dot = ScrollUiKit.Spot(path, $"Step_{i}", new Vector2(0.025f, center), new Vector2(20f, 20f),
                    done ? ScrollUiKit.Vermilion : ScrollUiKit.Ink, ScrollUiKit.Disc());
                if (!done)
                    ScrollUiKit.Fill(dot.transform, "Hollow", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), ScrollUiKit.Paper, ScrollUiKit.Disc());
                ScrollUiKit.Label(path, $"StepLevel_{i}", F("MetaProgression.Path.Level", "Lv {0}", rowLevel), 18, TextAnchor.MiddleLeft,
                    ScrollUiKit.Ink, new Vector2(0.06f, top - step), new Vector2(0.205f, top), 11);
                var label = ScrollUiKit.Label(path, $"StepText_{i}", RowText(parts, done), 18, TextAnchor.MiddleLeft,
                    ScrollUiKit.InkSoft, new Vector2(0.21f, top - step), new Vector2(1f, top), 10);
                label.supportRichText = true;
            }
        }

        // A reached row in vermilion, one not reached in soft ink (the label's colour).
        private static string RowText(List<string> parts, bool reached)
        {
            var text = string.Join(" · ", parts);
            return reached ? $"<color={ReachedColor}>{text}</color>" : text;
        }

        private List<(int level, List<string> parts)> PathRows(ClassId id, int level)
        {
            var texts = new SortedDictionary<int, List<string>>();
            void Add(int at, string text)
            {
                if (!texts.TryGetValue(at, out var list)) texts[at] = list = new List<string>();
                list.Add(text);
            }
            foreach (var (at, unlock) in ClassCatalog.GetAllUnlocks(id))
                Add(at, ClassUnlockText.Localize(unlock));
            var unknown = T("MetaProgression.Unknown", "???");
            var item = ItemService.GetExclusiveItemForClass(id);
            if (item.HasValue)
                Add(ItemService.GetExclusiveUnlockLevel(item.Value), F("MetaProgression.Path.ExclusiveItem", "exclusive item {0}",
                    level >= ItemService.GetExclusiveUnlockLevel(item.Value) ? ItemService.GetItemName(item.Value) : unknown));
            var relic = RelicService.GetExclusiveRelicForClass(id);
            if (relic.HasValue)
            {
                var relicLevel = RelicService.GetExclusiveRelicUnlockLevel(relic.Value);
                Add(relicLevel, F("MetaProgression.Path.ExclusiveRelic", "exclusive relic {0}",
                    level >= relicLevel ? RelicService.GetRelicName(relic.Value) : unknown));
            }
            Add(XpTable.MaxLevel, T("MetaProgression.Prestige", "Prestige"));   // its gold is on the prestige line below

            var rows = new List<(int, List<string>)>();
            foreach (var pair in texts) rows.Add((pair.Key, pair.Value));
            return rows;
        }

        private void BuildPrestige(RectTransform page, ClassId id, int level)
        {
            var tier = _progression.GetPrestigeTier(_meta, id);
            var max = ClassGardenProgressionService.MaxPrestigeTier;
            ScrollUiKit.Label(page, "ClassPrestigeLabel", T("MetaProgression.Prestige", "Prestige"), 20, TextAnchor.MiddleLeft,
                ScrollUiKit.Ink, new Vector2(0f, 0.065f), new Vector2(0.22f, 0.125f), 12);
            var stars = ScrollUiKit.Label(page, "ClassPrestigeStars",
                $"<color=#B23A2E>{new string('★', tier)}</color><color=#2A241C40>{new string('★', Mathf.Max(0, max - tier))}</color>",
                26, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion, new Vector2(0.22f, 0.065f), new Vector2(0.68f, 0.125f), 14);
            stars.supportRichText = true;
            ScrollUiKit.Label(page, "ClassPrestigeGold", tier > 0
                    ? F("MetaProgression.Prestige.Gold", "+{0} gold at start", ClassGardenProgressionService.PrestigeStartingGold(tier))
                    : T("MetaProgression.Prestige.PerTier", "+1 starting gold per tier"),
                19, TextAnchor.MiddleRight, ScrollUiKit.Ink, new Vector2(0.62f, 0.065f), new Vector2(1f, 0.125f), 12);

            string status;
            if (level < XpTable.MaxLevel)
                status = F("MetaProgression.Prestige.Opens", "Prestige opens at Level {0}.", XpTable.MaxLevel);
            else if (tier >= max)
                status = T("MetaProgression.Prestige.Max", "Highest prestige reached.");
            else if (!_progression.CanPrestige(_meta, id))
            {
                var required = ClassGardenProgressionService.RequiredBossesForPrestige(tier);
                status = F("MetaProgression.Prestige.Needs", "Prestige needs {0} boss defeats ({1}/{0}).", required,
                    _meta.ClassUnlocks?.BossesDefeated ?? 0);
            }
            else
            {
                status = T("MetaProgression.Prestige.ReadyShort", "Ready: back to Level 1, one more star.");
                BuildPrestigeButton(page, id);
            }
            ScrollUiKit.Label(page, "ClassPrestigeStatus", status, 17, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0f, 0f), new Vector2(_prestigeButton != null ? 0.60f : 1f, 0.06f), 11);
        }

        // [REQ: META-PRESTIGE-004] Two presses: the first asks, the second prestiges (GAP-017). The label changes in place,
        // so a controller keeps its selection between the two presses.
        private void BuildPrestigeButton(RectTransform page, ClassId id)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var button = OptionsUiFactory.BuildButton("BtnPrestige_" + id, page, T("MetaProgression.Prestige.Button", "Prestige"), 18);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = new Vector2(0.62f, -0.005f);
            rt.anchorMax = new Vector2(1f, 0.058f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var label = button.GetComponentInChildren<Text>();
            _prestigeLabel = label;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (_pendingPrestige != id)
                {
                    _pendingPrestige = id;
                    if (label != null) label.text = T("MetaProgression.Prestige.Confirm", "Confirm: reset to Level 1?");
                    return;
                }
                _pendingPrestige = null;
                PrestigeRequested?.Invoke(id);
            });
            _prestigeButton = button;
        }

        private void CancelPendingPrestige()
        {
            if (_pendingPrestige == null) return;
            _pendingPrestige = null;
            if (_prestigeLabel != null) _prestigeLabel.text = T("MetaProgression.Prestige.Button", "Prestige");
        }
    }
}
