// Headless doubles for CueController wiring. This test assembly is named SongCore
// solely to exercise the production optional metadata lookup. No Unity/NE simulation.
using System;
using System.Collections.Generic;
namespace Zenject
{
    public interface IInitializable { void Initialize(); }
    public interface ILateTickable { void LateTick(); }
    public sealed class InjectAttribute : Attribute {}
}
namespace UnityEngine
{
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} }
    public struct Bounds { public Vector3 min,max; }
    public class Transform
    {
        public Vector3 lossyScale = new Vector3(1,1,1);
        public Vector3 InverseTransformPoint(Vector3 point) { return point; }
    }
    public static class Mathf { public static float Abs(float value) {return Math.Abs(value);} }
    public static class Time { public static float time; }
    public sealed class BeforeRenderOrderAttribute : Attribute { public BeforeRenderOrderAttribute(int order) {} }
    public static class Application
    {
        public static event Action onBeforeRender;
        public static int Subscribers => onBeforeRender?.GetInvocationList().Length ?? 0;
        public static void Render() { onBeforeRender?.Invoke(); }
    }
}
public struct BeatmapKey {}
public class GameplayCoreSceneSetupData { public BeatmapKey beatmapKey; }
public class PlayerTransforms
{
    public int Reads;
    public UnityEngine.Vector3 headWorldPos { get { Reads++; return new UnityEngine.Vector3(0,1.5f,-1); } }
}
public class PlayerHeadAndObstacleInteraction { public HashSet<ObstacleController> _intersectingObstacles = new HashSet<ObstacleController>(); }
public class BeatmapObjectManager
{
    public event Action<ObstacleController> obstacleWasSpawnedEvent, obstacleWasDespawnedEvent;
    public List<ObstacleController> activeObstacleControllers = new List<ObstacleController>();
    public int Subscribers => (obstacleWasSpawnedEvent?.GetInvocationList().Length ?? 0) + (obstacleWasDespawnedEvent?.GetInvocationList().Length ?? 0);
    public void Spawn(ObstacleController wall) { activeObstacleControllers.Add(wall); obstacleWasSpawnedEvent?.Invoke(wall); }
    public void Despawn(ObstacleController wall) { activeObstacleControllers.Remove(wall); obstacleWasDespawnedEvent?.Invoke(wall); }
}
public class ObstacleController
{
    public bool isActiveAndEnabled=true, hasPassedAvoidedMark;
    public StretchableObstacle _stretchableObstacle = new StretchableObstacle();
    public UnityEngine.Transform transform = new UnityEngine.Transform();
    public int BoundsReads;
    public UnityEngine.Bounds bounds { get { BoundsReads++; return new UnityEngine.Bounds { min=new UnityEngine.Vector3(-1,1,0), max=new UnityEngine.Vector3(1,3,1) }; } }
}
public class StretchableObstacle { public ParametricBoxFrameController _obstacleFrame=new ParametricBoxFrameController(); public ParametricBoxFakeGlowController _obstacleFakeGlow=new ParametricBoxFakeGlowController(); }
public class ParametricBoxFrameController { public MaterialPropertyBlockController _materialPropertyBlockController=new MaterialPropertyBlockController(); }
public class ParametricBoxFakeGlowController { public MaterialPropertyBlockController _materialPropertyBlockController=new MaterialPropertyBlockController(); }
public class MaterialPropertyBlockController {}
namespace SongCore
{
    public class ExtraData { public string[] _requirements = new string[0], _suggestions = new string[0]; }
    public class DifficultyData { public ExtraData additionalDifficultyData = new ExtraData(); }
    public static class Collections
    {
        public static DifficultyData Data = new DifficultyData();
        public static DifficultyData GetCustomLevelSongDifficultyData(BeatmapKey key) { return Data; }
    }
}
namespace WallCue
{
    internal static class Plugin { internal static PluginConfig Config=new PluginConfig(); internal static TestLogger Log=new TestLogger(); }
    internal class TestLogger { public int Errors; public void Info(string text) {} public void Warn(string text) {} public void Error(string text) { Errors++; Console.WriteLine(text); } }
    public class WallVisual
    {
        public static int Created, Shown, Reapplied;
        public CueState State;
        public WallVisual(ObstacleController wall, ParametricBoxFrameController frame, MaterialPropertyBlockController frameBlock, ParametricBoxFakeGlowController glow, MaterialPropertyBlockController glowBlock) { Created++; }
        public void Restore() { State=CueState.Normal; }
        public void Show(CueState state, bool test) { State=state; Shown++; }
        public void Reapply() { Reapplied++; }
    }
    internal static class RenderDiagnostics { internal static void Log(object a,object b,object c,object d) {} }
}
