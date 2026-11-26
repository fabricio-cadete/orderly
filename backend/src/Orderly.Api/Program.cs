using System.Text.Json.Serialization;
using Orderly.Api.Auth;
using Orderly.Api.Endpoints;
using Orderly.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Orderly.Infrastructure;
using Orderly.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.AddOrderlyAuth();
builder.Services.AddOrderlyInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Em desenvolvimento (e no docker-compose) o banco é criado/atualizado ao subir a API.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrderlyDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapCategoryEndpoints();
app.MapTaskEndpoints();
app.MapHub<SyncHub>("/hubs/sync").RequireAuthorization();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
