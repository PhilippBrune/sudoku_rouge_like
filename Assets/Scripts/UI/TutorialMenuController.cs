using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialMenuController : MonoBehaviour
    {
        private static readonly BossModifierId[] CustomModifiers =
        {
            BossModifierId.GermanWhispers, BossModifierId.DutchWhispers,
            BossModifierId.ParityLines, BossModifierId.RenbanLines,
            BossModifierId.DifferenceKropki, BossModifierId.RatioKropki,
            BossModifierId.KillerCages, BossModifierId.ArrowSums,
            BossModifierId.FogOfWar, BossModifierId.Palindrome,
            BossModifierId.Thermo, BossModifierId.BetweenLines,
            BossModifierId.EvenOdd, BossModifierId.Nonconsecutive,
            BossModifierId.Antiknight, BossModifierId.Antiking,
            BossModifierId.AntiBishop, BossModifierId.NonconsecDiagonal,
            BossModifierId.DistanceGe2, BossModifierId.EntropyGlobal,
            BossModifierId.ModularRegions, BossModifierId.ConsecutiveLine,
            BossModifierId.SlowThermo, BossModifierId.UniqueSetLine,
            BossModifierId.FullKropki, BossModifierId.SumKropki,
            BossModifierId.GreaterLessThan, BossModifierId.XVPairs,
            BossModifierId.PrimeCells, BossModifierId.FortressCells
        };

        private Dropdown _sizeDropdown;
        private Dropdown _starsDropdown;
        private Dropdown _regionDropdown;
        private Toggle[] _modifierToggles;
        private Button _startButton;
        private Text _selectionSummaryText;
        private readonly Dictionary<Toggle, Color> _modifierLabelColors = new Dictionary<Toggle, Color>();

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
            _config = new TutorialSetupConfig { BoardSize = 5, Stars = 2 };

            if (_sizeDropdown != null)
                _sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
            if (_starsDropdown != null)
                _starsDropdown.onValueChanged.AddListener(OnStarsChanged);
            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.AddListener(OnRegionChanged);

            if (_modifierToggles != null)
                for (var i = 0; i < _modifierToggles.Length; i++)
                    if (_modifierToggles[i] != null)
                    {
                        var label = _modifierToggles[i].transform.Find("Label")?.GetComponent<Text>();
                        if (label != null)
                            _modifierLabelColors[_modifierToggles[i]] = label.color;
                        _modifierToggles[i].onValueChanged.AddListener(_ => OnModifierSelectionChanged());
                    }

            RefreshModifierAvailability();
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
            RefreshSelectionSummary();
            UpdateStartButtonState();
        }

        private void OnResourceModeChanged(bool isFree)
        {
            if (_config != null)
                _config.ResourceMode = isFree ? TutorialResourceMode.Free : TutorialResourceMode.Simulation;
            if (_classRow != null)
                _classRow.gameObject.SetActive(!isFree);
            RefreshSelectionSummary();
            UpdateStartButtonState();
        }

        public TutorialSetupConfig GetConfig()
        {
            if (_config == null) _config = new TutorialSetupConfig();

            RefreshModifierAvailability();
            SyncSelectedModifiersToConfig();

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

        private void OnModifierSelectionChanged()
        {
            RefreshModifierAvailability();
            UpdateStartButtonState();
        }

        private void RefreshModifierAvailability()
        {
            if (_config == null || _modifierToggles == null) return;

            var accepted = new List<BossModifierId>();
            var count = Mathf.Min(_modifierToggles.Length, CustomModifiers.Length);
            for (var i = 0; i < count; i++)
            {
                var toggle = _modifierToggles[i];
                if (toggle == null || !toggle.isOn) continue;

                if (TutorialModeService.CanAddCustomModifier(
                    _config.BoardSize, accepted, CustomModifiers[i], out _))
                {
                    accepted.Add(CustomModifiers[i]);
                }
                else
                {
                    toggle.SetIsOnWithoutNotify(false);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var toggle = _modifierToggles[i];
                if (toggle == null) continue;

                var modifier = CustomModifiers[i];
                var canSelect = accepted.Contains(modifier)
                    || TutorialModeService.CanAddCustomModifier(
                        _config.BoardSize, accepted, modifier, out _);
                toggle.interactable = canSelect;

                var label = toggle.transform.Find("Label")?.GetComponent<Text>();
                if (label == null) continue;
                if (!_modifierLabelColors.TryGetValue(toggle, out var normalColor))
                    normalColor = label.color;
                label.color = canSelect
                    ? normalColor
                    : new Color(0.42f, 0.42f, 0.42f, 0.78f);
            }

            SyncSelectedModifiersToConfig();
            RefreshSelectionSummary();
        }

        private void SyncSelectedModifiersToConfig()
        {
            if (_config == null) return;
            _config.SelectedModifiers.Clear();
            if (_modifierToggles == null) return;

            var count = Mathf.Min(_modifierToggles.Length, CustomModifiers.Length);
            for (var i = 0; i < count; i++)
            {
                if (_modifierToggles[i] != null && _modifierToggles[i].isOn)
                    _config.SelectedModifiers.Add(CustomModifiers[i]);
            }
        }

        private void UpdateStartButtonState()
        {
            if (_startButton == null) return;
            SyncSelectedModifiersToConfig();
            _startButton.interactable = TutorialModeService.TryValidateCustomSetup(
                _config, out _);
        }

        private void OnSizeChanged(int index)
        {
            _config.BoardSize = 5 + index; // 0→5, 1→6, ... 4→9
            RefreshModifierAvailability();
            UpdateStartButtonState();
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
            RefreshModifierAvailability();
            UpdateStartButtonState();
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

        public void SetSelectionSummaryText(Text text)
        {
            _selectionSummaryText = text;
            RefreshSelectionSummary();
        }

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
            RefreshSelectionSummary();
            UpdateStartButtonState();
        }

        private void RefreshSelectionSummary()
        {
            if (_selectionSummaryText == null || _config == null) return;

            var region = _config.RegionVariant switch
            {
                1 => "Alt Rectangular",
                >= 2 => "Jigsaw",
                _ => "Standard"
            };
            var resource = _config.ResourceMode == TutorialResourceMode.Simulation
                ? "Class-Based"
                : "Free";

            var mods = "None";
            if (_config.SelectedModifiers != null && _config.SelectedModifiers.Count > 0)
            {
                var names = new List<string>(_config.SelectedModifiers.Count);
                for (var i = 0; i < _config.SelectedModifiers.Count; i++)
                    names.Add(BossService.GetModifierName(_config.SelectedModifiers[i]));
                mods = string.Join(", ", names);
            }

            _selectionSummaryText.text =
                $"Size: {_config.BoardSize}x{_config.BoardSize}   Difficulty: {_config.Stars} star(s)\n" +
                $"Region: {region}   Resources: {resource}\n" +
                $"Modes ({_config.SelectedModifiers.Count}/{TutorialModeService.CustomModifierLimit}): {mods}";
        }

        // ── Gamepad D-pad navigation ─────────────────────────────────────────────────

        private List<Selectable> _gpSelectables;
        private int _gpIndex;
        private float _gpPrevV;
        private const float GpAxisThreshold = 0.5f;

        private void OnEnable()
        {
            _gpSelectables = null;
            _gpIndex = 0;
            _gpPrevV = 0f;
        }

        private void Update()
        {
            if (!HasAnyGamepadInput()) return;
            BuildSelectablesIfNeeded();
            if (_gpSelectables == null || _gpSelectables.Count == 0) return;

            var v = Input.GetAxis("Vertical");
            var moveUp   = (Input.GetKeyDown(KeyCode.JoystickButton12) ||
                            (v >  GpAxisThreshold && _gpPrevV <= GpAxisThreshold));
            var moveDown = (Input.GetKeyDown(KeyCode.JoystickButton13) ||
                            (v < -GpAxisThreshold && _gpPrevV >= -GpAxisThreshold));
            _gpPrevV = v;

            if (moveDown) GpNavigateTo((_gpIndex + 1) % _gpSelectables.Count);
            if (moveUp)   GpNavigateTo((_gpIndex - 1 + _gpSelectables.Count) % _gpSelectables.Count);

            if (Input.GetKeyDown(KeyCode.JoystickButton0))
                GpConfirm(_gpSelectables[_gpIndex]);
        }

        private void GpNavigateTo(int index)
        {
            _gpIndex = index;
            var sel = _gpSelectables[_gpIndex];
            if (sel != null)
                EventSystem.current?.SetSelectedGameObject(sel.gameObject);
        }

        private static void GpConfirm(Selectable sel)
        {
            if (sel == null) return;
            if (sel is Button btn)         { btn.onClick.Invoke(); return; }
            if (sel is Toggle tgl)         { tgl.isOn = !tgl.isOn; return; }
            if (sel is Dropdown dd)        { dd.Show(); return; }
        }

        private void BuildSelectablesIfNeeded()
        {
            if (_gpSelectables != null) return;
            _gpSelectables = new List<Selectable>();
            AddGp(_sizeDropdown);
            AddGp(_starsDropdown);
            AddGp(_regionDropdown);
            AddGp(_resourceModeDrop);
            AddGp(_classDropdown);
            if (_modifierToggles != null)
                foreach (var t in _modifierToggles) AddGp(t);
            AddGp(_startButton);
        }

        private void AddGp(Selectable s)
        {
            if (s != null) _gpSelectables.Add(s);
        }

        private static bool HasAnyGamepadInput()
        {
            if (!Input.anyKeyDown && Mathf.Abs(Input.GetAxis("Vertical")) < 0.1f) return false;
            for (var k = 0; k < 20; k++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + k)) return true;
            return Mathf.Abs(Input.GetAxis("Vertical")) >= GpAxisThreshold;
        }
    }
}
