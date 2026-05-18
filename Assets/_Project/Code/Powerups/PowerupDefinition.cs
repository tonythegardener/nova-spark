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

        [Header("Phase 0 placeholder parameters")]
        [Tooltip("Generic numeric tuning slot. Real fields per-power-up come later.")]
        [SerializeField] private float magnitude = 1f;

        public string ContentId => contentId;
        public int SchemaVersion => schemaVersion;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float Magnitude => magnitude;
    }
}
