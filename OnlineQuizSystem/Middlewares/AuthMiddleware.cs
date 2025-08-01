using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.TokenVM;
using Application.Interfaces.Auth;
using CommonOperations.Constants;
using Infrastructure.Context;
using Infrastructure.Services.Token;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PresentationAPI.Middlewares
{
    public class AuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        private readonly RequestDelegate _next = next;
        private readonly IConfiguration _config = config;

        public async Task Invoke(HttpContext context, TokenService tokenService, AppDBContext dbContext, IAuthService authService)
        {
            var path = context.Request.Path.Value?.ToLower();
            string[] excludedPaths = [
                "/api/auth/signin",
                "/api/auth/signup",
                "/api/auth/verify-otp",
                "/api/auth/resend-otp",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/refresh",
                "/api/language"
            ];

            if (excludedPaths.Any(p => path!.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            var response = ResponseVM.Instance;

            if (!tokenService.IsAccessTokenExpired && tokenService.UserID != Guid.Empty)
            {
                await _next(context);
                return;
            }

            string? authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Missing or malformed authorization header.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            string accessToken = authHeader["Bearer ".Length..].Trim();

            Guid userId;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:ValidIssuer"],
                    ValidAudience = _config["Jwt:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!)),
                    TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(_config["JWT:EncryptionKey"]!)),
                    ValidateLifetime = false
                };

                var principal = handler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);
                var nameIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                userId = Guid.Parse(nameIdClaim?.Value ?? Guid.Empty.ToString());
            }
            catch
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid token.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            if (userId == Guid.Empty)
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid or missing user ID in token.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            var refreshToken = dbContext.RefreshTokens
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow || refreshToken.IsRevoked)
            {
                if (refreshToken != null)
                    authService.SignOut(new TokenRequestVM { RefreshToken = refreshToken.Token });

                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Session expired. Please login again.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            var authResult = authService.RefreshToken(new TokenRequestVM { RefreshToken = refreshToken.Token });

            if (authResult.StatusCode != ResponseCode.Success)
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Session expired. Please login again.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            context.Response.Headers["X-New-Access-Token"] = authResult.Data!.AccessToken;
            context.Response.Headers["X-New-Refresh-Token"] = authResult.Data.RefreshToken;
            context.Response.Headers.AccessControlExposeHeaders = "X-New-Access-Token, X-New-Refresh-Token";

            await _next(context);
        }
    }
}