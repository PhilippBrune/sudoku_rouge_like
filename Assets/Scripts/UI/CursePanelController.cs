using System.Text;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class CursePanelController : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text curseListText;
        [SerializeField] private Text tensionText;

        private RunMapController _runMap;

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        public void Configure(Text title, Text list, Text tension)
        {
            titleText = title;
            curseListText = list;
            tensionText = tension;
        }

        public void RefreshPanel()
        {
            if (_runMap == null)
            {
                return;
            }

            var curses = _runMap.GetActiveCurses();
            if (titleText != null)
            {
                titleText.text = $"Curses ({curses.Count})";
            }

            if (curseListText != null)
            {
                if (curses.Count == 0)
                {
                    curseListText.text = "No active curses.";
                }
                else
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < curses.Count; i++)
                    {
                        sb.Append("• ");
                        sb.Append(CurseDescription(curses[i]));
                        if (i < curses.Count - 1)
                        {
                            sb.AppendLine();
                        }
                    }

                    curseListText.text = sb.ToString();
                }
            }

            if (tensionText != null)
            {
                var heat = _runMap.Run?.RunState?.CurrentHeatScore ?? 1f;
                tensionText.text = $"Heat pressure: {heat:0.00}";
            }
        }

        private static string CurseDescription(CurseType curse)
        {
            return curse switch
            {
                CurseType.CursedRelicBacklash => "Cursed relic backlash: upside with permanent downside",
                CurseType.LockedItemSlot => "Locked item slot for the rest of the run",
                CurseType.TemporaryBlindness => "Temporary blindness on next puzzle",
                CurseType.IncreasedMistakePenalty => "Mistakes deal +1 damage for 2 puzzles",
                _ => "Minor curse pressure"
            };
        }
    }
}
