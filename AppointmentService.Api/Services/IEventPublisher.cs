namespace AppointmentService.Api.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct);
}
