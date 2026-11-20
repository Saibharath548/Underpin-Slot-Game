using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Underpin.SlotMachine.Editor
{
    /// <summary>
    /// Automated build runner for compiling the WebGL build of the Slot Machine game.
    /// Builds into Build/WebGL inside the project repository.
    /// </summary>
    public static class SlotWebGLBuilder
    {
        [MenuItem("Underpin/Build/Build WebGL Player")]
        public static void BuildWebGL()
        {
            string buildPath = "Build/WebGL";

            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            string scenePath = "Assets/Underpin/Scenes/SlotMachineScene.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[SlotWebGLBuilder] Scene not found at {scenePath}! Run Scene Builder first.");
                return;
            }

            // Ensure proper WebGL settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Clean uncompressed build for easy local file/HTTP serving
            PlayerSettings.productName = "Underpin Slot Machine";
            PlayerSettings.companyName = "Underpin";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[SlotWebGLBuilder] Starting WebGL Build to: {buildPath}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[SlotWebGLBuilder] WebGL Build SUCCEEDED! Total size: {summary.totalSize} bytes, Time: {summary.totalTime.TotalSeconds:F1}s");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[SlotWebGLBuilder] WebGL Build FAILED! Total errors: {summary.totalErrors}");
            }
        }
    }
}
