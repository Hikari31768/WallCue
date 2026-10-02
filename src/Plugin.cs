using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using Zenject;
using BeatSaberMarkupLanguage.Attributes;
using System.Reflection;
[assembly: AssemblyTitle("WallCue")]
[assembly: AssemblyVersion("0.1.8.0")]
[assembly: AssemblyFileVersion("0.1.8.0")]
namespace WallCue
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static PluginConfig Config;
        internal static IPA.Logging.Logger Log;
        [Init]
        public Plugin(IPA.Logging.Logger logger, Config config, Zenjector zenjector)
        {
            Log = logger;
            Config = config.Generated<PluginConfig>();
            // Counters+ MenuUIInstaller creates the manifest-declared BSML Host.
            // Registering it again here prevents the entire menu scene from starting.
            zenjector.Install<GameplaySettingsInstaller>(Location.Menu);
            zenjector.Install<PlayerInstaller>(Location.StandardPlayer);
            Log.Info("WallCue 0.1.8 / Beat Saber 1.40.8. Counters+: Wall Hits + Wall Hit Icon. Warning distance and sensitivity: song selection / Mods / WallCue.");
        }
    }
    public class PlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<CollisionFeedback>().AsSingle();
            Container.BindInterfacesAndSelfTo<CueController>().AsSingle();
            Container.BindExecutionOrder<CueController>(10000);
            Container.BindExecutionOrder<WallHitsCounter>(10001);
            Container.BindExecutionOrder<WallHitIconCounter>(10001);
        }
    }
    public sealed class CounterOptions
    {
        [UIValue("enabled")]
        public bool Enabled { get { return Plugin.Config.Enabled; } set { Plugin.Config.Enabled = value; } }
        [UIValue("test-yellow")]
        public bool TestYellowWalls { get { return Plugin.Config.TestYellowWalls; } set { Plugin.Config.TestYellowWalls = value; Plugin.Log.Info("TestYellowWalls=" + value); } }
    }
}
