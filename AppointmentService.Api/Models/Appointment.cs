namespace AppointmentService.Api.Models;

public sealed class Appointment
{
    public Guid AppointmentId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ServiceId { get; private set; }

    public AppointmentType AppointmentType { get; private set; }
    public DateTimeOffset SlotStart { get; private set; }
    public DateTimeOffset SlotEnd { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public int PriorityLevel { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public Guid? QueueTicketId { get; private set; }

    private Appointment() { }

    private Appointment(
        Guid appointmentId,
        Guid userId,
        Guid tenantId,
        Guid serviceId,
        AppointmentType appointmentType,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        int priorityLevel,
        DateTimeOffset createdAt)
    {
        if (slotEnd <= slotStart)
            throw new ArgumentException("slot_end must be after slot_start.");

        AppointmentId = appointmentId;
        UserId = userId;
        TenantId = tenantId;
        ServiceId = serviceId;
        AppointmentType = appointmentType;
        SlotStart = slotStart;
        SlotEnd = slotEnd;
        PriorityLevel = priorityLevel < 0 ? 0 : priorityLevel;
        CreatedAt = createdAt;
        Status = AppointmentStatus.Booked;
    }

    public static Appointment CreateScheduled(
        Guid userId,
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        int priorityLevel = 0,
        DateTimeOffset? createdAt = null)
        => new(
            appointmentId: Guid.NewGuid(),
            userId: userId,
            tenantId: tenantId,
            serviceId: serviceId,
            appointmentType: AppointmentType.Scheduled,
            slotStart: slotStart,
            slotEnd: slotEnd,
            priorityLevel: priorityLevel,
            createdAt: createdAt ?? DateTimeOffset.UtcNow);

    public static Appointment CreateWalkIn(
        Guid userId,
        Guid tenantId,
        Guid serviceId,
        DateTimeOffset walkInTime,
        TimeSpan defaultDuration,
        int priorityLevel = 0,
        DateTimeOffset? createdAt = null)
        => new(
            appointmentId: Guid.NewGuid(),
            userId: userId,
            tenantId: tenantId,
            serviceId: serviceId,
            appointmentType: AppointmentType.WalkIn,
            slotStart: walkInTime,
            slotEnd: walkInTime.Add(defaultDuration),
            priorityLevel: priorityLevel,
            createdAt: createdAt ?? DateTimeOffset.UtcNow);

    public void CheckIn(DateTimeOffset checkedInAt)
    {
        if (Status != AppointmentStatus.Booked)
            throw new InvalidOperationException($"Cannot check-in when status is {Status}.");

        if (checkedInAt < CreatedAt.AddMinutes(-5))
            throw new InvalidOperationException("checked_in_at is invalid (earlier than created time).");

        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = checkedInAt;
    }

    public void MarkCompleted()
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new InvalidOperationException($"Cannot complete when status is {Status}.");

        Status = AppointmentStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.CheckedIn or AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel when status is {Status}.");

        Status = AppointmentStatus.Cancelled;
    }

    public void Reschedule(DateTimeOffset newSlotStart, DateTimeOffset newSlotEnd)
    {
        if (Status != AppointmentStatus.Booked)
            throw new InvalidOperationException($"Cannot reschedule when status is {Status}.");

        if (newSlotEnd <= newSlotStart)
            throw new ArgumentException("slot_end must be after slot_start.");

        SlotStart = newSlotStart;
        SlotEnd = newSlotEnd;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Booked)
            throw new InvalidOperationException($"Cannot mark no-show when status is {Status}.");

        Status = AppointmentStatus.NoShow;
    }

    public void LinkQueueTicket(Guid queueTicketId)
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new InvalidOperationException("Queue ticket can only be linked after check-in.");

        QueueTicketId = queueTicketId;
    }

    public static Appointment Rehydrate(
        Guid appointmentId,
        Guid userId,
        Guid tenantId,
        Guid serviceId,
        AppointmentType appointmentType,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        int priorityLevel,
        DateTimeOffset createdAt,
        AppointmentStatus status,
        DateTimeOffset? checkedInAt,
        Guid? queueTicketId)
    {
        var appointment = new Appointment(
            appointmentId,
            userId,
            tenantId,
            serviceId,
            appointmentType,
            slotStart,
            slotEnd,
            priorityLevel,
            createdAt);

        appointment.Status = status;
        appointment.CheckedInAt = checkedInAt;
        appointment.QueueTicketId = queueTicketId;

        return appointment;
    }
}
