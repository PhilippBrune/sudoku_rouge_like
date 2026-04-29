using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialMenuController : MonoBehaviour
    {
        private Dropdown _sizeDropdown;
        private Dropdown _starsDropdown;
        private Dropdown _regionDropdown;
        private Toggle[] _modifierToggles;
        private Button _startButton;

        // Resource mode
        private Toggle _freeModeToggle;     // legacy — kept for backwards compat, may be null
        private Dropdown _resourceModeDrop; // new dropdown: 0=Free, 1=ClassBased
        private Dropdown _classDropdown;
        private RectTransform _classRow;

        private static readonly ClassId[] ClassOrder =
        {
            ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
            ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
            ClassId.ReedDuelist, ClassId.QuietCartographer
        };

        // Tracks which classes are currently listed in the class dropdown (may be filtered to unlocked only)
        private List<ClassId> _availableClasses = new List<ClassId>(new[]
        {
            ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
            ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
            ClassId.ReedDuelist, ClassId.QuietCartographer
        });

        private TutorialSetupConfig _config;

        public void Configure(Dropdown sizeDropdown, Dropdown starsDropdown, Dropdown regionDropdown,
            Button startButton, params Toggle[] modifierToggles)
        {
            _sizeDropdown = sizeDropdown;
            _starsDropdown = starsDropdown;
            _regionDropdown = regionDropdown;
            _startButton = startButton;
            _modifierToggles = modifierToggles;
            _config = new TutorialSetupConfig { BoardSize = 5 }; // match dropdown default (index 0 = 5×5)

            if (_sizeDropdown != null)
                _sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            if (_starsDropdown != null)
                _starsDropdown.onValueChanged.AddListener(OnStarsChanged);
            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.AddListener(OnRegionChanged);

            if (_modifierToggles != null)
                for (var i = 0; i < _modifierToggles.Length; i++)
                    if (_modifierToggles[i] != null)
                        _modifierToggles[i].onValueChanged.AddListener(_ => UpdateStartButtonState());

            UpdateStartButtonState();
        }

        public void ConfigureResourceMode(Toggle freeModeToggle, Dropdown classDropdown, RectTransform classRow)
        {
            // Legacy toggle-based wiring (kept for compatibility)
            _freeModeToggle = freeModeToggle;
            _classDropdown = classDropdown;
            _classRow = classRow;

            if (_freeModeToggle != null)
            {
                _freeModeToggle.onValueChanged.AddListener(OnResourceModeChanged);
                OnResourceModeChanged(_freeModeToggle.isOn);
            }
        }

        public void ConfigureResourceModeDropdown(Dropdown resourceModeDrop, Dropdown classDropdown, RectTransform classRow)
        {
            _resourceModeDrop = resourceModeDrop;
            _classDropdown = classDropdown;
            _classRow = classRow;

            if (_resourceModeDrop != null)
            {
                _resourceModeDrop.onValueChanged.AddListener(OnResourceModeDropdownChanged);
                OnResourceModeDropdownChanged(_resourceModeDrop.value);
            }
        }

        private void OnResourceModeDropdownChanged(int index)
        {
            var isFree = index == 0;
            if (_config != null)
                _config.ResourceMode = isFree ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            if (_classRow != null)
                _classRow.gameObject.SetActive(!isFree);
            UpdateStartButtonState();
        }

        private void OnResourceModeChanged(bool isFree)
        {
            if (_config != null)
                _config.ResourceMode = isFree ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            if (_classRow != null)
                _classRow.gameObject.SetActive(!isFree);
            UpdateStartButtonState();
        }

        public TutorialSetupConfig GetConfig()
        {
            if (_config == null) _config = new TutorialSetupConfig();

            _config.SelectedModifiers.Clear();
            if (_modifierToggles != null)
            {
                var allModifiers = new[]
                {
                    // Original 15
                    BossModifierId.GermanWhispers, BossModifierId.DutchWhispers,
                    BossModifierId.ParityLines, BossModifierId.RenbanLines,
                    BossModifierId.DifferenceKropki, BossModifierId.RatioKropki,
                    BossModifierId.KillerCages, BossModifierId.ArrowSums,
                    BossModifierId.FogOfWar, BossModifierId.Palindrome,
                    BossModifierId.Thermo, BossModifierId.BetweenLines,
                    BossModifierId.EvenOdd, BossModifierId.Nonconsecutive,
                    BossModifierId.Antiknight,
                    // Extended 15
                    BossModifierId.Antiking, BossModifierId.AntiBishop,
                    BossModifierId.NonconsecDiagonal, BossModifierId.DistanceGe2,
                    BossModifierId.EntropyGlobal, BossModifierId.ModularRegions,
                    BossModifierId.ConsecutiveLine, BossModifierId.SlowThermo,
                    BossModifierId.UniqueSetLine, BossModifierId.FullKropki,
                    BossModifierId.SumKropki, BossModifierId.GreaterLessThan,
                    BossModifierId.XVPairs, BossModifierId.PrimeCells,
                    BossModifierId.FortressCells
                };

                for (var i = 0; i < _modifierToggles.Length && i < allModifiers.Length; i++)
                {
                    if (_modifierToggles[i] != null && _modifierToggles[i].isOn)
                        _config.SelectedModifiers.Add(allModifiers[i]);
                }
            }

            // Resource mode
            if (_resourceModeDrop != null)
                _config.ResourceMode = _resourceModeDrop.value == 0 ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            else if (_freeModeToggle != null)
                _config.ResourceMode = _freeModeToggle.isOn ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            if (_classDropdown != null && _config.ResourceMode == TutorialResourceMode.Simulation)
            {
                var idx = _classDropdown.value;
                _config.SimulationClassId = (idx >= 0 && idx < _availableClasses.Count)
                    ? _availableClasses[idx]
                    : ClassId.NumberFreak;
            }

            return _config;
        }

        private void UpdateStartButtonState()
        {
            if (_startButton == null) return;
            // 7-star (index 6) requires at least one modifier to be selected
            var is7Star = _config != null && _config.Stars == 7;
            if (!is7Star) { _startButton.interactable = true; return; }

            var anyModOn = false;
            if (_modifierToggles != null)
                for (var i = 0; i < _modifierToggles.Length; i++)
                    if (_modifierToggles[i] != null && _modifierToggles[i].isOn)
                    { anyModOn = true; break; }

            _startButton.interactable = anyModOn;
        }

        private void OnSizeChanged(int index)
        {
            _config.BoardSize = 5 + index; // 0→5, 1→6, ... 4→9
        }

        private void OnStarsChanged(int index)
        {
            _config.Stars = 1 + index; // 0→1, ... 5→6, 6→7
            UpdateStartButtonState();
        }

        private void OnRegionChanged(int index)
        {
            // 0→Standard(0), 1→Alt(1), 2→Jigsaw(3)
            _config.RegionVariant = index switch
            {
                0 => 0,
                1 => 1,
                2 => 3,
                _ => 0
            };
        }

        // Called from MainMenuController.OpenTutorial() to show only classes the player has unlocked
        public void RefreshClassDropdown(List<ClassId> unlockedClasses)
        {
            if (unlockedClasses == null || unlockedClasses.Count == 0) return;
            _availableClasses = new List<ClassId>(unlockedClasses);
            if (_classDropdown != null)
            {
                _classDropdown.ClearOptions();
                var names = new List<string>();
                for (var i = 0; i < _availableClasses.Count; i++)
                    names.Add(ClassCatalog.GetDefinition(_availableClasses[i]).Name);
                _classDropdown.AddOptions(names);
                if (_classDropdown.template != null)
                    _classDropdown.template.sizeDelta = new Vector2(0, Mathf.Min(_availableClasses.Count * 32f + 8f, 300f));
                _classDropdown.value = 0;
            }
            // MM-3: update the arrow-row label if one is registered
            if (_classArrowLabel != null && _availableClasses.Count > 0)
                _classArrowLabel.text = ClassCatalog.GetDefinition(_availableClasses[0]).Name;
            SetClassIndex(0);
        }

        public int GetBoardSize() => _config?.BoardSize ?? 9;
        public int GetStars() => _config?.Stars ?? 1;

        // ── MM-3: Public wrappers for arrow-row selectors (replaces Dropdown) ──────────────────

        /// <summary>Stores the class-row RectTransform for show/hide by resource mode.</summary>
        public void ConfigureArrowClassRow(RectTransform classRow) => _classRow = classRow;

        /// <summary>Stores the Text label so RefreshClassDropdown can update it.</summary>
        private Text _classArrowLabel;
        public void SetClassArrowLabel(Text lbl) => _classArrowLabel = lbl;

        public void SetBoardSizeIndex(int idx) => OnSizeChanged(idx);
        public void SetStarsIndex(int idx)     => OnStarsChanged(idx);
        public void SetRegionIndex(int idx)    => OnRegionChanged(idx);

        public void SetResourceModeIndex(int idx)
        {
            if (_config == null) _config = new TutorialSetupConfig();
            OnResourceModeDropdownChanged(idx);
        }

        public void SetClassIndex(int idx)
        {
            if (_config == null) _config = new TutorialSetupConfig();
            if (idx >= 0 && idx < _availableClasses.Count)
                _config.SimulationClassId = _availableClasses[idx];
            UpdateStartButtonState();
        }
    }
}
