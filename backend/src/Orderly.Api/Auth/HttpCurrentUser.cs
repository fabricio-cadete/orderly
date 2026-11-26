using Microsoft.IdentityModel.JsonWebTokens;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Auth;

/// <summary>Lê o usuário do JWT da requisição ("sub").</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
