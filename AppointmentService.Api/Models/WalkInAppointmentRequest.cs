namespace AppointmentService.Api.Models;

public sealed class WalkInAppointmentRequest
{
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public Guid ServiceId { get; init; }
    public int? PriorityLevel { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
}
