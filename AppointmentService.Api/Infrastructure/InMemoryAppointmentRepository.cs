using System.Collections.Concurrent;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;

namespace AppointmentService.Api.Infrastructure;

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly ConcurrentDictionary<Guid, Appointment> _store = new();

    public Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct)
    {
        _store.TryGetValue(appointmentId, out var appointment);
        return Task.FromResult(appointment);
    }

    public Task AddAsync(Appointment appointment, CancellationToken ct)
    {
        _store[appointment.AppointmentId] = appointment;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken ct)
    {
        _store[appointment.AppointmentId] = appointment;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Appointment>> GetTodayAsync(
        Guid tenantId,
        Guid serviceId,
        DateOnly date,
        CancellationToken ct)
    {
        var results = _store.Values
            .Where(a =>
                a.TenantId == tenantId &&
                a.ServiceId == serviceId &&
                DateOnly.FromDateTime(a.SlotStart.DateTime) == date)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Appointment>>(results);
    }
}
