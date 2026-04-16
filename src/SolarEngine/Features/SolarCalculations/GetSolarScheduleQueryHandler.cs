using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SolarCalculations;

internal static class GetSolarScheduleQueryHandler
{
    public static ValueTask<Result<SolarSchedule>> HandleAsync(
        GetSolarScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Result<SolarSchedule> scheduleResult =
                SolarPositionEngine.Calculate(query.Date, query.Coordinates);

            return ValueTask.FromResult(scheduleResult);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Error error = new(
                "solar.schedule.invalid_coordinates",
                $"Reject invalid coordinate input before publishing a solar schedule. {exception.Message}");

            return ValueTask.FromResult(Result<SolarSchedule>.Failure(error));
        }
        catch (InvalidOperationException exception)
        {
            Error error = new(
                "solar.schedule.calculation_failed",
                $"Isolate solar calculation failures behind a deterministic application boundary. {exception.Message}");

            return ValueTask.FromResult(Result<SolarSchedule>.Failure(error));
        }
    }
}
