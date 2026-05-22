using NovaSpark.Game.Chain;
using NovaSpark.Powerups;

namespace NovaSpark.Game.Powerups
{
    // Path-type powerups don't equip on the cannon — they apply effects to the
    // chain when a path-marble is hit. Implementations are stateless; the
    // active-effect bookkeeping lives on MarbleChain (extending duration on
    // re-application is the chain's responsibility).
    public interface IPathEffect
    {
        void Apply(MarbleChain chain, PowerupDefinition def);
    }
}
