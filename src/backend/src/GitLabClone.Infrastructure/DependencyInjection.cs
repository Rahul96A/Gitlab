using System.Text;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Interfaces;
using GitLabClone.Infrastructure.Ci;
using GitLabClone.Infrastructure.Git;
using GitLabClone.Infrastructure.Identity;
using GitLabClone.Infrastructure.Persistence;
using GitLabClone.Infrastructure.Persistence.Interceptors;
using GitLabClone.Infrastructure.Persistence.Repositories;
using GitLabClone.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GitLabClone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // EF Core DbContext
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            options.UseSqlServer(
                configuration.GetConnectionString("AppDb"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                }
            );

            options.AddInterceptors(auditInterceptor, softDeleteInterceptor);
        });

        // Register DbContext as IAppDbContext and IUnitOfWork
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();

        // Identity
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            // Support SignalR token via query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("MaintainerOrAbove", policy => policy.RequireRole("Admin", "Maintainer"))
            .AddPolicy("DeveloperOrAbove", policy => policy.RequireRole("Admin", "Maintainer", "Developer"));

        // Git Services
        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IGitHttpService, GitHttpService>();
        services.AddScoped<IGitBlobSyncService, GitBlobSyncService>();
        services.AddHostedService<RepoRestoreHostedService>();

        // Blob Storage
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // CI/CD
        services.AddScoped<ICiYamlParser, CiYamlParser>();
        services.AddHostedService<SimulatedJobRunner>();

        // Demo data seeder (applies migrations + seeds admin user)
        services.AddHostedService<DemoDataSeeder>();

        // MediatR handlers in Infrastructure (for domain events that need infra services)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Redis Cache (conditional — skip in development if not configured)
        var redisConnection = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "GitLabClone:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); // Fallback for local dev
        }

        return services;
    }
}
