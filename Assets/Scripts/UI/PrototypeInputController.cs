using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeInputController : MonoBehaviour
    {
        [SerializeField] private bool useRandomCellInput = true;
        [SerializeField] private GameObject bootstrapObject;

        private RunDirector _run;

        private void Awake()
        {
            if (bootstrapObject == null)
            {
                return;
            }

            var bootstrap = bootstrapObject.GetComponent<SudokuRoguelike.Bootstrap.GameBootstrap>();
            if (bootstrap == null)
            {
                return;
            }
        }

        public void Bind(RunDirector run)
        {
            _run = run;
        }

        private void Update()
        {
            if (_run == null || _run.CurrentBoard == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) && useRandomCellInput)
            {
                var row = Random.Range(0, _run.CurrentBoard.Size);
                var col = Random.Range(0, _run.CurrentBoard.Size);
                var value = Random.Range(1, _run.CurrentBoard.Size + 1);
                var ok = _run.PlaceNumber(row, col, value);
                Debug.Log($"Input ({row},{col})={value}, correct={ok}, hp={_run.RunState.CurrentHP}");
            }
        }
    }
}
