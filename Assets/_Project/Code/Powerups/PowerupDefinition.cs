using NovaSpark.Mods;
using UnityEngine;

namespace NovaSpark.Powerups
{
    public enum PowerupDurationKind
    {
        Permanent = 0,   // never expires (use sparingly; meta-progression items)
        PerLevel = 1,    // lasts N levels then unequips at level end
        PerShots = 2,    // lasts N projectile firings
        OneUse = 3,      // unequips after first activation (active powerups)
        Seconds = 4,     // expires after N seconds of in-level time
    }

    // Categorizes the powerup for the acquisition + equip constraint system.
    //   Shooter — modifies cannon, aim, projectile, queue. Max 1 equipped at a time.
    //   Path    — modifies chain motion / match rules / chain state. Stacks freely.
    public enum PowerupKind
    {
        Shooter = 0,
        Path = 1,
    }

    [CreateAssetMenu(
        fileName = "NewPowerup",
        menuName = "Nova Spark/Content/Powerup Definition",
        order = 0)]
    public sealed class PowerupDefinition : ScriptableObject, IContentDefinition
    {
        [Header("Identity")]
        [SerializeField] private string contentId = "powerup.unnamed";
        [SerializeField] private int schemaVersion = 1;

        [Header("Display")]
        [SerializeField] private string displayName = "Unnamed Powerup";
        [SerializeField, TextArea(2, 6)] private string description = "";
        [SerializeField] private Sprite icon;

        [Header("Effect")]
        [Tooltip("String key resolved by NovaSpark.Game.Powerups.PowerupEffects.Create. " +
                 "Examples: laser_sight, rapid_reload, long_scope, mimic_shot, explosive_round, vaporizer, knockout.")]
        [SerializeField] private string effectKind = "";
        [Tooltip("Shooter modifies the cannon (max 1 equipped). Path modifies the chain (stacks).")]
        [SerializeField] private PowerupKind kind = PowerupKind.Shooter;
        [Tooltip("Primary numeric parameter for the effect. Interpretation depends on effectKind.")]
        [SerializeField] private float magnitude = 1f;
        [Tooltip("Secondary numeric parameter. Effects that need only one param ignore this.")]
        [SerializeField] private float secondaryMagnitude = 0f;

        [Header("Duration")]
        [SerializeField] private PowerupDurationKind durationKind = PowerupDurationKind.Permanent;
        [Tooltip("Meaning depends on durationKind: PerLevel = N levels, PerShots = N firings, " +
                 "Seconds = N seconds (uses ceiling), OneUse/Permanent = ignored.")]
        [SerializeField] private float durationParam = 1f;

        public string ContentId => contentId;
        public int SchemaVersion => schemaVersion;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public string EffectKind => effectKind;
        public PowerupKind Kind => kind;
        public float Magnitude => magnitude;
        public float SecondaryMagnitude => secondaryMagnitude;
        public PowerupDurationKind DurationKind => durationKind;
        public float DurationParam => durationParam;
    }
}
