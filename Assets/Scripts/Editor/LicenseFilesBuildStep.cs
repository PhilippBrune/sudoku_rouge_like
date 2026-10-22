using System.IO;
using SudokuRoguelike.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SudokuRoguelike.EditorTools
{
    /// <summary>
    /// [REQ: LEGAL-LICENSE-001] After every standalone player build, the licence texts go into the build's Licenses folder
    /// next to the executable (GAP-068), so no release depends on a manual packaging step.
    /// </summary>
    public sealed class LicenseFilesBuildStep : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platformGroup != BuildTargetGroup.Standalone) return;
            var output = report.summary.outputPath.TrimEnd('/', '\\');
            // Windows and Linux name the executable, macOS the .app bundle: the folder that holds either.
            var buildFolder = Directory.Exists(output) && !output.EndsWith(".app") ? output : Path.GetDirectoryName(output);
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            int copied;
            try
            {
                copied = ThirdPartyLicenses.CopyToBuild(Path.Combine(projectRoot, ThirdPartyLicenses.SourceFolder), buildFolder);
            }
            catch (IOException e)
            {
                throw new BuildFailedException("The licence texts could not be added to the build (GAP-068): " + e.Message);
            }
            Debug.Log($"[LicenseFilesBuildStep] {copied} licence texts copied to {Path.Combine(buildFolder, ThirdPartyLicenses.BuildFolderName)}.");
        }
    }
}
