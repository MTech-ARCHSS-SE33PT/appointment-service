using System.Text.Json;
using AppointmentService.Api.Services;

namespace AppointmentService.Api.Infrastructure;

public sealed class ConsoleEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine("---- EVENT PUBLISHED ----");
        Console.WriteLine(json);
        Console.WriteLine("-------------------------");

        return Task.CompletedTask;
    }
}
