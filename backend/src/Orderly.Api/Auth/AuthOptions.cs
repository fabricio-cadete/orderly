namespace Orderly.Api.Auth;

public sealed class JwtOptions
{
    public const string Section = "Auth:Jwt";

    public string Issuer { get; set; } = "orderly";
    public string Audience { get; set; } = "orderly";

    /// <summary>Chave HMAC com pelo menos 32 bytes. Obrigatória fora de Development.</summary>
    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}

public sealed class MagicLinkOptions
{
    public const string Section = "Auth:MagicLink";

    /// <summary>Endereço que o e-mail abre; o token vai em ?token=. Pode ser web ou deep link do app.</summary>
    public string LinkBaseUrl { get; set; } = "http://localhost:5173/auth/magic";

    public int ExpiresMinutes { get; set; } = 15;
}

/// <summary>Configuração de Apple ou Google. Sem ClientIds, o provedor fica desligado.</summary>
public sealed class ExternalProviderOptions
{
    public const string AppleSection = "Auth:Apple";
    public const string GoogleSection = "Auth:Google";

    /// <summary>Client IDs aceitos como audiência do id_token (um por app: iOS, Android, web...).</summary>
    public string[] ClientIds { get; set; } = [];
}

public sealed class AuthRateLimitOptions
{
    public const string Section = "Auth:RateLimit";

    /// <summary>Requisições por minuto, por IP, nos endpoints de /auth.</summary>
    public int PermitsPerMinute { get; set; } = 10;
}
