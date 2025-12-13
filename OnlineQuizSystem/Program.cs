using Infrastructure.Context;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Infrastructure.Services.User;
using Infrastructure.Services.Wasabi;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Application.Interfaces.Gemini;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PresentationAPI.InjectServices;
using PresentationAPI.Middlewares;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 1. DATABASE CONFIG
// ----------------------
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString"))
);

// ----------------------
// 2. CUSTOM SERVICES
// ----------------------
builder.Services.AddCustomServices();

// ----------------------
// 3. CORS
// ----------------------
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

// ----------------------
// 4. JWT AUTHENTICATION
// ----------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);
var encKey = Convert.FromBase64String(jwtSettings["EncryptionKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings["ValidIssuer"],
        ValidAudience = jwtSettings["ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        TokenDecryptionKey = new SymmetricSecurityKey(encKey)
    };
});

// ----------------------
// 5. CONTROLLERS
// ----------------------
builder.Services.AddControllers();

// ----------------------
// 6. SWAGGER
// ----------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EduSync API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here"
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
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ----------------------
// 7. MIDDLEWARE PIPELINE
// ----------------------
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();

// Only apply authentication/authorization to non-MCP routes
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/mcp"), appBuilder =>
{
    appBuilder.UseAuthentication();
    appBuilder.UseMiddleware<AuthMiddleware>(); // Apply custom auth only to normal routes
    appBuilder.UseAuthorization();
});

app.MapControllers();

app.Run();
