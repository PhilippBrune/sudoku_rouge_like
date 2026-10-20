using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// The Custom Puzzle setup: board size, difficulty, regions, resources, class and up to five rules, in the order the
    /// player chose them. The rule book (<see cref="CustomPuzzleScreen"/>) shows it and calls back here.
    /// [REQ: UI-STYLE-012] Any of the 71 player modifiers; a rule that does not fit the board or the other rules
    /// cannot be chosen, and the reason is given (TutorialModeService.CanAddCustomModifier).
    /// </summary>
    public sealed class TutorialMenuController : MonoBehaviour
    {
        private static readonly int[] RegionVariants = { 0, 1, 3 };   // boxes, turned boxes, irregular

        private Button _startButton;
        private RectTransform _classRow;
        private Text _classArrowLabel;
        private TutorialSetupConfig _config = new TutorialSetupConfig { BoardSize = 5, Stars = 2 };

        // Tracks which classes are offered (unlocked ones only)
        private List<ClassId> _availableClasses = new List<ClassId>
        {
            ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
            ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
            ClassId.ReedDuelist, ClassId.QuietCartographer
        };
        private int _classIndex;

        /// <summary>Raised after any change (rules, size, difficulty, regions, resources, class).</summary>
        public event Action SetupChanged;

        public void Configure(Button startButton)
        {
            _startButton = startButton;
            UpdateStartButtonState();
        }

        /// <summary>A copy of the setup, so later menu changes cannot alter a launched puzzle.</summary>
        public TutorialSetupConfig GetConfig()
        {
            DropRulesThatNoLongerFit();
            return new TutorialSetupConfig
            {
                BoardSize = _config.BoardSize,
                Stars = _config.Stars,
                ResourceMode = _config.ResourceMode,
                SimulationClassId = SelectedClass,
                RegionVariant = _config.RegionVariant,
                SelectedModifiers = new List<BossModifierId>(_config.SelectedModifiers)
            };
        }

        public int GetBoardSize() => _config.BoardSize;
        public int GetStars() => _config.Stars;
        public int GetRegionIndex() => Math.Max(0, Array.IndexOf(RegionVariants, _config.RegionVariant));
        public bool IsClassBased => _config.ResourceMode == TutorialResourceMode.Simulation;
        public ClassId SelectedClass => _classIndex < _availableClasses.Count ? _availableClasses[_classIndex] : ClassId.NumberFreak;
        public IReadOnlyList<BossModifierId> SelectedModifiers => _config.SelectedModifiers;

        public bool IsSelected(BossModifierId id) => _config.SelectedModifiers.Contains(id);

        /// <summary>True when the rule is chosen or could be added now; otherwise <paramref name="reason"/> says why not.</summary>
        public bool CanSelect(BossModifierId id, out string reason)
        {
            reason = null;
            return IsSelected(id)
                || TutorialModeService.CanAddCustomModifier(_config.BoardSize, _config.SelectedModifiers, id, out reason);
        }

        /// <summary>Adds the rule, or removes it when it is chosen already. Returns false when it cannot be added.</summary>
        public bool ToggleModifier(BossModifierId id)
        {
            if (IsSelected(id))
                _config.SelectedModifiers.Remove(id);
            else if (TutorialModeService.CanAddCustomModifier(_config.BoardSize, _config.SelectedModifiers, id, out _))
                _config.SelectedModifiers.Add(id);
            else
                return false;
            Changed();
            return true;
        }

        /// <summary>Why Start is off (null when the setup is valid).</summary>
        public string ValidationMessage =>
            TutorialModeService.TryValidateCustomSetup(_config, out var message) ? null : message;

        // Called from MainMenuController.OpenTutorial() to show only classes the player has unlocked
        public void RefreshClassDropdown(List<ClassId> unlockedClasses)
        {
            if (unlockedClasses == null || unlockedClasses.Count == 0) return;
            var current = SelectedClass;
            _availableClasses = new List<ClassId>(unlockedClasses);
            SetClassIndex(Math.Max(0, _availableClasses.IndexOf(current)));   // reopening keeps the chosen class
        }

        /// <summary>The class row is dimmed while resources are free.</summary>
        public void ConfigureArrowClassRow(RectTransform classRow)
        {
            _classRow = classRow;
            RefreshClassRow();
        }

        public void SetClassArrowLabel(Text label)
        {
            _classArrowLabel = label;
            RefreshClassRow();
        }

        public void SetBoardSizeIndex(int idx)
        {
            _config.BoardSize = Mathf.Clamp(5 + idx, 5, 9);   // 0→5 … 4→9
            DropRulesThatNoLongerFit();
            Changed();
        }

        public void SetStarsIndex(int idx)
        {
            _config.Stars = Mathf.Clamp(1 + idx, 1, 7);   // 0→1 … 6→7
            Changed();
        }

        public void SetRegionIndex(int idx)
        {
            _config.RegionVariant = RegionVariants[Mathf.Clamp(idx, 0, RegionVariants.Length - 1)];
            Changed();
        }

        public void SetResourceModeIndex(int idx)
        {
            _config.ResourceMode = idx == 0 ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            Changed();
        }

        /// <summary>Steps through the unlocked classes (wraps).</summary>
        public void StepClass(int delta)
        {
            if (_availableClasses.Count == 0) return;
            SetClassIndex((_classIndex + delta + _availableClasses.Count) % _availableClasses.Count);
        }

        public void SetClassIndex(int idx)
        {
            if (idx < 0 || idx >= _availableClasses.Count) return;
            _classIndex = idx;
            _config.SimulationClassId = _availableClasses[idx];
            Changed();
        }

        // A smaller board can rule out chosen rules; keep them in the player's order while they still fit.
        private void DropRulesThatNoLongerFit()
        {
            var kept = new List<BossModifierId>();
            foreach (var id in _config.SelectedModifiers)
                if (TutorialModeService.CanAddCustomModifier(_config.BoardSize, kept, id, out _))
                    kept.Add(id);
            _config.SelectedModifiers.Clear();
            _config.SelectedModifiers.AddRange(kept);
        }

        private void Changed()
        {
            RefreshClassRow();
            UpdateStartButtonState();
            SetupChanged?.Invoke();
        }

        private void RefreshClassRow()
        {
            if (_classArrowLabel != null)
                _classArrowLabel.text = LocalizationService.T(ClassCatalog.GetDefinition(SelectedClass).Name,
                    ClassCatalog.GetDefinition(SelectedClass).Name);
            if (_classRow == null) return;
            var group = _classRow.GetComponent<CanvasGroup>();   // Unity null, not ??
            if (group == null) group = _classRow.gameObject.AddComponent<CanvasGroup>();
            group.alpha = IsClassBased ? 1f : 0.45f;
            group.interactable = IsClassBased;
        }

        private void UpdateStartButtonState()
        {
            if (_startButton != null)
                _startButton.interactable = TutorialModeService.TryValidateCustomSetup(_config, out _);
        }
    }
}
