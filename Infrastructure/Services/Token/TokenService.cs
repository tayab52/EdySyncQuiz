using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services.Token
{
    public class TokenService
    {
        public long UserID;
        public string Email = "";
        public bool IsAccessTokenExpired = true;

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                if (user.Identity is ClaimsIdentity identity)
                {
                    IEnumerable<Claim> claims = identity.Claims;
                    if (claims.Any())
                    {
                        UserID = Convert.ToInt64(identity.FindFirst("ID").Value);
                        Email = user.FindFirst(ClaimTypes.Email)?.Value!;
                        var expClaim = user.FindFirst("exp")?.Value;
                        if (long.TryParse(expClaim, out var expUnix))
                        {
                            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                            IsAccessTokenExpired = expiryDate < DateTime.UtcNow;
                        }
                    }
                }
            }
        }
    }
}
