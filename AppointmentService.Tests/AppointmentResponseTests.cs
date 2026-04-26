using AppointmentService.Api.Models;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentResponseTests
{
    [Fact]
    public void From_MapsScheduledAppointment()
    {
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2),
            priorityLevel: 4);

        var response = AppointmentResponse.From(appointment);

        Assert.Equal(appointment.AppointmentId, response.AppointmentId);
        Assert.Equal(appointment.TenantId, response.TenantId);
        Assert.Equal(appointment.UserId, response.UserId);
        Assert.Equal(appointment.ServiceId, response.ServiceId);
        Assert.Equal(appointment.SlotStart, response.SlotStart);
        Assert.Equal(appointment.SlotEnd, response.SlotEnd);
        Assert.Equal("BOOKED", response.Status);
        Assert.Equal("SCHEDULED", response.Type);
        Assert.Equal(4, response.Priority);
        Assert.Equal(appointment.CreatedAt, response.CreatedAt);
        Assert.Null(response.CheckedInAt);
    }

    [Fact]
    public void From_MapsWalkInAppointment()
    {
        var appointment = Appointment.CreateWalkIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(15),
            priorityLevel: 2);
        appointment.CheckIn(DateTimeOffset.UtcNow);

        var response = AppointmentResponse.From(appointment);

        Assert.Equal("WALK_IN", response.Type);
        Assert.Equal("CHECKEDIN", response.Status);
        Assert.Equal(2, response.Priority);
        Assert.Equal(appointment.CheckedInAt, response.CheckedInAt);
    }
}
