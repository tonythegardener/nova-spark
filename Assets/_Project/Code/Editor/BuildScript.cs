using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NovaSpark.Editor
{
    // Builds the Windows player. Content lives in the committed assets under
    // Assets/_Project/Content, so a build never rewrites it.
    //
    // Command line:
    //   Unity -batchmode -projectPath . -buildTarget Win64
    //     -executeMethod NovaSpark.Editor.BuildScript.BuildWindowsCli -logFile build.log
    public static class BuildScript
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string WindowsOutputPath = "Builds/NovaSpark/NovaSpark.exe";

        [MenuItem("Nova Spark/Build Windows")]
        public static void BuildWindows()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var outputFull = Path.GetFullPath(Path.Combine(projectRoot, WindowsOutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(outputFull)!);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapScenePath },
                locationPathName = outputFull,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            Debug.Log($"[NovaSpark.Build] Building Windows64 -> {outputFull}");
            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[NovaSpark.Build] Result: {summary.result}, size={summary.totalSize}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build did not succeed: {summary.result}");
            }
        }

        public static void BuildWindowsCli()
        {
            try
            {
                BuildWindows();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NovaSpark.Build] FAILED: {ex}");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }
    }
}
