using System;
using WallCue;
using UnityEngine;
internal static class ControllerTests
{
    static int checks;
    static void Check(string name, bool ok) { if (!ok) throw new Exception(name); checks++; }
    static void NoodleLevel(bool suggested)
    {
        SongCore.Collections.Data = new SongCore.DifficultyData();
        if (suggested) SongCore.Collections.Data.additionalDifficultyData._suggestions = new[] {"Noodle Extensions"};
        else SongCore.Collections.Data.additionalDifficultyData._requirements = new[] {"Noodle Extensions"};
        Plugin.Config = new PluginConfig { Enabled=true, TestYellowWalls=true };
        var manager = new BeatmapObjectManager();
        var player = new PlayerTransforms();
        var native = new PlayerHeadAndObstacleInteraction();
        var feedback = new CollisionFeedback();
        var wall = new ObstacleController { _stretchableObstacle=null };
        manager.activeObstacleControllers.Add(wall); // Already spawned at initialization.
        var cue = new CueController(manager, player, native, new GameplayCoreSceneSetupData(), feedback);
        cue.Initialize();
        Check("NE feedback starts at zero", feedback.Available && feedback.Count==0);
        Check("NE tracks lifecycle without a renderer", manager.Subscribers==2);
        Check("NE never subscribes to before-render", Application.Subscribers==0);
        cue.LateTick();
        Check("visual-only wall does not count", feedback.Count==0);
        native._intersectingObstacles.Add(wall);
        Time.time=1;
        cue.LateTick();
        Check("native collision on NE counts", feedback.Count==1 && feedback.CountIsRed(1.1));
        Check("native collision on NE triggers icon", feedback.IconAlpha(1.2)==1);
        cue.LateTick();
        native._intersectingObstacles.Clear(); cue.LateTick();
        native._intersectingObstacles.Add(wall); cue.LateTick();
        Check("same NE wall never counts twice", feedback.Count==1);
        var other = new ObstacleController { _stretchableObstacle=null };
        manager.Spawn(other);
        native._intersectingObstacles.Add(other);
        Plugin.Config.Enabled=false;
        Time.time=2; cue.LateTick();
        Check("new NE wall counts with wall cues switch off", feedback.Count==2);
        native._intersectingObstacles.Clear();
        manager.Despawn(wall); manager.Despawn(other);
        Check("despawn preserves icon lifetime", feedback.IconAlpha(2.2)==1);
        manager.Spawn(wall); native._intersectingObstacles.Add(wall);
        Time.time=3; cue.LateTick();
        Check("pooled wall may count as a new spawn", feedback.Count==3);
        Application.Render();
        Check("NE skips geometry even with test-yellow enabled", player.Reads==0 && wall.BoundsReads==0);
        Check("NE creates no visual objects", WallVisual.Created==0 && WallVisual.Shown==0 && WallVisual.Reapplied==0);
        cue.Dispose();
        Check("NE cleanup removes lifecycle subscriptions", manager.Subscribers==0 && Application.Subscribers==0);
        Check("NE cleanup stops feedback", !feedback.Available && feedback.IconAlpha(3.1)==0);
    }
    public static int Main()
    {
        NoodleLevel(false);
        NoodleLevel(true);
        // A following ordinary/ME level must regain normal cues despite NE being loaded.
        SongCore.Collections.Data = new SongCore.DifficultyData();
        SongCore.Collections.Data.additionalDifficultyData._requirements = new[] {"Mapping Extensions"};
        Plugin.Config=new PluginConfig();
        var manager = new BeatmapObjectManager();
        var player = new PlayerTransforms();
        var native = new PlayerHeadAndObstacleInteraction();
        var feedback = new CollisionFeedback();
        var cue = new CueController(manager,player,native,new GameplayCoreSceneSetupData(),feedback);
        cue.Initialize();
        var wall = new ObstacleController();manager.Spawn(wall);
        cue.LateTick();Application.Render();
        Check("following ME level has independent zero count", feedback.Available && feedback.Count==0);
        Check("following ME level restores frame path", WallVisual.Created==1 && WallVisual.Shown==1 && WallVisual.Reapplied==1 && player.Reads==1);
        native._intersectingObstacles.Add(wall); cue.LateTick();
        Check("ME level still counts native collisions", feedback.Count==1);
        cue.Dispose();
        Check("ordinary cleanup removes all subscriptions", manager.Subscribers==0 && Application.Subscribers==0);
        Check("controller reported no errors", Plugin.Log.Errors==0);
        Console.WriteLine("PASS: " + checks + " controller lifecycle checks with headless game/metadata doubles (not NE runtime)");
        return 0;
    }
}
