using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Magnitude = extra preview slots beyond the default 1-next preview.
    // E.g., magnitude=3 => total visible preview = 4 (so player sees next 4
    // chambered shots after the current one).
    public sealed class LongScopeEffect : PowerupEffect
    {
        public override int ExtraVisibleQueue
        {
            get
            {
                if (Definition == null) return 0;
                return Mathf.Max(0, Mathf.RoundToInt(Definition.Magnitude));
            }
        }
    }
}
