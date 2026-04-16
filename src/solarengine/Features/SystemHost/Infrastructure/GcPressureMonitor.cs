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

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: true, compacting: true);
            }
            catch
            {
            }
            finally
            {
                Volatile.Write(ref _compactionScheduled, 0);
            }
        });
    }
}
