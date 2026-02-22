namespace AppointmentService.Api.Events;

public sealed class AppointmentRescheduledEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = "appointment_rescheduled";
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid AppointmentId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ServiceId { get; init; }
    public DateTimeOffset PreviousSlotStart { get; init; }
    public DateTimeOffset PreviousSlotEnd { get; init; }
    public DateTimeOffset NewSlotStart { get; init; }
    public DateTimeOffset NewSlotEnd { get; init; }
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}
