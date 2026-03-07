using AppointmentService.Api.Events;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentManagementServiceMoreTests
{
    [Fact]
    public async Task RescheduleAppointmentAsync_WhenMissing_Throws()
    {
        var service = new AppointmentManagementService(new FakeRepository(), new FakePublisher());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RescheduleAppointmentAsync(Guid.NewGuid(), new RescheduleAppointmentRequest
        {
            NewSlotStart = DateTimeOffset.UtcNow.AddHours(1),
            NewSlotEnd = DateTimeOffset.UtcNow.AddHours(2)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_WhenFound_UpdatesAndPublishesEvent()
    {
        var repository = new FakeRepository();
        var appointment = Appointment.CreateScheduled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        await repository.AddAsync(appointment, CancellationToken.None);

        var publisher = new FakePublisher();
        var service = new AppointmentManagementService(repository, publisher);

        var updated = await service.RescheduleAppointmentAsync(appointment.AppointmentId, new RescheduleAppointmentRequest
        {
            NewSlotStart = DateTimeOffset.UtcNow.AddHours(3),
            NewSlotEnd = DateTimeOffset.UtcNow.AddHours(4)
        }, CancellationToken.None);

        Assert.Equal(appointment.AppointmentId, updated.AppointmentId);
        Assert.Equal(AppointmentStatus.Booked, updated.Status);
        Assert.IsType<AppointmentRescheduledEvent>(Assert.Single(publisher.Events));
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenFound_UpdatesAndPublishesEvent()
    {
        var repository = new FakeRepository();
        var appointment = Appointment.CreateScheduled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        await repository.AddAsync(appointment, CancellationToken.None);

        var publisher = new FakePublisher();
        var service = new AppointmentManagementService(repository, publisher);

        var cancelled = await service.CancelAppointmentAsync(appointment.AppointmentId, new CancelAppointmentRequest { Reason = "test" }, CancellationToken.None);

        Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
        var evt = Assert.IsType<AppointmentCancelledEvent>(Assert.Single(publisher.Events));
        Assert.Equal("test", evt.Reason);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_Throws()
    {
        var service = new AppointmentManagementService(new FakeRepository(), new FakePublisher());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetTodayAsync_ReturnsRepositoryData()
    {
        var repository = new FakeRepository();
        var service = new AppointmentManagementService(repository, new FakePublisher());
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 7);

        var appointment = Appointment.CreateScheduled(Guid.NewGuid(), tenantId, serviceId, new DateTimeOffset(2026, 3, 7, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 7, 10, 0, 0, TimeSpan.Zero));
        await repository.AddAsync(appointment, CancellationToken.None);

        var result = await service.GetTodayAsync(tenantId, serviceId, date, CancellationToken.None);

        Assert.Single(result);
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
        {
            var results = _store.Values.Where(x => x.TenantId == tenantId && x.ServiceId == serviceId && DateOnly.FromDateTime(x.SlotStart.DateTime) == date).ToList();
            return Task.FromResult<IReadOnlyList<Appointment>>(results);
        }
    }
}
