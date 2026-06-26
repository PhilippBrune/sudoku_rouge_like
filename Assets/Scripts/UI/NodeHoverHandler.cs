using System;
using UnityEngine;
using UnityEngine.EventSystems;
using SudokuRoguelike.Run;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Lightweight pointer/select handler for path-map node buttons.
    /// Replaces EventTrigger + per-node lambda allocations with a simple
    /// MonoBehaviour that holds pre-set delegate references.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class NodeHoverHandler : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private RunNode _node;
        private LevelConfig _config;
        private bool _isInvalidFirst;
        private bool _isHidden;

        // Delegate slots — set once, shared across all nodes in the same rebuild.
        private Action<RunNode, LevelConfig, Vector2> _onShow;
        private Action _onHide;
        private Action<Vector2> _onShowBlocked;

        internal void Setup(
            RunNode node,
            LevelConfig config,
            bool isInvalidFirst,
            bool isHidden,
            Action<RunNode, LevelConfig, Vector2> onShow,
            Action onHide,
            Action<Vector2> onShowBlocked)
        {
            _node           = node;
            _config         = config;
            _isInvalidFirst = isInvalidFirst;
            _isHidden       = isHidden;
            _onShow         = onShow;
            _onHide         = onHide;
            _onShowBlocked  = onShowBlocked;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (_node == null || _node.Visited) return;
            if (_isHidden) return; // undiscovered node — no tooltip
            if (_isInvalidFirst) _onShowBlocked?.Invoke(e.position);
            else                 _onShow?.Invoke(_node, _config, e.position);
        }

        public void OnPointerExit(PointerEventData e) => _onHide?.Invoke();

        public void OnSelect(BaseEventData e)
        {
            if (_node == null || _node.Visited || _isInvalidFirst || _isHidden) return;
            _onShow?.Invoke(_node, _config, Vector2.zero);
        }

        public void OnDeselect(BaseEventData e) => _onHide?.Invoke();
    }
}
