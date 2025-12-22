using BackendApi.Application.Services;
using BackendApi.Application.Services.AI;
using BackendApi.Application.Validators;
using BackendApi.Domain.Interfaces;
using BackendApi.Infrastructure.Events;
using BackendApi.Infrastructure.Persistence;
using BackendApi.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;

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
        services.AddValidatorsFromAssemblyContaining<CreateCollectionDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateCommentDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateCommentDtoValidator>();

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
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ICollectionRecipeRepository, CollectionRecipeRepository>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IJwtService, JwtService>();
        
        // Meal Plan & Shopping List
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IMealPlanItemRepository, MealPlanItemRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingListItemRepository, ShoppingListItemRepository>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        services.AddScoped<IShoppingListService, ShoppingListService>();

        // AI Configuration (Options Pattern)
        services.Configure<AiConfiguration>(configuration.GetSection(AiConfiguration.SectionName));
        // Fallback: Eğer section yoksa, eski formatı kullan
        if (configuration.GetSection(AiConfiguration.SectionName).Exists() == false)
        {
            services.Configure<AiConfiguration>(options =>
            {
                options.Provider = configuration["AiProvider"] ?? "HuggingFace";
                options.OpenAI.ApiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
                options.OpenAI.ApiUrl = configuration["OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
                options.OpenAI.Model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                options.OpenAI.Temperature = configuration.GetValue<double>("OpenAI:Temperature", 0.2);
                options.OpenAI.MaxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 1000);
                options.HuggingFace.ApiKey = configuration["HuggingFace:ApiKey"] ?? string.Empty;
                options.HuggingFace.ApiUrl = configuration["HuggingFace:ApiUrl"] ?? "https://api-inference.huggingface.co/v1/chat/completions";
                options.HuggingFace.Model = configuration["HuggingFace:Model"] ?? "meta-llama/Meta-Llama-3-8B-Instruct";
                options.HuggingFace.Enabled = configuration.GetValue<bool>("HuggingFace:Enabled", true);
                options.HuggingFace.Temperature = configuration.GetValue<double>("HuggingFace:Temperature", 0.3);
                options.HuggingFace.MaxTokens = configuration.GetValue<int>("HuggingFace:MaxTokens", 1000);
            });
        }

        // AI Ingredient Service
        services.AddHttpClient<IAiIngredientService, AiIngredientService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IAiIngredientService, AiIngredientService>();

        return services;
    }
}

