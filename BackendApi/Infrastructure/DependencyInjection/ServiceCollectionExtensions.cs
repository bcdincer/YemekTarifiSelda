using BackendApi.Application.Services;
using BackendApi.Application.Validators;
using BackendApi.Domain.Interfaces;
using BackendApi.Infrastructure.Events;
using BackendApi.Infrastructure.Persistence;
using BackendApi.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;

namespace BackendApi.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services, repositories, and validators
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateRecipeDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<CategoryDtoValidator>();

        // Hangfire (Background Jobs & Queue)
        var hangfireConnectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(hangfireConnectionString));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5; // Concurrent job count
            options.ServerTimeout = TimeSpan.FromMinutes(4);
            options.Queues = new[] { "default", "emails", "ai-processing" }; // Queue priorities
        });

        // Event Publisher (Async Domain Events)
        services.AddScoped<IEventPublisher, HangfireEventPublisher>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Unit of Work Pattern (Transaction Management)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories & Services (DI)
        // Note: Repositories are now accessed through UnitOfWork, but we still register them for backward compatibility if needed
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<ILikeService, LikeService>();

        // AI Agent Service (will be implemented later)
        // services.AddScoped<IAiAgentService, AiAgentService>();

        // External Services (will be implemented later)
        // services.AddScoped<IExternalService, SomeExternalService>();

        return services;
    }
}

