namespace AppointmentService.Api.Services;

public interface IAvailabilityValidator
{
    Task ValidateScheduledSlotAsync(
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken ct);
}

public sealed class AllowAllAvailabilityValidator : IAvailabilityValidator
{
    public Task ValidateScheduledSlotAsync(
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken ct) => Task.CompletedTask;
}
