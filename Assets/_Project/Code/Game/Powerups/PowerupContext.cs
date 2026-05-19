using NovaSpark.Game.Chain;
using NovaSpark.Game.Level;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Read-only handle to the gameplay-side objects an equipped PowerupEffect
    // needs to reach. Constructed by LevelController once a level is built and
    // handed to each effect on Equip.
    public sealed class PowerupContext
    {
        public LevelDefinition Level { get; }
        public MarbleChain Chain { get; }
        public Camera Camera { get; }
        public Cannon Cannon { get; }
        public Transform BarrelMuzzle { get; }

        public PowerupContext(
            LevelDefinition level,
            MarbleChain chain,
            Camera camera,
            Cannon cannon,
            Transform barrelMuzzle)
        {
            Level = level;
            Chain = chain;
            Camera = camera;
            Cannon = cannon;
            BarrelMuzzle = barrelMuzzle;
        }
    }
}
