using AppointmentService.Api.Models;

namespace AppointmentService.Api.Services;

public sealed class SampleAppointmentSeeder
{
    private readonly IAppointmentRepository _repository;

    public SampleAppointmentSeeder(IAppointmentRepository repository)
    {
        _repository = repository;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        var sampleAppointment = Appointment.CreateScheduled(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            serviceId: Guid.NewGuid(),
            slotStart: DateTimeOffset.UtcNow.AddMinutes(-10),
            slotEnd: DateTimeOffset.UtcNow.AddMinutes(20));

        await _repository.AddAsync(sampleAppointment, ct);
        Console.WriteLine($"Sample Appointment ID: {sampleAppointment.AppointmentId}");
    }
}
