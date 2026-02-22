# appointment-service

Single-project ASP.NET Core Web API for appointment lifecycle flows (book, walk-in, check-in, reschedule, cancel) using an in-memory repository.

## Structure

The codebase is organized by folders inside `AppointmentService.Api`:

- `Controllers` - HTTP endpoints
- `Services` - application logic and interfaces
- `Models` - domain models and API response models
- `Events` - event payloads
- `Infrastructure` - repository and event publisher implementations

## Run

```bash
dotnet restore
dotnet build AppointmentService.sln
dotnet run --project AppointmentService.Api
```

## Endpoints

- `POST /appointments` - Book appointment
- `GET /appointments/{id}` - Get appointment by id
- `GET /appointments/today?tenantId=...&serviceId=...&date=YYYY-MM-DD` - Today's appointments (scheduled + walk-ins)
- `POST /appointments/walk-in` - Create walk-in appointment
- `POST /appointments/{id}/check-in` - Check in appointment
- `PATCH /appointments/{id}` - Reschedule appointment
- `POST /appointments/{id}/cancel` - Cancel appointment
- `GET /health/db` - SQL connectivity check (returns not-configured when running in-memory only)

## Notes

- Default runtime is in-memory (`InMemoryAppointmentRepository`).
- SQL-related classes exist in the repo for later use, but are not active by default.
- On startup, the app seeds one sample appointment and prints its ID to the console for testing.
- Example HTTP requests for all endpoints are available in `AppointmentService.Api/AppointmentService.Api.http`.
