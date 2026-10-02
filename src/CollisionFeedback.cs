using System.Collections.Generic;

namespace WallCue
{
    // One instance per player scene. Pure state so timing/deduplication can be tested
    // without rendering. Wall identity lasts only between spawn and despawn.
    public sealed class CollisionFeedback
    {
        public const double CountRedSeconds = 0.5;
        public const double FadeInSeconds = 0.1;
        public const double HoldSeconds = 0.3;
        public const double FadeOutSeconds = 0.1;
        private readonly HashSet<object> _activeWalls = new HashSet<object>();
        private readonly HashSet<object> _countedWalls = new HashSet<object>();
        private double _lastHitTime = double.NegativeInfinity;
        private float _startAlpha;
        public bool Available { get; private set; }
        public int Count { get; private set; }

        internal void Start() { Available = true; }
        internal void Track(object wall)
        {
            _activeWalls.Add(wall);
            _countedWalls.Remove(wall);
        }
        internal void Release(object wall)
        {
            _activeWalls.Remove(wall);
            _countedWalls.Remove(wall);
            // A despawn must not cancel the independently timed visible feedback.
        }
        internal bool Record(object wall, double now)
        {
            if (!Available || !_activeWalls.Contains(wall) || !_countedWalls.Add(wall)) return false;
            _startAlpha = IconAlpha(now);
            _lastHitTime = now;
            Count++;
            return true;
        }
        internal bool CountIsRed(double now)
        {
            double elapsed = now - _lastHitTime;
            return Available && elapsed >= 0 && elapsed < CountRedSeconds;
        }
        internal float IconAlpha(double now)
        {
            if (!Available) return 0f;
            double elapsed = now - _lastHitTime;
            if (elapsed < 0 || elapsed >= FadeInSeconds + HoldSeconds + FadeOutSeconds) return 0f;
            if (elapsed < FadeInSeconds)
                return _startAlpha + (1f - _startAlpha) * (float)(elapsed / FadeInSeconds);
            if (elapsed < FadeInSeconds + HoldSeconds) return 1f;
            return (float)(1.0 - (elapsed - FadeInSeconds - HoldSeconds) / FadeOutSeconds);
        }
        internal void Stop()
        {
            Available = false;
            _activeWalls.Clear();
            _countedWalls.Clear();
            _lastHitTime = double.NegativeInfinity;
            _startAlpha = 0f;
        }
    }
}
