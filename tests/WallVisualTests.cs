using System;
using UnityEngine;
using WallCue;
internal static class WallVisualTests
{
    static int checks;
    static void Check(string name,bool ok) {if(!ok)throw new Exception(name);checks++;}
    public static int Main()
    {
        int id=Shader.PropertyToID("_Color");
        var original=new Color(.2f,.3f,.4f,.6f);
        var frame=new ParametricBoxFrameController{color=original};
        var glow=new ParametricBoxFakeGlowController{color=original};
        var fb=new MaterialPropertyBlockController();var gb=new MaterialPropertyBlockController();
        fb.materialPropertyBlock.SetColor(id,original);gb.materialPropertyBlock.SetColor(id,original);
        var visual=new WallVisual(new ObstacleController(),frame,fb,glow,gb);
        visual.Show(CueState.Warning);
        Check("yellow with preserved alpha",fb.materialPropertyBlock.GetColor(id)==new Color(1,1,0,.6f));
        Check("underlying frame colour untouched",frame.color==original);
        Check("underlying glow colour untouched",glow.color==original);
        Check("fake glow receives same hue",gb.materialPropertyBlock.GetColor(id)==new Color(1,1,0,.6f));
        int applies=fb.Applies;
        visual.Show(CueState.Critical);
        Check("resubmit same cached yellow",fb.Applies==applies+1);
        // Real failure mode: another renderer writer changes the submitted block only.
        fb.Submitted=original;
        visual.Show(CueState.Critical);
        Check("renderer overwrite repaired despite matching cache",fb.Submitted==new Color(1,1,0,.6f));
        fb.Submitted=original;
        visual.Reapply();
        Check("before-render repair",fb.Submitted==new Color(1,1,0,.6f));
        visual.Show(CueState.Collision);
        Check("red actual collision",fb.materialPropertyBlock.GetColor(id)==new Color(1,0,0,.6f));
        Time.unscaledTime=.124f;visual.Reapply();
        Check("first red phase lasts 125ms",fb.Submitted==new Color(1,0,0,.6f));
        Time.unscaledTime=.125f;visual.Show(CueState.Collision);
        Check("half-cycle clears RGB and alpha",fb.Submitted==new Color(0,0,0,0));
        Check("fake glow also transparent",gb.Submitted==new Color(0,0,0,0));
        Check("transparent phase remains collision state",visual.State==CueState.Collision);
        fb.Submitted=original;visual.Reapply();
        Check("before-render repairs transparent phase without resetting clock",fb.Submitted==new Color(0,0,0,0));
        Time.unscaledTime=.249f;visual.Reapply();
        Check("second half remains transparent",fb.Submitted==new Color(0,0,0,0));
        Time.unscaledTime=.25f;visual.Reapply();
        Check("next cycle red at 250ms",fb.Submitted==new Color(1,0,0,.6f));
        Time.unscaledTime=.375f;visual.Reapply();
        Check("subsequent transparent phase",fb.Submitted==new Color(0,0,0,0));
        visual.Show(CueState.Critical);
        Check("leaving collision restores steady yellow",fb.Submitted==new Color(1,1,0,.6f));
        Time.unscaledTime=.38f;visual.Show(CueState.Collision);
        Check("each reentry starts red independent of global phase",fb.Submitted==new Color(1,0,0,.6f));
        Time.unscaledTime=.53f;visual.Reapply();
        Check("reentry clock progresses",fb.Submitted==new Color(0,0,0,0));
        visual.Show(CueState.Collision,true);
        Check("test mode overrides collision flashing",fb.Submitted==new Color(1,1,0,.6f));
        Time.unscaledTime=.70f;visual.Reapply();
        Check("test yellow stays steady",fb.Submitted==new Color(1,1,0,.6f));
        visual.Show(CueState.Collision,false);
        Check("disabling test during collision starts red",fb.Submitted==new Color(1,0,0,.6f));
        Time.unscaledTime=.85f;visual.Reapply();
        visual.Restore();
        Check("disable in transparent phase restores frame and glow",fb.Submitted==original && gb.Submitted==original);
        visual.Show(CueState.Collision);
        Check("collision after cleanup starts red",fb.Submitted==new Color(1,0,0,.6f));
        // Simulate an external mod refreshing the base colour during warning.
        var updated=new Color(.4f,.1f,.7f,.8f);
        frame.color=updated;glow.color=updated;fb.materialPropertyBlock.SetColor(id,updated);
        visual.Show(CueState.Warning);
        Check("cue reapplied after other mod refresh",fb.materialPropertyBlock.GetColor(id)==new Color(1,1,0,.8f));
        visual.Show(CueState.Exit);
        Check("restore latest external base, not stale spawn colour",fb.materialPropertyBlock.GetColor(id)==updated);
        Check("restore latest glow",gb.materialPropertyBlock.GetColor(id)==updated);
        Check("exit state retained",visual.State==CueState.Exit);
        visual.Show(CueState.Normal);Check("settle to normal",visual.State==CueState.Normal);
        visual.Show(CueState.Collision);visual.Restore();
        Check("disable/despawn cleanup clears tint",fb.materialPropertyBlock.GetColor(id)==updated);
        Check("pooled state reset",visual.State==CueState.Normal);
        applies=fb.Applies;visual.Restore();Check("cleanup is idempotent",applies==fb.Applies);
        var noGlow=new WallVisual(new ObstacleController(),frame,fb,null,null);
        noGlow.Show(CueState.Warning);noGlow.Restore();Check("missing fake glow supported",fb.materialPropertyBlock.GetColor(id)==updated);
        noGlow.Show(CueState.Normal,true);
        Check("force-yellow mode paints distant safe walls",fb.Submitted==new Color(1,1,0,.8f));
        Check("test mode does not falsify geometric state",noGlow.State==CueState.Normal);
        fb.Submitted=original;noGlow.Reapply();
        Check("test mode persists before render",fb.Submitted==new Color(1,1,0,.8f));
        noGlow.Show(CueState.Normal,false);
        Check("turning off test mode restores base",fb.Submitted==updated);
        applies=fb.Applies;noGlow.Reapply();Check("safe wall not resubmitted before render",fb.Applies==applies);
        Console.WriteLine("PASS: "+checks+" render-state lifecycle checks using production WallVisual.cs and headless API doubles");
        return 0;
    }
}
