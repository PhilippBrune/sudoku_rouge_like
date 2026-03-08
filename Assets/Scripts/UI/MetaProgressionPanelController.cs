using System.Text;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class MetaProgressionPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Text metaSummaryText;
        [SerializeField] private Text classProgressText;
        [SerializeField] private Text selectedClassText;

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();
        private readonly ClassSelectService _classSelect = new();
        private static readonly (ClassId ClassId, string ButtonName)[] ClassButtons =
        {
            (ClassId.NumberFreak, "BtnClassNumberFreak"),
            (ClassId.GardenMonk, "BtnClassGardenMonk"),
            (ClassId.ShrineArchivist, "BtnClassShrineArchivist"),
            (ClassId.KoiGambler, "BtnClassKoiGambler"),
            (ClassId.StoneGardener, "BtnClassStoneGardener"),
            (ClassId.LanternSeer, "BtnClassLanternSeer")
        };

        private void Awake()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }
        }

        public void Configure(MainMenuController controller, Text summary, Text classProgress, Text selectedClass)
        {
            mainMenuController = controller;
            metaSummaryText = summary;
            classProgressText = classProgress;
            selectedClassText = selectedClass;
            RefreshView();
        }

        public void RefreshView()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            if (metaSummaryText != null)
            {
                metaSummaryText.text =
                    $"Essence: {_profile.Meta.GardenEssence}\n" +
                    $"Runs: {_profile.Stats.TotalRuns}\n" +
                    $"Boss Clears: {_profile.Stats.BossClears}\n" +
                    $"Achievements: {_profile.Stats.TotalAchievementsUnlocked}";
            }

            if (classProgressText != null)
            {
                var cards = _classSelect.BuildCards(_profile.Meta);
                var selectedId = mainMenuController != null ? mainMenuController.SelectedClass : ClassId.NumberFreak;
                var builder = new StringBuilder();
                for (var i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    if (card.ClassId != selectedId)
                    {
                        continue;
                    }

                    builder.AppendLine(card.Name);
                    builder.AppendLine($"  HP {card.HP} | Pencil {card.Pencil} | Slots {card.ItemSlots}");
                    builder.AppendLine($"  Passive: {card.PassiveDisplay}");
                    break;
                }

                classProgressText.text = builder.Length > 0
                    ? builder.ToString().TrimEnd()
                    : "No class selected.";
            }

            for (var i = 0; i < ClassButtons.Length; i++)
            {
                var row = ClassButtons[i];
                var visible = _profile.IsClassUnlocked(row.ClassId) || (mainMenuController != null && mainMenuController.DebugEnableAllFeatures);
                SetClassButtonVisible(row.ButtonName, visible);
            }

            RelayoutVisibleClassButtons();

            if (selectedClassText != null && mainMenuController != null)
            {
                selectedClassText.text = $"Selected Class: {mainMenuController.SelectedClass}";
            }
        }

        public void SelectClassNumberFreak() => TrySelectClass(ClassId.NumberFreak);
        public void SelectClassGardenMonk() => TrySelectClass(ClassId.GardenMonk);
        public void SelectClassShrineArchivist() => TrySelectClass(ClassId.ShrineArchivist);
        public void SelectClassKoiGambler() => TrySelectClass(ClassId.KoiGambler);
        public void SelectClassStoneGardener() => TrySelectClass(ClassId.StoneGardener);
        public void SelectClassLanternSeer() => TrySelectClass(ClassId.LanternSeer);

        public void UnlockDemoContent()
        {
            UnlockClass(ClassId.GardenMonk);
            UnlockClass(ClassId.ShrineArchivist);
            UnlockClass(ClassId.KoiGambler);
            UnlockClass(ClassId.StoneGardener);
            UnlockClass(ClassId.LanternSeer);

            _profile.Meta.EndlessZenUnlocked = true;
            _profile.Meta.SpiritTrialsUnlocked = true;

            SaveProfile();
            RefreshView();
        }

        private void TrySelectClass(ClassId classId)
        {
            var debugAll = mainMenuController != null && mainMenuController.DebugEnableAllFeatures;
            if (!_profile.IsClassUnlocked(classId) && !debugAll)
            {
                mainMenuController?.SetStatusExternal($"{classId} is still locked.");
                return;
            }

            mainMenuController?.SetSelectedClass(classId);
            RefreshView();
        }

        private void UnlockClass(ClassId classId)
        {
            if (!_profile.Meta.UnlockedClasses.Contains(classId))
            {
                _profile.Meta.UnlockedClasses.Add(classId);
            }
        }

        private void SaveProfile()
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

        private void SetClassButtonVisible(string buttonName, bool visible)
        {
            var button = FindSceneObject(buttonName);
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
        }

        private void RelayoutVisibleClassButtons()
        {
            var visibleButtons = new System.Collections.Generic.List<RectTransform>();
            for (var i = 0; i < ClassButtons.Length; i++)
            {
                var row = ClassButtons[i];
                var isVisible = _profile.IsClassUnlocked(row.ClassId) || (mainMenuController != null && mainMenuController.DebugEnableAllFeatures);
                if (!isVisible)
                {
                    continue;
                }

                var button = FindSceneObject(row.ButtonName);
                if (button == null)
                {
                    continue;
                }

                var rect = button as RectTransform;
                if (rect != null)
                {
                    visibleButtons.Add(rect);
                }
            }

            var top = 0.66f;
            var rowHeight = 0.07f;
            var rowGap = 0.01f;

            for (var i = 0; i < visibleButtons.Count; i++)
            {
                var col = i % 2;
                var row = i / 2;
                var yMax = top - row * (rowHeight + rowGap);
                var yMin = yMax - rowHeight;

                if (col == 0)
                {
                    visibleButtons[i].anchorMin = new Vector2(0.50f, yMin);
                    visibleButtons[i].anchorMax = new Vector2(0.70f, yMax);
                }
                else
                {
                    visibleButtons[i].anchorMin = new Vector2(0.72f, yMin);
                    visibleButtons[i].anchorMax = new Vector2(0.92f, yMax);
                }

                visibleButtons[i].offsetMin = Vector2.zero;
                visibleButtons[i].offsetMax = Vector2.zero;
            }
        }

        private static Transform FindSceneObject(string objectName)
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
