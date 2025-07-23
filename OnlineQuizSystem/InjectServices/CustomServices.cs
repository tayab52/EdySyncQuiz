using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Token;
using Infrastructure.Services.User;

namespace PresentationAPI.InjectServices
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserDetailsService, UserDetailsService>();
            services.AddScoped<TokenService>();
        }
    }
}
