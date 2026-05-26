using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class GeneratedUiRegistryTests
    {
        [Test]
        public void Validate_PassesWhenRequiredButtonIsRegisteredWithAction()
        {
            var go = new GameObject("BtnStart");
            try
            {
                var button = go.AddComponent<Button>();
                var registry = new GeneratedUiRegistry();
                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Button("MainMenu", "BtnStart", "StartGame")
                });

                registry.RegisterButton("MainMenu", "BtnStart", button, "StartGame");

                var result = registry.Validate();

                Assert.IsTrue(result.IsValid, result.ToLogString());
                Assert.AreEqual(1, result.Records.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Validate_FailsWhenRequiredButtonWasNotRegistered()
        {
            var registry = new GeneratedUiRegistry();
            registry.BeginRebuild(new[]
            {
                GeneratedUiControlRequirement.Button("MainMenu", "BtnStart", "StartGame")
            });

            var result = registry.Validate();

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Missing required Button MainMenu/BtnStart", result.ToLogString());
        }

        [Test]
        public void Validate_FailsWhenRegisteredButtonHasNoAction()
        {
            var go = new GameObject("BtnStart");
            try
            {
                var button = go.AddComponent<Button>();
                var registry = new GeneratedUiRegistry();
                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Button("MainMenu", "BtnStart", "StartGame")
                });

                registry.RegisterButton("MainMenu", "BtnStart", button, string.Empty);

                var result = registry.Validate();

                Assert.IsFalse(result.IsValid);
                StringAssert.Contains("missing action StartGame", result.ToLogString());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BeginRebuild_ClearsOldRecords()
        {
            var go = new GameObject("BtnStart");
            try
            {
                var button = go.AddComponent<Button>();
                var registry = new GeneratedUiRegistry();
                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Button("MainMenu", "BtnStart", "StartGame")
                });
                registry.RegisterButton("MainMenu", "BtnStart", button, "StartGame");

                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Button("MainMenu", "BtnOptions", "OpenOptions")
                });

                var result = registry.Validate();

                Assert.IsFalse(result.IsValid);
                StringAssert.Contains("Missing required Button MainMenu/BtnOptions", result.ToLogString());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Validate_FailsWhenRequiredToggleHasNoRegisteredAction()
        {
            var go = new GameObject("TglMute");
            try
            {
                var toggle = go.AddComponent<Toggle>();
                var registry = new GeneratedUiRegistry();
                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Toggle("Options", "TglMute", "SetMute")
                });

                registry.RegisterControl(
                    "Options",
                    "TglMute",
                    GeneratedUiControlType.Toggle,
                    string.Empty,
                    toggle,
                    hasRegisteredAction: false);

                var result = registry.Validate();

                Assert.IsFalse(result.IsValid);
                StringAssert.Contains("Toggle Options/TglMute is missing action SetMute", result.ToLogString());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Validate_FailsWhenControlIsRegisteredTwice()
        {
            var go = new GameObject("BtnStart");
            try
            {
                var button = go.AddComponent<Button>();
                var registry = new GeneratedUiRegistry();
                registry.BeginRebuild(new[]
                {
                    GeneratedUiControlRequirement.Button("MainMenu", "BtnStart", "StartGame")
                });

                registry.RegisterButton("MainMenu", "BtnStart", button, "StartGame");
                registry.RegisterButton("MainMenu", "BtnStart", button, "StartGame");

                var result = registry.Validate();

                Assert.IsFalse(result.IsValid);
                StringAssert.Contains("Duplicate generated UI registration for MainMenu/BtnStart", result.ToLogString());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
