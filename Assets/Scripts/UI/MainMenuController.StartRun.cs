using UnityEngine;
using UnityEngine.UI;
using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // Start-run modal extras: optional seed entry (GAP-020) and the warning before a saved run is abandoned (GAP-032).
    public sealed partial class MainMenuController
    {
        private InputField _seedInput;
        private Button _weeklySeedButton;
        private bool _hasPendingSeed;
        private int _pendingSeed;

        // [REQ: REQ-RUN-100] Seed row (keyboard entry; an empty field means a random seed).
        // [REQ: REQ-RUN-101] Warning when Begin would replace the saved run.
        private void BuildStartRunExtras(Transform modal, bool abandonsSavedRun)
        {
            _seedInput = CreateSeedInput(modal, new Vector2(0.30f, 0.26f), new Vector2(0.70f, 0.34f));
            _weeklySeedButton = CreateWeeklySeedButton(modal, _seedInput, DateTime.Today);

            if (!abandonsSavedRun) return;
            var warning = InRunUiFactory.CreateText(modal, "AbandonWarning",
                LocalizationService.T("RunConfirm.AbandonWarning",
                    "Your saved run will end as a defeat. Its XP and progress are kept."),
                11, TextAnchor.MiddleCenter, InRunUiFactory.CursedTitleRed);
            warning.rectTransform.anchorMin = new Vector2(0.05f, 0.15f);
            warning.rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
            warning.rectTransform.offsetMin = warning.rectTransform.offsetMax = Vector2.zero;
            warning.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void BeginRunFromModal(bool abandonSavedRun)
        {
            _hasPendingSeed = TryParseSeed(_seedInput != null ? _seedInput.text : null, out _pendingSeed);
            _seedInput = null;
            if (abandonSavedRun) _gameBootstrap.AbandonSavedRun();
            ConfirmClassAndStart();
        }

        private void ApplyPendingSeed(LaunchRequest request)
        {
            request.HasSeed = _hasPendingSeed;
            request.Seed = _pendingSeed;
            _hasPendingSeed = false;
        }

        /// <summary>Digits only, up to 9 of them; anything else falls back to a random seed.</summary>
        public static bool TryParseSeed(string text, out int seed)
        {
            seed = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (text.Length > 9) return false;
            foreach (var ch in text)
                if (ch < '0' || ch > '9') return false;
            return int.TryParse(text, out seed);
        }

        // [REQ: RUN-WEEK-001] Seed of the week: one button fills in this week's seed, the same for every player.
        private static Button CreateWeeklySeedButton(Transform modal, InputField seedInput, DateTime today)
        {
            var (_, week) = WeeklySeedService.IsoWeek(today);
            var seed = WeeklySeedService.SeedFor(today);
            var btn = InRunUiFactory.CreatePanelButton(modal, "BtnWeeklySeed",
                new Vector2(0.72f, 0.26f), new Vector2(0.95f, 0.34f),
                LocalizationService.Format("RunConfirm.WeeklySeed", "Week {0} seed", week));
            ConfigureModalActionLabel(btn);
            btn.onClick.AddListener(() => seedInput.text = seed.ToString());
            return btn;
        }

        private static InputField CreateSeedInput(Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject("SeedInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.85f);

            var text = InRunUiFactory.CreateText(go.transform, "Text", "", 14, TextAnchor.MiddleCenter, GamePalette.TextPrimary);
            text.supportRichText = false;
            Stretch(text.rectTransform);
            var placeholder = InRunUiFactory.CreateText(go.transform, "Placeholder",
                LocalizationService.T("RunConfirm.Seed.Placeholder", "Seed (optional)"),
                12, TextAnchor.MiddleCenter, GamePalette.TextMuted);
            Stretch(placeholder.rectTransform);

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;
            return input;
        }

        // GAP-029: modal action labels were 12 pt. They are enlarged per modal; the shared button factory keeps its
        // compact size (same approach as RewardViewController.ConfigureRestActionLabel).
        internal static void ConfigureModalActionLabel(Button button)
        {
            var label = button != null ? button.transform.Find("Label")?.GetComponent<Text>() : null;
            if (label == null) return;
            label.fontSize = 16;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 16;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
        }
    }
}
