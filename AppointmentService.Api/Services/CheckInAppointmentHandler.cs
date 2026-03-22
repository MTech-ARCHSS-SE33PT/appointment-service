using AppointmentService.Api.Events;

namespace AppointmentService.Api.Services;

public sealed class CheckInAppointmentHandler
{
    private readonly IAppointmentRepository _repository;
    private readonly IEventPublisher _publisher;

    public CheckInAppointmentHandler(
        IAppointmentRepository repository,
        IEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task CheckInAsync(Guid appointmentId, CancellationToken ct)
    {
        var appointment = await _repository.GetByIdAsync(appointmentId, ct);

        if (appointment is null)
            throw new KeyNotFoundException("Appointment not found.");

        appointment.CheckIn(DateTimeOffset.UtcNow);

        await _repository.UpdateAsync(appointment, ct);

        var evt = new AppointmentCheckedInEvent
        {
            AppointmentId = appointment.AppointmentId,
            TenantId = appointment.TenantId,
            ServiceId = appointment.ServiceId,
            PriorityLevel = appointment.PriorityLevel
        };

        await _publisher.PublishAsync(evt, ct);
    }
}
