using Amazon.S3;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Infrastructure.Services.User;
using Infrastructure.Services.Wasabi;

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
            services.AddScoped<WasabiService>();
            services.AddScoped<GeminiQuizService>();
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var settings = config.GetSection("WasabiSettings");

                return new AmazonS3Client(
                    settings["AccessKey"],
                    settings["SecretKey"],
                    new AmazonS3Config
                    {
                        ServiceURL = settings["ServiceUrl"],
                        ForcePathStyle = true
                    });
            });
        }
    }
}
