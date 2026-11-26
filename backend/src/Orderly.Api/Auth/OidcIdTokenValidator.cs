using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Orderly.Domain.Entities;

namespace Orderly.Api.Auth;

/// <summary>
/// Valida id_tokens de provedores OpenID Connect usando as chaves públicas publicadas por eles
/// (baixadas e cacheadas a partir do documento de descoberta).
/// </summary>
internal sealed class OidcIdTokenValidator : IExternalIdTokenValidator
{
    private readonly string[] _issuers;
    private readonly string[] _clientIds;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configuration;
    private readonly ILogger _logger;

    private OidcIdTokenValidator(
        AuthProvider provider,
        string metadataAddress,
        string[] issuers,
        string[] clientIds,
        ILogger logger)
    {
        Provider = provider;
        _issuers = issuers;
        _clientIds = clientIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
        _logger = logger;
        _configuration = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public static OidcIdTokenValidator Apple(string[] clientIds, ILogger logger) => new(
        AuthProvider.Apple,
        "https://appleid.apple.com/.well-known/openid-configuration",
        ["https://appleid.apple.com"],
        clientIds,
        logger);

    public static OidcIdTokenValidator Google(string[] clientIds, ILogger logger) => new(
        AuthProvider.Google,
        "https://accounts.google.com/.well-known/openid-configuration",
        ["https://accounts.google.com", "accounts.google.com"],
        clientIds,
        logger);

    public AuthProvider Provider { get; }

    public bool IsConfigured => _clientIds.Length > 0;

    public async Task<ExternalIdentity?> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var config = await _configuration.GetConfigurationAsync(ct);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = _issuers,
            ValidAudiences = _clientIds,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        });

        if (!result.IsValid)
        {
            _logger.LogInformation(result.Exception, "id_token de {Provider} rejeitado", Provider);
            return null;
        }

        var claims = result.Claims;
        var subject = Claim(claims, JwtRegisteredClaimNames.Sub)?.ToString();
        if (string.IsNullOrEmpty(subject))
        {
            return null;
        }

        var email = Claim(claims, JwtRegisteredClaimNames.Email)?.ToString();
        var name = Claim(claims, "name")?.ToString();
        return new ExternalIdentity(Provider, subject, email, IsTrue(Claim(claims, "email_verified")), name);
    }

    private static object? Claim(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value : null;

    // Google manda email_verified como booleano; a Apple, às vezes, como texto "true".
    private static bool IsTrue(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var parsed) && parsed,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.String } e => bool.TryParse(e.GetString(), out var parsed) && parsed,
        _ => false,
    };
}
