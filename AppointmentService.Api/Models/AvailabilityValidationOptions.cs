namespace AppointmentService.Api.Models;

public sealed class AvailabilityValidationOptions
{
    public int SlotDurationMinutes { get; set; } = 15;
    public int CapacityPerSlot { get; set; } = 3;
    public string TimeZone { get; set; } = "Asia/Singapore";
}
