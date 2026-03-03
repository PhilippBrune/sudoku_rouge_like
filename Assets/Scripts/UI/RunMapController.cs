using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class RunMapController : MonoBehaviour
    {
        [SerializeField] private int seed = 9901;

        private readonly SaveFileService _saveFile = new();
        private readonly ProfileService _profile = new();
        private readonly RunResumeService _resume = new();

        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;

        public void Initialize(ClassId classId, MetaProgressionState meta)
        {
            _run = new RunDirector(seed);
            _run.StartRun(classId, GameMode.GardenRun, runNumber: 1, meta: meta);
            BindAutoSave();
        }

        public bool ResumeFromEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope == null)
            {
                return false;
            }

            _profile.ApplyEnvelope(envelope);
            _run = new RunDirector(seed);
            var resumed = _resume.TryResumeFromSave(_run, envelope);
            if (!resumed)
            {
                return false;
            }

            BindAutoSave();
            return true;
        }

        public List<RunNode> GetVisibleNodes()
        {
            var output = new List<RunNode>();
            var graph = _run.CurrentRunGraph;
            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i].IsRevealed)
                {
                    output.Add(graph[i]);
                }
            }

            return output;
        }

        public RunNode SelectPath(bool risk)
        {
            return _run.AdvanceToNextNode(risk);
        }

        public RunEvent OpenEventNode()
        {
            return _run?.BuildCurrentEvent();
        }

        public bool ChooseEventOption(string optionId)
        {
            return _run != null && _run.ResolveCurrentEventChoice(optionId);
        }

        public List<CurseType> GetActiveCurses()
        {
            var output = new List<CurseType>();
            if (_run?.RunState == null)
            {
                return output;
            }

            for (var i = 0; i < _run.RunState.ActiveCurses.Count; i++)
            {
                output.Add(_run.RunState.ActiveCurses[i]);
            }

            return output;
        }

        public List<float> GetHeatCurve()
        {
            var output = new List<float>();
            if (_run?.RunState == null)
            {
                return output;
            }

            for (var i = 0; i < _run.RunState.HeatHistory.Count; i++)
            {
                output.Add(_run.RunState.HeatHistory[i]);
            }

            return output;
        }

        public RunResult BuildRunResult(bool victory, int bossPhaseReached, int secondsPlayed)
        {
            if (_run == null)
            {
                return null;
            }

            return _run.BuildRunResult(victory, bossPhaseReached, secondsPlayed);
        }

        public RunDirector Run => _run;

        private void BindAutoSave()
        {
            _autoSave ??= new RunAutoSaveCoordinator(_saveFile, _profile);
            _autoSave.Bind(_run);
        }
    }
}
