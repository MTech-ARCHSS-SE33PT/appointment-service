using AppointmentService.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppointmentService.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task Database_WhenDbNotConfigured_ReturnsServiceUnavailable()
    {
        var controller = new HealthController();
        var provider = new ServiceCollection().BuildServiceProvider();

        var result = await controller.Database(provider, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }
}
