using System.Globalization;
using System.Text;
using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SolarCalculations;

internal static class GetSolarScheduleQueryHandler
{
    private const string CalculationFailedCode = "solar.schedule.calculation_failed";
    private const string InvalidCoordinatesCode = "solar.schedule.invalid_coordinates";
    private static readonly CompositeFormat s_calculationFailedDescriptionFormat =
        CompositeFormat.Parse("Isolate solar calculation failures behind a deterministic application boundary. {0}");
    private static readonly CompositeFormat s_invalidCoordinatesDescriptionFormat =
        CompositeFormat.Parse("Reject invalid coordinate input before publishing a solar schedule. {0}");

    public static ValueTask<Result<SolarSchedule>> HandleAsync(
        GetSolarScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Result<SolarSchedule> scheduleResult =
                SolarPositionEngine.Calculate(query.Date, query.Coordinates, query.TimeZone);

            return ValueTask.FromResult(scheduleResult);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Error error = new(
                InvalidCoordinatesCode,
                string.Format(
                    CultureInfo.InvariantCulture,
                    s_invalidCoordinatesDescriptionFormat,
                    exception.Message));

            return ValueTask.FromResult(Result<SolarSchedule>.Failure(error));
        }
        catch (UnexpectedStateException exception)
        {
            Error error = new(
                CalculationFailedCode,
                string.Format(
                    CultureInfo.InvariantCulture,
                    s_calculationFailedDescriptionFormat,
                    exception.Message));

            return ValueTask.FromResult(Result<SolarSchedule>.Failure(error));
        }
    }
}
