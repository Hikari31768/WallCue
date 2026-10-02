using System;
namespace WallCue
{
    public enum CueState { Normal, Warning, Critical, Collision, Exit }
    // Pure geometry, independent of Unity. Collision truth is supplied by the game.
    public static class WarningLogic
    {
        public const float ClearanceMargin = 0.05f;
        public static CueState Evaluate(bool eligible, bool colliding,
            float x, float y, float z, float minX, float minY, float minZ,
            float maxX, float maxY, float maxZ, float scaleX, float scaleY, float scaleZ,
            CueState previous, float clearanceMargin = ClearanceMargin,
            bool limitForwardDistance = false, float forwardDistance = WarningSettings.DefaultDistanceMetres)
        {
            if (!eligible) return Safe(previous);
            if (colliding) return CueState.Collision;
            if (!Finite(x) || !Finite(y) || !Finite(z) ||
                !Finite(minX) || !Finite(minY) || !Finite(minZ) ||
                !Finite(maxX) || !Finite(maxY) || !Finite(maxZ) ||
                !Finite(scaleX) || !Finite(scaleY) || !Finite(scaleZ) ||
                scaleX <= 0.0001f || scaleY <= 0.0001f || scaleZ <= 0.0001f ||
                maxX <= minX || maxY <= minY || maxZ <= minZ) return Safe(previous);
            if (!Finite(clearanceMargin)) clearanceMargin = ClearanceMargin;
            clearanceMargin = Math.Max(0f, Math.Min(0.1f, clearanceMargin));
            float mx = clearanceMargin / scaleX, my = clearanceMargin / scaleY;
            // The obstacle extends along local +Z, approaching the player toward -Z.
            // No warning behind its tail. Clearance is measured in world metres.
            bool nearCrossSection = x >= minX - mx && x <= maxX + mx &&
                                    y >= minY - my && y <= maxY + my;
            // Gate only approaching warnings. Neither critical cues nor native hits are distance-gated.
            if (nearCrossSection && z < minZ && (!limitForwardDistance ||
                (minZ - z) * scaleZ <= WarningSettings.DistanceMetres(forwardDistance)))
                return CueState.Warning;
            if (nearCrossSection && z >= minZ && z <= maxZ)
                return CueState.Critical;
            return Safe(previous);
        }
        private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
        private static CueState Safe(CueState previous)
        {
            return previous == CueState.Warning || previous == CueState.Critical || previous == CueState.Collision
                ? CueState.Exit : CueState.Normal;
        }
    }
}
