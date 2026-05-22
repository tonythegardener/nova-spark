using System.Collections.Generic;
using NovaSpark.Powerups;
using UnityEngine;

namespace NovaSpark.Run
{
    // Static singleton holding the current run's state. Survives scene reloads
    // automatically (static class); resets when the app quits.
    //
    // Two pools:
    //   - metaPool: every powerup the player has unlocked at meta-progression
    //     level. For Phase 1 this is just "every registered PowerupDefinition";
    //     real meta unlocks land in Phase 4.
    //   - runPool: subset of metaPool active in THIS run. Drops draw from here.
    //     Drafts between levels add new items from metaPool into runPool.
    //
    // Phase 1 starter rule (Tony 2026-05-21): on StartNewRun, runPool is seeded
    // with the full metaPool so drops can happen from level 1. Drafts will only
    // become meaningful once metaPool > runPool (meta unlocks in Phase 4).
    public static class RunState
    {
        private static readonly List<PowerupDefinition> _metaPool = new();
        private static readonly List<PowerupDefinition> _runPool = new();

        public static IReadOnlyList<PowerupDefinition> MetaPool => _metaPool;
        public static IReadOnlyList<PowerupDefinition> RunPool => _runPool;
        public static bool RunActive { get; private set; }
        public static int LevelsWon { get; private set; }
        public static string CurrentLevelContentId { get; private set; }

        public static void InitializeMetaPool(IEnumerable<PowerupDefinition> all)
        {
            _metaPool.Clear();
            if (all == null) return;
            foreach (var def in all)
            {
                if (def != null) _metaPool.Add(def);
            }
        }

        public static void StartNewRun()
        {
            _runPool.Clear();
            _runPool.AddRange(_metaPool); // Phase 1 starter rule
            LevelsWon = 0;
            CurrentLevelContentId = null;
            RunActive = true;
        }

        public static void EndRun()
        {
            _runPool.Clear();
            LevelsWon = 0;
            CurrentLevelContentId = null;
            RunActive = false;
        }

        // Called by LevelController as the player enters a level. Ensures a run
        // is active and tags which level we're on.
        public static void EnterLevel(string levelContentId)
        {
            if (!RunActive) StartNewRun();
            CurrentLevelContentId = levelContentId;
        }

        public static void OnLevelWon() => LevelsWon++;

        // Single-loss = run end (Tony's call).
        public static void OnLevelLost() => EndRun();

        public static void AddToRunPool(PowerupDefinition def)
        {
            if (def == null) return;
            if (_runPool.Contains(def)) return;
            _runPool.Add(def);
        }

        // Returns up to `count` random PowerupDefinitions from metaPool not yet
        // in runPool. Used to populate the between-level draft overlay.
        public static List<PowerupDefinition> SampleDraftOptions(int count)
        {
            var available = new List<PowerupDefinition>();
            foreach (var def in _metaPool)
            {
                if (!_runPool.Contains(def)) available.Add(def);
            }
            // Fisher-Yates shuffle
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }
            int take = Mathf.Min(count, available.Count);
            return available.GetRange(0, take);
        }

        // Returns a random powerup from runPool, or null if pool is empty.
        public static PowerupDefinition PickRandomFromRunPool()
        {
            if (_runPool.Count == 0) return null;
            return _runPool[Random.Range(0, _runPool.Count)];
        }

        // Drops can only spawn Shooter-kind powerups — Path-kind effects don't
        // equip on the cannon (they apply to the chain). Used by DropSpawner.
        public static PowerupDefinition PickRandomShooterFromRunPool() => PickRandomOfKind(PowerupKind.Shooter);
        public static PowerupDefinition PickRandomPathFromRunPool() => PickRandomOfKind(PowerupKind.Path);

        private static PowerupDefinition PickRandomOfKind(PowerupKind kind)
        {
            int count = 0;
            for (int i = 0; i < _runPool.Count; i++)
            {
                if (_runPool[i] != null && _runPool[i].Kind == kind) count++;
            }
            if (count == 0) return null;
            int target = Random.Range(0, count);
            int seen = 0;
            for (int i = 0; i < _runPool.Count; i++)
            {
                if (_runPool[i] != null && _runPool[i].Kind == kind)
                {
                    if (seen == target) return _runPool[i];
                    seen++;
                }
            }
            return null;
        }
    }
}
