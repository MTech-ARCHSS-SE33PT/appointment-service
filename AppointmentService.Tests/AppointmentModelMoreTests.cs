using AppointmentService.Api.Models;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentModelMoreTests
{
    [Fact]
    public void CreateScheduled_WithInvalidRange_Throws()
    {
        var slot = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            slot,
            slot));
    }

    [Fact]
    public void MarkCompleted_WhenNotCheckedIn_Throws()
    {
        var appointment = CreateBooked();
        Assert.Throws<InvalidOperationException>(() => appointment.MarkCompleted());
    }

    [Fact]
    public void MarkCompleted_WhenCheckedIn_SetsCompleted()
    {
        var appointment = CreateBooked();
        appointment.CheckIn(DateTimeOffset.UtcNow);

        appointment.MarkCompleted();

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void MarkNoShow_WhenBooked_SetsNoShow()
    {
        var appointment = CreateBooked();
        appointment.MarkNoShow();
        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
    }

    [Fact]
    public void MarkNoShow_WhenNotBooked_Throws()
    {
        var appointment = CreateBooked();
        appointment.CheckIn(DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => appointment.MarkNoShow());
    }

    [Fact]
    public void LinkQueueTicket_WhenCheckedIn_SetsQueueTicketId()
    {
        var appointment = CreateBooked();
        appointment.CheckIn(DateTimeOffset.UtcNow);
        var ticketId = Guid.NewGuid();

        appointment.LinkQueueTicket(ticketId);

        Assert.Equal(ticketId, appointment.QueueTicketId);
    }

    [Fact]
    public void LinkQueueTicket_WhenNotCheckedIn_Throws()
    {
        var appointment = CreateBooked();
        Assert.Throws<InvalidOperationException>(() => appointment.LinkQueueTicket(Guid.NewGuid()));
    }

    [Fact]
    public void CheckIn_WhenEarlierThanCreated_Throws()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt.AddHours(1),
            createdAt.AddHours(2),
            createdAt: createdAt);

        Assert.Throws<InvalidOperationException>(() => appointment.CheckIn(createdAt.AddMinutes(-6)));
    }

    [Fact]
    public void Rehydrate_PreservesPersistedFields()
    {
        var appointment = Appointment.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentType.WalkIn,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15),
            2,
            DateTimeOffset.UtcNow,
            AppointmentStatus.CheckedIn,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.Equal(AppointmentStatus.CheckedIn, appointment.Status);
        Assert.NotNull(appointment.CheckedInAt);
        Assert.NotNull(appointment.QueueTicketId);
    }

    private static Appointment CreateBooked() => Appointment.CreateScheduled(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        DateTimeOffset.UtcNow.AddHours(1),
        DateTimeOffset.UtcNow.AddHours(2));
}
