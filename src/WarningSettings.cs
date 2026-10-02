using System;
namespace WallCue
{
    // UI and gameplay share normalization, including hand-edited configuration files.
    public static class WarningSettings
    {
        public const float DefaultDistanceMetres = 10f;
        public const int DefaultSensitivityCentimetres = 5;
        public static float DistanceMetres(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return DefaultDistanceMetres;
            value = Math.Max(5f, Math.Min(25f, value));
            return (float)(Math.Round(value * 10.0, MidpointRounding.AwayFromZero) / 10.0);
        }
        public static int SensitivityCentimetres(int value) { return Math.Max(0, Math.Min(10, value)); }
        public static float ClearanceMetres(int centimetres) { return SensitivityCentimetres(centimetres) / 100f; }
    }
}
