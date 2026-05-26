using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialRunBannerController : MonoBehaviour
    {
        private RunMapController _runMapController;
        private GameObject _bannerPanel;
        private Text _bannerText;

        public void Configure(RunMapController runMap, Text text)
        {
            _runMapController = runMap;
            _bannerText = text;
            _bannerPanel = _bannerText != null ? _bannerText.transform.parent?.gameObject : null;
            Refresh();
        }

        public void Refresh()
        {
            if (_bannerText == null) return;

            var run = _runMapController?.Run;
            var isTutorial = run?.State != null && run.State.Mode == GameMode.Tutorial;
            if (_bannerPanel != null)
                _bannerPanel.SetActive(isTutorial);
            _bannerText.gameObject.SetActive(isTutorial);
            if (isTutorial)
            {
                _bannerText.text = LocalizationService.T(
                    "InRun.TutorialBanner",
                    "TUTORIAL MODE\nNo Progression Rewards");
            }
        }

        private void Update()
        {
            Refresh();
        }
    }
}
