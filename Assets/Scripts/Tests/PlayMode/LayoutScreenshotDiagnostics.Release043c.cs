using System.Collections;
using System.Linq;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // The new screens of the owner's picks after 0.4.2: the rule card (FB1), the Lantern Seer's light (LS1) and the
    // feedback form (K3).
    public sealed partial class LayoutScreenshotDiagnostics
    {
        [UnityTest]
        public IEnumerator RenderRuleCardAndLantern()
        {
            var camera = CreateCamera();
            var (host, run) = StartRun(7444, true, BossModifierId.Thermo, BossModifierId.KillerCages);
            yield return DismissIntro();
            yield return new WaitForSecondsRealtime(0.5f);
            // The card asks the profile's save which rules it has shown; the shot's profile is new.
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            run.State.ClassId = ClassId.LanternSeer;
            var screen = host.GetComponentInChildren<InRunController>(true);
            // The puzzle opens (the path shows while a ruled board is made); ShowSudoku opens the rule card.
            typeof(InRunController).GetMethod("ShowSudoku", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(screen, null);
            typeof(InRunController).GetMethod("RenderBoard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, System.Type.EmptyTypes, null)?.Invoke(screen, null);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "rel043c_rule_card.png");
            for (var i = 0; i < 2; i++)
            {
                host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "BtnRuleCardNext")?.onClick.Invoke();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "rel043c_lantern_light.png");
            host.SetActive(false);
        }

        [UnityTest]
        public IEnumerator RenderFeedbackForm()
        {
            var camera = CreateCamera();
            var (host, _) = StartRun(7445, true);
            yield return DismissIntro();
            host.AddComponent<FeedbackScreen>();
            yield return null;
            FeedbackScreen.Open();
            for (var frame = 0; frame < 10; frame++) yield return null;
            yield return Shoot(host, camera, "rel043c_feedback_form.png");
            host.SetActive(false);
        }
    }
}
