using NovaSpark.Game.Powerups.Effects;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Factory mapping effectKind -> IPathEffect, mirror of PowerupEffects for
    // path-type definitions. Used when a player shot strikes a special chain
    // marble — Cannon hands the hit to MarbleChain which resolves the
    // PowerupDefinition through this factory.
    public static class PathEffects
    {
        public static IPathEffect Create(string effectKind)
        {
            switch (effectKind)
            {
                case "chrono_field": return new ChronoFieldPathEffect();
                default:
                    Debug.LogWarning($"[NovaSpark.PathEffects] Unknown path effectKind '{effectKind}'.");
                    return null;
            }
        }
    }
}
