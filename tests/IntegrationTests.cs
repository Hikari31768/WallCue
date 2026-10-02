// Uses the user's actual Counters+ and IPA DLLs, but never calls Unity native APIs.
// Checks native collision/render fields, loader dependency ranges, two features in
// one manifest, defaults, resources and settings bindings. Not a game/VR render test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using CountersPlus.Custom;
using CountersPlus.Counters.Custom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zenject;

internal static class IntegrationTests
{
    static int checks;
    static void Check(string name, bool ok) { if (!ok) throw new Exception(name); checks++; }
    public static int Main(string[] args)
    {
        string[] folders = { Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), Path.GetFullPath(args[2]),
            Path.GetFullPath(Path.Combine(args[1], "..", "Libs")) };
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (string folder in folders)
            {
                string path = Path.Combine(folder, name.Name + ".dll");
                if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
            }
            return null;
        };
        return Run(folders);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(string[] folders)
    {
        var mod = Assembly.LoadFrom(Path.Combine(folders[2], "WallCue.dll"));
        string Read(string name)
        {
            using (var stream = mod.GetManifestResourceStream(name))
            {
                if (stream == null) throw new Exception("Missing resource: " + name);
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
            }
        }
        var manifest = JObject.Parse(Read("WallCue.manifest.json"));
        var ipa = Assembly.LoadFrom(Path.Combine(folders[0], "IPA.Loader.dll"));
        Check("manifest targets the 1.40.8 branch", (string)manifest["gameVersion"] == "1.40.8");
        foreach (var dependency in ((JObject)manifest["dependsOn"]).Properties())
        {
            string file = dependency.Name == "BSIPA" ? Path.Combine(folders[0], "IPA.Loader.dll") :
                Path.Combine(folders[1], (dependency.Name == "BeatSaberMarkupLanguage" ? "BSML" : dependency.Name) + ".dll");
            var assembly = Assembly.LoadFrom(file);
            string resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".manifest.json"));
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resource)))
            {
                var installed = JObject.Parse(reader.ReadToEnd());
                var range = new Hive.Versioning.VersionRange((string)dependency.Value);
                var version = new Hive.Versioning.Version((string)installed["version"]);
                Check("dependency ID matches: " + dependency.Name, (string)installed["id"] == dependency.Name);
                Check("actual loader range accepts installed " + dependency.Name + " " + version, range.Matches(version));
            }
        }
        // Compile success cannot catch renamed private fields. Exercise the actual
        // production lookup against the game's metadata without creating Unity objects.
        var gameAssemblies = new[] { "Main", "GameplayCore", "HMLib", "HMRendering" }
            .Select(n => Assembly.LoadFrom(Path.Combine(folders[0], n + ".dll"))).ToArray();
        Type GameType(string name) => gameAssemblies.Select(a => a.GetType(name)).First(t => t != null);
        var required = mod.GetType("WallCue.PrivateFields", true).GetMethod("Required", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo NativeField(string type, string name) => (FieldInfo)required.Invoke(null, new object[] { GameType(type), name });
        Type obstacleType = GameType("ObstacleController");
        Check("native collision collection implements expected interface",
            typeof(ICollection<>).MakeGenericType(obstacleType).IsAssignableFrom(
                NativeField("PlayerHeadAndObstacleInteraction", "_intersectingObstacles").FieldType));
        Check("native wall exposes stretchable visual", NativeField("ObstacleController", "_stretchableObstacle").FieldType == GameType("StretchableObstacle"));
        Check("native frame field matches", NativeField("StretchableObstacle", "_obstacleFrame").FieldType == GameType("ParametricBoxFrameController"));
        Check("native glow field matches", NativeField("StretchableObstacle", "_obstacleFakeGlow").FieldType == GameType("ParametricBoxFakeGlowController"));
        foreach (string visual in new[] { "ParametricBoxFrameController", "ParametricBoxFakeGlowController" })
            Check("native property-block field matches: " + visual,
                NativeField(visual, "_materialPropertyBlockController").FieldType == GameType("MaterialPropertyBlockController"));
        string songCorePath = Path.Combine(folders[1], "SongCore.dll");
        if (File.Exists(songCorePath))
        {
            var songCore = Assembly.LoadFrom(songCorePath);
            Type keyType = Assembly.LoadFrom(Path.Combine(folders[0], "DataModels.dll")).GetType("BeatmapKey", true);
            var lookup = songCore.GetType("SongCore.Collections", true).GetMethod("GetCustomLevelSongDifficultyData",
                BindingFlags.Public | BindingFlags.Static, null, new[] { keyType }, null);
            Check("optional SongCore selected-difficulty lookup exists", lookup != null);
            var extra = lookup.ReturnType.GetField("additionalDifficultyData");
            Check("optional SongCore difficulty metadata exists", extra != null);
            foreach (string field in new[] { "_requirements", "_suggestions" })
                Check("optional SongCore NE declaration exists: " + field,
                    typeof(IEnumerable<string>).IsAssignableFrom(extra.FieldType.GetField(field).FieldType));
        }
        var converter = (JsonConverter)Activator.CreateInstance(ipa.GetType("IPA.JsonConverters.FeaturesFieldConverter", true), true);
        var features = JsonConvert.DeserializeObject<Dictionary<string, List<JObject>>>(manifest["features"].ToString(), converter);
        var definitions = features["CountersPlus.CustomCounter"];
        Check("actual IPA accepts both feature entries", definitions.Count == 2);
        string[] names = { "Wall Hits", "Wall Hit Icon" };
        string[] positions = { "BelowCombo", "AboveHighway" };
        Type customType = typeof(BasicCustomCounter).Assembly.GetType("CountersPlus.Custom.CustomCounter", true);
        for (int i = 0; i < definitions.Count; i++)
        {
            var counter = definitions[i].ToObject(customType);
            Check("actual Counters+ decodes name " + i, (string)customType.GetField("Name").GetValue(counter) == names[i]);
            string location = (string)customType.GetField("CounterLocation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(counter);
            Type type = mod.GetType(location, true);
            Check("counter type resolves " + i, typeof(BasicCustomCounter).IsAssignableFrom(type));
            Check("counter is constructible " + i, Activator.CreateInstance(type) != null);
            var defaults = (CustomConfigModel)customType.GetField("ConfigDefaults", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(counter);
            Check("defaults enabled " + i, defaults.Enabled);
            Check("defaults select intended position " + i, defaults.Position.ToString() == positions[i]);
            Check("defaults select intended distance " + i, defaults.Distance == (i == 0 ? 2 : 0));
            Check("uses standard Counters+ canvas " + i, defaults.CanvasID == -1);
            Check("multiplayer not advertised " + i, !(bool)customType.GetField("MultiplayerReady").GetValue(counter));
            Check("custom settings resource exists " + i, Read((string)definitions[i]["BSML"]["Resource"]).Contains("<vertical "));
        }
        string hostName = (string)definitions[0]["BSML"]["Host"];
        Check("embedded custom settings host resolves", Activator.CreateInstance(mod.GetType(hostName, true)) != null);
        Type hostType = mod.GetType(hostName, true);
        // Actual Counters+ MenuUIInstaller uses this named AsCached binding.
        // Reproduce 0.1.4's duplicate and verify the corrected single owner.
        var brokenMenu = new DiContainer();
        brokenMenu.Bind(hostType).WithId("Wall Hits").AsCached();
        brokenMenu.Bind(hostType).WithId("Wall Hits").AsSingle();
        bool reproduced = false;
        try { brokenMenu.ResolveId(hostType, "Wall Hits"); }
        catch (ZenjectException e) { reproduced = e.ToString().Contains("multiple creation bindings"); }
        Check("0.1.4 duplicate startup binding reproduces log failure", reproduced);
        Check("WallCue no longer installs duplicate menu owner", mod.GetType("WallCue.MenuInstaller") == null);
        var fixedMenu = new DiContainer();
        fixedMenu.Bind(hostType).WithId("Wall Hits").AsCached();
        object host = fixedMenu.ResolveId(hostType, "Wall Hits");
        Check("framework-owned settings host resolves", host != null && host.GetType() == hostType);
        Check("framework-owned host retains cached identity", ReferenceEquals(host, fixedMenu.ResolveId(hostType, "Wall Hits")));
        // Run the NEW installer in the same actual Zenject container as Counters+.
        // Only the unused Unity-facing GameplaySetup dependency is a placeholder.
        var bsml = Assembly.LoadFrom(Path.Combine(folders[1], "BSML.dll"));
        Type setupType = bsml.GetType("BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup", true);
        fixedMenu.Bind(setupType).FromInstance(RuntimeHelpers.GetUninitializedObject(setupType));
        var installer = (Installer)Activator.CreateInstance(mod.GetType("WallCue.GameplaySettingsInstaller", true));
        fixedMenu.Inject(installer);
        installer.InstallBindings();
        Type settingsType = mod.GetType("WallCue.GameplaySettings", true);
        object tabHost = fixedMenu.Resolve(settingsType);
        Check("new gameplay installer resolves beside Counters+", tabHost != null);
        Check("new gameplay installer preserves Counters+ host", ReferenceEquals(host, fixedMenu.ResolveId(hostType, "Wall Hits")));
        Check("gameplay host has a single cached instance", ReferenceEquals(tabHost, fixedMenu.Resolve(settingsType)));
        var config = Activator.CreateInstance(mod.GetType("WallCue.PluginConfig", true));
        mod.GetType("WallCue.Plugin").GetField("Config", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, config);
        var values = settingsType.GetProperties().Select(p => new { Property = p,
            Attribute = p.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "UIValue") })
            .Where(p => p.Attribute != null).ToDictionary(p => (string)p.Attribute.ConstructorArguments[0].Value, p => p.Property);
        var actions = settingsType.GetMethods().Select(m => new { Method = m,
            Attribute = m.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "UIAction") })
            .Where(m => m.Attribute != null).ToDictionary(m => (string)m.Attribute.ConstructorArguments[0].Value, m => m.Method);
        var page = XDocument.Parse(Read("WallCue.Resources.GameplaySettings.bsml"));
        Check("gameplay page has a settings container", page.Descendants("settings-container").Any());
        var propertyValueType = bsml.GetType("BeatSaberMarkupLanguage.Parser.BSMLPropertyValue", true);
        var actionType = bsml.GetType("BeatSaberMarkupLanguage.Parser.BSMLAction", true);
        foreach (var attribute in page.Descendants().Attributes())
        {
            if (attribute.Name.LocalName == "value" || attribute.Value.StartsWith("~"))
            {
                string key = attribute.Value.TrimStart('~');
                Check("gameplay value binding exists: " + key, values.ContainsKey(key));
                var binding = Activator.CreateInstance(propertyValueType, tabHost, values[key], true);
                object value = propertyValueType.GetMethod("GetValue").Invoke(binding, null);
                Check("actual BSML can read gameplay value: " + key, value != null);
                if (attribute.Name.LocalName == "value")
                    propertyValueType.GetMethod("SetValue").Invoke(binding, new[] { value });
            }
            if (attribute.Name.LocalName == "formatter")
            {
                Check("gameplay formatter exists: " + attribute.Value, actions.ContainsKey(attribute.Value));
                var action = Activator.CreateInstance(actionType, tabHost, actions[attribute.Value], true);
                bool integer = (string)attribute.Parent.Attribute("integer-only") == "true";
                object sample = integer ? (object)5 : (object)12.3f;
                string formatted = (string)actionType.GetMethod("Invoke").Invoke(action, new object[] { new object[] { sample } });
                Check("actual BSML formatter accepts slider value type", formatted == (integer ? "5 cm" : "12.3 m"));
            }
        }
        var changes = new List<string>();
        ((INotifyPropertyChanged)tabHost).PropertyChanged += (sender, e) => changes.Add(e.PropertyName);
        values["warning-distance"].SetValue(tabHost, 12.36f);
        values["limit-distance"].SetValue(tabHost, true);
        Check("toggle emits CLR name for live interactability", changes.Contains("LimitDistance") && changes.Contains("DistanceStatus"));
        Check("toggle saves config immediately", (bool)config.GetType().GetProperty("LimitWarningDistance").GetValue(config));
        values["limit-distance"].SetValue(tabHost, false);
        Check("disabling preserves selected distance", Math.Abs((float)values["warning-distance"].GetValue(tabHost) - 12.4f) < 0.00001f);
        Check("disabled status describes unlimited warning", ((string)values["distance-status"].GetValue(tabHost)).Contains("Unlimited"));
        values["sensitivity"].SetValue(tabHost, 10);
        Check("sensitivity saves config immediately", (int)config.GetType().GetProperty("SensitivityCentimetres").GetValue(config) == 10);
        string settings = Read((string)definitions[0]["BSML"]["Resource"]);
        Check("embedded settings retain wall cues toggle", settings.Contains("value='enabled'"));
        Check("no standalone settings menu resource", mod.GetManifestResourceStream("WallCue.Resources.Settings.bsml") == null);
        using (var icon = mod.GetManifestResourceStream("WallCue.Resources.square_and_x.png"))
        {
            byte[] header = new byte[8]; icon.Read(header, 0, header.Length);
            Check("PNG embedded", header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71);
        }
        Console.WriteLine("PASS: " + checks + " integration checks using actual game/IPA/plugin DLLs (no Unity rendering)");
        return 0;
    }
}
