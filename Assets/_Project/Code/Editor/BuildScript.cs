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
        private const string RapidReloadAssetPath = "Assets/_Project/Content/Resources/Powerups/RapidReload.asset";
        private const string LongScopeAssetPath = "Assets/_Project/Content/Resources/Powerups/LongScope.asset";
        private const string MimicShotAssetPath = "Assets/_Project/Content/Resources/Powerups/MimicShot.asset";
        private const string ExplosiveRoundAssetPath = "Assets/_Project/Content/Resources/Powerups/ExplosiveRound.asset";
        private const string VaporizerAssetPath = "Assets/_Project/Content/Resources/Powerups/Vaporizer.asset";
        private const string KnockoutAssetPath = "Assets/_Project/Content/Resources/Powerups/KnockoutRound.asset";
        private const string ChronoFieldAssetPath = "Assets/_Project/Content/Resources/Powerups/ChronoField.asset";
        private const string AutoShotAssetPath = "Assets/_Project/Content/Resources/Powerups/AutoShot.asset";
        private const string Phase0bLevelAssetPath = "Assets/_Project/Content/Resources/Levels/Phase0bSmokeTest.asset";
        private const string SwitchbackLevelAssetPath = "Assets/_Project/Content/Resources/Levels/Switchback.asset";
        private const string LongSweepLevelAssetPath = "Assets/_Project/Content/Resources/Levels/LongSweep.asset";

        [MenuItem("Nova Spark/Prepare Phase 1b Assets")]
        public static void PrepareAssets()
        {
            EnsureLaserSightAsset();
            // Rapid Reload deferred (Tony 2026-05-22): base fire rate is
            // already click-rate-limited so halving cooldown is imperceptible.
            // Keep RapidReloadEffect + EnsureRapidReloadAsset around for a
            // future redesign; just don't ship the asset in Phase 1.
            // EnsureRapidReloadAsset();
            EnsureLongScopeAsset();
            EnsureMimicShotAsset();
            EnsureExplosiveRoundAsset();
            EnsureVaporizerAsset();
            EnsureKnockoutAsset();
            EnsureChronoFieldAsset();
            EnsureAutoShotAsset();
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

        [MenuItem("Nova Spark/Build Windows (Phase 1b)")]
        public static void BuildWindowsPhase1b()
        {
            PrepareAssets();
            BuildWindowsTo("Builds/Phase1b/NovaSpark.exe");
        }

        public static void BuildWindowsPhase0aCli() => RunCli(BuildWindowsPhase0a);
        public static void BuildWindowsPhase0bCli() => RunCli(BuildWindowsPhase0b);
        public static void BuildWindowsPhase1aCli() => RunCli(BuildWindowsPhase1a);
        public static void BuildWindowsPhase1bCli() => RunCli(BuildWindowsPhase1b);

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
            EnsurePowerupAsset(LaserSightAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.laser_sight");
                SetPrivate(so, "displayName", "Laser Sight");
                SetPrivate(so, "description", "Projects a trajectory line from your cannon along the aim direction.");
                SetPrivate(so, "effectKind", "laser_sight");
                SetPrivate(so, "magnitude", 18f);   // line length in world units
                SetPrivate(so, "durationKind", PowerupDurationKind.Seconds);
                SetPrivate(so, "durationParam", 30f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureRapidReloadAsset()
        {
            EnsurePowerupAsset(RapidReloadAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.rapid_reload");
                SetPrivate(so, "displayName", "Rapid Reload");
                SetPrivate(so, "description", "Halves the cannon's cooldown between shots.");
                SetPrivate(so, "effectKind", "rapid_reload");
                SetPrivate(so, "magnitude", 0.5f);
                SetPrivate(so, "durationKind", PowerupDurationKind.Seconds);
                SetPrivate(so, "durationParam", 20f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureLongScopeAsset()
        {
            EnsurePowerupAsset(LongScopeAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.long_scope");
                SetPrivate(so, "displayName", "Long Scope");
                SetPrivate(so, "description", "Reveals additional upcoming shots in the queue.");
                SetPrivate(so, "effectKind", "long_scope");
                SetPrivate(so, "magnitude", 3f);
                SetPrivate(so, "durationKind", PowerupDurationKind.PerLevel);
                SetPrivate(so, "durationParam", 1f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureMimicShotAsset()
        {
            EnsurePowerupAsset(MimicShotAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.mimic_shot");
                SetPrivate(so, "displayName", "Mimic Shot");
                SetPrivate(so, "description", "Your next shots take on the color of the marble they strike before settling.");
                SetPrivate(so, "effectKind", "mimic_shot");
                SetPrivate(so, "magnitude", 1f);
                SetPrivate(so, "durationKind", PowerupDurationKind.PerShots);
                SetPrivate(so, "durationParam", 5f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureExplosiveRoundAsset()
        {
            EnsurePowerupAsset(ExplosiveRoundAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.explosive_round");
                SetPrivate(so, "displayName", "Explosive Round");
                SetPrivate(so, "description", "Detonates on impact, destroying nearby marbles regardless of color.");
                SetPrivate(so, "effectKind", "explosive_round");
                SetPrivate(so, "magnitude", 1.2f);  // explosion radius in world units
                SetPrivate(so, "durationKind", PowerupDurationKind.PerShots);
                SetPrivate(so, "durationParam", 3f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureVaporizerAsset()
        {
            EnsurePowerupAsset(VaporizerAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.vaporizer");
                SetPrivate(so, "displayName", "Vaporizer");
                SetPrivate(so, "description", "Disintegrates the single marble it strikes — no insertion, no radius.");
                SetPrivate(so, "effectKind", "vaporizer");
                SetPrivate(so, "magnitude", 1f);
                SetPrivate(so, "durationKind", PowerupDurationKind.PerShots);
                SetPrivate(so, "durationParam", 5f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureKnockoutAsset()
        {
            EnsurePowerupAsset(KnockoutAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.knockout");
                SetPrivate(so, "displayName", "Knockout Round");
                SetPrivate(so, "description", "Dislodges the marble it hits and sends it flying onward — it settles where it next touches, or off-screen.");
                SetPrivate(so, "effectKind", "knockout");
                SetPrivate(so, "magnitude", 1f);
                SetPrivate(so, "durationKind", PowerupDurationKind.PerShots);
                SetPrivate(so, "durationParam", 3f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureChronoFieldAsset()
        {
            EnsurePowerupAsset(ChronoFieldAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.chrono_field");
                SetPrivate(so, "displayName", "Chrono Field");
                SetPrivate(so, "description", "Hit the supply marble to slow the enemy chain for a short time. Same effect re-applied extends duration.");
                SetPrivate(so, "effectKind", "chrono_field");
                SetPrivate(so, "magnitude", 0.3f);       // chain speed multiplier
                SetPrivate(so, "durationKind", PowerupDurationKind.Seconds);
                SetPrivate(so, "durationParam", 5f);
                SetPrivate(so, "kind", PowerupKind.Path);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        private static void EnsureAutoShotAsset()
        {
            EnsurePowerupAsset(AutoShotAssetPath, so =>
            {
                SetPrivate(so, "contentId", "powerup.auto_shot");
                SetPrivate(so, "displayName", "Auto Shot");
                SetPrivate(so, "description", "Cannon takes over briefly: auto-aims at the nearest target and fires a short burst.");
                SetPrivate(so, "effectKind", "auto_shot");
                SetPrivate(so, "magnitude", 0.25f);      // seconds between auto-shots
                SetPrivate(so, "durationKind", PowerupDurationKind.PerShots);
                SetPrivate(so, "durationParam", 3f);
                SetPrivate(so, "kind", PowerupKind.Shooter);
                SetPrivate(so, "schemaVersion", 1);
            });
        }

        // Upserts a PowerupDefinition: creates the asset if missing, or applies
        // configure() to the existing instance to keep effectKind / magnitude up
        // to date with code changes. EditorUtility.SetDirty + SaveAssets persist.
        private static void EnsurePowerupAsset(string assetPath, Action<PowerupDefinition> configure)
        {
            EnsureAssetFolders(assetPath);
            var existing = AssetDatabase.LoadAssetAtPath<PowerupDefinition>(assetPath);
            if (existing != null)
            {
                configure(existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[NovaSpark.Build] Refreshed powerup asset {assetPath}");
                return;
            }

            var so = ScriptableObject.CreateInstance<PowerupDefinition>();
            configure(so);
            AssetDatabase.CreateAsset(so, assetPath);
            Debug.Log($"[NovaSpark.Build] Created powerup asset {assetPath}");
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

                SetPrivate(level, "shooterPosition", new Vector2(0f, -5.0f));
                SetPrivate(level, "shotVelocity", 18f);
                SetPrivate(level, "shooterCooldown", 0.10f);
                SetPrivate(level, "projectileRadius", 0.25f);

                SetPrivate(level, "matchThreshold", 3);

                SetPrivate(level, "cameraOrthographicSize", 6.0f);
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
