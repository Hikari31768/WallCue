namespace WallCue
{
    public class PluginConfig
    {
        public virtual bool Enabled { get; set; } = true;
        public virtual bool TestYellowWalls { get; set; } = false;
        public virtual bool LimitWarningDistance { get; set; } = false;
        public virtual float WarningDistanceMetres { get; set; } = WarningSettings.DefaultDistanceMetres;
        public virtual int SensitivityCentimetres { get; set; } = WarningSettings.DefaultSensitivityCentimetres;
    }
}
