using AppointmentService.Api.Infrastructure;
using AppointmentService.Api.Models;
using Xunit;

namespace AppointmentService.Tests.Infrastructure;

public sealed class AdoNetAppointmentRepositoryIntegrationTests
{
    [Fact]
    public async Task AddAndUpdate_RoundTrips_WhenSqlConnectionProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var factory = new SqlConnectionFactory(connectionString);
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync(CancellationToken.None);

        var repository = new AdoNetAppointmentRepository(factory);

        var appointment = Appointment.CreateScheduled(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            serviceId: Guid.NewGuid(),
            slotStart: DateTimeOffset.UtcNow.AddHours(1),
            slotEnd: DateTimeOffset.UtcNow.AddHours(2));

        await repository.AddAsync(appointment, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(appointment.AppointmentId, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(appointment.AppointmentId, loaded!.AppointmentId);

        loaded.CheckIn(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(loaded, CancellationToken.None);

        var updated = await repository.GetByIdAsync(appointment.AppointmentId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.CheckedIn, updated!.Status);
    }
}
