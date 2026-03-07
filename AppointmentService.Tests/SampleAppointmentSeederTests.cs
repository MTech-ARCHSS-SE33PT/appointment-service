using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Xunit;

namespace AppointmentService.Tests;

public sealed class SampleAppointmentSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsOneSampleAppointment()
    {
        var repository = new FakeAppointmentRepository();
        var seeder = new SampleAppointmentSeeder(repository);

        await seeder.SeedAsync(CancellationToken.None);

        Assert.Single(repository.AddedAppointments);
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public List<Appointment> AddedAppointments { get; } = new();

        public Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct)
            => Task.FromResult<Appointment?>(null);

        public Task AddAsync(Appointment appointment, CancellationToken ct)
        {
            AddedAppointments.Add(appointment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Appointment appointment, CancellationToken ct) => Task.CompletedTask;

        public Task<IReadOnlyList<Appointment>> GetTodayAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Appointment>>(Array.Empty<Appointment>());
    }
}
