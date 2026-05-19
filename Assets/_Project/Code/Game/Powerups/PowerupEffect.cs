using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Powerups;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Hook-based powerup effect base class. Each equipped powerup creates one
    // instance whose virtual methods the Cannon dispatches at appropriate times.
    // Effects own any visuals/GameObjects they spawn — they MUST clean up in
    // OnUnequip().
    public abstract class PowerupEffect
    {
        public PowerupDefinition Definition { get; private set; }
        protected PowerupContext Context { get; private set; }

        public void Equip(PowerupDefinition def, PowerupContext ctx)
        {
            Definition = def;
            Context = ctx;
            OnEquip();
        }

        public void Unequip()
        {
            OnUnequip();
            Context = null;
            Definition = null;
        }

        // --- override these in subclasses as needed; defaults are no-ops ---

        protected virtual void OnEquip() { }
        protected virtual void OnUnequip() { }

        /// <summary>Called once per Cannon Update.</summary>
        public virtual void OnUpdate(float dt) { }

        /// <summary>Called each Update after aim is computed. Provides barrel origin and aim direction.</summary>
        public virtual void OnAim(Vector2 origin, Vector2 dir) { }

        /// <summary>Multiplicative on the level's base ShooterCooldown. Default: identity.</summary>
        public virtual float ModifyCooldown(float baseCooldown) => baseCooldown;

        /// <summary>Called just before a projectile spawns. May mutate color or velocity via the Cannon-managed state.</summary>
        public virtual void OnBeforeFire(Vector2 origin, Vector2 dir, ref MarbleColor color) { }

        /// <summary>Called immediately after a projectile is spawned and launched.</summary>
        public virtual void OnProjectileLaunched(GameObject projectile, MarbleColor color, Vector2 velocity) { }

        /// <summary>Called when a projectile hits a chain marble (after the chain has handled insertion).</summary>
        public virtual void OnProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity) { }

        /// <summary>Extra preview slots beyond the default 1-next-preview the cannon shows. Max across equipped effects wins.</summary>
        public virtual int ExtraVisibleQueue => 0;
    }
}
