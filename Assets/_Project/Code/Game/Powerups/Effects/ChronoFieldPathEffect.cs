using NovaSpark.Game.Chain;
using NovaSpark.Powerups;

namespace NovaSpark.Game.Powerups.Effects
{
    // Chrono Field: multiplies the chain's forward advance speed by Magnitude
    // for DurationParam seconds. Same effectKind re-applied to chain just
    // extends remaining duration (MarbleChain.ApplySpeedModifier handles that).
    public sealed class ChronoFieldPathEffect : IPathEffect
    {
        public void Apply(MarbleChain chain, PowerupDefinition def)
        {
            float multiplier = def.Magnitude > 0.01f ? def.Magnitude : 0.3f;
            float duration = def.DurationParam > 0.1f ? def.DurationParam : 5f;
            chain.ApplySpeedModifier(def.EffectKind, def, multiplier, duration);
        }
    }
}
