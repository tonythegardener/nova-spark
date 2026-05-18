using System;
using System.IO;
using NovaSpark.Game;
using NovaSpark.Powerups;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaSpark.Editor
{
    public static class BuildScript
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string LaserSightAssetPath = "Assets/_Project/Content/Resources/Powerups/LaserSight.asset";
        private const string LaserSightContentId = "powerup.laser_sight";

        [MenuItem("Nova Spark/Prepare Phase 0a Assets")]
        public static void PrepareAssets()
        {
            EnsureLaserSightAsset();
            EnsureBootstrapScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Nova Spark/Build Windows (Phase 0a)")]
        public static void BuildWindowsPhase0a()
        {
            PrepareAssets();
            BuildWindowsTo("Builds/Phase0a/NovaSpark.exe");
        }

        public static void BuildWindowsPhase0aCli()
        {
            try
            {
                BuildWindowsPhase0a();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NovaSpark.Build] FAILED: {ex}");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        private static void EnsureLaserSightAsset()
        {
            EnsureDirectory("Assets/_Project/Content");
            EnsureDirectory("Assets/_Project/Content/Resources");
            EnsureDirectory("Assets/_Project/Content/Resources/Powerups");

            var existing = AssetDatabase.LoadAssetAtPath<PowerupDefinition>(LaserSightAssetPath);
            if (existing != null)
            {
                Debug.Log($"[NovaSpark.Build] Existing LaserSight asset found at {LaserSightAssetPath}");
                return;
            }

            var so = ScriptableObject.CreateInstance<PowerupDefinition>();
            var idField = typeof(PowerupDefinition).GetField("contentId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameField = typeof(PowerupDefinition).GetField("displayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = typeof(PowerupDefinition).GetField("description",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var magField = typeof(PowerupDefinition).GetField("magnitude",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var schemaField = typeof(PowerupDefinition).GetField("schemaVersion",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            idField?.SetValue(so, LaserSightContentId);
            nameField?.SetValue(so, "Laser Sight");
            descField?.SetValue(so, "Shows shot trajectory line with bounces.");
            magField?.SetValue(so, 1f);
            schemaField?.SetValue(so, 1);

            AssetDatabase.CreateAsset(so, LaserSightAssetPath);
            Debug.Log($"[NovaSpark.Build] Created LaserSight asset at {LaserSightAssetPath}");
        }

        private static void EnsureBootstrapScene()
        {
            EnsureDirectory("Assets/_Project/Scenes");

            if (File.Exists(BootstrapScenePath))
            {
                Debug.Log($"[NovaSpark.Build] Existing Bootstrap scene at {BootstrapScenePath}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();

            SceneManager.MoveGameObjectToScene(go, scene);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            Debug.Log($"[NovaSpark.Build] Created Bootstrap scene at {BootstrapScenePath}");
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[NovaSpark.Build] EditorBuildSettings.scenes configured.");
        }

        private static void BuildWindowsTo(string outputRelativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var outputFull = Path.GetFullPath(Path.Combine(projectRoot, outputRelativePath));
            var outputDir = Path.GetDirectoryName(outputFull);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir!);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapScenePath },
                locationPathName = outputFull,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            Debug.Log($"[NovaSpark.Build] Building Windows64 -> {outputFull}");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[NovaSpark.Build] Result: {summary.result}, size={summary.totalSize}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build did not succeed: {summary.result}");
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
