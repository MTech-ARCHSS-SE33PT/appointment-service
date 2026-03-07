using AppointmentService.Api.Events;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Xunit;

namespace AppointmentService.Tests;

public sealed class CheckInAppointmentHandlerTests
{
    [Fact]
    public async Task CheckInAsync_WhenMissing_ThrowsKeyNotFoundException()
    {
        var handler = new CheckInAppointmentHandler(new FakeRepository(), new FakePublisher());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.CheckInAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CheckInAsync_WhenFound_UpdatesAndPublishesEvent()
    {
        var repository = new FakeRepository();
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2));
        await repository.AddAsync(appointment, CancellationToken.None);

        var publisher = new FakePublisher();
        var handler = new CheckInAppointmentHandler(repository, publisher);

        await handler.CheckInAsync(appointment.AppointmentId, CancellationToken.None);

        var updated = await repository.GetByIdAsync(appointment.AppointmentId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.CheckedIn, updated!.Status);

        var evt = Assert.IsType<AppointmentCheckedInEvent>(Assert.Single(publisher.Events));
        Assert.Equal(appointment.AppointmentId, evt.AppointmentId);
    }

    private sealed class FakePublisher : IEventPublisher
    {
        public List<object> Events { get; } = new();
        public Task PublishAsync<T>(T @event, CancellationToken ct)
        {
            Events.Add(@event!);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : IAppointmentRepository
    {
        private readonly Dictionary<Guid, Appointment> _store = new();

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

        public Task<IReadOnlyList<Appointment>> GetTodayAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Appointment>>(Array.Empty<Appointment>());
    }
}
