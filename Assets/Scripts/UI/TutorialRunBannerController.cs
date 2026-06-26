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
            // Banner is intentionally hidden; Custom Puzzle mode does not need a tutorial overlay.
            if (_bannerPanel != null) _bannerPanel.SetActive(false);
            _bannerText.gameObject.SetActive(false);
        }

        private void Update()
        {
            Refresh();
        }
    }
}
