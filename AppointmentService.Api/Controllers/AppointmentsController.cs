using AppointmentService.Api.Models;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("appointments")]
public sealed class AppointmentsController : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Book(
        [FromBody] BookAppointmentRequest request,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        if (!IsValidBookingRequest(request, out var error))
            return BadRequest(new ErrorResponse(error));

        try
        {
            var appointment = await service.BookAppointmentAsync(request, ct);
            return Ok(AppointmentResponse.From(appointment));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        if (request.NewSlotEnd <= request.NewSlotStart)
            return BadRequest(new ErrorResponse("newSlotEnd must be after newSlotStart."));

        try
        {
            var appointment = await service.RescheduleAppointmentAsync(id, request, ct);
            return Ok(AppointmentResponse.From(appointment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelAppointmentRequest? request,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        try
        {
            var appointment = await service.CancelAppointmentAsync(id, request ?? new CancelAppointmentRequest(), ct);
            return Ok(AppointmentResponse.From(appointment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("walk-in")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWalkIn(
        [FromBody] WalkInAppointmentRequest request,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        if (!IsValidWalkInRequest(request, out var error))
            return BadRequest(new ErrorResponse(error));

        var appointment = await service.CreateWalkInAsync(request, ct);
        return Ok(AppointmentResponse.From(appointment));
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetToday(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly? date,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty || serviceId == Guid.Empty)
            return BadRequest(new ErrorResponse("tenantId and serviceId are required."));

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var appointments = await service.GetTodayAsync(tenantId, serviceId, targetDate, ct);
        return Ok(appointments.Select(AppointmentResponse.From).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] AppointmentManagementService service,
        CancellationToken ct)
    {
        try
        {
            var appointment = await service.GetByIdAsync(id, ct);
            return Ok(AppointmentResponse.From(appointment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/check-in")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(
        Guid id,
        [FromServices] CheckInAppointmentHandler handler,
        CancellationToken ct)
    {
        try
        {
            await handler.CheckInAsync(id, ct);
            return Ok(new MessageResponse("Appointment checked in successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
    }

    private static bool IsValidBookingRequest(BookAppointmentRequest request, out string error)
    {
        if (request.TenantId == Guid.Empty || request.UserId == Guid.Empty || request.ServiceId == Guid.Empty)
        {
            error = "tenantId, userId, and serviceId are required.";
            return false;
        }

        if (request.SlotEnd <= request.SlotStart)
        {
            error = "slotEnd must be after slotStart.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsValidWalkInRequest(WalkInAppointmentRequest request, out string error)
    {
        if (request.TenantId == Guid.Empty || request.UserId == Guid.Empty || request.ServiceId == Guid.Empty)
        {
            error = "tenantId, userId, and serviceId are required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
