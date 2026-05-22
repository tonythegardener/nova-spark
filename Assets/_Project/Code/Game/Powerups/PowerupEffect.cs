using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Powerups;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Hook-based powerup effect base class. Each equipped powerup creates one
    // instance whose virtual methods the Cannon dispatches at appropriate times.
    // Effects own any visuals/GameObjects they spawn — they MUST clean up in
    // OnUnequip(). Effects also self-track their duration (PerShots / Seconds /
    // OneUse) and call Expire() when they should be removed.
    public abstract class PowerupEffect
    {
        public PowerupDefinition Definition { get; private set; }
        protected PowerupContext Context { get; private set; }

        // Duration bookkeeping. Initialized from Definition.DurationKind/Param on Equip.
        private int _shotsRemaining;
        private float _secondsRemaining;
        private bool _expired;

        public bool IsExpired => _expired;

        // Exposed so the powerup HUD can render the current countdown.
        public int ShotsRemaining => _shotsRemaining;
        public float SecondsRemaining => _secondsRemaining;

        public void Equip(PowerupDefinition def, PowerupContext ctx)
        {
            Definition = def;
            Context = ctx;
            InitializeDuration();
            OnEquip();
        }

        public void Unequip()
        {
            OnUnequip();
            Context = null;
            Definition = null;
        }

        private void InitializeDuration()
        {
            switch (Definition.DurationKind)
            {
                case PowerupDurationKind.PerShots:
                    _shotsRemaining = Mathf.Max(1, Mathf.RoundToInt(Definition.DurationParam));
                    break;
                case PowerupDurationKind.Seconds:
                    _secondsRemaining = Mathf.Max(0.05f, Definition.DurationParam);
                    break;
                case PowerupDurationKind.OneUse:
                    _shotsRemaining = 1;
                    break;
                case PowerupDurationKind.PerLevel:
                case PowerupDurationKind.Permanent:
                default:
                    break;
            }
        }

        // Called by Cannon each Update tick. Drives Seconds expiry.
        public void TickLifetime(float dt)
        {
            if (_expired) return;
            if (Definition.DurationKind == PowerupDurationKind.Seconds)
            {
                _secondsRemaining -= dt;
                if (_secondsRemaining <= 0f) Expire();
            }
            OnUpdate(dt);
        }

        // Called by Cannon after a successful firing (whether or not THIS effect
        // affected the shot — the cannon's `firing` itself is what counts).
        // PerShots and OneUse decrement here.
        public void NotifyShotFired()
        {
            if (_expired) return;
            if (Definition.DurationKind == PowerupDurationKind.PerShots
                || Definition.DurationKind == PowerupDurationKind.OneUse)
            {
                _shotsRemaining--;
                if (_shotsRemaining <= 0) Expire();
            }
        }

        // Triggers expiry. Effect will be removed from the Cannon's equipped list
        // before the next frame.
        protected void Expire()
        {
            if (_expired) return;
            _expired = true;
            Debug.Log($"[NovaSpark.Powerups] Effect '{Definition.ContentId}' expired ({Definition.DurationKind}).");
        }

        // --- override these in subclasses as needed; defaults are no-ops ---

        protected virtual void OnEquip() { }
        protected virtual void OnUnequip() { }
        public virtual void OnUpdate(float dt) { }

        /// <summary>Called each Update after aim is computed.</summary>
        public virtual void OnAim(Vector2 origin, Vector2 dir) { }

        /// <summary>Multiplicative on the level's base ShooterCooldown. Default: identity.</summary>
        public virtual float ModifyCooldown(float baseCooldown) => baseCooldown;

        /// <summary>Called just before a projectile spawns. May mutate color via ref.</summary>
        public virtual void OnBeforeFire(Vector2 origin, Vector2 dir, ref MarbleColor color) { }

        /// <summary>Called immediately after a projectile is spawned and launched.</summary>
        public virtual void OnProjectileLaunched(GameObject projectile, MarbleColor color, Vector2 velocity) { }

        /// <summary>
        /// Called when a projectile hits a chain marble. Returns true if this
        /// effect fully handled the hit (default insert is then skipped). Only the
        /// FIRST effect that returns true wins; later handlers are skipped.
        /// </summary>
        public virtual bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity) => false;

        /// <summary>Modify the color of the to-be-inserted marble. Default: pass through.</summary>
        public virtual MarbleColor ModifyInsertColor(ChainMarble hit, MarbleColor color) => color;

        /// <summary>Notification AFTER the projectile-hit has been resolved (insert, destroy, dislodge, etc.).</summary>
        public virtual void OnProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity) { }

        /// <summary>Extra preview slots beyond the default 1-next-preview. Max across equipped effects wins.</summary>
        public virtual int ExtraVisibleQueue => 0;

        /// <summary>
        /// Visual telegraph: if non-null, the Cannon tints its barrel stem with
        /// this color while this effect is equipped. First non-null effect in
        /// equip order wins. Used to signal Shooter-mode powerups that don't
        /// otherwise change the chambered marble (e.g., Vaporizer).
        /// </summary>
        public virtual Color? BarrelTint => null;

        /// <summary>
        /// If non-null, the Cannon swaps the chambered marble (and visible
        /// previews) to this sprite while the effect is equipped. Use to
        /// signal that the loaded shot is NOT an ordinary colored marble —
        /// e.g., Vaporizer (destroys on hit) and Mimic Shot (color doesn't
        /// matter). First non-null effect in equip order wins.
        /// </summary>
        public virtual Sprite ChamberOverrideSprite => null;

        /// <summary>
        /// Companion to ChamberOverrideSprite: tints the chambered + previews
        /// while the effect is equipped. Falls back to the chambered marble's
        /// real color if both this and ChamberOverrideSprite are null.
        /// </summary>
        public virtual Color? ChamberOverrideColor => null;
    }
}
