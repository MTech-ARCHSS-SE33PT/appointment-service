using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Microsoft.Data.SqlClient;

namespace AppointmentService.Api.Infrastructure;

public sealed class AdoNetAppointmentRepository : IAppointmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AdoNetAppointmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct)
    {
        const string sql = """
            SELECT AppointmentId, UserId, TenantId, ServiceId, AppointmentType, SlotStart, SlotEnd,
                   Status, PriorityLevel, CreatedAt, CheckedInAt, QueueTicketId
            FROM dbo.Appointments
            WHERE AppointmentId = @AppointmentId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AppointmentId", appointmentId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapAppointment(reader);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Appointments
            (
                AppointmentId, UserId, TenantId, ServiceId, AppointmentType, SlotStart, SlotEnd,
                Status, PriorityLevel, CreatedAt, CheckedInAt, QueueTicketId
            )
            VALUES
            (
                @AppointmentId, @UserId, @TenantId, @ServiceId, @AppointmentType, @SlotStart, @SlotEnd,
                @Status, @PriorityLevel, @CreatedAt, @CheckedInAt, @QueueTicketId
            )
            """;

        await ExecuteWriteAsync(sql, appointment, ct);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.Appointments
            SET Status = @Status,
                CheckedInAt = @CheckedInAt,
                QueueTicketId = @QueueTicketId,
                SlotStart = @SlotStart,
                SlotEnd = @SlotEnd,
                PriorityLevel = @PriorityLevel
            WHERE AppointmentId = @AppointmentId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddCommonParameters(command, appointment);

        var rows = await command.ExecuteNonQueryAsync(ct);
        if (rows == 0)
            throw new KeyNotFoundException("Appointment not found for update.");
    }

    public async Task<IReadOnlyList<Appointment>> GetTodayAsync(
        Guid tenantId,
        Guid serviceId,
        DateOnly date,
        CancellationToken ct)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        const string sql = """
            SELECT AppointmentId, UserId, TenantId, ServiceId, AppointmentType, SlotStart, SlotEnd,
                   Status, PriorityLevel, CreatedAt, CheckedInAt, QueueTicketId
            FROM dbo.Appointments
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND SlotStart >= @StartUtc
              AND SlotStart < @EndUtc
            ORDER BY SlotStart
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@ServiceId", serviceId);
        command.Parameters.AddWithValue("@StartUtc", new DateTimeOffset(start, TimeSpan.Zero));
        command.Parameters.AddWithValue("@EndUtc", new DateTimeOffset(end, TimeSpan.Zero));

        var results = new List<Appointment>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapAppointment(reader));
        }

        return results;
    }

    private async Task ExecuteWriteAsync(string sql, Appointment appointment, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddCommonParameters(command, appointment);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddCommonParameters(SqlCommand command, Appointment appointment)
    {
        command.Parameters.AddWithValue("@AppointmentId", appointment.AppointmentId);
        command.Parameters.AddWithValue("@UserId", appointment.UserId);
        command.Parameters.AddWithValue("@TenantId", appointment.TenantId);
        command.Parameters.AddWithValue("@ServiceId", appointment.ServiceId);
        command.Parameters.AddWithValue("@AppointmentType", (int)appointment.AppointmentType);
        command.Parameters.AddWithValue("@SlotStart", appointment.SlotStart);
        command.Parameters.AddWithValue("@SlotEnd", appointment.SlotEnd);
        command.Parameters.AddWithValue("@Status", (int)appointment.Status);
        command.Parameters.AddWithValue("@PriorityLevel", appointment.PriorityLevel);
        command.Parameters.AddWithValue("@CreatedAt", appointment.CreatedAt);
        command.Parameters.AddWithValue("@CheckedInAt", (object?)appointment.CheckedInAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@QueueTicketId", (object?)appointment.QueueTicketId ?? DBNull.Value);
    }

    private static Appointment MapAppointment(SqlDataReader reader)
    {
        return Appointment.Rehydrate(
            appointmentId: reader.GetGuid(0),
            userId: reader.GetGuid(1),
            tenantId: reader.GetGuid(2),
            serviceId: reader.GetGuid(3),
            appointmentType: (AppointmentType)reader.GetInt32(4),
            slotStart: reader.GetFieldValue<DateTimeOffset>(5),
            slotEnd: reader.GetFieldValue<DateTimeOffset>(6),
            priorityLevel: reader.GetInt32(8),
            createdAt: reader.GetFieldValue<DateTimeOffset>(9),
            status: (AppointmentStatus)reader.GetInt32(7),
            checkedInAt: reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
            queueTicketId: reader.IsDBNull(11) ? null : reader.GetGuid(11));
    }
}
