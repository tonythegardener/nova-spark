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
        private const string Phase0bLevelAssetPath = "Assets/_Project/Content/Resources/Levels/Phase0bSmokeTest.asset";
        private const string SwitchbackLevelAssetPath = "Assets/_Project/Content/Resources/Levels/Switchback.asset";
        private const string LongSweepLevelAssetPath = "Assets/_Project/Content/Resources/Levels/LongSweep.asset";

        [MenuItem("Nova Spark/Prepare Phase 1a Assets")]
        public static void PrepareAssets()
        {
            EnsureLaserSightAsset();
            EnsurePhase0bLevelAsset();
            EnsureSwitchbackLevelAsset();
            EnsureLongSweepLevelAsset();
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

        [MenuItem("Nova Spark/Build Windows (Phase 1a)")]
        public static void BuildWindowsPhase1a()
        {
            PrepareAssets();
            BuildWindowsTo("Builds/Phase1a/NovaSpark.exe");
        }

        public static void BuildWindowsPhase0aCli() => RunCli(BuildWindowsPhase0a);
        public static void BuildWindowsPhase0bCli() => RunCli(BuildWindowsPhase0b);
        public static void BuildWindowsPhase1aCli() => RunCli(BuildWindowsPhase1a);

        private static void RunCli(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Debug.LogError($"[NovaSpark.Build] FAILED: {ex}");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        // -------------------- powerup assets --------------------

        private static void EnsureLaserSightAsset()
        {
            EnsureAssetFolders(LaserSightAssetPath);
            var existing = AssetDatabase.LoadAssetAtPath<PowerupDefinition>(LaserSightAssetPath);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<PowerupDefinition>();
            SetPrivate(so, "contentId", "powerup.laser_sight");
            SetPrivate(so, "displayName", "Laser Sight");
            SetPrivate(so, "description", "Shows shot trajectory line with bounces.");
            SetPrivate(so, "magnitude", 1f);
            SetPrivate(so, "schemaVersion", 1);

            AssetDatabase.CreateAsset(so, LaserSightAssetPath);
            Debug.Log($"[NovaSpark.Build] Created LaserSight asset at {LaserSightAssetPath}");
        }

        // -------------------- level assets --------------------

        private static void EnsurePhase0bLevelAsset()
        {
            EnsureLevelAsset(Phase0bLevelAssetPath, level =>
            {
                SetPrivate(level, "contentId", "level.phase0b_smoke_test");
                SetPrivate(level, "schemaVersion", 1);
                SetPrivate(level, "displayName", "Smoke Test (gentle S)");
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
            });
        }

        private static void EnsureSwitchbackLevelAsset()
        {
            EnsureLevelAsset(SwitchbackLevelAssetPath, level =>
            {
                SetPrivate(level, "contentId", "level.switchback");
                SetPrivate(level, "schemaVersion", 1);
                SetPrivate(level, "displayName", "Switchback (sharp Z)");
                SetPrivate(level, "waypoints", new List<Vector2>
                {
                    new(-9.0f,  3.5f),
                    new( 4.0f,  3.5f),
                    new( 4.0f,  0.5f),
                    new(-4.0f,  0.5f),
                    new(-4.0f, -3.0f),
                    new( 8.5f, -3.0f),
                });

                SetPrivate(level, "totalMarbles", 36);
                SetPrivate(level, "spawnIntervalSeconds", 0.25f);
                SetPrivate(level, "chainAdvanceSpeed", 1.45f);
                SetPrivate(level, "marbleSpacing", 0.58f);
                SetPrivate(level, "catchUpSpeedMultiplier", 3f);
                SetPrivate(level, "marbleRadius", 0.30f);

                SetPrivate(level, "shooterPosition", new Vector2(0f, -4.4f));
                SetPrivate(level, "shotVelocity", 19f);
                SetPrivate(level, "shooterCooldown", 0.09f);
                SetPrivate(level, "projectileRadius", 0.24f);

                SetPrivate(level, "matchThreshold", 3);

                SetPrivate(level, "cameraOrthographicSize", 5.6f);
                SetPrivate(level, "cameraCenter", new Vector2(0f, 0f));
                SetPrivate(level, "backgroundColor", new Color(0.07f, 0.05f, 0.10f, 1f));
            });
        }

        private static void EnsureLongSweepLevelAsset()
        {
            EnsureLevelAsset(LongSweepLevelAssetPath, level =>
            {
                SetPrivate(level, "contentId", "level.long_sweep");
                SetPrivate(level, "schemaVersion", 1);
                SetPrivate(level, "displayName", "Long Sweep (wide arc)");
                SetPrivate(level, "waypoints", new List<Vector2>
                {
                    new(-10.0f, -1.0f),
                    new( -9.5f,  2.0f),
                    new( -7.0f,  3.6f),
                    new( -2.0f,  4.0f),
                    new(  3.0f,  3.6f),
                    new(  7.0f,  2.0f),
                    new(  9.5f, -1.0f),
                    new(  9.0f, -3.0f),
                    new(  3.0f, -3.6f),
                    new( -3.0f, -3.6f),
                    new( -8.0f, -3.0f),
                });

                SetPrivate(level, "totalMarbles", 44);
                SetPrivate(level, "spawnIntervalSeconds", 0.28f);
                SetPrivate(level, "chainAdvanceSpeed", 0.95f);
                SetPrivate(level, "marbleSpacing", 0.60f);
                SetPrivate(level, "catchUpSpeedMultiplier", 3f);
                SetPrivate(level, "marbleRadius", 0.30f);

                SetPrivate(level, "shooterPosition", new Vector2(0f, 0f));
                SetPrivate(level, "shotVelocity", 17f);
                SetPrivate(level, "shooterCooldown", 0.10f);
                SetPrivate(level, "projectileRadius", 0.24f);

                SetPrivate(level, "matchThreshold", 3);

                SetPrivate(level, "cameraOrthographicSize", 6.0f);
                SetPrivate(level, "cameraCenter", new Vector2(0f, 0f));
                SetPrivate(level, "backgroundColor", new Color(0.04f, 0.08f, 0.10f, 1f));
            });
        }

        private static void EnsureLevelAsset(string assetPath, Action<LevelDefinition> configure)
        {
            EnsureAssetFolders(assetPath);
            var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[NovaSpark.Build] Existing level at {assetPath}");
                return;
            }

            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            configure(level);
            AssetDatabase.CreateAsset(level, assetPath);
            Debug.Log($"[NovaSpark.Build] Created level asset at {assetPath}");
        }

        // -------------------- scene --------------------

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
            Debug.Log(created
                ? $"[NovaSpark.Build] Created Bootstrap scene at {BootstrapScenePath}"
                : $"[NovaSpark.Build] Updated Bootstrap scene at {BootstrapScenePath}");
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

        // -------------------- build --------------------

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

        // -------------------- helpers --------------------

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
