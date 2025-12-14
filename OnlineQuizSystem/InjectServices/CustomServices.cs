using Amazon.S3;
using Application.Interfaces.Auth;
using Application.Interfaces.Gemini;
using Application.Interfaces.User;
using Infrastructure.Context;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Infrastructure.Services.User;
using Infrastructure.Services.Wasabi;
using Microsoft.Extensions.Configuration;
using static System.Net.Mime.MediaTypeNames;

namespace PresentationAPI.InjectServices
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            // User & Auth
            services.AddScoped<IAuthService, Infrastructure.Services.Auth.AuthService>();
            services.AddScoped<IUserService, Infrastructure.Services.User.UserService>();
            services.AddScoped<IUserDetailsService, Infrastructure.Services.User.UserDetailsService>();

            // Token & Wasabi
            services.AddScoped<TokenService>();
            services.AddScoped<WasabiService>();

            // Gemini HttpClient (named)
            services.AddHttpClient("GeminiClient", client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Quiz service with injected HttpClient
            services.AddScoped<IQuizService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var db = sp.GetRequiredService<AppDBContext>();
                var token = sp.GetRequiredService<TokenService>();
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("GeminiClient");
                return new GeminiQuizService(config, db, token, httpClient);
            });

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