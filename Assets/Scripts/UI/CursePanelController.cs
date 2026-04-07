using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class CursePanelController : MonoBehaviour
    {
        private Text _titleText;
        private Text _curseListText;
        private RunMapController _runMap;

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        public void Configure(Text title, Text list)
        {
            _titleText = title;
            _curseListText = list;
        }

        public void RefreshPanel()
        {
            // Curse system is stubbed — no active curses yet
            if (_titleText != null)
                _titleText.text = "Curses (0)";
            if (_curseListText != null)
                _curseListText.text = "No active curses.";
        }
    }
}
