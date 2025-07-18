using Infrastructure.Services.User;
using Application.Interfaces.User;
using System.IO;

namespace PresentationAPI.InjectServices
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
        }
    }
}
