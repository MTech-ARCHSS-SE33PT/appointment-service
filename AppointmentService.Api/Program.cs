using AppointmentService.Api.Infrastructure;
using AppointmentService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(defaultConnection));
}

builder.Services.AddSingleton<IAppointmentRepository, InMemoryAppointmentRepository>();
builder.Services.AddSingleton<IEventPublisher, ConsoleEventPublisher>();
builder.Services.AddScoped<AppointmentManagementService>();
builder.Services.AddScoped<CheckInAppointmentHandler>();
builder.Services.AddScoped<SampleAppointmentSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SampleAppointmentSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.Run();
