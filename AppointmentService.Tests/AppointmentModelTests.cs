using AppointmentService.Api.Models;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentModelTests
{
    [Fact]
    public void CheckIn_WhenBooked_UpdatesStatusAndTimestamp()
    {
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2));

        var checkInAt = DateTimeOffset.UtcNow;
        appointment.CheckIn(checkInAt);

        Assert.Equal(AppointmentStatus.CheckedIn, appointment.Status);
        Assert.Equal(checkInAt, appointment.CheckedInAt);
    }

    [Fact]
    public void Cancel_WhenCheckedIn_ThrowsInvalidOperationException()
    {
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2));
        appointment.CheckIn(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel());
    }

    [Fact]
    public void Reschedule_WhenCancelled_ThrowsInvalidOperationException()
    {
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2));
        appointment.Cancel();

        Assert.Throws<InvalidOperationException>(() =>
            appointment.Reschedule(DateTimeOffset.UtcNow.AddHours(3), DateTimeOffset.UtcNow.AddHours(4)));
    }
}
