using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class InRunInputController : MonoBehaviour
    {
        [SerializeField] private bool useRandomCellInput = true;

        private RunDirector _run;

        public void Bind(RunDirector run)
        {
            _run = run;
        }

        private void Update()
        {
            if (_run == null || _run.CurrentBoard == null) return;

            if (Input.GetKeyDown(KeyCode.Space) && useRandomCellInput)
            {
                var size = _run.CurrentBoard.Size;
                var row = Random.Range(0, size);
                var col = Random.Range(0, size);
                var value = Random.Range(1, size + 1);
                var result = _run.PlaceNumber(row, col, value);
                Debug.Log($"[Input] ({row},{col})={value} → {result}, HP={_run.State.CurrentHP}");
            }
        }
    }
}
