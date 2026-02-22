using AppointmentService.Api.Events;
using AppointmentService.Api.Models;

namespace AppointmentService.Api.Services;

public sealed class AppointmentManagementService
{
    private static readonly TimeSpan DefaultWalkInDuration = TimeSpan.FromMinutes(15);

    private readonly IAppointmentRepository _repository;
    private readonly IEventPublisher _publisher;

    public AppointmentManagementService(IAppointmentRepository repository, IEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Appointment> BookAppointmentAsync(BookAppointmentRequest request, CancellationToken ct)
    {
        var appointment = Appointment.CreateScheduled(
            userId: request.UserId,
            tenantId: request.TenantId,
            serviceId: request.ServiceId,
            slotStart: request.SlotStart,
            slotEnd: request.SlotEnd,
            priorityLevel: request.PriorityLevel ?? 0);

        await _repository.AddAsync(appointment, ct);

        await _publisher.PublishAsync(new AppointmentBookedEvent
        {
            AppointmentId = appointment.AppointmentId,
            TenantId = appointment.TenantId,
            ServiceId = appointment.ServiceId
        }, ct);

        return appointment;
    }

    public async Task<Appointment> RescheduleAppointmentAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken ct)
    {
        var appointment = await _repository.GetByIdAsync(appointmentId, ct);
        if (appointment is null)
            throw new KeyNotFoundException("Appointment not found.");

        var previousSlotStart = appointment.SlotStart;
        var previousSlotEnd = appointment.SlotEnd;

        appointment.Reschedule(request.NewSlotStart, request.NewSlotEnd);
        await _repository.UpdateAsync(appointment, ct);

        await _publisher.PublishAsync(new AppointmentRescheduledEvent
        {
            AppointmentId = appointment.AppointmentId,
            TenantId = appointment.TenantId,
            ServiceId = appointment.ServiceId,
            PreviousSlotStart = previousSlotStart,
            PreviousSlotEnd = previousSlotEnd,
            NewSlotStart = appointment.SlotStart,
            NewSlotEnd = appointment.SlotEnd
        }, ct);

        return appointment;
    }

    public async Task<Appointment> CancelAppointmentAsync(Guid appointmentId, CancelAppointmentRequest request, CancellationToken ct)
    {
        var appointment = await _repository.GetByIdAsync(appointmentId, ct);
        if (appointment is null)
            throw new KeyNotFoundException("Appointment not found.");

        appointment.Cancel();
        await _repository.UpdateAsync(appointment, ct);

        await _publisher.PublishAsync(new AppointmentCancelledEvent
        {
            AppointmentId = appointment.AppointmentId,
            TenantId = appointment.TenantId,
            ServiceId = appointment.ServiceId,
            Reason = request.Reason
        }, ct);

        return appointment;
    }

    public async Task<Appointment> CreateWalkInAsync(WalkInAppointmentRequest request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Appointment.CreateWalkIn(
            userId: request.UserId,
            tenantId: request.TenantId,
            serviceId: request.ServiceId,
            walkInTime: now,
            defaultDuration: DefaultWalkInDuration,
            priorityLevel: request.PriorityLevel ?? 0);

        await _repository.AddAsync(appointment, ct);

        await _publisher.PublishAsync(new AppointmentBookedEvent
        {
            AppointmentId = appointment.AppointmentId,
            TenantId = appointment.TenantId,
            ServiceId = appointment.ServiceId
        }, ct);

        return appointment;
    }

    public Task<IReadOnlyList<Appointment>> GetTodayAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
        => _repository.GetTodayAsync(tenantId, serviceId, date, ct);

    public async Task<Appointment> GetByIdAsync(Guid appointmentId, CancellationToken ct)
    {
        var appointment = await _repository.GetByIdAsync(appointmentId, ct);
        if (appointment is null)
            throw new KeyNotFoundException("Appointment not found.");

        return appointment;
    }
}
