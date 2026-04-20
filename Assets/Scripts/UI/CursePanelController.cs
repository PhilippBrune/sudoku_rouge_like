using System.Text;
using SudokuRoguelike.Run;
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
            var state = _runMap?.Run?.State;
            if (state == null)
            {
                if (_titleText != null) _titleText.text = "Curses (0)";
                if (_curseListText != null) _curseListText.text = "No active curses.";
                return;
            }

            var curses = CurseService.GetActiveCurses(state);
            if (_titleText != null)
                _titleText.text = curses.Count > 0 ? $"Curses ({curses.Count})" : "Curses (0)";

            if (_curseListText != null)
            {
                if (curses.Count == 0)
                {
                    _curseListText.text = "No active curses.";
                }
                else
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < curses.Count; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append($"• {curses[i].Name} — {curses[i].Description}");
                    }
                    _curseListText.text = sb.ToString();
                }
            }
        }
    }
}
