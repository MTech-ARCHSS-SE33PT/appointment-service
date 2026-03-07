using AppointmentService.Api.Controllers;
using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentsControllerTests
{
    [Fact]
    public async Task Book_WithInvalidSlotRange_ReturnsBadRequest()
    {
        var controller = new AppointmentsController();
        var service = CreateManagementService(new FakeAppointmentRepository(), new FakeEventPublisher());
        var now = DateTimeOffset.UtcNow;

        var result = await controller.Book(
            new BookAppointmentRequest
            {
                TenantId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                SlotStart = now,
                SlotEnd = now
            },
            service,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Contains("slotEnd must be after slotStart", payload.Error);
    }

    [Fact]
    public async Task CreateWalkIn_WithEmptyRequiredFields_ReturnsBadRequest()
    {
        var controller = new AppointmentsController();
        var service = CreateManagementService(new FakeAppointmentRepository(), new FakeEventPublisher());

        var result = await controller.CreateWalkIn(
            new WalkInAppointmentRequest(),
            service,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Contains("tenantId, userId, and serviceId are required", payload.Error);
    }

    [Fact]
    public async Task GetToday_WithEmptyTenantOrService_ReturnsBadRequest()
    {
        var controller = new AppointmentsController();
        var service = CreateManagementService(new FakeAppointmentRepository(), new FakeEventPublisher());

        var result = await controller.GetToday(Guid.Empty, Guid.Empty, null, service, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Contains("tenantId and serviceId are required", payload.Error);
    }

    [Fact]
    public async Task CheckIn_WhenAppointmentMissing_ReturnsNotFound()
    {
        var controller = new AppointmentsController();
        var repository = new FakeAppointmentRepository();
        var handler = new CheckInAppointmentHandler(repository, new FakeEventPublisher());

        var result = await controller.CheckIn(Guid.NewGuid(), handler, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.IsType<ErrorResponse>(notFound.Value);
    }

    [Fact]
    public void WalkInEndpoint_DoesNotAllowAnonymous()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.CreateWalkIn));
        Assert.NotNull(method);
        Assert.Empty(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void BookEndpoint_AllowsAnonymous()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.Book));
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    private static AppointmentManagementService CreateManagementService(IAppointmentRepository repository, IEventPublisher publisher)
        => new(repository, publisher);

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public Task PublishAsync<T>(T @event, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
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
            var appointments = _store.Values
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ServiceId == serviceId &&
                    DateOnly.FromDateTime(x.SlotStart.DateTime) == date)
                .ToList();

            return Task.FromResult<IReadOnlyList<Appointment>>(appointments);
        }
    }
}
