using Amazon.S3;
using Application.DataTransferModels.ResponseModel;
using CommonOperations.Constants;
using Infrastructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PresentationAPI.InjectServices;
using PresentationAPI.Middlewares;
using System.Text;

namespace PresentationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("ConnectionString");
            builder.Services.AddDbContext<ClientDBContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddControllers();

            var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(builder.Configuration["JWT:EncryptionKey"]!));
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!));

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
                        ValidAudience = builder.Configuration["Jwt:ValidAudience"],
                        IssuerSigningKey = signingKey,
                        TokenDecryptionKey = encryptionKey,
                        ClockSkew = TimeSpan.Zero // disables the default 5-minute clock skew
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // Get the request path
                            var path = context.HttpContext.Request.Path.Value;

                            var excludedPaths = new[] {
                                "/api/auth/SignIn",
                                "/api/auth/SignUp",
                                "/api/auth/Verify-OTP",
                                "/api/auth/Resend-OTP",
                                "/api/auth/Forgot-Password",
                                "/api/auth/Reset-Password",
                                "/api/auth/Refresh",
                                "/api/language"
                            };

                            if (excludedPaths.Any(p => path!.Equals(p, StringComparison.OrdinalIgnoreCase)))
                            {
                                return Task.CompletedTask;
                            }

                            if (context.Exception is SecurityTokenExpiredException)
                            {
                                ResponseVM response = ResponseVM.Instance;
                                response.StatusCode = ResponseCode.ProxyAuthenticationRequired;
                                response.ErrorMessage = "Your session has expired. Please login again.";
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                context.Response.ContentType = "application/json";
                                return context.Response.WriteAsJsonAsync(response);
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddSingleton<IAmazonS3>(sp =>
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

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddCustomServices();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new() { Title = "Quiz App API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", 
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste your JWT token here. No need to add 'Bearer' prefix."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "bearer",  
                            Name = "Authorization",
                            In = ParameterLocation.Header
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!;

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            app.UseCors("AllowFrontend");

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseHttpsRedirection();

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
