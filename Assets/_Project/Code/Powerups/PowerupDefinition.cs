using NovaSpark.Mods;
using UnityEngine;

namespace NovaSpark.Powerups
{
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
                 "Examples: laser_sight, rapid_reload, long_scope.")]
        [SerializeField] private string effectKind = "";
        [Tooltip("Primary numeric parameter for the effect. Interpretation depends on effectKind " +
                 "(e.g., line length in units for laser_sight, cooldown multiplier for rapid_reload, " +
                 "extra preview slots for long_scope).")]
        [SerializeField] private float magnitude = 1f;
        [Tooltip("Secondary numeric parameter. Effects that need only one param ignore this.")]
        [SerializeField] private float secondaryMagnitude = 0f;

        public string ContentId => contentId;
        public int SchemaVersion => schemaVersion;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public string EffectKind => effectKind;
        public float Magnitude => magnitude;
        public float SecondaryMagnitude => secondaryMagnitude;
    }
}
