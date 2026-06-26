using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class EventChoiceScreenController : MonoBehaviour
    {
        private GameObject _panelRoot;
        private Text _promptText;
        private Text _resultText;
        private Transform _optionsRoot;
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
        private RunMapController _runMap;
        private RunEvent _currentEvent;

        public GameObject ActivePanel { get; private set; }

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        public void Configure(GameObject panel, Text prompt, Text result, Transform optionsContainer)
        {
            _panelRoot = panel;
            _promptText = prompt;
            _resultText = result;
            _optionsRoot = optionsContainer;
        }

        public void OpenEvent()
        {
            if (_runMap == null) return;

            _currentEvent = _runMap.OpenEventNode();
            if (_currentEvent == null) return;

            if (_panelRoot != null) _panelRoot.SetActive(true);
            if (_promptText != null) _promptText.text = $"{_currentEvent.Title}\n{_currentEvent.Description}";
            if (_resultText != null) _resultText.text = string.Empty;

            BuildOptionButtons(_currentEvent.Options);
            ActivePanel = _panelRoot;
            InRunUiFactory.SelectFirstInteractable(_panelRoot);
        }

        public void CloseEvent()
        {
            ActivePanel = null;
            if (_panelRoot != null) _panelRoot.SetActive(false);
            ClearButtons();
            _currentEvent = null;
        }

        private void BuildOptionButtons(List<EventOption> options)
        {
            ClearButtons();
            if (options == null || _optionsRoot == null) return;

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var go = new GameObject($"EventOption_{i}", typeof(RectTransform), typeof(Button), typeof(Image));
                go.transform.SetParent(_optionsRoot, false);

                var img = go.GetComponent<Image>();
                img.color = InRunUiFactory.BtnColor;

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var rt = textGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8, 2);
                rt.offsetMax = new Vector2(-8, -2);
                var label = textGo.GetComponent<Text>();
                label.text = LocalizationService.Format(
                    "RunEvent.OptionLabelWithEffect",
                    "{0} - {1}",
                    option.Label,
                    option.EffectDescription);
                label.font = InRunUiFactory.GetFont();
                label.fontSize = 14;
                label.color = InRunUiFactory.TextColor;
                label.alignment = TextAnchor.MiddleLeft;

                var optionIndex = i;
                var btn = go.GetComponent<Button>();
                var cols = btn.colors;
                cols.highlightedColor = InRunUiFactory.BtnHighlighted;
                cols.selectedColor    = InRunUiFactory.BtnSelected;
                cols.pressedColor     = InRunUiFactory.BtnPressed;
                btn.colors = cols;
                btn.onClick.AddListener(() => OnOptionClicked(optionIndex));
                _spawnedButtons.Add(go);
            }

            // Explicit vertical nav chain between option buttons
            for (var j = 0; j < _spawnedButtons.Count - 1; j++)
            {
                var a = _spawnedButtons[j]?.GetComponent<Button>();
                var b = _spawnedButtons[j + 1]?.GetComponent<Button>();
                if (a == null || b == null) continue;
                var na = a.navigation; na.mode = Navigation.Mode.Explicit; na.selectOnDown = b; a.navigation = na;
                var nb = b.navigation; nb.mode = Navigation.Mode.Explicit; nb.selectOnUp = a; b.navigation = nb;
            }
        }

        private void OnOptionClicked(int optionIndex)
        {
            if (_runMap == null) return;

            var summary = _runMap.ChooseEventOption(optionIndex);
            if (_resultText != null)
                _resultText.text = string.IsNullOrEmpty(summary)
                    ? LocalizationService.T("RunEvent.Result.Done", "Done.")
                    : summary;

            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                var btn = _spawnedButtons[i]?.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }

            // Refresh the curse panel now that state may have changed (e.g. Ink Peddler gave a curse)
            var flowCtrl = GetComponentInParent<InRunUiFlowController>();
            flowCtrl?.RefreshRuntimePanels();
        }

        private void ClearButtons()
        {
            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                    Destroy(_spawnedButtons[i]);
            }
            _spawnedButtons.Clear();
        }

    }
}
