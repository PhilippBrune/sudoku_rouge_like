using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: INPUT-HINT-001] A key hint that shows the pad's buttons after a pad press and the keys after a key or mouse
    /// press (RM-207). The catalog key holds the pad's text, "&lt;key&gt;.Keys" the keyboard's; without that variant the
    /// one text stays.
    /// </summary>
    public sealed class DeviceHint : MonoBehaviour
    {
        private Text _text;
        private string _key;
        private object[] _args;

        /// <summary>Shows <paramref name="key"/> on <paramref name="text"/> for the device in use, and again when it changes.</summary>
        public static void Attach(Text text, string key, params object[] args)
        {
            if (text == null) return;
            var hint = OptionsUiFactory.EnsureComponent<DeviceHint>(text.gameObject);
            hint._text = text;
            hint._key = key;
            hint._args = args;
            hint.Render();
        }

        /// <summary>The text of <paramref name="key"/> for the device in use.</summary>
        public static string For(string key, params object[] args)
        {
            var keys = key + ".Keys";
            var useKeys = !InputDeviceTracker.UsingPad && LocalizationService.HasExternalCatalogEntry(LocalizationService.Current, keys);
            return LocalizationService.Format(useKeys ? keys : key, LocalizationService.T(key), args);
        }

        private void OnEnable()
        {
            InputDeviceTracker.Changed += Render;
            Render();
        }

        private void OnDisable() => InputDeviceTracker.Changed -= Render;

        private void Render()
        {
            if (_text != null && _key != null) _text.text = For(_key, _args ?? System.Array.Empty<object>());
        }
    }
}
