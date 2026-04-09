using System.Threading.RateLimiting;
using GitLabClone.Api.Hubs;
using GitLabClone.Api.Middleware;
using GitLabClone.Application;
using GitLabClone.Infrastructure;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, logConfig) =>
{
    logConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// ─── Clean Architecture DI ──────────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ─── API Services ───────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "GitLabClone API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

// ─── SignalR ────────────────────────────────────────────────────────────────
var signalRBuilder = builder.Services.AddSignalR();
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("GitLabClone", StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });
}

// ─── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});

// ─── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GitLabClone.Infrastructure.Persistence.AppDbContext>("database");

// ─── Rate Limiting ──────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 10
            }));
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 2
            }));
});

var app = builder.Build();

// ─── Middleware Pipeline ────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GitLabClone API v1"));
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("Frontend");

// Git Basic Auth must run BEFORE JWT auth so .git/ requests
// with Basic credentials get a ClaimsPrincipal set before
// the Git Smart HTTP middleware checks permissions.
app.UseMiddleware<GitBasicAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Git Smart HTTP middleware handles clone/push/pull.
// Runs after auth so it can check the user's identity.
app.UseMiddleware<GitSmartHttpMiddleware>();

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
