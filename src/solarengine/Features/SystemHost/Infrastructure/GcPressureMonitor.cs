using System.Runtime;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal static class GcPressureMonitor
{
    private static int _compactionScheduled;

    public static void ScheduleCompaction()
    {
        if (Interlocked.Exchange(ref _compactionScheduled, 1) == 1)
        {
            return;
        }

        _ = Task.Run(static async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);

                if (Environment.HasShutdownStarted)
                {
                    return;
                }

                // Hiding the native settings window releases a burst of short-lived UI allocations.
                // A single post-hide compaction keeps the tray process stable in long-running idle scenarios
                // without putting explicit collections on the normal scheduler hot path.
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: true, compacting: true);
            }
            catch
            {
                // Compaction is best-effort. If the runtime rejects it, the app should continue running normally.
            }
            finally
            {
                Volatile.Write(ref _compactionScheduled, 0);
            }
        });
    }
}
