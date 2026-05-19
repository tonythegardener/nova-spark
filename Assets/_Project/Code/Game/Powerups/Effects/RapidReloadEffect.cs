using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Magnitude = multiplier on the level's base ShooterCooldown (default 0.5 = 2x fire rate).
    public sealed class RapidReloadEffect : PowerupEffect
    {
        public override float ModifyCooldown(float baseCooldown)
        {
            float m = Definition != null && Definition.Magnitude > 0.001f ? Definition.Magnitude : 0.5f;
            return baseCooldown * m;
        }
    }
}
