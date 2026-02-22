namespace AppointmentService.Api.Models;

public sealed class RescheduleAppointmentRequest
{
    public DateTimeOffset NewSlotStart { get; init; }
    public DateTimeOffset NewSlotEnd { get; init; }
}
