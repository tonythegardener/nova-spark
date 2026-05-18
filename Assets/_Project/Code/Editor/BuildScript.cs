using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NovaSpark.Game;
using NovaSpark.Game.Level;
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
        private const string Phase0bLevelAssetPath = "Assets/_Project/Content/Resources/Levels/Phase0bSmokeTest.asset";
        private const string Phase0bLevelContentId = "level.phase0b_smoke_test";

        [MenuItem("Nova Spark/Prepare Phase 0 Assets")]
        public static void PrepareAssets()
        {
            EnsureLaserSightAsset();
            EnsurePhase0bLevelAsset();
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

        [MenuItem("Nova Spark/Build Windows (Phase 0b)")]
        public static void BuildWindowsPhase0b()
        {
            PrepareAssets();
            BuildWindowsTo("Builds/Phase0b/NovaSpark.exe");
        }

        public static void BuildWindowsPhase0aCli() => RunCli(BuildWindowsPhase0a);
        public static void BuildWindowsPhase0bCli() => RunCli(BuildWindowsPhase0b);

        private static void RunCli(Action action)
        {
            try
            {
                action();
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
            EnsureAssetFolders(LaserSightAssetPath);

            var existing = AssetDatabase.LoadAssetAtPath<PowerupDefinition>(LaserSightAssetPath);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<PowerupDefinition>();
            SetPrivate(so, "contentId", LaserSightContentId);
            SetPrivate(so, "displayName", "Laser Sight");
            SetPrivate(so, "description", "Shows shot trajectory line with bounces.");
            SetPrivate(so, "magnitude", 1f);
            SetPrivate(so, "schemaVersion", 1);

            AssetDatabase.CreateAsset(so, LaserSightAssetPath);
            Debug.Log($"[NovaSpark.Build] Created LaserSight asset at {LaserSightAssetPath}");
        }

        private static void EnsurePhase0bLevelAsset()
        {
            EnsureAssetFolders(Phase0bLevelAssetPath);

            var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Phase0bLevelAssetPath);
            if (existing != null)
            {
                Debug.Log($"[NovaSpark.Build] Existing Phase 0b level at {Phase0bLevelAssetPath}");
                return;
            }

            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivate(level, "contentId", Phase0bLevelContentId);
            SetPrivate(level, "schemaVersion", 1);
            SetPrivate(level, "displayName", "Phase 0b — Smoke Test");
            SetPrivate(level, "waypoints", new List<Vector2>
            {
                new(-8.5f,  3.5f),
                new(-3.0f,  3.5f),
                new(-1.0f,  2.0f),
                new( 0.0f,  0.0f),
                new( 1.0f, -2.0f),
                new( 3.0f, -2.5f),
                new( 8.5f, -2.5f),
            });

            SetPrivate(level, "totalMarbles", 28);
            SetPrivate(level, "spawnIntervalSeconds", 0.30f);
            SetPrivate(level, "chainAdvanceSpeed", 1.25f);
            SetPrivate(level, "marbleSpacing", 0.62f);
            SetPrivate(level, "catchUpSpeedMultiplier", 3f);
            SetPrivate(level, "marbleRadius", 0.32f);

            SetPrivate(level, "shooterPosition", new Vector2(0f, -4.2f));
            SetPrivate(level, "shotVelocity", 18f);
            SetPrivate(level, "shooterCooldown", 0.10f);
            SetPrivate(level, "projectileRadius", 0.25f);

            SetPrivate(level, "matchThreshold", 3);

            SetPrivate(level, "cameraOrthographicSize", 5.5f);
            SetPrivate(level, "cameraCenter", new Vector2(0f, 0f));
            SetPrivate(level, "backgroundColor", new Color(0.05f, 0.06f, 0.10f, 1f));

            AssetDatabase.CreateAsset(level, Phase0bLevelAssetPath);
            Debug.Log($"[NovaSpark.Build] Created Phase 0b level asset at {Phase0bLevelAssetPath}");
        }

        private static void EnsureBootstrapScene()
        {
            EnsureAssetFolders(BootstrapScenePath);

            UnityEngine.SceneManagement.Scene scene;
            bool created = false;
            if (File.Exists(BootstrapScenePath))
            {
                scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                created = true;
            }

            var bootstrapGo = FindOrCreateBootstrapObject(scene);
            if (bootstrapGo.GetComponent<Bootstrap>() == null) bootstrapGo.AddComponent<Bootstrap>();
            if (bootstrapGo.GetComponent<LevelController>() == null) bootstrapGo.AddComponent<LevelController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            if (created) Debug.Log($"[NovaSpark.Build] Created Bootstrap scene at {BootstrapScenePath}");
            else Debug.Log($"[NovaSpark.Build] Updated Bootstrap scene at {BootstrapScenePath}");
        }

        private static GameObject FindOrCreateBootstrapObject(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Bootstrap") return root;
            }
            var go = new GameObject("Bootstrap");
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
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
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir!);

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

        private static void EnsureAssetFolders(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            EnsureFolderRecursive(dir);
        }

        private static void EnsureFolderRecursive(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder)) return;
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetFolder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolderRecursive(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                Debug.LogWarning($"[NovaSpark.Build] Could not find field '{fieldName}' on {target.GetType().Name}");
                return;
            }
            f.SetValue(target, value);
        }
    }
}
