using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        // ── Panel References ──
        private GameObject _mainMenuPanel;
        private GameObject _classSelectPanel;
        private GameObject _optionsPanel;
        private GameObject _creditsPanel;
        private GameObject _tutorialSetupPanel;
        private GameObject _metaProgressionPanel;
        private GameObject _gameModesPanel;
        private GameObject _itemsPanel;

        // ── Controllers ──
        private OptionsController _optionsController;
        private TutorialMenuController _tutorialMenuController;
        private MetaProgressionPanelController _metaProgressionController;
        private GameModesPanelController _gameModesController;
        private ItemsMenuController _itemsMenuController;
        private GameBootstrap _gameBootstrap;

        // ── State ──
        private ClassId _selectedClass = ClassId.NumberFreak;
        private bool _allowIrregularPuzzles = true;
        private bool _debugEnableAllFeatures;

        // ── Services ──
        private MenuFlowService _menu;
        private SaveFileService _save;
        private ProfileService _profile;
        private ClassUnlockService _classUnlockService;
        private ICloudSaveProvider _cloud;
        private SaveConflictService _conflicts;
        private InputRemapService _inputRemap;

        // ── UI Widgets ──
        private Text _statusText;
        private Button _resumeButton;
        private Text _classInfoText;
        private readonly Dictionary<ClassId, Button> _classButtons = new Dictionary<ClassId, Button>();
        private ClassId? _highlightedClassId;

        private void Awake()
        {
            _inputRemap = new InputRemapService();
            _menu = new MenuFlowService();
            _save = new SaveFileService();
            _profile = new ProfileService(_save);
            _classUnlockService = new ClassUnlockService();
            _cloud = new LocalCloudSaveProvider();
            _conflicts = new SaveConflictService(_save, _cloud);
        }

        private void Start()
        {
            ShowMainMenu();
        }

        // ── Configuration (called by BlueprintBuilder) ──

        public void ConfigureUi(
            GameBootstrap bootstrap,
            GameObject mainMenuPanel, GameObject classSelectPanel,
            GameObject optionsPanel, GameObject creditsPanel,
            GameObject tutorialSetupPanel, GameObject metaProgressionPanel,
            GameObject gameModesPanel, GameObject itemsPanel,
            OptionsController optionsCtrl, TutorialMenuController tutorialCtrl,
            MetaProgressionPanelController metaCtrl, GameModesPanelController modesCtrl,
            ItemsMenuController itemsCtrl, Text statusText, Button resumeButton)
        {
            _gameBootstrap = bootstrap;
            _mainMenuPanel = mainMenuPanel;
            _classSelectPanel = classSelectPanel;
            _optionsPanel = optionsPanel;
            _creditsPanel = creditsPanel;
            _tutorialSetupPanel = tutorialSetupPanel;
            _metaProgressionPanel = metaProgressionPanel;
            _gameModesPanel = gameModesPanel;
            _itemsPanel = itemsPanel;
            _optionsController = optionsCtrl;
            _tutorialMenuController = tutorialCtrl;
            _metaProgressionController = metaCtrl;
            _gameModesController = modesCtrl;
            _itemsMenuController = itemsCtrl;
            _statusText = statusText;
            _resumeButton = resumeButton;

            // Register panels with flow service
            _menu.RegisterPanel(MenuScreen.MainMenu, mainMenuPanel);
            _menu.RegisterPanel(MenuScreen.ClassSelect, classSelectPanel);
            _menu.RegisterPanel(MenuScreen.Options, optionsPanel);
            _menu.RegisterPanel(MenuScreen.Credits, creditsPanel);
            _menu.RegisterPanel(MenuScreen.TutorialSetup, tutorialSetupPanel);
            _menu.RegisterPanel(MenuScreen.MetaProgression, metaProgressionPanel);
            _menu.RegisterPanel(MenuScreen.GameModes, gameModesPanel);
            _menu.RegisterPanel(MenuScreen.Items, itemsPanel);

            SetupResumeButton();
        }

        // ── Navigation ──

        public void ShowMainMenu()
        {
            _menu.ShowMainMenu();
            SetupResumeButton();
        }

        public void StartGame()
        {
            RefreshClassLockStates();
            _menu.Show(MenuScreen.ClassSelect);
        }
        public void OpenTutorial()
        {
            var meta = _profile.LoadMetaProgress();
            var unlocked = new List<ClassId>();
            var allClasses = new[]
            {
                ClassId.NumberFreak, ClassId.GardenMonk, ClassId.ShrineArchivist,
                ClassId.KoiGambler, ClassId.StoneGardener, ClassId.LanternSeer,
                ClassId.ReedDuelist, ClassId.QuietCartographer
            };
            foreach (var cls in allClasses)
                if (IsClassUnlockedOrDebug(cls, meta)) unlocked.Add(cls);
            if (unlocked.Count == 0) unlocked.Add(ClassId.NumberFreak);
            _tutorialMenuController?.RefreshClassDropdown(unlocked);
            _menu.Show(MenuScreen.TutorialSetup);
        }
        public void OpenOptions() => _menu.Show(MenuScreen.Options);
        public void OpenCredits() => _menu.Show(MenuScreen.Credits);
        public void OpenItems()
        {
            var meta = _profile.LoadMetaProgress();
            _itemsMenuController?.Refresh(meta);
            _menu.Show(MenuScreen.Items);
        }

        public void OpenMetaProgression()
        {
            var meta = _profile.LoadMetaProgress();
            _metaProgressionController?.Refresh(meta);
            _menu.Show(MenuScreen.MetaProgression);
        }

        public void OpenGameModes() => _menu.Show(MenuScreen.GameModes);

        public void BackToMainMenu() => _menu.ShowMainMenu();
        public void BackFromPanel() => _menu.Back();

        // ── Resume ──

        public void ResumeGame()
        {
            if (_gameBootstrap == null) return;

            if (_gameBootstrap.HasResumableRun())
            {
                _gameBootstrap.LaunchResume();
            }
            else
            {
                SetStatus(LocalizationService.T("No run to resume."));
            }
        }

        private void SetupResumeButton()
        {
            if (_resumeButton != null)
                _resumeButton.interactable = _gameBootstrap != null && _gameBootstrap.HasResumableRun();
        }

        // ── Class Select ──

        public void RegisterClassButton(ClassId classId, Button button)
        {
            _classButtons[classId] = button;
        }

        public void RefreshClassLockStates()
        {
            var meta = _profile.LoadMetaProgress();
            foreach (var kvp in _classButtons)
            {
                var locked = !IsClassUnlockedOrDebug(kvp.Key, meta);
                var img = kvp.Value.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                    img.color = locked ? new Color(0.40f, 0.37f, 0.32f, 0.80f) : new Color(0.82f, 0.75f, 0.62f, 0.92f);
                var lbl = kvp.Value.transform.Find("Label")?.GetComponent<Text>();
                if (lbl != null)
                    lbl.color = locked ? new Color(0.65f, 0.60f, 0.50f, 1f) : new Color(0.12f, 0.10f, 0.08f, 1f);
            }
        }

        public void SetClassInfoText(Text infoText)
        {
            _classInfoText = infoText;
        }

        public void SetSelectedClass(ClassId classId)
        {
            _selectedClass = classId;
            _highlightedClassId = classId;

            // Update button outlines
            var goldColor = new Color(1f, 0.84f, 0f, 1f);
            var defaultColor = new Color(0f, 0f, 0f, 0f);

            foreach (var kvp in _classButtons)
            {
                var outline = kvp.Value.GetComponent<Outline>();
                if (outline == null)
                    outline = kvp.Value.gameObject.AddComponent<Outline>();

                if (kvp.Key == classId)
                {
                    outline.effectColor = goldColor;
                    outline.effectDistance = new Vector2(2, -2);
                    outline.enabled = true;
                }
                else
                {
                    outline.effectColor = defaultColor;
                    outline.enabled = false;
                }
            }

            // Update class info text
            if (_classInfoText != null)
            {
                var def = ClassCatalog.GetDefinition(classId);
                var meta = _profile.LoadMetaProgress();
                var isUnlocked = IsClassUnlockedOrDebug(classId, meta);
                var allUnlocks = ClassCatalog.GetAllUnlocks(classId);

                _classInfoText.supportRichText = true;
                if (isUnlocked)
                {
                    // Derive level and XP progress
                    var garden = meta?.GardenProgression;
                    var totalXp = 0;
                    if (garden != null)
                        for (var i = 0; i < garden.ClassEntries.Count; i++)
                            if (garden.ClassEntries[i].ClassId == classId) { totalXp = garden.ClassEntries[i].TotalXp; break; }

                    var level = XpTable.DeriveLevel(totalXp);
                    var xpIntoLevel = totalXp - XpTable.CumulativeXpForLevel(level);
                    var xpToNext = level < 40 ? XpTable.XpToNextLevel(level) : 0;

                    _classInfoText.color = new Color(0.96f, 0.93f, 0.82f, 1f);
                    var sb = new StringBuilder();
                    sb.Append($"<b>{LocalizationService.T(def.Name)}</b>  Lv{level}{(level >= 40 ? " MAX" : "")}");
                    sb.AppendLine(level < 40 ? $"   XP: {xpIntoLevel}/{xpToNext}" : $"   XP: MAX");
                    sb.AppendLine($"HP:{def.BaseHP}  Pencil:{def.BasePencil}  Slots:{def.BaseItemSlots}  |  {def.PassiveDescription}");
                    // Compact unlock table: 3 per row, highlight achieved (gold) vs upcoming (dim)
                    sb.AppendLine("─ Level Unlocks ─");
                    var itemsOnRow = 0;
                    for (var ui = 0; ui < allUnlocks.Length; ui++)
                    {
                        var (ulvl, udesc) = allUnlocks[ui];
                        var achieved = ulvl <= level;
                        if (achieved)
                            sb.Append($"<color=#C8A44A>[L{ulvl}✓]</color> {udesc}");
                        else
                            sb.Append($"<color=#888888>[L{ulvl}]</color> {udesc}");
                        itemsOnRow++;
                        if (itemsOnRow == 2 || ui == allUnlocks.Length - 1) { sb.AppendLine(); itemsOnRow = 0; }
                        else sb.Append("  ");
                    }
                    _classInfoText.text = sb.ToString().TrimEnd();
                }
                else
                {
                    _classInfoText.color = new Color(0.80f, 0.55f, 0.35f, 1f);
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>{LocalizationService.T(def.Name)}  —  LOCKED</b>");
                    sb.Append($"Unlock: {def.UnlockCondition}");
                    var progress = GetUnlockProgress(classId, meta);
                    if (!string.IsNullOrEmpty(progress)) sb.AppendLine($"   ({progress})");
                    else sb.AppendLine();
                    sb.AppendLine($"HP:{def.BaseHP}  Pencil:{def.BasePencil}  Slots:{def.BaseItemSlots}  |  {def.PassiveDescription}");
                    sb.AppendLine("─ Level Unlocks (preview) ─");
                    var itemsOnRow = 0;
                    for (var ui = 0; ui < allUnlocks.Length; ui++)
                    {
                        var (ulvl, udesc) = allUnlocks[ui];
                        sb.Append($"<color=#888888>[L{ulvl}]</color> {udesc}");
                        itemsOnRow++;
                        if (itemsOnRow == 2 || ui == allUnlocks.Length - 1) { sb.AppendLine(); itemsOnRow = 0; }
                        else sb.Append("  ");
                    }
                    _classInfoText.text = sb.ToString().TrimEnd();
                }
            }
        }

        public void ConfirmClassAndStart()
        {
            if (_gameBootstrap == null) return;

            var meta = _profile.LoadMetaProgress();
            if (!IsClassUnlockedOrDebug(_selectedClass, meta))
            {
                SetStatus(LocalizationService.T("Class not yet unlocked."));
                return;
            }

            var request = new LaunchRequest
            {
                ClassId = _selectedClass,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = _allowIrregularPuzzles
            };

            _gameBootstrap.LaunchRun(request);
        }

        public void SetAllowIrregularPuzzles(bool allow)
        {
            _allowIrregularPuzzles = allow;
        }

        // ── Tutorial ──

        public void StartTutorialGame()
        {
            if (_gameBootstrap == null || _tutorialMenuController == null) return;

            var setup = _tutorialMenuController.GetConfig();
            _gameBootstrap.LaunchTutorial(setup);
        }

        // ── Game Modes ──

        public void StartEndlessZen()
        {
            if (_gameBootstrap == null) return;

            var request = new LaunchRequest
            {
                Mode = GameMode.EndlessZen,
                ClassId = _selectedClass
            };
            _gameBootstrap.LaunchRun(request);
        }

        public void StartSpiritTrials()
        {
            if (_gameBootstrap == null) return;

            var request = new LaunchRequest
            {
                Mode = GameMode.SpiritTrials,
                ClassId = _selectedClass
            };
            _gameBootstrap.LaunchRun(request);
        }

        // ── Debug ──

        public void OnDebugEnableAllChanged(bool isOn)
        {
            _debugEnableAllFeatures = isOn;
            SetStatus(isOn ? "Debug: All features enabled." : "Debug mode off.");
        }

        // ── Quit ──

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Status ──

        public void SetStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
        }

        // ── Language ──

        public void RefreshLanguage()
        {
            var lang = _profile.LoadOptions().Language;
            // Rebuild menu to apply new language
            var builder = GetComponent<MainMenuBlueprintBuilder>();
            if (builder != null)
            {
                // Force rebuild with new language
                builder.ForceRebuild();
            }
        }

        // ── Helpers ──

        private bool IsClassUnlockedOrDebug(ClassId classId, MetaProgressionState meta)
        {
            if (_debugEnableAllFeatures) return true;
            if (meta?.UnlockedClasses == null) return classId == ClassId.NumberFreak;
            return meta.UnlockedClasses.Contains(classId);
        }

        /// <summary>Returns a "X/Y" progress string for the class's unlock condition, or null if N/A.</summary>
        private static string GetUnlockProgress(ClassId classId, MetaProgressionState meta)
        {
            var p = meta?.ClassUnlocks;
            if (p == null) return null;
            return classId switch
            {
                ClassId.GardenMonk        => $"Bosses defeated: {p.BossesDefeated}/2",
                ClassId.ShrineArchivist   => $"Items used: {p.ItemsUsed}/15",
                ClassId.KoiGambler        => $"Relics collected: {p.RelicsCollected}/10",
                ClassId.StoneGardener     => $"Bosses defeated: {p.BossesDefeated}/10",
                ClassId.LanternSeer       => $"Gold collected: {p.GoldCollected:N0}/50,000",
                ClassId.ReedDuelist       => p.ItemCodexComplete ? "Item codex: complete!" : "Item codex: not yet complete",
                ClassId.QuietCartographer => p.PerfectNoPencilStage ? "Perfect run: achieved!" : "Perfect run: not yet achieved",
                _ => null
            };
        }
    }
}
