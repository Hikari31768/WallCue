using System;
using WallCue;
internal static class WarningLogicTests
{
    private static int count;
    private static CueState At(float x, float y, float z, bool hit = false,
        CueState previous = CueState.Normal, bool eligible = true, float scale = 1f,
        float margin = 0.05f, bool limit = false, float distance = 10f)
    { return WarningLogic.Evaluate(eligible,hit,x,y,z,-1f,1f,0f,1f,3f,10f,scale,scale,scale,previous,margin,limit,distance); }
    private static void Check(string name, CueState expected, CueState actual)
    { if(expected!=actual) throw new Exception(name+": expected "+expected+", got "+actual); count++; }
    private static void Check(string name, bool ok)
    { if (!ok) throw new Exception(name); count++; }
    public static int Main()
    {
        Check("far approaching wall has no range gate",CueState.Warning,At(0,1.5f,-1000f));
        Check("near front still warns",CueState.Warning,At(0,1.5f,-0.5f));
        Check("approaching with insufficient crouch",CueState.Warning,At(0,1.02f,-0.1f));
        Check("already safely crouched",CueState.Normal,At(0,0.94f,-0.1f));
        Check("near lower edge before wall",CueState.Warning,At(0,0.97f,-0.1f));
        Check("safely underneath long wall",CueState.Normal,At(0,0.9f,5f));
        Check("rising near underside",CueState.Critical,At(0,0.97f,5f));
        Check("clearance inclusive",CueState.Critical,At(0,0.95f,5f));
        Check("actual collision overrides caution",CueState.Collision,At(0,1.1f,5f,true));
        Check("native collision truth wins over estimated geometry",CueState.Collision,At(5,5,5,true));
        Check("geometry alone cannot claim actual collision",CueState.Critical,At(0,1.1f,5f));
        Check("escape collision into clearance zone",CueState.Critical,At(0,0.97f,5f,false,CueState.Collision));
        Check("escape collision into safe position",CueState.Exit,At(0,0.9f,5f,false,CueState.Collision));
        Check("exit then normal",CueState.Normal,At(0,0.9f,5f,false,CueState.Exit));
        Check("dodge side near",CueState.Critical,At(1.04f,1.5f,5f));
        Check("dodge side safe",CueState.Normal,At(1.051f,1.5f,5f));
        Check("wall behind player",CueState.Exit,At(0,1.5f,10.01f,false,CueState.Warning));
        Check("despawn restores collision",CueState.Exit,At(0,1.5f,5f,true,CueState.Collision,false));
        Check("ineligible starts normal",CueState.Normal,At(0,1.5f,5f,true,CueState.Normal,false));
        Check("scale 2 halves local clearance",CueState.Normal,At(0,0.97f,5f,false,CueState.Normal,true,2f));
        Check("scale 2 correct clearance",CueState.Critical,At(0,0.98f,5f,false,CueState.Normal,true,2f));
        Check("scale 2 also has unlimited forward range",CueState.Warning,At(0,1.5f,-1000f,false,CueState.Normal,true,2f));
        Check("scale 2 near front",CueState.Warning,At(0,1.5f,-0.24f,false,CueState.Normal,true,2f));
        Check("zero scale safely ignored",CueState.Normal,At(0,1.5f,5f,false,CueState.Normal,true,0));
        Check("NaN safely ignored",CueState.Exit,At(float.NaN,1.5f,5f,false,CueState.Warning));
        Check("zero-sized/fake bounds ignored",CueState.Normal,WarningLogic.Evaluate(true,false,0,0,0,0,0,0,0,0,0,1,1,1,CueState.Normal));
        Check("warning corrected before arrival",CueState.Exit,At(0,0.8f,-0.1f,false,CueState.Warning));
        Check("front reaches player",CueState.Critical,At(0,0.97f,0f,false,CueState.Warning));
        Check("distant safe crouch stays normal",CueState.Normal,At(0,0.9f,-1000f));
        Check("distant safe side dodge stays normal",CueState.Normal,At(2,1.5f,-1000f));
        Check("distant clearance edge still warns",CueState.Warning,At(0,0.97f,-1000f));
        Check("distant dodge clears warning immediately",CueState.Exit,At(2,1.5f,-1000f,false,CueState.Warning));
        Check("past tail never forward warning",CueState.Normal,At(0,1.5f,1000f));
        Check("unspawned or inactive wall ignored",CueState.Normal,At(0,1.5f,-1000f,false,CueState.Normal,false));
        Check("distance off ignores retained slider value", CueState.Warning, At(0,1.5f,-1000,limit:false,distance:5));
        Check("distance on suppresses far wall", CueState.Normal, At(0,1.5f,-1000,limit:true));
        Check("5m limit inclusive", CueState.Warning, At(0,1.5f,-5,limit:true,distance:5));
        Check("just outside 5m", CueState.Normal, At(0,1.5f,-5.001f,limit:true,distance:5));
        Check("25m limit inclusive", CueState.Warning, At(0,1.5f,-25,limit:true,distance:25));
        Check("just outside 25m", CueState.Normal, At(0,1.5f,-25.001f,limit:true,distance:25));
        Check("fractional distance inclusive", CueState.Warning, At(0,1.5f,-7.3f,limit:true,distance:7.3f));
        Check("one step closer excludes wall", CueState.Normal, At(0,1.5f,-7.3f,limit:true,distance:7.2f));
        Check("world distance accounts for scale", CueState.Warning, At(0,1.5f,-2.5f,scale:2,limit:true,distance:5));
        Check("scale cannot double allowed world range", CueState.Normal, At(0,1.5f,-2.6f,scale:2,limit:true,distance:5));
        Check("range only applies in front", CueState.Critical, At(0,1.5f,9,limit:true,distance:5));
        Check("range never hides actual native collision", CueState.Collision, At(0,1.5f,-1000,hit:true,limit:true,distance:5));
        Check("leaving distance range restores yellow", CueState.Exit, At(0,1.5f,-6,previous:CueState.Warning,limit:true,distance:5));
        Check("limit still ignores safe cross-section", CueState.Normal, At(0,0.9f,-4,limit:true,distance:5));
        Check("limit never warns behind tail", CueState.Normal, At(0,1.5f,11,limit:true));
        Check("0cm removes near-edge warning", CueState.Normal, At(0,0.99f,-4,margin:0));
        Check("0cm retains direct-path warning", CueState.Warning, At(0,1.01f,-4,margin:0));
        Check("0cm removes near-edge critical", CueState.Normal, At(0,0.99f,4,margin:0));
        Check("0cm retains native hits", CueState.Collision, At(0,0.99f,4,hit:true,margin:0));
        Check("10cm warning margin inclusive", CueState.Warning, At(0,0.90f,-4,margin:0.1f));
        Check("10cm critical margin inclusive", CueState.Critical, At(0,0.90f,4,margin:0.1f));
        Check("outside 10cm remains safe", CueState.Normal, At(0,0.899f,4,margin:0.1f));
        Check("10cm lateral margin", CueState.Warning, At(1.09f,1.5f,-4,margin:0.1f));
        Check("clearance uses world metres with scaling", CueState.Warning, At(0,0.95f,-4,scale:2,margin:0.1f));
        Check("scaling does not expand clearance", CueState.Normal, At(0,0.949f,-4,scale:2,margin:0.1f));
        var config = new PluginConfig();
        Check("old config receives compatible defaults", !config.LimitWarningDistance && config.SensitivityCentimetres == 5 && config.WarningDistanceMetres == 10);
        Check("distance clamps lower bound", WarningSettings.DistanceMetres(-4) == 5);
        Check("distance clamps upper bound", WarningSettings.DistanceMetres(40) == 25);
        Check("distance rounds to 0.1m", Math.Abs(WarningSettings.DistanceMetres(12.36f) - 12.4f) < 0.00001f);
        Check("NaN distance falls back", WarningSettings.DistanceMetres(float.NaN) == 10);
        Check("infinite distance falls back", WarningSettings.DistanceMetres(float.PositiveInfinity) == 10);
        Check("sensitivity clamps lower bound", WarningSettings.ClearanceMetres(-1) == 0);
        Check("sensitivity clamps upper bound", WarningSettings.ClearanceMetres(99) == 0.1f);
        Check("centimetres convert correctly", WarningSettings.ClearanceMetres(5) == 0.05f);
        Console.WriteLine("PASS: "+count+" geometry/state regression cases using the production WarningLogic.cs");
        return 0;
    }
}
