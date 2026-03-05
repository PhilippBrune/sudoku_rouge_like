using System.Collections;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class MenuPanelAnimator : MonoBehaviour
    {
        [SerializeField] private float duration = 0.22f;
        [SerializeField] private float hiddenScale = 0.97f;

        private CanvasGroup _group;
        private Coroutine _running;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetImmediate(bool visible)
        {
            Ensure();
            if (visible)
            {
                gameObject.SetActive(true);
                _group.alpha = 1f;
                _group.interactable = true;
                _group.blocksRaycasts = true;
                transform.localScale = Vector3.one;
            }
            else
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
                transform.localScale = Vector3.one * hiddenScale;
                gameObject.SetActive(false);
            }
        }

        public void Play(bool visible)
        {
            Ensure();

            if (!visible && !gameObject.activeInHierarchy)
            {
                SetImmediate(false);
                return;
            }

            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!isActiveAndEnabled)
            {
                SetImmediate(visible);
                return;
            }

            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(Animate(visible));
        }

        private IEnumerator Animate(bool visible)
        {
            if (visible)
            {
                gameObject.SetActive(true);
            }

            var fromAlpha = _group.alpha;
            var toAlpha = visible ? 1f : 0f;
            var fromScale = transform.localScale;
            var toScale = visible ? Vector3.one : Vector3.one * hiddenScale;

            _group.interactable = false;
            _group.blocksRaycasts = false;

            var t = 0f;
            var animDuration = Mathf.Max(0.01f, duration);
            while (t < animDuration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / animDuration);
                var smooth = k * k * (3f - 2f * k);
                _group.alpha = Mathf.Lerp(fromAlpha, toAlpha, smooth);
                transform.localScale = Vector3.Lerp(fromScale, toScale, smooth);
                yield return null;
            }

            _group.alpha = toAlpha;
            transform.localScale = toScale;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;

            if (!visible)
            {
                gameObject.SetActive(false);
            }

            _running = null;
        }

        private void Ensure()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                {
                    _group = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }
}
