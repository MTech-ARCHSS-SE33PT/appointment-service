using System.Reflection;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AppointmentService.Api.Services;

namespace AppointmentService.Api.Infrastructure;

public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ServiceBusSender _sender;

    public ServiceBusEventPublisher(ServiceBusClient client, string topicName)
    {
        _sender = client.CreateSender(topicName);
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct)
    {
        var subject = GetStringProperty(@event, "EventType") ?? typeof(T).Name;
        var correlationId = GetStringProperty(@event, "CorrelationId");

        var json = JsonSerializer.Serialize(@event, JsonOptions);
        var msg = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            Subject = subject
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
            msg.CorrelationId = correlationId;

        await _sender.SendMessageAsync(msg, ct);
    }

    private static string? GetStringProperty<T>(T instance, string propertyName)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.PropertyType == typeof(string) ? prop.GetValue(instance) as string : null;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }
}

