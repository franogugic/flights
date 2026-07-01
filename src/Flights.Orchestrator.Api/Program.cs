var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Flights.Orchestrator.Api is running.");

app.Run();
