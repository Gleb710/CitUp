using CitUp.Sim;
using Xunit;

namespace Tests;

public class LivenessTrackerTests
{
    [Fact]
    public void Participant_IsSilentAfterTimeout()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(7, nowMs: 1000);

        Assert.Equal(new[] { 7 }, tracker.FindSilent(nowMs: 7001, timeoutMs: 6000));
    }

    [Fact]
    public void ExactlyAtTimeout_IsNotYetSilent()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(7, 1000);

        Assert.Empty(tracker.FindSilent(7000, 6000));
    }

    [Fact]
    public void Touch_ResetsTheTimer()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(7, 1000);

        tracker.Touch(7, 6000);

        Assert.Empty(tracker.FindSilent(9000, 6000));
        Assert.Equal(new[] { 7 }, tracker.FindSilent(12001, 6000));
    }

    [Fact]
    public void Removed_IsNotReported()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(7, 0);

        tracker.Remove(7);

        Assert.Empty(tracker.FindSilent(100000, 6000));
        Assert.False(tracker.IsTracked(7));
    }

    [Fact]
    public void OnlySilentOnesAreReturned_InOrder()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(30, 0);       // давно молчит
        tracker.Touch(10, 0);       // давно молчит
        tracker.Touch(20, 9000);    // недавно был

        Assert.Equal(new[] { 10, 30 }, tracker.FindSilent(10000, 6000));
    }

    [Fact]
    public void SilenceMs_ForUnknownId_IsNull()
    {
        var tracker = new LivenessTracker();

        Assert.Null(tracker.SilenceMs(5, 1000));
    }

    [Fact]
    public void SilenceMs_ReturnsTimeSinceLastTouch()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(5, 1000);

        Assert.Equal(2500, tracker.SilenceMs(5, 3500));
    }

    [Fact]
    public void Clear_ForgetsEveryone()
    {
        var tracker = new LivenessTracker();
        tracker.Touch(1, 0);
        tracker.Touch(2, 0);

        tracker.Clear();

        Assert.Equal(0, tracker.Count);
        Assert.Empty(tracker.FindSilent(100000, 6000));
    }
}