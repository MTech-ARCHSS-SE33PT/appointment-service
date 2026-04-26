using AppointmentService.Api.Infrastructure;
using Xunit;

namespace AppointmentService.Tests;

public sealed class ConsoleEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_WritesSerializedEvent()
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            var publisher = new ConsoleEventPublisher();
            await publisher.PublishAsync(new { EventType = "TestEvent", Value = 42 }, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = writer.ToString();
        Assert.Contains("EVENT PUBLISHED", output);
        Assert.Contains("TestEvent", output);
        Assert.Contains("42", output);
    }
}
