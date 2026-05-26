using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class InputRemapServiceTests
    {
        private const int PrimaryTestSlot = SaveFileService.MaxSlots - 1;
        private const int IsolationTestSlot = SaveFileService.MaxSlots - 2;

        private readonly Dictionary<string, string> _preservedPrefs = new();
        private readonly HashSet<string> _existingPrefs = new();
        private int _originalActiveSlot;

        [SetUp]
        public void SetUp()
        {
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            PreserveAndDelete(PrefsKey(PrimaryTestSlot));
            PreserveAndDelete(PrefsKey(IsolationTestSlot));
            SaveProfileService.ActiveSlot = PrimaryTestSlot;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var key in _preservedPrefs.Keys)
            {
                if (_existingPrefs.Contains(key))
                    PlayerPrefs.SetString(key, _preservedPrefs[key]);
                else
                    PlayerPrefs.DeleteKey(key);
            }

            _preservedPrefs.Clear();
            _existingPrefs.Clear();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            PlayerPrefs.Save();
        }

        [Test]
        public void SetBindingBeforeAnyRead_PersistsForActiveSlot()
        {
            var remap = new InputRemapService();

            remap.SetBinding(InputAction.TogglePencil, KeyCode.K);

            var reloaded = new InputRemapService();
            Assert.AreEqual(KeyCode.K, reloaded.GetBinding(InputAction.TogglePencil));
            Assert.AreEqual(InputAction.TogglePencil, reloaded.FindConflict(KeyCode.K, InputAction.ClearCell));
        }

        [Test]
        public void ReloadForSlot_IsolatesOverridesAndResetRemovesStoredBinding()
        {
            var remap = new InputRemapService();

            remap.ReloadForSlot(PrimaryTestSlot);
            remap.SetBinding(InputAction.TogglePencil, KeyCode.K);

            remap.ReloadForSlot(IsolationTestSlot);
            Assert.AreEqual(InputRemapService.GetDefault(InputAction.TogglePencil),
                remap.GetBinding(InputAction.TogglePencil));

            remap.SetBinding(InputAction.ClearCell, KeyCode.K);
            Assert.AreEqual(KeyCode.K, remap.GetBinding(InputAction.ClearCell));

            remap.ResetBinding(InputAction.ClearCell);
            Assert.AreEqual(InputRemapService.GetDefault(InputAction.ClearCell),
                remap.GetBinding(InputAction.ClearCell));

            remap.ReloadForSlot(PrimaryTestSlot);
            Assert.AreEqual(KeyCode.K, remap.GetBinding(InputAction.TogglePencil));
            Assert.AreEqual(InputRemapService.GetDefault(InputAction.ClearCell),
                remap.GetBinding(InputAction.ClearCell));
        }

        private static string PrefsKey(int slot) => $"InputOverrides_{slot}";

        private void PreserveAndDelete(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                _existingPrefs.Add(key);
                _preservedPrefs[key] = PlayerPrefs.GetString(key);
            }
            else
            {
                _preservedPrefs[key] = null;
            }

            PlayerPrefs.DeleteKey(key);
        }
    }
}
