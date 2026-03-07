using AppointmentService.Api.Infrastructure;
using AppointmentService.Api.Models;
using Xunit;

namespace AppointmentService.Tests.Infrastructure;

public sealed class InMemoryAppointmentRepositoryTests
{
    [Fact]
    public async Task AddGetUpdateAndGetToday_WorkAsExpected()
    {
        var repository = new InMemoryAppointmentRepository();
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todayAppointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            tenantId,
            serviceId,
            today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(9),
            today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10));

        var otherAppointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1));

        await repository.AddAsync(todayAppointment, CancellationToken.None);
        await repository.AddAsync(otherAppointment, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(todayAppointment.AppointmentId, CancellationToken.None);
        Assert.NotNull(loaded);

        loaded!.CheckIn(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(loaded, CancellationToken.None);

        var todayResults = await repository.GetTodayAsync(tenantId, serviceId, today, CancellationToken.None);
        Assert.Single(todayResults);
        Assert.Equal(AppointmentStatus.CheckedIn, todayResults[0].Status);
    }
}
