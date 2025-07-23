using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services.Token
{
    public class TokenService
    {
        public string UserID = "";
        public string Email = "";

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
                        UserID = user.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                        Email = user.FindFirst(ClaimTypes.Email)?.Value!;
                    }
                }
            }
        }
    }
}
