using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;
namespace WallCue
{
    internal static class PrivateFields
    {
        internal static FieldInfo Required(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }
    }
    public sealed class CueController : IInitializable, ILateTickable, IDisposable
    {
        private readonly BeatmapObjectManager _manager;
        private readonly PlayerTransforms _player;
        private readonly PlayerHeadAndObstacleInteraction _interaction;
        private readonly GameplayCoreSceneSetupData _setup;
        private readonly CollisionFeedback _feedback;
        private readonly Dictionary<ObstacleController, WallVisual> _walls = new Dictionary<ObstacleController, WallVisual>();
        private ICollection<ObstacleController> _hits;
        private FieldInfo _stretchField, _frameField, _glowField, _frameBlockField, _glowBlockField;
        private bool _ready, _subscribed, _renderSubscribed, _visualsAllowed, _missingVisualReported, _renderDetailsReported;
        private int _beforeRenderCount;
        private int _wallCount, _warnings, _criticals, _collisions;
        [Inject]
        public CueController(BeatmapObjectManager manager, PlayerTransforms player,
            PlayerHeadAndObstacleInteraction interaction, GameplayCoreSceneSetupData setup, CollisionFeedback feedback)
        { _manager = manager; _player = player; _interaction = interaction; _setup = setup; _feedback = feedback; }

        public void Initialize()
        {
            try
            {
                _visualsAllowed = !IsDeclaredNoodleMap();
                if (!_visualsAllowed)
                    Plugin.Log.Info("This difficulty declares Noodle Extensions: wall frame effects disabled; native collision counter and icon remain enabled.");
                _hits = PrivateFields.Required(typeof(PlayerHeadAndObstacleInteraction), "_intersectingObstacles")
                    .GetValue(_interaction) as ICollection<ObstacleController>;
                if (_hits == null) throw new InvalidOperationException("Unexpected native collision collection type.");
                // NE still uses the game's collision collection, with its own fake-wall
                // filtering. Do not inspect or modify its animated wall renderers/bounds.
                if (_visualsAllowed)
                {
                    _stretchField = PrivateFields.Required(typeof(ObstacleController), "_stretchableObstacle");
                    _frameField = PrivateFields.Required(typeof(StretchableObstacle), "_obstacleFrame");
                    _glowField = PrivateFields.Required(typeof(StretchableObstacle), "_obstacleFakeGlow");
                    _frameBlockField = PrivateFields.Required(typeof(ParametricBoxFrameController), "_materialPropertyBlockController");
                    _glowBlockField = PrivateFields.Required(typeof(ParametricBoxFakeGlowController), "_materialPropertyBlockController");
                    Application.onBeforeRender += BeforeRender;
                    _renderSubscribed = true;
                }
                _manager.obstacleWasSpawnedEvent += OnSpawn;
                _manager.obstacleWasDespawnedEvent += OnDespawn;
                _subscribed = true;
                _ready = true;
                _feedback.Start();
                foreach (var wall in _manager.activeObstacleControllers) OnSpawn(wall);
                Plugin.Log.Info("Level ready; enabled=" + Plugin.Config.Enabled + ", testYellow=" + Plugin.Config.TestYellowWalls +
                    ", wallFrameEffectsAllowed=" + _visualsAllowed + ", nativeCollisionFeedback=" + _feedback.Available +
                    ", limitWarningDistance=" + Plugin.Config.LimitWarningDistance +
                    ", warningDistanceMetres=" + WarningSettings.DistanceMetres(Plugin.Config.WarningDistanceMetres) +
                    ", sensitivityCentimetres=" + WarningSettings.SensitivityCentimetres(Plugin.Config.SensitivityCentimetres) +
                    ". Native collision data is read-only; frame/core colour fields are not changed.");
            }
            catch (Exception e) { Fail(e); }
        }

        // SongCore is optional. Inspect ONLY the selected difficulty, not all difficulties.
        private bool IsDeclaredNoodleMap()
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name != "SongCore") continue;
                    var type = assembly.GetType("SongCore.Collections");
                    var method = type?.GetMethod("GetCustomLevelSongDifficultyData", BindingFlags.Public | BindingFlags.Static,
                        null, new[] { typeof(BeatmapKey) }, null);
                    var data = method?.Invoke(null, new object[] { _setup.beatmapKey });
                    if (data == null) return false;
                    var extra = data.GetType().GetField("additionalDifficultyData")?.GetValue(data);
                    if (extra == null) return false;
                    foreach (string field in new[] { "_requirements", "_suggestions" })
                    {
                        var names = extra.GetType().GetField(field)?.GetValue(extra) as IEnumerable<string>;
                        if (names == null) continue;
                        foreach (string name in names)
                            if (string.Equals(name, "Noodle Extensions", StringComparison.OrdinalIgnoreCase)) return true;
                    }
                }
            }
            catch (Exception e) { Plugin.Log.Warn("Could not inspect map requirements; use ordinary FitBeat maps for this prototype. " + e.Message); }
            return false;
        }

        private void OnSpawn(ObstacleController wall)
        {
            if (!_ready || wall == null) return;
            try
            {
                // Spawn events follow the game's Init; discard any previous pooled-wall state.
                _feedback.Track(wall);
                _wallCount++;
                if (!_visualsAllowed) return;
                WallVisual previous;
                if (_walls.TryGetValue(wall, out previous)) previous.Restore();
                var stretch = _stretchField.GetValue(wall) as StretchableObstacle;
                var frame = stretch == null ? null : _frameField.GetValue(stretch) as ParametricBoxFrameController;
                if (frame == null)
                {
                    if (!_missingVisualReported) { Plugin.Log.Warn("A wall has no original frame controller; it will be skipped."); _missingVisualReported = true; }
                    _walls.Remove(wall);
                    return;
                }
                var glow = _glowField.GetValue(stretch) as ParametricBoxFakeGlowController;
                var frameBlock = _frameBlockField.GetValue(frame) as MaterialPropertyBlockController;
                var glowBlock = glow == null ? null : _glowBlockField.GetValue(glow) as MaterialPropertyBlockController;
                if (frameBlock == null) throw new InvalidOperationException("Wall frame has no property-block controller.");
                _walls[wall] = new WallVisual(wall, frame, frameBlock, glow, glowBlock);
                if (!_renderDetailsReported)
                {
                    _renderDetailsReported = true;
                    RenderDiagnostics.Log(frame, frameBlock, glow, glowBlock);
                }
            }
            catch (Exception e) { Fail(e); }
        }
        private void OnDespawn(ObstacleController wall)
        {
            _feedback.Release(wall);
            WallVisual visual;
            if (_walls.TryGetValue(wall, out visual))
            {
                try { visual.Restore(); }
                catch (Exception e) { Plugin.Log.Warn("Wall restore during despawn: " + e.Message); }
                _walls.Remove(wall);
            }
        }
        public void LateTick()
        {
            if (!_ready) return;
            try
            {
                // HUD feedback uses native collisions even if wall colouring is disabled
                // or a particular wall has no drawable frame.
                foreach (var hit in _hits)
                    if (hit != null && hit.isActiveAndEnabled && !hit.hasPassedAvoidedMark)
                        _feedback.Record(hit, Time.time);
                if (!_visualsAllowed) return;
                if (!Plugin.Config.Enabled) { RestoreAll(); return; }
                Vector3 head = _player.headWorldPos;
                float clearance = WarningSettings.ClearanceMetres(Plugin.Config.SensitivityCentimetres);
                bool limitDistance = Plugin.Config.LimitWarningDistance;
                float distance = WarningSettings.DistanceMetres(Plugin.Config.WarningDistanceMetres);
                foreach (var entry in _walls)
                {
                    var wall = entry.Key;
                    var visual = entry.Value;
                    bool eligible = wall != null && wall.isActiveAndEnabled && !wall.hasPassedAvoidedMark;
                    CueState state;
                    if (!eligible) state = WarningLogic.Evaluate(false, false, 0,0,0, 0,0,0, 0,0,0, 1,1,1, visual.State);
                    else
                    {
                        Vector3 local = wall.transform.InverseTransformPoint(head);
                        Bounds bounds = wall.bounds;
                        Vector3 min = bounds.min, max = bounds.max, scale = wall.transform.lossyScale;
                        state = WarningLogic.Evaluate(true, _hits.Contains(wall), local.x, local.y, local.z,
                            min.x, min.y, min.z, max.x, max.y, max.z,
                            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z), visual.State,
                            clearance, limitDistance, distance);
                    }
                    if (state != visual.State)
                    {
                        if (state == CueState.Warning) _warnings++;
                        if (state == CueState.Critical) _criticals++;
                        if (state == CueState.Collision)
                        {
                            _collisions++;
                            if (_collisions == 1) Plugin.Log.Info("First native head collision received: 4 Hz red/transparent frame blink active (red first).");
                        }
                    }
                    visual.Show(state, Plugin.Config.TestYellowWalls && eligible);
                }
            }
            catch (Exception e) { Fail(e); }
        }
        [BeforeRenderOrder(10000)]
        private void BeforeRender()
        {
            if (!_ready || !_visualsAllowed) return;
            try
            {
                _beforeRenderCount++;
                if (!Plugin.Config.Enabled) { RestoreAll(); return; }
                foreach (var visual in _walls.Values) visual.Reapply();
            }
            catch (Exception e) { Fail(e); }
        }
        private void RestoreAll()
        {
            foreach (var visual in _walls.Values) visual.Restore();
        }
        private void Fail(Exception e)
        {
            _ready = false;
            _feedback.Stop();
            // Restore each independently: one broken/destroyed visual must not strand others.
            foreach (var visual in _walls.Values) { try { visual.Restore(); } catch { } }
            Plugin.Log.Error("WallCue disabled for this level after an error (gameplay is unchanged): " + e);
        }
        public void Dispose()
        {
            _ready = false;
            _feedback.Stop();
            if (_subscribed)
            {
                _manager.obstacleWasSpawnedEvent -= OnSpawn;
                _manager.obstacleWasDespawnedEvent -= OnDespawn;
                _subscribed = false;
            }
            if (_renderSubscribed)
            {
                Application.onBeforeRender -= BeforeRender;
                _renderSubscribed = false;
            }
            foreach (var visual in _walls.Values) { try { visual.Restore(); } catch { } }
            _walls.Clear();
            Plugin.Log.Info("Level summary: walls=" + _wallCount + ", warning entries=" + _warnings +
                ", critical entries=" + _criticals + ", collision entries=" + _collisions + ", unique wall hits=" + _feedback.Count + ", beforeRender callbacks=" + _beforeRenderCount + ". Entries are state transitions; unique wall hits counts each spawned wall at most once.");
        }
    }
}
