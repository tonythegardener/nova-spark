using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Picks which marble Auto Shot aims at when it triggers a shot. Pluggable
    // so we can swap strategies later (closest vs match-priority vs highest-
    // value). For Phase 1 only ClosestTargetStrategy ships; the interface is
    // here so adding new strategies is one-class-plus-factory-line.
    public interface IAutoShotStrategy
    {
        ChainMarble PickTarget(MarbleChain chain, Cannon cannon);
    }

    public sealed class ClosestTargetStrategy : IAutoShotStrategy
    {
        public ChainMarble PickTarget(MarbleChain chain, Cannon cannon)
        {
            if (chain == null || cannon == null) return null;
            Vector2 origin = cannon.transform.position;
            ChainMarble best = null;
            float bestDistSq = float.MaxValue;
            var marbles = chain.Marbles;
            for (int i = 0; i < marbles.Count; i++)
            {
                var m = marbles[i];
                if (m == null) continue;
                float d = ((Vector2)m.transform.position - origin).sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = m;
                }
            }
            return best;
        }
    }
}
