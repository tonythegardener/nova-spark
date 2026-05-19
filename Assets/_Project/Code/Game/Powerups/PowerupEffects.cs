using NovaSpark.Game.Powerups.Effects;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Factory mapping the string effectKind on a PowerupDefinition to a concrete
    // PowerupEffect implementation. Add new effects here when authoring new
    // PowerupDefinitions. Unknown keys log a warning and return null so the
    // missing effect is visible but doesn't crash the game.
    public static class PowerupEffects
    {
        public static PowerupEffect Create(string effectKind)
        {
            switch (effectKind)
            {
                case "laser_sight":   return new LaserSightEffect();
                case "rapid_reload":  return new RapidReloadEffect();
                case "long_scope":    return new LongScopeEffect();
                default:
                    Debug.LogWarning($"[NovaSpark.Powerups] Unknown effectKind '{effectKind}' — no effect created.");
                    return null;
            }
        }
    }
}
