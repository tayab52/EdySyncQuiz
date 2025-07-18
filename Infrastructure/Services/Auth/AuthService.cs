using Application.DataTransferModels.ResponseModel;
using Application.Interfaces.Auth;
using Infrastructure.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models.Entities.Users;

namespace Infrastructure.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ClientDBContext _clientDBContext;
        private IConfiguration _config;

        public AuthService(ClientDBContext clientDBContext, IConfiguration config)
        {
            this._clientDBContext = clientDBContext;
            this._config = config;
        }

        public string GenerateJWT(Domain.Models.Entities.Users.User user)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]));
            var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(_config["JWT:EncryptionKey"]));
            var encryptionKeyBytes = Convert.FromBase64String(_config["JWT:EncryptionKey"]);

            var claims = new List<Claim>
            {
                new Claim("sub", user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _config["JWT:ValidIssuer"],
                Audience = _config["JWT:ValidAudience"],
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),

                EncryptingCredentials = new EncryptingCredentials(
                    encryptionKey,
                    SecurityAlgorithms.Aes256KW,
                    SecurityAlgorithms.Aes256CbcHmacSha512)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(securityToken);
        }

        Task<ResponseVM> IAuthService.ForgotPassword(string email)
        {
            throw new NotImplementedException();
        }

        Task<ResponseVM> IAuthService.ResendOTP(string email)
        {
            throw new NotImplementedException();
        }

        Task<ResponseVM> IAuthService.SendOTP(string email)
        {
            throw new NotImplementedException();
        }

        Task<ResponseVM> IAuthService.VerifyOTP(string email, long otp)
        {
            throw new NotImplementedException();
        }
    }
}
