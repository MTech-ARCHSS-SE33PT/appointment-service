using AppointmentService.Api.Models;

namespace AppointmentService.Api.Services;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct);
    Task AddAsync(Appointment appointment, CancellationToken ct);
    Task UpdateAsync(Appointment appointment, CancellationToken ct);
    Task<IReadOnlyList<Appointment>> GetTodayAsync(
        Guid tenantId,
        Guid serviceId,
        DateOnly date,
        CancellationToken ct);
}
