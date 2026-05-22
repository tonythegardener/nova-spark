using System.Collections.Generic;
using NovaSpark.Core;
using NovaSpark.Game.Level;
using UnityEngine;

namespace NovaSpark.Game.Chain
{
    public sealed class MarbleChain : MonoBehaviour
    {
        private readonly List<ChainMarble> _marbles = new();
        private LevelDefinition _level;
        private PathSampler _path;
        private Sprite _sprite;

        private int _spawnedCount;
        private float _spawnTimer;
        private bool _spawnComplete;

        // Match detection is event-driven (Phase 1a.3):
        // - TryInsertAt: directly runs a local match check around the inserted marble.
        // - DestroyRunAround: when it destroys a run flanked by surviving marbles,
        //   it records a "pending seam" — the head-side surviving marble. Each Tick
        //   we check pending seams; once the gap from a destruction has closed and
        //   the two flanking marbles touch, we re-run a local match check there.
        //   That preserves Zuma-style cascades while never auto-eliminating
        //   spawn-time runs of >= MatchThreshold same-color marbles.
        private readonly List<ChainMarble> _pendingSeams = new();

        public IReadOnlyList<ChainMarble> Marbles => _marbles;
        public bool SpawnComplete => _spawnComplete;
        public bool ReachedBastion { get; private set; }
        public System.Action OnMatchResolved;
        public System.Action OnReachedBastion;
        public System.Action OnCleared;
        public System.Action<Vector2, MarbleColor> OnMarbleDestroyed;
        // Fired after any change to the marble list (spawn, insert, destroy).
        // Subscribers can re-query ActiveColors() to keep palettes in sync.
        public System.Action OnChainChanged;

        // Pre-allocated set reused by ActiveColors to avoid per-frame garbage.
        private readonly HashSet<MarbleColor> _activeColorsScratch = new();

        public IReadOnlyCollection<MarbleColor> ActiveColors()
        {
            _activeColorsScratch.Clear();
            for (int i = 0; i < _marbles.Count; i++)
            {
                _activeColorsScratch.Add(_marbles[i].Color);
            }
            return _activeColorsScratch;
        }

        public void Configure(LevelDefinition level, PathSampler path, Sprite sprite)
        {
            _level = level;
            _path = path;
            _sprite = sprite;
        }

        public void Tick(float dt)
        {
            HandleSpawning(dt);
            AdvanceChain(dt);
            UpdatePositions();
            ProcessPendingSeams();
            CheckEndConditions();
        }

        // -------------------- spawning --------------------

        private void HandleSpawning(float dt)
        {
            if (_spawnComplete) return;
            _spawnTimer += dt;
            while (!_spawnComplete && _spawnTimer >= _level.SpawnIntervalSeconds)
            {
                _spawnTimer -= _level.SpawnIntervalSeconds;
                SpawnNext();
            }
        }

        private void SpawnNext()
        {
            float arc;
            if (_marbles.Count == 0)
            {
                arc = 0f;
            }
            else
            {
                arc = _marbles[_marbles.Count - 1].ArcLength - _level.MarbleSpacing;
            }
            var color = _level.AllowedColors[Random.Range(0, _level.AllowedColors.Length)];
            var marble = CreateMarble(color, arc, sortingOrder: _spawnedCount);
            _marbles.Add(marble);
            _spawnedCount++;
            if (_spawnedCount >= _level.TotalMarbles) _spawnComplete = true;
            OnChainChanged?.Invoke();
        }

        private ChainMarble CreateMarble(MarbleColor color, float arc, int sortingOrder)
        {
            var go = new GameObject($"Marble_{sortingOrder}_{color}");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var marble = go.AddComponent<ChainMarble>();
            marble.Initialize(color, arc, _level.MarbleRadius, _sprite, sortingOrder);
            marble.transform.position = _path.SampleByDistance(arc);
            return marble;
        }

        // -------------------- advance --------------------

        // Phase 1a model: spawn pressure drives the chain from the TAIL.
        // The tail (last spawned, lowest arc) advances at ChainAdvanceSpeed.
        // Each marble closer to the head has a natural arc = trailing.arc + spacing.
        // - If current <= natural, snap forward (spawn pressure pushing). Clears the
        //   frozen flag because the gap has closed.
        // - If current > natural and the marble is FROZEN (Phase 1b.2b rule:
        //   destruction left flanks that wouldn't form a match), HOLD position so
        //   the gap stays open and the back catches up.
        // - Otherwise retreat at CatchUpSpeedMultiplier * speed (same-color flanks
        //   closing into a match).
        private void AdvanceChain(float dt)
        {
            if (_marbles.Count == 0) return;

            int tail = _marbles.Count - 1;
            _marbles[tail].ArcLength += _level.ChainAdvanceSpeed * dt;

            float retreatStep = _level.ChainAdvanceSpeed * _level.CatchUpSpeedMultiplier * dt;

            for (int i = tail - 1; i >= 0; i--)
            {
                float natural = _marbles[i + 1].ArcLength + _level.MarbleSpacing;
                float current = _marbles[i].ArcLength;
                if (current <= natural)
                {
                    _marbles[i].ArcLength = natural;
                    _marbles[i].FrozenAfterGap = false;
                }
                else if (_marbles[i].FrozenAfterGap)
                {
                    // Hold position. Back will catch up; freeze clears when it does.
                }
                else
                {
                    _marbles[i].ArcLength = Mathf.Max(natural, current - retreatStep);
                }
            }
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < _marbles.Count; i++)
            {
                var m = _marbles[i];
                m.transform.position = _path.SampleByDistance(m.ArcLength);
            }
        }

        // -------------------- insertion + match --------------------

        public bool TryInsertAt(ChainMarble hit, Vector2 projectileVelocity, MarbleColor newColor)
        {
            int k = _marbles.IndexOf(hit);
            if (k < 0) return false;

            var tangent = _path.SampleTangent(hit.ArcLength);
            float dot = Vector2.Dot(projectileVelocity.normalized, tangent);
            int insertIndex = dot > 0f ? k : k + 1;

            float newArc;
            if (insertIndex == 0)
            {
                newArc = _marbles[0].ArcLength + _level.MarbleSpacing;
            }
            else if (insertIndex >= _marbles.Count)
            {
                newArc = _marbles[_marbles.Count - 1].ArcLength - _level.MarbleSpacing;
            }
            else
            {
                newArc = (_marbles[insertIndex - 1].ArcLength + _marbles[insertIndex].ArcLength) * 0.5f;
            }

            var inserted = CreateMarble(newColor, newArc, sortingOrder: _spawnedCount);
            _spawnedCount++;
            _marbles.Insert(insertIndex, inserted);

            for (int i = insertIndex + 1; i < _marbles.Count; i++)
            {
                float natural = _marbles[i - 1].ArcLength - _level.MarbleSpacing;
                if (_marbles[i].ArcLength > natural)
                    _marbles[i].ArcLength = natural;
            }

            OnChainChanged?.Invoke();

            // Local match check at the insertion site. Pure spawn never triggers
            // matches; only insertions do.
            ResolveMatchAround(inserted);
            return true;
        }

        // Looks for a contiguous same-color run including `origin`. Destroys it
        // if length >= MatchThreshold, registers a pending seam, and fires events.
        private void ResolveMatchAround(ChainMarble origin)
        {
            if (origin == null) return;
            int k = _marbles.IndexOf(origin);
            if (k < 0) return;

            var color = origin.Color;

            int left = k;
            while (left > 0
                   && _marbles[left - 1].Color == color
                   && AreContiguous(_marbles[left - 1], _marbles[left]))
            {
                left--;
            }
            int right = k;
            while (right < _marbles.Count - 1
                   && _marbles[right + 1].Color == color
                   && AreContiguous(_marbles[right], _marbles[right + 1]))
            {
                right++;
            }

            int runLength = right - left + 1;
            if (runLength < _level.MatchThreshold) return;

            DestroyRange(left, right);
        }

        // Destroys marbles [L..R] inclusive. If both flanks survive, records the
        // head-side surviving marble as a pending seam so a future Tick can
        // recheck once the gap closes (Zuma cascade behavior).
        // Phase 1b.2b: also decides whether to FREEZE the front segment based on
        // whether retreating would form a same-color run >= MatchThreshold.
        private void DestroyRange(int L, int R)
        {
            ChainMarble headSideSurvivor = L > 0 ? _marbles[L - 1] : null;
            bool tailSideSurvivorExists = R < _marbles.Count - 1;
            ChainMarble tailSideSurvivor = tailSideSurvivorExists ? _marbles[R + 1] : null;

            bool shouldFreeze = ShouldFreezeFrontAfterDestruction(L, R, headSideSurvivor, tailSideSurvivor);

            for (int i = R; i >= L; i--)
            {
                var m = _marbles[i];
                OnMarbleDestroyed?.Invoke(m.transform.position, m.Color);
                Destroy(m.gameObject);
                _marbles.RemoveAt(i);
            }

            if (headSideSurvivor != null && tailSideSurvivorExists)
            {
                _pendingSeams.Add(headSideSurvivor);
                if (shouldFreeze) FreezeSegmentBackFrom(headSideSurvivor);
            }

            OnMatchResolved?.Invoke();
            OnChainChanged?.Invoke();
        }

        // Returns true if the destruction should freeze the front segment in place
        // (because retreating wouldn't form a match), false if the front should
        // retreat to close the gap and form a cascade match.
        // Called BEFORE the destruction so indices L-1 and R+1 still reference the
        // surviving flanks at their pre-destruction positions.
        private bool ShouldFreezeFrontAfterDestruction(int L, int R, ChainMarble headFlank, ChainMarble tailFlank)
        {
            if (headFlank == null || tailFlank == null) return false; // no flank to freeze against
            if (headFlank.Color != tailFlank.Color) return true;       // can't form a match
            int totalRun = CountFlankColorRun(L - 1, R + 1, headFlank.Color);
            return totalRun < _level.MatchThreshold;                   // not enough for a match
        }

        // Counts contiguous same-color marbles walking outward from both flanks of
        // a (yet-to-be-destroyed) range. Used to predict if a retreat would form
        // a match >= MatchThreshold.
        private int CountFlankColorRun(int headFlankIdx, int tailFlankIdx, MarbleColor color)
        {
            int count = 0;
            for (int i = headFlankIdx; i >= 0 && _marbles[i].Color == color; i--) count++;
            for (int i = tailFlankIdx; i < _marbles.Count && _marbles[i].Color == color; i++) count++;
            return count;
        }

        // Marks the head-side surviving marble and everything ahead of it (lower
        // index = closer to Bastion) as frozen. Those marbles will hold position
        // in AdvanceChain instead of retreating to close the gap.
        private void FreezeSegmentBackFrom(ChainMarble headSideMarble)
        {
            int idx = _marbles.IndexOf(headSideMarble);
            if (idx < 0) return;
            for (int i = idx; i >= 0; i--) _marbles[i].FrozenAfterGap = true;
        }

        // Returns true if `headSide` and `tailSide` are adjacent in the chain
        // and within marble-spacing arc-length of each other (i.e., the gap from
        // an earlier destruction has closed enough for them to be touching).
        private bool AreContiguous(ChainMarble headSide, ChainMarble tailSide)
        {
            float contiguityEpsilon = _level.MarbleSpacing * 0.4f;
            return (headSide.ArcLength - tailSide.ArcLength) <= _level.MarbleSpacing + contiguityEpsilon;
        }

        // Each tick, examine seams left behind by previous destructions. When
        // the gap has closed and the two flanking marbles match in color, run
        // ResolveMatchAround (which may chain into more destructions).
        private void ProcessPendingSeams()
        {
            if (_pendingSeams.Count == 0) return;

            for (int i = _pendingSeams.Count - 1; i >= 0; i--)
            {
                var headSide = _pendingSeams[i];
                if (headSide == null)
                {
                    _pendingSeams.RemoveAt(i);
                    continue;
                }
                int idx = _marbles.IndexOf(headSide);
                if (idx < 0 || idx + 1 >= _marbles.Count)
                {
                    // head-side was itself destroyed, or now sits at the tail
                    _pendingSeams.RemoveAt(i);
                    continue;
                }

                var tailSide = _marbles[idx + 1];
                if (!AreContiguous(headSide, tailSide))
                {
                    // Gap still closing — leave the seam pending.
                    continue;
                }

                // Gap closed. Done with this seam.
                _pendingSeams.RemoveAt(i);

                if (headSide.Color == tailSide.Color)
                {
                    // Cascade: maybe a longer same-color run formed. This may add
                    // a new seam if it destroys, but never re-adds *this* one.
                    ResolveMatchAround(headSide);
                }
            }
        }

        // -------------------- public destroy / dislodge API --------------------

        // Destroy a single marble in the chain. Used by powerup effects like
        // Vaporizer. Registers a pending seam if both flanks survive.
        public bool DestroyMarble(ChainMarble marble)
        {
            int idx = _marbles.IndexOf(marble);
            if (idx < 0) return false;
            DestroyRange(idx, idx);
            return true;
        }

        // Destroy every marble within `radius` arc-length sphere of `center` in
        // world space. Used by Explosive Round. Each contiguous index range
        // becomes its own pending seam so cascades can chain through them.
        public int DestroyInRadius(Vector2 center, float radius)
        {
            if (_marbles.Count == 0) return 0;
            float r2 = radius * radius;

            // Collect indices that fall within the explosion radius. Already
            // sorted ascending because we walk in order.
            var toRemove = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _marbles.Count; i++)
            {
                if (((Vector2)_marbles[i].transform.position - center).sqrMagnitude <= r2)
                {
                    toRemove.Add(i);
                }
            }
            if (toRemove.Count == 0) return 0;

            // Walk contiguous index ranges, registering a seam per range before
            // we mutate the list. Each range's head-side survivor is the marble
            // immediately before rangeStart.
            int rangeStart = toRemove[0];
            int rangeEnd = rangeStart;
            for (int i = 1; i < toRemove.Count; i++)
            {
                if (toRemove[i] == rangeEnd + 1)
                {
                    rangeEnd = toRemove[i];
                }
                else
                {
                    MaybeRegisterSeamWithFreeze(rangeStart, rangeEnd);
                    rangeStart = toRemove[i];
                    rangeEnd = rangeStart;
                }
            }
            MaybeRegisterSeamWithFreeze(rangeStart, rangeEnd);

            // Now actually destroy, walking in reverse so earlier indices stay valid.
            for (int i = toRemove.Count - 1; i >= 0; i--)
            {
                var m = _marbles[toRemove[i]];
                OnMarbleDestroyed?.Invoke(m.transform.position, m.Color);
                Destroy(m.gameObject);
                _marbles.RemoveAt(toRemove[i]);
            }

            OnMatchResolved?.Invoke();
            OnChainChanged?.Invoke();
            return toRemove.Count;
        }

        // Called BEFORE the actual destruction step in DestroyInRadius — indices
        // still reference the surviving flanks. Registers a pending seam and
        // applies the freeze rule per contiguous destroyed range.
        private void MaybeRegisterSeamWithFreeze(int rangeStart, int rangeEnd)
        {
            ChainMarble headSide = rangeStart > 0 ? _marbles[rangeStart - 1] : null;
            ChainMarble tailSide = rangeEnd < _marbles.Count - 1 ? _marbles[rangeEnd + 1] : null;
            if (headSide == null || tailSide == null) return;

            _pendingSeams.Add(headSide);
            if (ShouldFreezeFrontAfterDestruction(rangeStart, rangeEnd, headSide, tailSide))
            {
                FreezeSegmentBackFrom(headSide);
            }
        }

        // Remove `marble` from the chain WITHOUT firing OnMarbleDestroyed
        // (so visuals/scoring don't double-count) but still registering the seam
        // and the chain-changed event. Used by Knockout Round, which immediately
        // spawns a flying marble carrying the dislodged color.
        public bool DislodgeMarble(ChainMarble marble)
        {
            int idx = _marbles.IndexOf(marble);
            if (idx < 0) return false;

            ChainMarble headSide = idx > 0 ? _marbles[idx - 1] : null;
            ChainMarble tailSide = idx + 1 < _marbles.Count ? _marbles[idx + 1] : null;
            bool tailExists = tailSide != null;
            bool shouldFreeze = headSide != null && tailSide != null
                && ShouldFreezeFrontAfterDestruction(idx, idx, headSide, tailSide);

            // Don't fire OnMarbleDestroyed — the marble isn't really "destroyed",
            // it's being launched. Knockout will eventually settle it elsewhere
            // (which causes a normal insert) or off-screen (eliminated silently).
            Destroy(marble.gameObject);
            _marbles.RemoveAt(idx);

            if (headSide != null && tailExists)
            {
                _pendingSeams.Add(headSide);
                if (shouldFreeze) FreezeSegmentBackFrom(headSide);
            }

            OnChainChanged?.Invoke();
            return true;
        }

        // -------------------- end conditions --------------------

        private void CheckEndConditions()
        {
            if (!ReachedBastion && _marbles.Count > 0 && _marbles[0].ArcLength >= _path.TotalLength)
            {
                ReachedBastion = true;
                OnReachedBastion?.Invoke();
            }
            else if (_spawnComplete && _marbles.Count == 0)
            {
                OnCleared?.Invoke();
            }
        }
    }
}
