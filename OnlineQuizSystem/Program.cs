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
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.Server;
using System.ComponentModel;


var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://localhost:3003");
// 1. DATABASE CONFIG
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString"))
);

// 2. CUSTOM SERVICES
builder.Services.AddCustomServices();

// 3. CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
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

// 4. JWT AUTHENTICATION
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

// 5. CONTROLLERS
builder.Services.AddControllers();

// 6. SWAGGER
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
// 1) Register MCP server and tools
builder.Services.AddMcpServer()
    .WithHttpTransport()            // expose MCP over HTTP
    .WithToolsFromAssembly();       // scan this assembly for tool classes

var app = builder.Build();

// 7. FORWARDED HEADERS (for reverse proxies / containers)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 8. CORS
app.UseCors("AllowFrontend");

// 9. SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (app.Environment.IsProduction())
{
    // Keep Swagger available but inform users of route restriction
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 10. HTTPS REDIRECTION (optional in containers; keep if using TLS terminator upstream)
app.UseHttpsRedirection();

// 11. GLOBAL ERROR HANDLING
app.UseMiddleware<ErrorHandlingMiddleware>();

// 12. AUTH: only apply to non-MCP routes
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/mcp"), appBuilder =>
{
    appBuilder.UseAuthentication();
    appBuilder.UseMiddleware<AuthMiddleware>();
    appBuilder.UseAuthorization();
});

// 13. MCP ROUTE RESTRICTION IN PRODUCTION
if (app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        // Allow only MCP quiz endpoints
        if (!path.StartsWith("/mcp/quiz"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Endpoint not available in MCP container.");
            return;
        }
        await next();
    });
}

// 14. MAP CONTROLLERS
app.MapControllers();

app.MapMcp();
// Optional health endpoint for container readiness/liveness
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();