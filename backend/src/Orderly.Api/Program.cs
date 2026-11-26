using Orderly.Api.Hubs;
using Orderly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddOrderlyInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<SyncHub>("/hubs/sync");

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
