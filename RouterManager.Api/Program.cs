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
using Microsoft.AspNetCore.Authentication;
using RouterManager.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

var services = builder.Services;
var configuration = builder.Configuration;
var env = builder.Environment;

services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.WriteIndented = false;
    });

// CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(
                "http://localhost",
                "https://localhost",
                "https://localhost:7070",
                "http://localhost:5283",
                "https://localhost:44386",
                "http://localhost:5134",
                "https://localhost:7183"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

services.AddHealthChecks();

// Rate Limiting (re-add)
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
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

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
services.AddScoped<ITokenStore, TokenStore>();

services.AddScoped<ICredentialService, CredentialService>();
services.AddScoped<ITelemetryService, TelemetryService>();
services.AddScoped<IUpdateService, UpdateService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IProvidersService, ProvidersService>();
services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

// JWT + DB token
var jwtKey = configuration["Jwt:Key"];
var jwtIssuer = configuration["Jwt:Issuer"] ?? "RouterManager";
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key não configurado ou com tamanho insuficiente (mínimo 32 bytes).");
}
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
})
.AddScheme<AuthenticationSchemeOptions, DbTokenAuthenticationHandler>(DbTokenAuthenticationHandler.SchemeName, null);

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DeviceBootstrap", policy => policy.RequireClaim("type", "bootstrap"));
    options.AddPolicy("DeviceWithSerial", policy => policy.RequireClaim("serial"));
    options.AddPolicy("DeviceBasic", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Type == "serial") || ctx.User.HasClaim(c => c.Type == "type" && c.Value == "bootstrap")));
    // Provisioning: generic/bootstrap/serial
    options.AddPolicy("PublicProvisioning", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Type == "serial") ||
        ctx.User.HasClaim(c => c.Type == "type" && (c.Value == "bootstrap" || c.Value == "generic"))));
    options.AddPolicy("PublicProviders", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Type == "serial") ||
        ctx.User.HasClaim(c => c.Type == "type" && (c.Value == "bootstrap" || c.Value == "generic")) ||
        ctx.User.HasClaim(c => c.Type == "providerId") ||
        ctx.User.IsInRole("Admin")));
    options.AddPolicy("CanRegisterDevice", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Type == "type" && (c.Value == "bootstrap" || c.Value == "generic"))));
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
    var tokenStore = scope.ServiceProvider.GetRequiredService<ITokenStore>();
    await tokenStore.EnsureSchemaAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandling();
app.UseMiddleware<ApiAuditMiddleware>();
app.UseCors(MyAllowSpecificOrigins);

app.UseRateLimiter();

// Encadeia JWT, depois DB token
app.Use(async (ctx, next) =>
{
    await ctx.RequestServices.GetRequiredService<IAuthenticationService>().AuthenticateAsync(ctx, JwtBearerDefaults.AuthenticationScheme);
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
    {
        await ctx.RequestServices.GetRequiredService<IAuthenticationService>().AuthenticateAsync(ctx, DbTokenAuthenticationHandler.SchemeName);
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
