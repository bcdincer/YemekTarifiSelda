using BackendApi.Application.DTOs;
using BackendApi.Application.Exceptions;
using BackendApi.Application.Mappings;
using BackendApi.Application.Services;
using BackendApi.Application.Validators;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using BackendApi.Infrastructure.DependencyInjection;
using BackendApi.Infrastructure.Hangfire;
using BackendApi.Infrastructure.Persistence;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7036", "http://localhost:5036")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JSON serialization ayarları (enum'ları string olarak serialize et)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = null; // PascalCase kullan
});

// Minimal API için JSON options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = null; // PascalCase kullan
});

// PostgreSQL DbContext (Infrastructure)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Dependency Injection Configuration (IoC Container)
// ASP.NET Core'un built-in DI container'ı kullanılıyor
// Tüm servisler, repository'ler ve validator'lar merkezi olarak yönetiliyor
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Hangfire Dashboard (Development only - protect with authentication in production)
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Recipe Site - Background Jobs",
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });
}

// Enable CORS
app.UseCors();

app.UseHttpsRedirection();

// Apply pending migrations on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Minimal API endpoints for recipes (Application Service üzerinden)
app.MapGet("/api/recipes", async (IRecipeService service, int pageNumber = 1, int pageSize = 10) =>
{
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 10;
    
    return Results.Ok(await service.GetAllPagedAsync(pageNumber, pageSize));
});

app.MapGet("/api/recipes/{id:int}", async (int id, IRecipeService service) =>
    await service.GetByIdAsync(id) is { } recipe ? Results.Ok(recipe) : Results.NotFound());

app.MapPost("/api/recipes", async (CreateRecipeDto dto, IValidator<CreateRecipeDto> validator, IRecipeService service) =>
{
    // FluentValidation
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }

    try
    {
        // DTO'dan Recipe entity'sine map et (SRP: Mapping logic separated)
        var recipe = dto.ToEntity();
        var created = await service.CreateAsync(recipe);
        return Results.Created($"/api/recipes/{created.Id}", created);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.MapPut("/api/recipes/{id:int}", async (int id, CreateRecipeDto dto, IValidator<CreateRecipeDto> validator, IRecipeService service) =>
{
    // FluentValidation
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }

    try
    {
        // DTO'dan Recipe entity'sine map et
        var recipe = dto.ToEntity();
        var updated = await service.UpdateAsync(id, recipe);
        return updated ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.MapDelete("/api/recipes/{id:int}", async (int id, IRecipeService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

// Yeni Recipe endpoint'leri
app.MapGet("/api/recipes/featured", async (IRecipeService service) =>
    await service.GetFeaturedAsync());

app.MapGet("/api/recipes/popular", async (IRecipeService service) =>
    await service.GetPopularAsync());

app.MapGet("/api/recipes/category/{categoryId:int}", async (int categoryId, IRecipeService service, int pageNumber = 1, int pageSize = 10) =>
{
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 10;
    
    return Results.Ok(await service.GetByCategoryPagedAsync(categoryId, pageNumber, pageSize));
});

app.MapGet("/api/recipes/search", async (string q, IRecipeService service, int pageNumber = 1, int pageSize = 10) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest(new { error = "Search term is required" });
    
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 10;
    
    return Results.Ok(await service.SearchPagedAsync(q, pageNumber, pageSize));
});

app.MapPost("/api/recipes/{id:int}/view", async (int id, IRecipeService service) =>
{
    await service.IncrementViewCountAsync(id);
    return Results.Ok();
});

// Rating endpoint'leri
app.MapPost("/api/recipes/{id:int}/rate", async (int id, int rating, string userId, IRatingService service) =>
{
    if (rating < 1 || rating > 5)
        return Results.BadRequest("Rating must be between 1 and 5");
    
    await service.RateRecipeAsync(id, userId, rating);
    return Results.Ok(new { averageRating = await service.GetAverageRatingAsync(id), ratingCount = await service.GetRatingCountAsync(id) });
});

app.MapGet("/api/recipes/{id:int}/rating", async (int id, IRatingService service) =>
{
    var averageRating = await service.GetAverageRatingAsync(id);
    var ratingCount = await service.GetRatingCountAsync(id);
    return Results.Ok(new { averageRating, ratingCount });
});

app.MapGet("/api/recipes/{id:int}/rating/user/{userId}", async (int id, string userId, IRatingService service) =>
{
    var userRating = await service.GetUserRatingAsync(id, userId);
    return Results.Ok(new { rating = userRating });
});

// Like endpoint'leri
app.MapPost("/api/recipes/{id:int}/like", async (int id, string userId, ILikeService service) =>
{
    await service.ToggleLikeAsync(id, userId);
    var isLiked = await service.IsLikedAsync(id, userId);
    var likeCount = await service.GetLikeCountAsync(id);
    return Results.Ok(new { isLiked, likeCount });
});

app.MapGet("/api/recipes/{id:int}/like", async (int id, string userId, ILikeService service) =>
{
    var isLiked = await service.IsLikedAsync(id, userId);
    var likeCount = await service.GetLikeCountAsync(id);
    return Results.Ok(new { isLiked, likeCount });
});

// Kullanıcının beğendiği tarifleri getir
app.MapGet("/api/users/{userId}/liked-recipes", async (string userId, IUnitOfWork unitOfWork, IRecipeService recipeService, IRatingService ratingService, ILikeService likeService) =>
{
    var likeRepository = unitOfWork.Likes;
    var likedLikes = await likeRepository.GetByUserIdAsync(userId);
    
    if (!likedLikes.Any())
    {
        return Results.Ok(new List<RecipeResponseDto>());
    }
    
    // Recipe'leri direkt Like'dan al (zaten Include edilmiş)
    var recipes = likedLikes
        .Where(l => l.Recipe != null)
        .Select(l => l.Recipe!)
        .ToList();
    
    if (!recipes.Any())
    {
        return Results.Ok(new List<RecipeResponseDto>());
    }
    
    // Recipe ID'lerini al (beğenme sırasına göre)
    var recipeIds = likedLikes
        .Where(l => l.Recipe != null)
        .OrderByDescending(l => l.CreatedAt)
        .Select(l => l.Recipe!.Id)
        .ToList();
    
    // Rating'leri ve LikeCount'u toplu olarak hesapla
    var averageRatings = await ratingService.GetAverageRatingsAsync(recipeIds);
    var ratingCounts = await ratingService.GetRatingCountsAsync(recipeIds);
    var likeCounts = await likeService.GetLikeCountsAsync(recipeIds);
    
    // DTO'ları oluştur ve rating'leri ve likeCount'u ekle
    var recipeDtos = recipes
        .Where(r => recipeIds.Contains(r.Id))
        .OrderBy(r => recipeIds.IndexOf(r.Id)) // Beğenme sırasına göre sırala
        .Select(r =>
        {
            var dto = r.ToDto();
            if (averageRatings.TryGetValue(r.Id, out var avgRating))
            {
                dto.AverageRating = avgRating;
            }
            if (ratingCounts.TryGetValue(r.Id, out var count))
            {
                dto.RatingCount = count;
            }
            if (likeCounts.TryGetValue(r.Id, out var likeCount))
            {
                dto.LikeCount = likeCount;
            }
            return dto;
        })
        .ToList();
    
    return Results.Ok(recipeDtos);
});

// Kullanıcının beğendiği tariflerde arama
app.MapGet("/api/users/{userId}/liked-recipes/search", async (string userId, string? q, IUnitOfWork unitOfWork, IRecipeService recipeService, IRatingService ratingService, ILikeService likeService) =>
{
    var likeRepository = unitOfWork.Likes;
    var likedLikes = await likeRepository.GetByUserIdAsync(userId);
    
    if (!likedLikes.Any())
    {
        return Results.Ok(new List<RecipeResponseDto>());
    }
    
    // Recipe'leri direkt Like'dan al (zaten Include edilmiş)
    var recipes = likedLikes
        .Where(l => l.Recipe != null)
        .Select(l => l.Recipe!)
        .ToList();
    
    if (!recipes.Any())
    {
        return Results.Ok(new List<RecipeResponseDto>());
    }
    
    // Arama terimi varsa filtrele
    if (!string.IsNullOrWhiteSpace(q))
    {
        var searchTerm = q.ToLower();
        recipes = recipes
            .Where(r =>
                r.Title.ToLower().Contains(searchTerm) ||
                (r.Description != null && r.Description.ToLower().Contains(searchTerm)) ||
                (r.Ingredients != null && r.Ingredients.ToLower().Contains(searchTerm)) ||
                (r.Steps != null && r.Steps.ToLower().Contains(searchTerm)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(searchTerm)))
            .ToList();
    }
    
    if (!recipes.Any())
    {
        return Results.Ok(new List<RecipeResponseDto>());
    }
    
    // Recipe ID'lerini al (beğenme sırasına göre)
    var recipeIds = recipes.Select(r => r.Id).ToList();
    
    // Rating'leri ve LikeCount'u toplu olarak hesapla
    var averageRatings = await ratingService.GetAverageRatingsAsync(recipeIds);
    var ratingCounts = await ratingService.GetRatingCountsAsync(recipeIds);
    var likeCounts = await likeService.GetLikeCountsAsync(recipeIds);
    
    // DTO'ları oluştur ve rating'leri ve likeCount'u ekle
    var recipeDtos = recipes
        .Select(r =>
        {
            var dto = r.ToDto();
            if (averageRatings.TryGetValue(r.Id, out var avgRating))
            {
                dto.AverageRating = avgRating;
            }
            if (ratingCounts.TryGetValue(r.Id, out var count))
            {
                dto.RatingCount = count;
            }
            if (likeCounts.TryGetValue(r.Id, out var likeCount))
            {
                dto.LikeCount = likeCount;
            }
            return dto;
        })
        .ToList();
    
    return Results.Ok(recipeDtos);
});

// Sıralama endpoint'leri
app.MapGet("/api/recipes/sorted/by-rating", async (IRecipeService service) =>
{
    var recipes = await service.GetAllAsync();
    var sorted = recipes
        .Where(r => r.AverageRating.HasValue)
        .OrderByDescending(r => r.AverageRating)
        .ThenByDescending(r => r.RatingCount)
        .ToList();
    return Results.Ok(sorted);
});

app.MapGet("/api/recipes/sorted/by-likes", async (IRecipeService service) =>
{
    var recipes = await service.GetAllAsync();
    var sorted = recipes.OrderByDescending(r => r.LikeCount).ToList();
    return Results.Ok(sorted);
});

// Category endpoint'leri
app.MapGet("/api/categories", async (ICategoryService service) =>
    await service.GetAllAsync());

app.MapGet("/api/categories/{id:int}", async (int id, ICategoryService service) =>
    await service.GetByIdAsync(id) is { } category ? Results.Ok(category) : Results.NotFound());

app.MapPost("/api/categories", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, ICategoryService service) =>
{
    // FluentValidation
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }

    try
    {
        var category = dto.ToEntity();
        var created = await service.CreateAsync(category);
        return Results.Created($"/api/categories/{created.Id}", created);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.MapPut("/api/categories/{id:int}", async (int id, CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, ICategoryService service) =>
{
    // FluentValidation
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }

    try
    {
        var category = dto.ToEntity();
        var updated = await service.UpdateAsync(id, category);
        return updated ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.MapDelete("/api/categories/{id:int}", async (int id, ICategoryService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();
