using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialRunBannerController : MonoBehaviour
    {
        private RunMapController _runMapController;
        private Text _bannerText;

        public void Configure(RunMapController runMap, Text text)
        {
            _runMapController = runMap;
            _bannerText = text;
            Refresh();
        }

        public void Refresh()
        {
            if (_bannerText == null) return;

            var run = _runMapController?.Run;
            var isTutorial = run?.State != null && run.State.Mode == GameMode.Tutorial;
            _bannerText.gameObject.SetActive(isTutorial);
            if (isTutorial)
            {
                _bannerText.text = "TUTORIAL MODE\nNo Progression Rewards";
            }
        }

        private void Update()
        {
            Refresh();
        }
    }
}
