using AppointmentService.Api.Controllers;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentsControllerMoreTests
{
    [Fact]
    public async Task Reschedule_WithInvalidRange_ReturnsBadRequest()
    {
        var controller = new AppointmentsController();
        var service = CreateService(new FakeRepository(), new FakePublisher());

        var result = await controller.Reschedule(Guid.NewGuid(), new RescheduleAppointmentRequest
        {
            NewSlotStart = DateTimeOffset.UtcNow,
            NewSlotEnd = DateTimeOffset.UtcNow
        }, service, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Reschedule_WhenMissing_ReturnsNotFound()
    {
        var controller = new AppointmentsController();
        var service = CreateService(new FakeRepository(), new FakePublisher());

        var result = await controller.Reschedule(Guid.NewGuid(), new RescheduleAppointmentRequest
        {
            NewSlotStart = DateTimeOffset.UtcNow.AddHours(1),
            NewSlotEnd = DateTimeOffset.UtcNow.AddHours(2)
        }, service, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenCheckedIn_ReturnsConflict()
    {
        var repository = new FakeRepository();
        var appointment = Appointment.CreateScheduled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        appointment.CheckIn(DateTimeOffset.UtcNow);
        await repository.AddAsync(appointment, CancellationToken.None);

        var controller = new AppointmentsController();
        var service = CreateService(repository, new FakePublisher());

        var result = await controller.Cancel(appointment.AppointmentId, new CancelAppointmentRequest(), service, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenMissing_ReturnsNotFound()
    {
        var controller = new AppointmentsController();
        var service = CreateService(new FakeRepository(), new FakePublisher());

        var result = await controller.Cancel(Guid.NewGuid(), new CancelAppointmentRequest(), service, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var controller = new AppointmentsController();
        var service = CreateService(new FakeRepository(), new FakePublisher());

        var result = await controller.GetById(Guid.NewGuid(), service, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CheckIn_WhenAlreadyCheckedIn_ReturnsConflict()
    {
        var repository = new FakeRepository();
        var appointment = Appointment.CreateScheduled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        appointment.CheckIn(DateTimeOffset.UtcNow);
        await repository.AddAsync(appointment, CancellationToken.None);

        var controller = new AppointmentsController();
        var handler = new CheckInAppointmentHandler(repository, new FakePublisher());

        var result = await controller.CheckIn(appointment.AppointmentId, handler, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private static AppointmentManagementService CreateService(IAppointmentRepository repository, IEventPublisher publisher)
        => new(repository, publisher);

    private sealed class FakePublisher : IEventPublisher
    {
        public Task PublishAsync<T>(T @event, CancellationToken ct) => Task.CompletedTask;
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
