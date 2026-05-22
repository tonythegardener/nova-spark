using System.Collections.Generic;
using NovaSpark.Core;
using NovaSpark.Mods;
using UnityEngine;

namespace NovaSpark.Game.Level
{
    [CreateAssetMenu(
        fileName = "NewLevel",
        menuName = "Nova Spark/Content/Level Definition",
        order = 1)]
    public sealed class LevelDefinition : ScriptableObject, IContentDefinition
    {
        [Header("Identity")]
        [SerializeField] private string contentId = "level.unnamed";
        [SerializeField] private int schemaVersion = 1;

        [Header("Display")]
        [SerializeField] private string displayName = "Unnamed Level";

        [Header("Path (world-space waypoints, head moves from index 0 to last)")]
        [SerializeField] private List<Vector2> waypoints = new();

        [Header("Chain")]
        [Min(1)]
        [SerializeField] private int totalMarbles = 30;
        [Min(0.01f)]
        [SerializeField] private float spawnIntervalSeconds = 0.25f;
        [Min(0.01f)]
        [SerializeField] private float chainAdvanceSpeed = 1.4f;
        [Min(0.05f)]
        [SerializeField] private float marbleSpacing = 0.55f;
        [Tooltip("Multiplier applied to advance speed when marbles need to close a gap from a match.")]
        [Min(1f)]
        [SerializeField] private float catchUpSpeedMultiplier = 3f;
        [Min(0.05f)]
        [SerializeField] private float marbleRadius = 0.30f;

        [Header("Shooter")]
        [SerializeField] private Vector2 shooterPosition = new(0f, -3.5f);
        [Min(0.01f)]
        [SerializeField] private float shotVelocity = 18f;
        [Min(0.01f)]
        [SerializeField] private float shooterCooldown = 0.08f;
        [Min(0.05f)]
        [SerializeField] private float projectileRadius = 0.25f;

        [Header("Match Rules")]
        [Min(2)]
        [SerializeField] private int matchThreshold = 3;

        [Header("Camera")]
        [Min(1f)]
        [SerializeField] private float cameraOrthographicSize = 6.5f;
        [SerializeField] private Vector2 cameraCenter = Vector2.zero;
        [SerializeField] private Color backgroundColor = new(0.05f, 0.06f, 0.10f, 1f);

        [Header("Powerup Drops (in-level shooter pickups)")]
        [Tooltip("Seconds between drop spawn attempts. <= 0 uses spawner default.")]
        [SerializeField] private float dropSpawnIntervalSeconds = 22f;
        [Tooltip("Max concurrent uncollected drops on screen. <= 0 uses spawner default.")]
        [SerializeField] private int dropMaxConcurrent = 2;
        [Tooltip("Seconds each drop lingers before expiring uncollected. <= 0 uses spawner default.")]
        [SerializeField] private float dropLifetimeSeconds = 15f;

        [Header("Path-Type Marbles (special chain spawns)")]
        [Tooltip("Per-spawn chance (0..1) that a chain marble is a path-effect supply package drawn from the run pool's Path-kind subset.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pathMarbleSpawnChance = 0.05f;

        public string ContentId => contentId;
        public int SchemaVersion => schemaVersion;
        public string DisplayName => displayName;
        public IReadOnlyList<Vector2> Waypoints => waypoints;

        public int TotalMarbles => totalMarbles;
        public float SpawnIntervalSeconds => spawnIntervalSeconds;
        public float ChainAdvanceSpeed => chainAdvanceSpeed;
        public float MarbleSpacing => marbleSpacing;
        public float CatchUpSpeedMultiplier => catchUpSpeedMultiplier;
        public float MarbleRadius => marbleRadius;

        public Vector2 ShooterPosition => shooterPosition;
        public float ShotVelocity => shotVelocity;
        public float ShooterCooldown => shooterCooldown;
        public float ProjectileRadius => projectileRadius;

        public int MatchThreshold => matchThreshold;

        public float CameraOrthographicSize => cameraOrthographicSize;
        public Vector2 CameraCenter => cameraCenter;
        public Color BackgroundColor => backgroundColor;

        public float DropSpawnIntervalSeconds => dropSpawnIntervalSeconds;
        public int DropMaxConcurrent => dropMaxConcurrent;
        public float DropLifetimeSeconds => dropLifetimeSeconds;

        public float PathMarbleSpawnChance => pathMarbleSpawnChance;

        public MarbleColor[] AllowedColors => MarbleColors.All;
    }
}
