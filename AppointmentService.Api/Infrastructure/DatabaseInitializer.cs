using AppointmentService.Api.Services;
using System.Data.Common;

namespace AppointmentService.Api.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly Func<DbConnection> _createConnection;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _createConnection = connectionFactory.CreateConnection;
    }

    public DatabaseInitializer(Func<DbConnection> createConnection)
    {
        _createConnection = createConnection;
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

        await using var connection = _createConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }
}
