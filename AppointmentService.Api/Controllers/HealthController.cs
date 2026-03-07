using AppointmentService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("db")]
    [AllowAnonymous]
    public async Task<IActionResult> Database([FromServices] IServiceProvider services, CancellationToken ct)
    {
        var connectionFactory = services.GetService<IDbConnectionFactory>();
        if (connectionFactory is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "disabled",
                database = "not-configured"
            });
        }

        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(ct);

            return Ok(new
            {
                status = "ok",
                database = "reachable",
                result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "error",
                database = "unreachable",
                error = ex.Message
            });
        }
    }
}
