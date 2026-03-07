using AppointmentService.Api.Events;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentManagementServiceTests
{
    [Fact]
    public async Task BookAppointmentAsync_PersistsAppointment_AndPublishesBookedEvent()
    {
        var repository = new FakeAppointmentRepository();
        var publisher = new FakeEventPublisher();
        var service = new AppointmentManagementService(repository, publisher);

        var request = new BookAppointmentRequest
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            SlotStart = DateTimeOffset.UtcNow.AddHours(1),
            SlotEnd = DateTimeOffset.UtcNow.AddHours(1).AddMinutes(30),
            PriorityLevel = 1
        };

        var appointment = await service.BookAppointmentAsync(request, CancellationToken.None);

        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Equal(AppointmentType.Scheduled, appointment.AppointmentType);
        Assert.Single(repository.Appointments);

        var bookedEvent = Assert.IsType<AppointmentBookedEvent>(Assert.Single(publisher.Events));
        Assert.Equal(appointment.AppointmentId, bookedEvent.AppointmentId);
        Assert.Equal(request.TenantId, bookedEvent.TenantId);
        Assert.Equal(request.ServiceId, bookedEvent.ServiceId);
    }

    [Fact]
    public async Task CreateWalkInAsync_UsesDefaultDuration_AndPublishesBookedEvent()
    {
        var repository = new FakeAppointmentRepository();
        var publisher = new FakeEventPublisher();
        var service = new AppointmentManagementService(repository, publisher);

        var request = new WalkInAppointmentRequest
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            PriorityLevel = 2
        };

        var appointment = await service.CreateWalkInAsync(request, CancellationToken.None);

        Assert.Equal(AppointmentType.WalkIn, appointment.AppointmentType);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Equal(TimeSpan.FromMinutes(15), appointment.SlotEnd - appointment.SlotStart);
        Assert.Single(repository.Appointments);
        Assert.IsType<AppointmentBookedEvent>(Assert.Single(publisher.Events));
    }

    [Fact]
    public async Task CancelAppointmentAsync_WhenAppointmentMissing_ThrowsKeyNotFoundException()
    {
        var repository = new FakeAppointmentRepository();
        var publisher = new FakeEventPublisher();
        var service = new AppointmentManagementService(repository, publisher);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CancelAppointmentAsync(Guid.NewGuid(), new CancelAppointmentRequest(), CancellationToken.None));
    }

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public List<object> Events { get; } = new();

        public Task PublishAsync<T>(T @event, CancellationToken ct)
        {
            Events.Add(@event!);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        private readonly Dictionary<Guid, Appointment> _store = new();

        public IReadOnlyCollection<Appointment> Appointments => _store.Values.ToList();

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
            var results = _store.Values
                .Where(a => a.TenantId == tenantId &&
                            a.ServiceId == serviceId &&
                            DateOnly.FromDateTime(a.SlotStart.DateTime) == date)
                .ToList();

            return Task.FromResult<IReadOnlyList<Appointment>>(results);
        }
    }
}
