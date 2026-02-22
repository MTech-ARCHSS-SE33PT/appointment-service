namespace AppointmentService.Api.Models;

public sealed class AppointmentResponse
{
    public Guid AppointmentId { get; init; }
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public Guid ServiceId { get; init; }
    public DateTimeOffset SlotStart { get; init; }
    public DateTimeOffset SlotEnd { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Priority { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CheckedInAt { get; init; }

    public static AppointmentResponse From(Appointment appointment) => new()
    {
        AppointmentId = appointment.AppointmentId,
        TenantId = appointment.TenantId,
        UserId = appointment.UserId,
        ServiceId = appointment.ServiceId,
        SlotStart = appointment.SlotStart,
        SlotEnd = appointment.SlotEnd,
        Status = appointment.Status.ToString().ToUpperInvariant(),
        Type = appointment.AppointmentType == AppointmentType.WalkIn ? "WALK_IN" : "SCHEDULED",
        Priority = appointment.PriorityLevel,
        CreatedAt = appointment.CreatedAt,
        CheckedInAt = appointment.CheckedInAt
    };
}
