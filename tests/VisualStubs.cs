// Headless test doubles for the small render API surface used by WallVisual.
// They do not emulate Unity rendering, shader output, VR or the game's collision system.
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Time { public static float unscaledTime; }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a) {this.r=r;this.g=g;this.b=b;this.a=a;}
        public static bool operator ==(Color x,Color y) {return x.r==y.r&&x.g==y.g&&x.b==y.b&&x.a==y.a;}
        public static bool operator !=(Color x,Color y) {return !(x==y);}
        public override bool Equals(object o) {return o is Color && this==(Color)o;}
        public override int GetHashCode() {return r.GetHashCode();}
    }
    public static class Shader { public static int PropertyToID(string name) {return name.GetHashCode();} }
    public sealed class MaterialPropertyBlock
    {
        readonly Dictionary<int,Color> colors=new Dictionary<int,Color>();
        public Color GetColor(int id) {Color c;return colors.TryGetValue(id,out c)?c:default(Color);}
        public void SetColor(int id,Color c) {colors[id]=c;}
    }
}
public class ObstacleController {}
public class ParametricBoxFrameController {public UnityEngine.Color color;}
public class ParametricBoxFakeGlowController {public UnityEngine.Color color;public bool isActiveAndEnabled=true;}
public class MaterialPropertyBlockController
{
    public UnityEngine.MaterialPropertyBlock materialPropertyBlock=new UnityEngine.MaterialPropertyBlock();
    public int Applies;
    public UnityEngine.Color Submitted;
    public void ApplyChanges() {Applies++; Submitted=materialPropertyBlock.GetColor(UnityEngine.Shader.PropertyToID("_Color"));}
}
