using AppointmentService.Api.Services;

namespace AppointmentService.Api.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Appointments
                (
                    AppointmentId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    ServiceId UNIQUEIDENTIFIER NOT NULL,
                    AppointmentType INT NOT NULL,
                    SlotStart DATETIMEOFFSET NOT NULL,
                    SlotEnd DATETIMEOFFSET NOT NULL,
                    Status INT NOT NULL,
                    PriorityLevel INT NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL,
                    CheckedInAt DATETIMEOFFSET NULL,
                    QueueTicketId UNIQUEIDENTIFIER NULL
                );

                CREATE INDEX IX_Appointments_Tenant_Service_SlotStart
                    ON dbo.Appointments (TenantId, ServiceId, SlotStart);
            END
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }
}
