using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AppointmentService.Api.Infrastructure;

public sealed class SqlAvailabilityValidator : IAvailabilityValidator
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly AvailabilityValidationOptions _options;
    private readonly TimeZoneInfo _timeZone;

    public SqlAvailabilityValidator(
        IDbConnectionFactory connectionFactory,
        IOptions<AvailabilityValidationOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _timeZone = ResolveTimeZone(_options.TimeZone);
    }

    public async Task ValidateScheduledSlotAsync(
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken ct)
    {
        if (slotEnd <= slotStart)
            throw new ArgumentException("slotEnd must be after slotStart.");

        var localStart = TimeZoneInfo.ConvertTime(slotStart, _timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(slotEnd, _timeZone);
        var localDate = DateOnly.FromDateTime(localStart.DateTime);

        if (DateOnly.FromDateTime(localEnd.DateTime) != localDate)
            throw new ArgumentException("Appointment slot must start and end on the same local date.");

        var dayOfWeek = ToDomainDayOfWeek(localDate.DayOfWeek);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime = TimeOnly.FromDateTime(localEnd.DateTime);

        if (!IsAlignedToSlotBoundary(startTime) || !IsAlignedToSlotBoundary(endTime))
            throw new ArgumentException($"Appointment time must align to {_options.SlotDurationMinutes}-minute slot boundaries.");

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        if (await HasActiveExceptionAsync(connection, tenantId, serviceId, localDate, ct))
            throw new ArgumentException("Selected date is closed for this service.");

        if (!await IsInsideActiveAvailabilityRuleAsync(connection, tenantId, serviceId, dayOfWeek, startTime, endTime, ct))
            throw new ArgumentException("Selected time is outside configured availability.");

        var bookedCount = await CountExistingAppointmentsAsync(connection, tenantId, serviceId, slotStart, ct);
        if (bookedCount >= _options.CapacityPerSlot)
            throw new ArgumentException("Selected slot is fully booked.");
    }

    private static async Task<bool> HasActiveExceptionAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid serviceId,
        DateOnly localDate,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM dbo.AvailabilityExceptions
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND ExceptionDate = @ExceptionDate
              AND IsActive = 1
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@ServiceId", serviceId);
        command.Parameters.AddWithValue("@ExceptionDate", localDate.ToDateTime(TimeOnly.MinValue));

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private static async Task<bool> IsInsideActiveAvailabilityRuleAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid serviceId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM dbo.AvailabilityRules
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND DayOfWeek = @DayOfWeek
              AND IsActive = 1
              AND OperatingStartTime <= @StartTime
              AND OperatingEndTime >= @EndTime
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@ServiceId", serviceId);
        command.Parameters.AddWithValue("@DayOfWeek", dayOfWeek);
        command.Parameters.AddWithValue("@StartTime", startTime.ToTimeSpan());
        command.Parameters.AddWithValue("@EndTime", endTime.ToTimeSpan());

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private static async Task<int> CountExistingAppointmentsAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset slotStart,
        CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Appointments
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND SlotStart = @SlotStart
              AND Status IN (1, 2)
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@ServiceId", serviceId);
        command.Parameters.AddWithValue("@SlotStart", slotStart);

        return (int)await command.ExecuteScalarAsync(ct);
    }

    private static int ToDomainDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    private bool IsAlignedToSlotBoundary(TimeOnly time)
    {
        var minutesSinceMidnight = (int)time.ToTimeSpan().TotalMinutes;
        return minutesSinceMidnight % _options.SlotDurationMinutes == 0;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
