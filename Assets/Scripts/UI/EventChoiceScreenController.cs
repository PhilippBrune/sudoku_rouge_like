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
        }

        public void CloseEvent()
        {
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
                img.color = new Color(0.15f, 0.22f, 0.29f, 0.9f);

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var rt = textGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8, 2);
                rt.offsetMax = new Vector2(-8, -2);
                var label = textGo.GetComponent<Text>();
                label.text = $"{option.Label} — {option.EffectDescription}";
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 14;
                label.color = new Color(0.96f, 0.93f, 0.82f);
                label.alignment = TextAnchor.MiddleLeft;

                var optionIndex = i;
                go.GetComponent<Button>().onClick.AddListener(() => OnOptionClicked(optionIndex));
                _spawnedButtons.Add(go);
            }
        }

        private void OnOptionClicked(int optionIndex)
        {
            if (_runMap == null) return;

            _runMap.ChooseEventOption(optionIndex);
            if (_resultText != null)
                _resultText.text = "Choice resolved.";

            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                var btn = _spawnedButtons[i]?.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
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
