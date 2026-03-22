using AppointmentService.Api.Events;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("debug/events")]
public sealed class DebugEventsController : ControllerBase
{
    [HttpPost("appointment-checked-in")]
    [AllowAnonymous]
    public async Task<IActionResult> PublishAppointmentCheckedIn(
        [FromServices] IEventPublisher publisher,
        CancellationToken ct)
    {
        var evt = new AppointmentCheckedInEvent
        {
            AppointmentId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            PriorityLevel = 1
        };

        await publisher.PublishAsync(evt, ct);
        return Ok(new { ok = true, published = evt.EventType, payload = evt });
    }
}

