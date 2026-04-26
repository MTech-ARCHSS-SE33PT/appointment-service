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
    public async Task BookAppointmentAsync_WhenSlotUnavailable_DoesNotPersistOrPublish()
    {
        var repository = new FakeAppointmentRepository();
        var publisher = new FakeEventPublisher();
        var service = new AppointmentManagementService(
            repository,
            publisher,
            new RejectingAvailabilityValidator("Selected time is outside configured availability."));

        var request = new BookAppointmentRequest
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            SlotStart = DateTimeOffset.UtcNow.AddHours(1),
            SlotEnd = DateTimeOffset.UtcNow.AddHours(1).AddMinutes(30)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.BookAppointmentAsync(request, CancellationToken.None));

        Assert.Contains("outside configured availability", ex.Message);
        Assert.Empty(repository.Appointments);
        Assert.Empty(publisher.Events);
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

    [Fact]
    public async Task RescheduleAppointmentAsync_UpdatesSlot_AndPublishesPreviousAndNewTimes()
    {
        var repository = new FakeAppointmentRepository();
        var publisher = new FakeEventPublisher();
        var service = new AppointmentManagementService(repository, publisher);
        var originalStart = DateTimeOffset.UtcNow.AddHours(1);
        var originalEnd = originalStart.AddMinutes(30);
        var appointment = Appointment.CreateScheduled(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            originalStart,
            originalEnd,
            priorityLevel: 0);
        await repository.AddAsync(appointment, CancellationToken.None);

        var newStart = originalStart.AddDays(1);
        var newEnd = newStart.AddMinutes(45);

        var result = await service.RescheduleAppointmentAsync(
            appointment.AppointmentId,
            new RescheduleAppointmentRequest
            {
                NewSlotStart = newStart,
                NewSlotEnd = newEnd
            },
            CancellationToken.None);

        Assert.Equal(newStart, result.SlotStart);
        Assert.Equal(newEnd, result.SlotEnd);

        var rescheduledEvent = Assert.IsType<AppointmentRescheduledEvent>(Assert.Single(publisher.Events));
        Assert.Equal(originalStart, rescheduledEvent.PreviousSlotStart);
        Assert.Equal(originalEnd, rescheduledEvent.PreviousSlotEnd);
        Assert.Equal(newStart, rescheduledEvent.NewSlotStart);
        Assert.Equal(newEnd, rescheduledEvent.NewSlotEnd);
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

    private sealed class RejectingAvailabilityValidator : IAvailabilityValidator
    {
        private readonly string _message;

        public RejectingAvailabilityValidator(string message)
        {
            _message = message;
        }

        public Task ValidateScheduledSlotAsync(
            Guid tenantId,
            Guid serviceId,
            DateTimeOffset slotStart,
            DateTimeOffset slotEnd,
            CancellationToken ct)
        {
            throw new ArgumentException(_message);
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
