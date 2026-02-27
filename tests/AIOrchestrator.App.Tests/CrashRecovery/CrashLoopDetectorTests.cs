using FluentAssertions;
using AIOrchestrator.App.CrashRecovery;

namespace AIOrchestrator.App.Tests.CrashRecovery;

public class CrashLoopDetectorTests
{
    [Fact]
    public void CrashLoopDetector_allows_single_restart()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDir);
        var detector = new CrashLoopDetector(dataDir);

        detector.RecordRestart();

        detector.ShouldEnterSafeMode().Should().BeFalse();
        detector.RestartCount.Should().Be(1);

        Directory.Delete(dataDir, recursive: true);
    }

    [Fact]
    public void CrashLoopDetector_allows_three_restarts_within_window()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDir);
        var detector = new CrashLoopDetector(dataDir);

        detector.RecordRestart();
        detector.RecordRestart();
        detector.RecordRestart();

        detector.ShouldEnterSafeMode().Should().BeFalse("at limit but not over");
        detector.RestartCount.Should().Be(3);

        Directory.Delete(dataDir, recursive: true);
    }

    [Fact]
    public void CrashLoopDetector_enters_safe_mode_on_fourth_restart()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDir);
        var detector = new CrashLoopDetector(dataDir);

        detector.RecordRestart();
        detector.RecordRestart();
        detector.RecordRestart();
        detector.RecordRestart();

        detector.ShouldEnterSafeMode().Should().BeTrue("exceeded max");
        detector.RestartCount.Should().Be(4);

        Directory.Delete(dataDir, recursive: true);
    }

    [Fact]
    public void CrashLoopDetector_resets_counter()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDir);
        var detector = new CrashLoopDetector(dataDir);
        detector.RecordRestart();
        detector.RecordRestart();

        detector.ResetCounter();

        detector.RestartCount.Should().Be(0);
        detector.ShouldEnterSafeMode().Should().BeFalse();

        Directory.Delete(dataDir, recursive: true);
    }

    [Fact]
    public void CrashLoopDetector_persists_restart_history()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDir);
        var detector = new CrashLoopDetector(dataDir);
        detector.RecordRestart();
        detector.RecordRestart();

        var detector2 = new CrashLoopDetector(dataDir);

        detector2.RestartCount.Should().Be(2, "history should be loaded from disk");

        Directory.Delete(dataDir, recursive: true);
    }
}
