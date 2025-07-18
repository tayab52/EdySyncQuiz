using Infrastructure.Services.User;
using Application.Interfaces.User;
using Application.Interfaces.Auth;
using Infrastructure.Services.Auth;

namespace PresentationAPI.InjectServices
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
        }
    }
}
