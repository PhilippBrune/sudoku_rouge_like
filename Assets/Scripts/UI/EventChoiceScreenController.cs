using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class EventChoiceScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private Text resultText;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private Button optionButtonPrefab;

        private readonly List<Button> _spawnedButtons = new();
        private RunMapController _runMap;
        private RunEvent _currentEvent;

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        public void Configure(GameObject panel, Text prompt, Text result, Transform optionsContainer, Button optionTemplate)
        {
            panelRoot = panel;
            promptText = prompt;
            resultText = result;
            optionsRoot = optionsContainer;
            optionButtonPrefab = optionTemplate;
        }

        public void OpenEvent()
        {
            if (_runMap == null)
            {
                return;
            }

            _currentEvent = _runMap.OpenEventNode();
            if (_currentEvent == null)
            {
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (promptText != null)
            {
                promptText.text = _currentEvent.Prompt;
            }

            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            BuildOptionButtons(_currentEvent.Options);
        }

        public void CloseEvent()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            ClearButtons();
            _currentEvent = null;
        }

        private void BuildOptionButtons(List<RunEventOption> options)
        {
            ClearButtons();

            if (options == null || optionButtonPrefab == null || optionsRoot == null)
            {
                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var button = Instantiate(optionButtonPrefab, optionsRoot);
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{option.Label} — {option.Tradeoff}";
                }

                var optionId = option.OptionId;
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => OnOptionClicked(optionId));
                _spawnedButtons.Add(button);
            }
        }

        private void OnOptionClicked(string optionId)
        {
            if (_runMap == null)
            {
                return;
            }

            var success = _runMap.ChooseEventOption(optionId);
            if (resultText != null)
            {
                resultText.text = success ? "Choice resolved." : "Choice failed (requirements not met).";
            }

            if (success)
            {
                SetButtonsInteractable(false);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    _spawnedButtons[i].interactable = interactable;
                }
            }
        }

        private void ClearButtons()
        {
            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    Destroy(_spawnedButtons[i].gameObject);
                }
            }

            _spawnedButtons.Clear();
        }
    }
}
