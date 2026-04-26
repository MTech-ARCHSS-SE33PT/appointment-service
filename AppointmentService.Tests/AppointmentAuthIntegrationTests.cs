using AppointmentService.Api.Infrastructure;
using AppointmentService.Api.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AppointmentService.Tests;

public sealed class AppointmentAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppointmentAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "",
                    ["Auth:LocalIssuer"] = "IdentityService",
                    ["Auth:LocalAudience"] = "queuex-platform",
                    ["Auth:LocalSigningKey"] = "iX4UrgHAFL2ELwXtwFCWKhGghe98PPEC"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbConnectionFactory>();
                services.RemoveAll<DatabaseInitializer>();
                services.RemoveAll<IAppointmentRepository>();
                services.RemoveAll<IAvailabilityValidator>();
                services.AddSingleton<IAppointmentRepository, InMemoryAppointmentRepository>();
                services.AddScoped<IAvailabilityValidator, AllowAllAvailabilityValidator>();
            });
        });
    }

    [Fact]
    public async Task GetToday_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync($"/appointments/today?tenantId={Guid.NewGuid()}&serviceId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Book_WithoutToken_AllowsAnonymousPath()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/appointments", new
        {
            tenantId = Guid.Empty,
            userId = Guid.Empty,
            serviceId = Guid.Empty,
            slotStart = DateTimeOffset.UtcNow,
            slotEnd = DateTimeOffset.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WalkIn_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/appointments/walk-in", new
        {
            tenantId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            serviceId = Guid.NewGuid(),
            reason = "walk in"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
