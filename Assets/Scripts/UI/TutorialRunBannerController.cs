using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class TutorialRunBannerController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private Text bannerText;

        public void Configure(RunMapController runMap, Text text)
        {
            runMapController = runMap;
            bannerText = text;
            Refresh();
        }

        public void Refresh()
        {
            if (bannerText == null)
            {
                return;
            }

            var run = runMapController?.Run;
            var isTutorial = run?.RunState != null && run.RunState.Mode == GameMode.Tutorial;
            bannerText.gameObject.SetActive(isTutorial);
            if (isTutorial)
            {
                bannerText.text = "TUTORIAL MODE\nNo Progression Rewards";
            }
        }

        private void Update()
        {
            Refresh();
        }
    }
}
