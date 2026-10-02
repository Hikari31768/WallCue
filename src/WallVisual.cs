using UnityEngine;
namespace WallCue
{
    internal sealed class WallVisual
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        // Four complete red/transparent cycles per real-time second, 50% duty cycle.
        internal const float CollisionBlinkHz = 4f;
        private static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);
        private readonly ParametricBoxFrameController _frame;
        private readonly ParametricBoxFakeGlowController _glow;
        private readonly MaterialPropertyBlockController _frameBlock, _glowBlock;
        private bool _painted, _forceYellow, _blinking;
        private float _collisionStartedAt;
        internal CueState State { get; private set; }
        internal WallVisual(ObstacleController wall, ParametricBoxFrameController frame,
            MaterialPropertyBlockController frameBlock, ParametricBoxFakeGlowController glow,
            MaterialPropertyBlockController glowBlock)
        { _frame = frame; _frameBlock = frameBlock; _glow = glow; _glowBlock = glowBlock; }
        internal void Show(CueState state, bool forceYellow = false)
        {
            _forceYellow = forceYellow;
            CueState display = forceYellow ? CueState.Warning : state;
            bool red = display == CueState.Collision;
            // Anchor each collision to a red phase; never reset it in Reapply.
            // A frame-stable clock keeps LateTick and both VR eyes in the same phase.
            float now = Time.unscaledTime;
            if (red && !_blinking) _collisionStartedAt = now;
            _blinking = red;
            if (display == CueState.Normal || display == CueState.Exit)
            { Restore(); State = state; return; }
            float cycles = (now - _collisionStartedAt) * CollisionBlinkHz;
            bool hidden = red && cycles % 1f >= 0.5f;
            _painted = true; // Also permits cleanup if a later render operation fails.
            // Preserve base alpha on visible phases. Zero RGB as well as alpha on hidden
            // phases: additive fake-glow shaders may not become invisible with alpha alone.
            // Preserve dimensions, core materials and controller colour fields.
            // Other mods can keep updating their base colour; no stale spawn-time colour is saved.
            if (_frame != null && _frameBlock != null)
                Paint(_frameBlock, hidden ? Transparent : new Color(1f, red ? 0f : 1f, 0f, _frame.color.a));
            if (_glow != null && _glowBlock != null && _glow.isActiveAndEnabled)
                Paint(_glowBlock, hidden ? Transparent : new Color(1f, red ? 0f : 1f, 0f, _glow.color.a));
            State = state;
        }
        private static void Paint(MaterialPropertyBlockController block, Color color)
        {
            // Cache equality is NOT evidence that Renderer still holds this block.
            // Another component can replace its submitted block while our cache is unchanged.
            // Submit on every cue update, then reapply immediately before VR rendering.
            var properties = block.materialPropertyBlock;
            properties.SetColor(ColorId, color);
            block.ApplyChanges();
        }
        internal void Reapply()
        {
            if (_painted) Show(State, _forceYellow);
        }
        internal void Restore()
        {
            if (_painted)
            {
                if (_frame != null && _frameBlock != null) Paint(_frameBlock, _frame.color);
                if (_glow != null && _glowBlock != null) Paint(_glowBlock, _glow.color);
                _painted = false;
            }
            _forceYellow = false;
            _blinking = false;
            _collisionStartedAt = 0f;
            State = CueState.Normal;
        }
    }
}
