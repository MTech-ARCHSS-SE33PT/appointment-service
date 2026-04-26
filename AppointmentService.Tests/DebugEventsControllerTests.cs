using AppointmentService.Api.Controllers;
using AppointmentService.Api.Events;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AppointmentService.Tests;

public sealed class DebugEventsControllerTests
{
    [Fact]
    public async Task PublishAppointmentCheckedIn_PublishesEventAndReturnsOk()
    {
        var publisher = new CapturingPublisher();
        var controller = new DebugEventsController();

        var result = await controller.PublishAppointmentCheckedIn(publisher, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var published = Assert.IsType<AppointmentCheckedInEvent>(Assert.Single(publisher.Events));
        Assert.Equal("appointment_checked_in", published.EventType);
        Assert.Equal(1, published.PriorityLevel);
    }

    private sealed class CapturingPublisher : IEventPublisher
    {
        public List<object> Events { get; } = new();

        public Task PublishAsync<T>(T @event, CancellationToken ct)
        {
            Events.Add(@event!);
            return Task.CompletedTask;
        }
    }
}
