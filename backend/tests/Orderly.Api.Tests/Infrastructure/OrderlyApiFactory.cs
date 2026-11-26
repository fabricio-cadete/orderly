using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orderly.Api.Auth;
using Orderly.Api.Email;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orderly.Api.Tests.Infrastructure;

/// <summary>
/// Sobe a API contra um PostgreSQL de verdade. Usa Testcontainers (precisa de Docker), ou o banco
/// apontado por ORDERLY_TEST_CONNECTION quando essa variável existir.
/// </summary>
public sealed class OrderlyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container;
    private string _connectionString = Environment.GetEnvironmentVariable("ORDERLY_TEST_CONNECTION") ?? "";

    public OrderlyApiFactory()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        }
    }

    public CapturingEmailSender Emails { get; } = new();

    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderlyDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Orderly", _connectionString);
        builder.UseSetting("Auth:Jwt:SigningKey", "testing-signing-key-with-at-least-32-bytes!");
        builder.UseSetting("Auth:RateLimit:PermitsPerMinute", "10000");
        builder.UseSetting("Database:MigrateOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            services.RemoveAll<IExternalIdTokenValidator>();
            services.AddSingleton<IExternalIdTokenValidator>(new FakeIdTokenValidator(AuthProvider.Apple, configured: true));
            services.AddSingleton<IExternalIdTokenValidator>(new FakeIdTokenValidator(AuthProvider.Google, configured: false));
        });
    }
}

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public EmailMessage LastTo(string email) =>
        _messages.Last(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Aceita tokens no formato "valid|sub|email" no lugar de um id_token real.</summary>
public sealed class FakeIdTokenValidator(AuthProvider provider, bool configured) : IExternalIdTokenValidator
{
    public AuthProvider Provider => provider;

    public bool IsConfigured => configured;

    public Task<ExternalIdentity?> ValidateAsync(string idToken, CancellationToken ct)
    {
        var parts = idToken.Split('|');
        ExternalIdentity? identity = parts is ["valid", var sub, var email]
            ? new ExternalIdentity(provider, sub, email, EmailVerified: true, Name: null)
            : null;
        return Task.FromResult(identity);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<OrderlyApiFactory>
{
    public const string Name = "api";
}
