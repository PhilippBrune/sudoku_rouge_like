using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialMenuController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private MainMenuController mainMenuController;

        [Header("Setup Widgets")]
        [SerializeField] private Dropdown boardSizeDropdown;
        [SerializeField] private Dropdown starDropdown;
        [SerializeField] private Dropdown resourceModeDropdown;
        [SerializeField] private Dropdown regionLayoutDropdown;
        [SerializeField] private Text validationText;
        [SerializeField] private Text completionHintText;
        [SerializeField] private Text modifierDescriptionText;
        [SerializeField] private Button startButton;

        [Header("Modifier Toggles")]
        [SerializeField] private Toggle fogOfWarToggle;
        [SerializeField] private Toggle arrowSumsToggle;
        [SerializeField] private Toggle germanWhispersToggle;
        [SerializeField] private Toggle dutchWhispersToggle;
        [SerializeField] private Toggle parityLinesToggle;
        [SerializeField] private Toggle renbanLinesToggle;
        [SerializeField] private Toggle killerCagesToggle;
        [SerializeField] private Toggle differenceKropkiToggle;
        [SerializeField] private Toggle ratioKropkiToggle;

        [Header("Progress Widgets")]
        [SerializeField] private Text boardProgressText;
        [SerializeField] private Text modifierProgressText;
        [SerializeField] private Text completionPercentText;

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();
        private readonly List<ClassId> _classDropdownIds = new();

        private TutorialSetupConfig _currentSetup = new();

        private void Awake()
        {
            if (mainMenuController == null)
            {
                mainMenuController = GetComponent<MainMenuController>();
            }

            if (_save.TryLoadProfile(out var profileEnvelope))
            {
                _profile.ApplyEnvelope(profileEnvelope);
            }

            InitializeDropdowns();
            WireEvents();
            RefreshSetupView();
            RefreshProgressView();
        }

        public void Configure(
            MainMenuController controller,
            Dropdown boardDropdown,
            Dropdown starsDropdown,
            Dropdown resourceDropdown,
            Dropdown regionDropdown,
            Text validation,
            Text completionHint,
            Text modifierDescription,
            Button start,
            Toggle fog,
            Toggle arrow,
            Toggle german,
            Toggle dutch,
            Toggle parity,
            Toggle renban,
            Toggle killer,
            Toggle difference,
            Toggle ratio,
            Text boardProgress,
            Text modifierProgress,
            Text completionPercent)
        {
            if (controller != null) mainMenuController = controller;
            if (boardDropdown != null) boardSizeDropdown = boardDropdown;
            if (starsDropdown != null) starDropdown = starsDropdown;
            if (resourceDropdown != null) resourceModeDropdown = resourceDropdown;
            if (regionDropdown != null) regionLayoutDropdown = regionDropdown;
            if (validation != null) validationText = validation;
            if (completionHint != null) completionHintText = completionHint;
            if (modifierDescription != null) modifierDescriptionText = modifierDescription;
            if (start != null) startButton = start;

            if (fog != null) fogOfWarToggle = fog;
            if (arrow != null) arrowSumsToggle = arrow;
            if (german != null) germanWhispersToggle = german;
            if (dutch != null) dutchWhispersToggle = dutch;
            if (parity != null) parityLinesToggle = parity;
            if (renban != null) renbanLinesToggle = renban;
            if (killer != null) killerCagesToggle = killer;
            if (difference != null) differenceKropkiToggle = difference;
            if (ratio != null) ratioKropkiToggle = ratio;

            if (boardProgress != null) boardProgressText = boardProgress;
            if (modifierProgress != null) modifierProgressText = modifierProgress;
            if (completionPercent != null) completionPercentText = completionPercent;

            InitializeDropdowns();
            WireEvents();
            RefreshSetupView();
            RefreshProgressView();
        }

        public void RefreshSetupView()
        {
            SyncSetupFromControls();
            UpdateModifierAvailability();
            UpdateValidationAndDescriptions();
        }

        public void RefreshProgressView()
        {
            var progress = new TutorialProgressService(_profile.TutorialProgress);
            var boardRows = progress.BuildBoardGridProgress();
            var modifierRows = progress.BuildModifierProgress();

            if (boardProgressText != null)
            {
                var builder = new StringBuilder();
                var sizes = TutorialModeService.GetBoardSizes();
                var stars = TutorialModeService.GetStars();

                for (var si = 0; si < sizes.Count; si++)
                {
                    builder.AppendLine($"{sizes[si]}×{sizes[si]}");
                    for (var st = 0; st < stars.Count; st++)
                    {
                        var done = false;
                        for (var i = 0; i < boardRows.Count; i++)
                        {
                            if (boardRows[i].BoardSize == sizes[si] && boardRows[i].Stars == stars[st])
                            {
                                done = boardRows[i].Completed;
                                break;
                            }
                        }

                        builder.AppendLine($"  {stars[st]}★ {(done ? "✔" : "✖")}");
                    }
                }

                boardProgressText.text = builder.ToString().TrimEnd();
            }

            if (modifierProgressText != null)
            {
                var builder = new StringBuilder();
                builder.AppendLine("Modifier Training");
                for (var i = 0; i < modifierRows.Count; i++)
                {
                    builder.AppendLine($"{ToLabel(modifierRows[i].Modifier)} {(modifierRows[i].Completed ? "✔" : "✖")}");
                }

                modifierProgressText.text = builder.ToString().TrimEnd();
            }

            if (completionPercentText != null)
            {
                var percent = Mathf.RoundToInt(progress.GetCompletionPercent() * 100f);
                completionPercentText.text = $"Completion: {percent}%";
            }
        }

        public void StartTutorialFromSetup()
        {
            SyncSetupFromControls();
            var validation = TutorialModeService.ValidateSetup(_currentSetup);
            if (!validation.IsValid)
            {
                if (validationText != null)
                {
                    validationText.text = validation.Message;
                }

                return;
            }

            mainMenuController?.StartTutorialGame(CloneSetup(_currentSetup));
        }

        public void MarkCurrentConfigurationSolvedForPrototype()
        {
            SyncSetupFromControls();
            var progress = new TutorialProgressService(_profile.TutorialProgress);
            progress.MarkCompleted(_currentSetup);
            PersistProfile();
            RefreshProgressView();
            UpdateValidationAndDescriptions();
        }

        private void InitializeDropdowns()
        {
            if (boardSizeDropdown != null)
            {
                boardSizeDropdown.ClearOptions();
                var options = new List<string>();
                var sizes = TutorialModeService.GetBoardSizes();
                for (var i = 0; i < sizes.Count; i++)
                {
                    options.Add($"{sizes[i]}×{sizes[i]}");
                }

                boardSizeDropdown.AddOptions(options);
                boardSizeDropdown.value = 0;
            }

            if (starDropdown != null)
            {
                starDropdown.ClearOptions();
                var options = new List<string>();
                var stars = TutorialModeService.GetStars();
                for (var i = 0; i < stars.Count; i++)
                {
                    options.Add($"{stars[i]}★ ({StarDensityService.MissingPercentLabelForStars(stars[i])}% missing)");
                }

                starDropdown.AddOptions(options);
                starDropdown.value = 0;
            }

            if (resourceModeDropdown != null)
            {
                resourceModeDropdown.ClearOptions();
                var resourceOptions = new List<string> { "Free Mode (∞ HP / ∞ Pencil)" };
                _classDropdownIds.Clear();
                foreach (ClassId cid in Enum.GetValues(typeof(ClassId)))
                {
                    var snap = ClassCatalog.Build(cid);
                    var label = System.Text.RegularExpressions.Regex.Replace(cid.ToString(), "(?<!^)([A-Z])", " $1");
                    resourceOptions.Add($"{label} ({snap.HP} HP / {snap.Pencil} Pencil)");
                    _classDropdownIds.Add(cid);
                }
                resourceModeDropdown.AddOptions(resourceOptions);
                resourceModeDropdown.value = 1;
            }

            if (regionLayoutDropdown != null)
            {
                regionLayoutDropdown.ClearOptions();
                regionLayoutDropdown.AddOptions(new List<string>
                {
                    "Standard",
                    "Rectangular Alt",
                    "Irregular (Jigsaw)"
                });
                regionLayoutDropdown.value = 0;
            }
        }

        private void WireEvents()
        {
            if (boardSizeDropdown != null)
            {
                boardSizeDropdown.onValueChanged.RemoveAllListeners();
                boardSizeDropdown.onValueChanged.AddListener(_ => RefreshSetupView());
            }

            if (starDropdown != null)
            {
                starDropdown.onValueChanged.RemoveAllListeners();
                starDropdown.onValueChanged.AddListener(_ => RefreshSetupView());
            }

            if (resourceModeDropdown != null)
            {
                resourceModeDropdown.onValueChanged.RemoveAllListeners();
                resourceModeDropdown.onValueChanged.AddListener(_ => RefreshSetupView());
            }

            if (regionLayoutDropdown != null)
            {
                regionLayoutDropdown.onValueChanged.RemoveAllListeners();
                regionLayoutDropdown.onValueChanged.AddListener(_ => RefreshSetupView());
            }

            WireToggle(fogOfWarToggle);
            WireToggle(arrowSumsToggle);
            WireToggle(germanWhispersToggle);
            WireToggle(dutchWhispersToggle);
            WireToggle(parityLinesToggle);
            WireToggle(renbanLinesToggle);
            WireToggle(killerCagesToggle);
            WireToggle(differenceKropkiToggle);
            WireToggle(ratioKropkiToggle);

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartTutorialFromSetup);
            }
        }

        private void WireToggle(Toggle toggle)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(_ =>
            {
                EnforceModifierCountLimit(toggle);
                RefreshSetupView();
            });
        }

        private void EnforceModifierCountLimit(Toggle changedToggle)
        {
            var activeCount = CountActiveModifiers();
            if (activeCount <= 2)
            {
                return;
            }

            if (changedToggle != null)
            {
                changedToggle.SetIsOnWithoutNotify(false);
            }

            if (validationText != null)
            {
                validationText.text = "Select up to 2 modifiers.";
            }
        }

        private int CountActiveModifiers()
        {
            var count = 0;
            if (fogOfWarToggle != null && fogOfWarToggle.isOn) count++;
            if (arrowSumsToggle != null && arrowSumsToggle.isOn) count++;
            if (germanWhispersToggle != null && germanWhispersToggle.isOn) count++;
            if (dutchWhispersToggle != null && dutchWhispersToggle.isOn) count++;
            if (parityLinesToggle != null && parityLinesToggle.isOn) count++;
            if (renbanLinesToggle != null && renbanLinesToggle.isOn) count++;
            if (killerCagesToggle != null && killerCagesToggle.isOn) count++;
            if (differenceKropkiToggle != null && differenceKropkiToggle.isOn) count++;
            if (ratioKropkiToggle != null && ratioKropkiToggle.isOn) count++;
            return count;
        }

        private void SyncSetupFromControls()
        {
            var sizes = TutorialModeService.GetBoardSizes();
            var stars = TutorialModeService.GetStars();

            _currentSetup.BoardSize = sizes[Mathf.Clamp(boardSizeDropdown != null ? boardSizeDropdown.value : 0, 0, sizes.Count - 1)];
            _currentSetup.Stars = stars[Mathf.Clamp(starDropdown != null ? starDropdown.value : 0, 0, stars.Count - 1)];
            if (resourceModeDropdown != null && resourceModeDropdown.value == 0)
            {
                _currentSetup.ResourceMode = TutorialResourceMode.Free;
            }
            else
            {
                _currentSetup.ResourceMode = TutorialResourceMode.ClassBased;
                var idx = (resourceModeDropdown != null ? resourceModeDropdown.value : 1) - 1;
                _currentSetup.SimulationClassId = idx >= 0 && idx < _classDropdownIds.Count
                    ? _classDropdownIds[idx]
                    : ClassId.NumberFreak;
            }

            _currentSetup.RegionVariant = regionLayoutDropdown != null
                ? Mathf.Clamp(regionLayoutDropdown.value, 0, 2)
                : 0;

            _currentSetup.SelectedModifiers.Clear();
            TryAddModifier(fogOfWarToggle, BossModifierId.FogOfWar);
            TryAddModifier(arrowSumsToggle, BossModifierId.ArrowSums);
            TryAddModifier(germanWhispersToggle, BossModifierId.GermanWhispers);
            TryAddModifier(dutchWhispersToggle, BossModifierId.DutchWhispers);
            TryAddModifier(parityLinesToggle, BossModifierId.ParityLines);
            TryAddModifier(renbanLinesToggle, BossModifierId.RenbanLines);
            TryAddModifier(killerCagesToggle, BossModifierId.KillerCages);
            TryAddModifier(differenceKropkiToggle, BossModifierId.DifferenceKropki);
            TryAddModifier(ratioKropkiToggle, BossModifierId.RatioKropki);
        }

        private void TryAddModifier(Toggle toggle, BossModifierId modifier)
        {
            if (toggle != null && toggle.isOn)
            {
                _currentSetup.SelectedModifiers.Add(modifier);
            }
        }

        private void UpdateModifierAvailability()
        {
            var boardSize = _currentSetup.BoardSize;
            UpdateToggleAvailability(germanWhispersToggle, BossModifierId.GermanWhispers, boardSize);
            UpdateToggleAvailability(killerCagesToggle, BossModifierId.KillerCages, boardSize);
        }

        private void UpdateToggleAvailability(Toggle toggle, BossModifierId modifier, int boardSize)
        {
            if (toggle == null)
            {
                return;
            }

            var available = TutorialModeService.IsModifierAvailable(modifier, boardSize);
            toggle.interactable = available;
            if (!available)
            {
                toggle.SetIsOnWithoutNotify(false);
            }
        }

        private void UpdateValidationAndDescriptions()
        {
            var validation = TutorialModeService.ValidateSetup(_currentSetup);
            var progress = new TutorialProgressService(_profile.TutorialProgress);

            if (validationText != null)
            {
                validationText.text = validation.IsValid
                    ? (validation.ShowDualModifierWarning ? "Warning: Dual modifiers selected." : "Ready.")
                    : validation.Message;
            }

            if (completionHintText != null)
            {
                completionHintText.text = progress.IsCompleted(_currentSetup)
                    ? "Current configuration: ✔ Completed"
                    : "Current configuration: ✖ Not completed";
            }

            if (modifierDescriptionText != null)
            {
                if (_currentSetup.SelectedModifiers.Count == 0)
                {
                    modifierDescriptionText.text = "None selected.\nDiagram preview: N/A";
                }
                else
                {
                    var first = _currentSetup.SelectedModifiers[0];
                    modifierDescriptionText.text = TutorialModeService.GetModifierDescription(first) + "\nDiagram preview: (placeholder)";
                }
            }

            if (startButton != null)
            {
                startButton.interactable = validation.IsValid;
            }
        }

        private void PersistProfile()
        {
            var envelope = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = _profile.Options },
                MetaProgress = _profile.Meta,
                TutorialProgress = _profile.TutorialProgress,
                Statistics = _profile.Stats,
                Mastery = _profile.Mastery,
                Completion = _profile.Completion
            };

            _save.SaveProfile(envelope);
        }

        private static TutorialSetupConfig CloneSetup(TutorialSetupConfig source)
        {
            var copy = new TutorialSetupConfig
            {
                BoardSize = source.BoardSize,
                Stars = source.Stars,
                ResourceMode = source.ResourceMode
            };

            for (var i = 0; i < source.SelectedModifiers.Count; i++)
            {
                copy.SelectedModifiers.Add(source.SelectedModifiers[i]);
            }

            return copy;
        }

        private static string ToLabel(BossModifierId modifier)
        {
            var raw = modifier.ToString();
            var builder = new StringBuilder(raw.Length + 6);
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (i > 0 && char.IsUpper(c))
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
