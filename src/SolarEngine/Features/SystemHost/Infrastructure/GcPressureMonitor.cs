using System.Runtime;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal static class GcPressureMonitor
{
    private const int CompactionDelayMilliseconds = 250;
    private const int CompactionNotScheduled = 0;
    private const int CompactionScheduled = 1;

    private static int s_compactionScheduled;

    public static void ScheduleCompaction()
    {
        if (Interlocked.Exchange(ref s_compactionScheduled, CompactionScheduled) == CompactionScheduled)
        {
            return;
        }

        _ = Task.Run(static async () =>
        {
            try
            {
                await Task.Delay(
                        TimeSpan.FromMilliseconds(CompactionDelayMilliseconds))
                    .ConfigureAwait(false);

                if (Environment.HasShutdownStarted)
                {
                    return;
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: true, compacting: true);
            }
            catch
            {
            }
            finally
            {
                Volatile.Write(ref s_compactionScheduled, CompactionNotScheduled);
            }
        });
    }
}
