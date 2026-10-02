using System;
using WallCue;

internal static class CollisionFeedbackTests
{
    private static int checks;
    private static void Check(string name, bool ok) { if (!ok) throw new Exception(name); checks++; }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < 0.0001f; }
    public static int Main()
    {
        var f = new CollisionFeedback();
        var wall = new object(); var second = new object();
        f.Track(wall);
        Check("not counting before native detector is ready", !f.Record(wall, 10));
        Check("no startup icon", f.IconAlpha(10) == 0);
        Check("no startup red count", !f.CountIsRed(10));
        f.Start();
        Check("first hit recorded", f.Record(wall, 10) && f.Count == 1);
        Check("red immediately", f.CountIsRed(10));
        Check("fade starts transparent", Near(f.IconAlpha(10), 0));
        Check("fade-in halfway", Near(f.IconAlpha(10.05), .5f));
        Check("fade-in reaches full", Near(f.IconAlpha(10.1), 1));
        Check("hold remains full", Near(f.IconAlpha(10.399), 1));
        Check("fade-out halfway", Near(f.IconAlpha(10.45), .5f));
        Check("icon hidden after total 0.5s", Near(f.IconAlpha(10.5), 0));
        Check("red held through 0.499s", f.CountIsRed(10.499));
        Check("red ends at 0.5s", !f.CountIsRed(10.5));
        Check("continuous collision never recounts", !f.Record(wall, 11) && f.Count == 1);
        Check("same wall reentry never recounts", !f.Record(wall, 20) && f.Count == 1);
        Check("reentry does not restart feedback", f.IconAlpha(20) == 0 && !f.CountIsRed(20));
        Check("untracked wall rejected", !f.Record(second, 21));
        f.Track(second);
        Check("different wall counted", f.Record(second, 21) && f.Count == 2);
        f.Release(second);
        Check("thin-wall despawn keeps fade", Near(f.IconAlpha(21.05), .5f));
        Check("thin-wall despawn keeps red count", f.CountIsRed(21.25));
        Check("stale native wall rejected after despawn", !f.Record(second, 21.3));
        f.Track(second);
        Check("reused pooled controller represents new wall", f.Record(second, 21.3) && f.Count == 3);
        Check("repeat hit during hold stays visible", Near(f.IconAlpha(21.3), 1));
        Check("repeat hit restarts red hold", f.CountIsRed(21.75));
        Check("repeat hit extends icon hold", Near(f.IconAlpha(21.65), 1));
        var third = new object(); f.Track(third);
        Check("new hit during fade-out counted", f.Record(third, 21.75));
        Check("fade-out retrigger preserves current alpha", Near(f.IconAlpha(21.75), .5f));
        Check("retrigger smoothly returns to full", Near(f.IconAlpha(21.80), .75f));
        Check("no movement or dependence on wall lifetime in envelope", Near(f.IconAlpha(21.9), 1));
        var fourth = new object(); f.Track(fourth);
        Check("simultaneous different wall counts separately", f.Record(fourth, 21.75) && f.Count == 5);
        Check("one fixed envelope for simultaneous hits", Near(f.IconAlpha(21.8), .75f));
        f.Stop();
        Check("scene teardown clears icon", f.IconAlpha(21.85) == 0);
        Check("scene teardown clears red", !f.CountIsRed(21.85));
        Check("scene teardown marks detector unavailable", !f.Available);
        Check("total retained for end-of-level diagnostic", f.Count == 5);
        Check("stopped scene cannot add hits", !f.Record(fourth, 22));
        var fresh = new CollisionFeedback(); fresh.Start();
        Check("next level starts from zero", fresh.Count == 0 && fresh.IconAlpha(22) == 0);
        var a = new object(); var b = new object(); fresh.Track(a);fresh.Track(b);
        fresh.Record(a, 30);fresh.Record(b, 30.05);
        Check("retrigger during fade-in stays continuous", Near(fresh.IconAlpha(30.05), .5f));
        Check("retrigger fade-in reaches full after 0.1s", Near(fresh.IconAlpha(30.15), 1));
        Console.WriteLine("PASS: " + checks + " collision counting / independent feedback lifetime checks using production CollisionFeedback.cs");
        return 0;
    }
}
