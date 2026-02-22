# appointment-service

Single-project ASP.NET Core Web API for appointment check-in flows.

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

## Current endpoint

- `POST /appointments/{id}/check-in`

On startup, the app seeds one sample appointment and prints its ID to the console so you can test the check-in endpoint.
