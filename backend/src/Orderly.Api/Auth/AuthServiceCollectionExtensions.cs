using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orderly.Api.Email;
using Orderly.Api.Endpoints;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Auth;

internal static class AuthServiceCollectionExtensions
{
    private const string DevelopmentSigningKey = "orderly-dev-only-signing-key-change-me-0123456789";

    public static WebApplicationBuilder AddOrderlyAuth(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException($"{JwtOptions.Section}:SigningKey não configurada.");
            }

            jwt.SigningKey = DevelopmentSigningKey;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
        {
            throw new InvalidOperationException($"{JwtOptions.Section}:SigningKey precisa de pelo menos 32 bytes.");
        }

        services.AddSingleton(Options.Create(jwt));
        services.Configure<MagicLinkOptions>(configuration.GetSection(MagicLinkOptions.Section));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<TokenService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AuthDeps>();

        var apple = configuration.GetSection(ExternalProviderOptions.AppleSection).Get<ExternalProviderOptions>() ?? new();
        var google = configuration.GetSection(ExternalProviderOptions.GoogleSection).Get<ExternalProviderOptions>() ?? new();
        services.AddSingleton<IExternalIdTokenValidator>(sp =>
            OidcIdTokenValidator.Apple(apple.ClientIds, sp.GetRequiredService<ILoggerFactory>().CreateLogger("Auth.Apple")));
        services.AddSingleton<IExternalIdTokenValidator>(sp =>
            OidcIdTokenValidator.Google(google.ClientIds, sp.GetRequiredService<ILoggerFactory>().CreateLogger("Auth.Google")));

        var smtpHost = configuration[$"{EmailOptions.Section}:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = TokenService.SigningKey(jwt),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                // WebSockets não mandam cabeçalho: o SignalR passa o token em ?access_token=.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });
        services.AddAuthorization();

        var rateLimit = configuration.GetSection(AuthRateLimitOptions.Section).Get<AuthRateLimitOptions>() ?? new();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthEndpoints.RateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.PermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return builder;
    }
}
