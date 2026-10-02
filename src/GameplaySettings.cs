using System;
using System.ComponentModel;
using System.Globalization;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using Zenject;
namespace WallCue
{
    public sealed class GameplaySettingsInstaller : Installer
    {
        public override void InstallBindings()
        {
            // This is a separate BSML gameplay tab, never the Counters+ CounterOptions host.
            Container.BindInterfacesAndSelfTo<GameplaySettings>().AsSingle();
            Container.BindExecutionOrder<GameplaySettings>(10000);
        }
    }

    public sealed class GameplaySettings : IInitializable, IDisposable, INotifyPropertyChanged
    {
        private readonly GameplaySetup _setup;
        private bool _registered;
        public event PropertyChangedEventHandler PropertyChanged;
        [Inject]
        public GameplaySettings(GameplaySetup setup) { _setup = setup; }

        public void Initialize()
        {
            try
            {
                _setup.AddTab("WallCue", "WallCue.Resources.GameplaySettings.bsml", this, MenuType.Solo);
                _registered = true;
                Plugin.Log.Info("Song-selection Mods / WallCue tab registered.");
            }
            catch (Exception e) { Plugin.Log.Error("Could not register WallCue gameplay settings: " + e); }
        }
        public void Dispose()
        {
            if (!_registered) return;
            _registered = false;
            try { _setup.RemoveTab("WallCue"); }
            catch (Exception e) { Plugin.Log.Warn("Could not remove WallCue gameplay tab: " + e.Message); }
        }

        [UIValue("limit-distance")]
        public bool LimitDistance
        {
            get { return Plugin.Config.LimitWarningDistance; }
            set
            {
                if (Plugin.Config.LimitWarningDistance == value) return;
                Plugin.Config.LimitWarningDistance = value;
                // BSML NotifyUpdater listens to the CLR property name, not the UIValue alias.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LimitDistance)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistanceStatus)));
            }
        }
        [UIValue("warning-distance")]
        public float WarningDistance
        {
            get { return WarningSettings.DistanceMetres(Plugin.Config.WarningDistanceMetres); }
            set { Plugin.Config.WarningDistanceMetres = WarningSettings.DistanceMetres(value); }
        }
        [UIValue("sensitivity")]
        public int Sensitivity
        {
            get { return WarningSettings.SensitivityCentimetres(Plugin.Config.SensitivityCentimetres); }
            set { Plugin.Config.SensitivityCentimetres = WarningSettings.SensitivityCentimetres(value); }
        }
        [UIValue("distance-status")]
        public string DistanceStatus { get { return LimitDistance ? "Warn within the selected distance" : "Unlimited distance for spawned walls"; } }
        [UIAction("format-distance")]
        public string FormatDistance(float value) { return value.ToString("0.0", CultureInfo.InvariantCulture) + " m"; }
        [UIAction("format-sensitivity")]
        public string FormatSensitivity(int value) { return value.ToString(CultureInfo.InvariantCulture) + " cm"; }
    }
}
