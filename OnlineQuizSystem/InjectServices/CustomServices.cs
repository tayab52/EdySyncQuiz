using Amazon.S3;
using Application.Interfaces.Auth;
using Application.Interfaces.Gemini;
using Application.Interfaces.User;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Infrastructure.Services.User;
using Infrastructure.Services.Wasabi;
using Microsoft.Extensions.Configuration;

namespace PresentationAPI.InjectServices
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            // User & Auth
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserDetailsService, UserDetailsService>();

            // Quiz
            services.AddScoped<IQuizService, GeminiQuizService>();

            // Token & Wasabi
            services.AddScoped<TokenService>();
            services.AddScoped<WasabiService>();

            // Amazon S3 / Wasabi client
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
