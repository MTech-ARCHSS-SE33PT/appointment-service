namespace AppointmentService.Api.Events;

public sealed class AppointmentCancelledEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = "appointment_cancelled";
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid AppointmentId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ServiceId { get; init; }
    public string? Reason { get; init; }
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}
