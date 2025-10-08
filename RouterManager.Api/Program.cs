using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RouterManager.Application.Interfaces;
using RouterManager.Infrastructure.Persistence;
using RouterManager.Infrastructure.Repositories;
using RouterManager.Infrastructure.Seed;
using System.Text;
using RouterManager.Application.Services;
using RouterManager.Application.Abstractions;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using RouterManager.Api.Middleware;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

var services = builder.Services;
var configuration = builder.Configuration;

services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.WriteIndented = false;
    });

// CORS: inclui portas do AdminWeb
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(
                "http://localhost",
                "https://localhost",
                "https://localhost:7070",
                "http://localhost:5283",   // API HTTP dev
                "https://localhost:44386",
                "http://localhost:5134",   // AdminWeb HTTP
                "https://localhost:7183"    // AdminWeb HTTPS
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Evita 400 automáticos por ModelState
services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Health checks necessários para MapHealthChecks
services.AddHealthChecks();

var conn = configuration.GetConnectionString("Default") ?? "Server=localhost;Database=RouterManagerDb;Trusted_Connection=True;TrustServerCertificate=True";
services.AddDbContext<RouterManagerDbContext>(o => o.UseSqlServer(conn));

services.AddScoped<ICredentialRepository, CredentialRepository>();
services.AddScoped<IRouterModelRepository, RouterModelRepository>();
services.AddScoped<IDeviceRepository, DeviceRepository>();
services.AddScoped<ITelemetryRepository, TelemetryRepository>();
services.AddScoped<IUpdateRepository, UpdateRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IProviderRepository, ProviderRepository>();
services.AddScoped<ICredentialLookupRepository, CredentialLookupRepository>();

services.AddScoped<ICredentialService, CredentialService>();
services.AddScoped<ITelemetryService, TelemetryService>();
services.AddScoped<IUpdateService, UpdateService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IProvidersService, ProvidersService>();
services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

// JWT
var jwtKey = configuration["Jwt:Key"] ?? "dev-secret-key-change";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "RouterManager";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidIssuer = jwtIssuer,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Rate limiting
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RouterManager API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandling();
app.UseMiddleware<ApiAuditMiddleware>();
// app.UseHttpsRedirection(); // mantenha comentado se rodando só HTTP no dev
app.UseCors(MyAllowSpecificOrigins);

// API Key middleware mantido para rotas públicas
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/v1/credentials")
        || ctx.Request.Path.StartsWithSegments("/api/providers")
        || ctx.Request.Path.StartsWithSegments("/api/v1/providers"),
    branch => { branch.UseMiddleware<RouterManager.Api.Middleware.ApiKeyMiddleware>(); }
);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
