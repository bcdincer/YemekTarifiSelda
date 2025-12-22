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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// JSON serialization ayarları (enum'ları string olarak serialize et, camelCase kullan)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // camelCase kullan
});

// Minimal API için JSON options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // camelCase kullan
});

// PostgreSQL DbContext (Infrastructure)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? "RecipeSite";
var audience = jwtSettings["Audience"] ?? "RecipeSiteUsers";

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Token expiration'ı tam zamanında kontrol et
    };
});

builder.Services.AddAuthorization();

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

// Authentication & Authorization (CORS'tan sonra, endpoint'lerden önce)
app.UseAuthentication();
app.UseAuthorization();

// Apply pending migrations on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    
    // Geçici: 10 numaralı tarife yorum ekle (sadece yoksa)
    var existingComment = await db.RecipeComments
        .FirstOrDefaultAsync(c => c.RecipeId == 10 && !c.IsDeleted);
    
    if (existingComment == null)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == 10);
        if (recipe != null)
        {
            var comment = new RecipeComment
            {
                RecipeId = 10,
                UserId = "test-user-1",
                UserName = "Test Kullanıcı",
                Content = "Bu tarif gerçekten çok lezzetli! Kesinlikle denemelisiniz. Malzemeleri hazırlamak biraz zaman aldı ama sonuç harika oldu.",
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                ParentCommentId = null
            };
            db.RecipeComments.Add(comment);
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ 10 numaralı tarife yorum eklendi! Yorum ID: {comment.Id}");
        }
        else
        {
            Console.WriteLine("⚠️ 10 numaralı tarif bulunamadı!");
        }
    }
    else
    {
        Console.WriteLine($"ℹ️ 10 numaralı tarifte zaten yorum var. Yorum ID: {existingComment.Id}");
    }
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

// Gelişmiş filtreleme endpoint'i
app.MapPost("/api/recipes/filter", async (RecipeFilterDto filter, IRecipeService service, int pageNumber = 1, int pageSize = 10) =>
{
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 10;
    
    return Results.Ok(await service.SearchWithFiltersAsync(filter, pageNumber, pageSize));
});

app.MapPost("/api/recipes/{id:int}/view", async (int id, IRecipeService service) =>
{
    await service.IncrementViewCountAsync(id);
    return Results.Ok();
});

// AI ile malzeme miktarı ayarlama endpoint'i
app.MapPost("/api/recipes/adjust-ingredients", async (AdjustIngredientsRequestDto request, IAiIngredientService aiService) =>
{
    if (request.Ingredients == null || !request.Ingredients.Any())
    {
        return Results.BadRequest(new { error = "Malzeme listesi gereklidir." });
    }

    if (request.OriginalServings <= 0 || request.NewServings <= 0)
    {
        return Results.BadRequest(new { error = "Kişi sayıları 0'dan büyük olmalıdır." });
    }

    try
    {
        var adjustedIngredients = await aiService.AdjustIngredientsAsync(
            request.Ingredients,
            request.OriginalServings,
            request.NewServings
        );

        return Results.Ok(new { ingredients = adjustedIngredients });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

// Rating endpoint'leri
app.MapPost("/api/recipes/{id:int}/rate", async (int id, int rating, HttpContext httpContext, IRatingService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    if (rating < 1 || rating > 5)
        return Results.BadRequest("Rating must be between 1 and 5");
    
    await service.RateRecipeAsync(id, userId, rating);
    return Results.Ok(new { averageRating = await service.GetAverageRatingAsync(id), ratingCount = await service.GetRatingCountAsync(id) });
}).RequireAuthorization();

app.MapGet("/api/recipes/{id:int}/rating", async (int id, IRatingService service) =>
{
    var averageRating = await service.GetAverageRatingAsync(id);
    var ratingCount = await service.GetRatingCountAsync(id);
    return Results.Ok(new { averageRating, ratingCount });
});

app.MapGet("/api/recipes/{id:int}/rating/user", async (int id, HttpContext httpContext, IRatingService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Ok(new { rating = (int?)null });
    }
    
    var userRating = await service.GetUserRatingAsync(id, userId);
    return Results.Ok(new { rating = userRating });
}).RequireAuthorization();

// Like endpoint'leri
app.MapPost("/api/recipes/{id:int}/like", async (int id, HttpContext httpContext, ILikeService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    await service.ToggleLikeAsync(id, userId);
    var isLiked = await service.IsLikedAsync(id, userId);
    var likeCount = await service.GetLikeCountAsync(id);
    return Results.Ok(new { isLiked, likeCount });
}).RequireAuthorization();

app.MapGet("/api/recipes/{id:int}/like", async (int id, HttpContext httpContext, ILikeService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    var isLiked = userId != null ? await service.IsLikedAsync(id, userId) : false;
    var likeCount = await service.GetLikeCountAsync(id);
    return Results.Ok(new { isLiked, likeCount });
});

// Kullanıcının beğendiği tarifleri getir
app.MapGet("/api/users/liked-recipes", async (HttpContext httpContext, IUnitOfWork unitOfWork, IRecipeService recipeService, IRatingService ratingService, ILikeService likeService, ILogger<Program> logger) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        logger.LogWarning("Liked recipes endpoint: UserId is null or empty");
        return Results.Unauthorized();
    }
    
    logger.LogInformation("Liked recipes endpoint: Fetching likes for userId: {UserId}", userId);
    
    var likeRepository = unitOfWork.Likes;
    var likedLikes = await likeRepository.GetByUserIdAsync(userId);
    
    logger.LogInformation("Liked recipes endpoint: Found {Count} likes for userId: {UserId}", likedLikes.Count, userId);
    
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
}).RequireAuthorization();

// Kullanıcının beğendiği tariflerde arama
app.MapGet("/api/users/liked-recipes/search", async (string? q, HttpContext httpContext, IUnitOfWork unitOfWork, IRecipeService recipeService, IRatingService ratingService, ILikeService likeService, ILogger<Program> logger) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        logger.LogWarning("Liked recipes search endpoint: UserId is null or empty");
        return Results.Unauthorized();
    }
    
    logger.LogInformation("Liked recipes search endpoint: Searching for userId: {UserId}, query: {Query}", userId, q);
    
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
                (r.Ingredients != null && r.Ingredients.Any(i => i.Name.ToLower().Contains(searchTerm))) ||
                (r.Steps != null && r.Steps.Any(s => s.Description.ToLower().Contains(searchTerm))) ||
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
}).RequireAuthorization();

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

// Collection endpoint'leri
app.MapGet("/api/users/collections", async (HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    return Results.Ok(await service.GetByUserIdAsync(userId));
}).RequireAuthorization();

app.MapGet("/api/users/collections/{id:int}", async (int id, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    var collection = await service.GetByIdAsync(id, userId);
    return collection != null ? Results.Ok(collection) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/users/collections/{id:int}/detail", async (int id, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    var collection = await service.GetDetailByIdAsync(id, userId);
    return collection != null ? Results.Ok(collection) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/users/collections", async (CreateCollectionDto dto, IValidator<CreateCollectionDto> validator, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
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
        var created = await service.CreateAsync(dto, userId);
        return Results.Created($"/api/users/collections/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPut("/api/users/collections/{id:int}", async (int id, UpdateCollectionDto dto, IValidator<UpdateCollectionDto> validator, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
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
        var updated = await service.UpdateAsync(id, dto, userId);
        return Results.Ok(updated);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
});

app.MapDelete("/api/users/collections/{id:int}", async (int id, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    try
    {
        await service.DeleteAsync(id, userId);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPost("/api/users/collections/{collectionId:int}/recipes/{recipeId:int}", async (int collectionId, int recipeId, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    try
    {
        await service.ToggleRecipeInCollectionAsync(collectionId, recipeId, userId);
        return Results.Ok(new { message = "Tarif koleksiyona eklendi/kaldırıldı" });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapDelete("/api/users/collections/{collectionId:int}/recipes/{recipeId:int}", async (int collectionId, int recipeId, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    try
    {
        await service.RemoveRecipeFromCollectionAsync(collectionId, recipeId, userId);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapGet("/api/recipes/{recipeId:int}/collections", async (int recipeId, HttpContext httpContext, ICollectionService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    return Results.Ok(await service.GetCollectionsForRecipeAsync(recipeId, userId));
}).RequireAuthorization();

// Helper method to get userId from JWT token
static string? GetUserIdFromToken(HttpContext httpContext)
{
    var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    // Debug: Tüm claim'leri logla (sadece development'ta)
    if (string.IsNullOrEmpty(userId) && httpContext.User?.Claims != null)
    {
        var allClaims = httpContext.User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        // Log sadece development'ta aktif olacak
    }
    
    return userId;
}

static string? GetUserNameFromToken(HttpContext httpContext)
{
    return httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
}

app.MapGet("/api/recipes/{recipeId:int}/collections/{userId}", async (int recipeId, string userId, ICollectionService service) =>
    Results.Ok(await service.GetCollectionIdsForRecipeAsync(recipeId, userId)));

// Meal Plan endpoint'leri
app.MapGet("/api/meal-plans", async (HttpContext httpContext, IMealPlanService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var mealPlans = await service.GetByUserIdAsync(userId);
    return Results.Ok(mealPlans);
}).RequireAuthorization();

app.MapGet("/api/meal-plans/{id:int}", async (int id, HttpContext httpContext, IMealPlanService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var mealPlan = await service.GetByIdAsync(id, userId);
    return mealPlan != null ? Results.Ok(mealPlan) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/meal-plans", async (CreateMealPlanDto dto, HttpContext httpContext, IMealPlanService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var created = await service.CreateAsync(dto, userId);
        return Results.Created($"/api/meal-plans/{created.Id}", created);
    }
    catch (Exception ex)
    {
        var errorMessage = ex.Message;
        if (ex.InnerException != null)
        {
            errorMessage += $" Inner: {ex.InnerException.Message}";
        }
        return Results.Problem($"An error occurred: {errorMessage}");
    }
}).RequireAuthorization();

app.MapPut("/api/meal-plans/{id:int}", async (int id, CreateMealPlanDto dto, HttpContext httpContext, IMealPlanService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var updated = await service.UpdateAsync(id, dto, userId);
        return updated ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapDelete("/api/meal-plans/{id:int}", async (int id, HttpContext httpContext, IMealPlanService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var deleted = await service.DeleteAsync(id, userId);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

// Shopping List endpoint'leri
app.MapGet("/api/shopping-lists", async (HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var shoppingLists = await service.GetByUserIdAsync(userId);
    return Results.Ok(shoppingLists);
}).RequireAuthorization();

app.MapGet("/api/shopping-lists/{id:int}", async (int id, HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var shoppingList = await service.GetByIdAsync(id, userId);
    return shoppingList != null ? Results.Ok(shoppingList) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/shopping-lists", async (CreateShoppingListDto dto, HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var created = await service.CreateAsync(dto, userId);
        return Results.Created($"/api/shopping-lists/{created.Id}", created);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPost("/api/shopping-lists/from-meal-plan/{mealPlanId:int}", async (int mealPlanId, HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var created = await service.CreateFromMealPlanAsync(mealPlanId, userId);
        return Results.Created($"/api/shopping-lists/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPut("/api/shopping-lists/{id:int}/items/{itemId:int}/checked", async (int id, int itemId, bool isChecked, HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var updated = await service.UpdateItemCheckedAsync(id, itemId, isChecked, userId);
    return updated ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapDelete("/api/shopping-lists/{id:int}", async (int id, HttpContext httpContext, IShoppingListService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var deleted = await service.DeleteAsync(id, userId);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

// Authentication endpoint'leri
app.MapPost("/api/auth/login", async (LoginRequestDto dto, IJwtService jwtService) =>
{
    // Bu endpoint Frontend'deki Identity ile entegre olacak
    // Frontend'den gelen userId, userName, email ile token oluştur
    if (string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.UserName))
    {
        return Results.BadRequest(new { error = "UserId ve UserName gereklidir." });
    }

    var token = jwtService.GenerateToken(dto.UserId, dto.UserName, dto.Email ?? dto.UserName);
    return Results.Ok(new { token, userId = dto.UserId, userName = dto.UserName });
});

// Comment endpoint'leri
app.MapGet("/api/recipes/{recipeId:int}/comments", async (int recipeId, int? skip, int? take, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    var comments = await service.GetByRecipeIdAsync(recipeId, skip, take, userId);
    var totalCount = await service.GetCountByRecipeIdAsync(recipeId);
    return Results.Ok(new { comments, totalCount });
});

app.MapGet("/api/comments/{id:int}", async (int id, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    var comment = await service.GetByIdAsync(id, userId);
    return comment != null ? Results.Ok(comment) : Results.NotFound();
});

app.MapPost("/api/recipes/{recipeId:int}/comments", async (int recipeId, CreateCommentDto dto, IValidator<CreateCommentDto> validator, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    var userName = GetUserNameFromToken(httpContext);
    
    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
    {
        return Results.Unauthorized();
    }
    
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }
    
    try
    {
        var comment = await service.CreateAsync(recipeId, dto, userId, userName);
        return Results.Created($"/api/comments/{comment.Id}", comment);
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPut("/api/comments/{id:int}", async (int id, UpdateCommentDto dto, IValidator<UpdateCommentDto> validator, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.BadRequest(new { errors });
    }
    
    try
    {
        var comment = await service.UpdateAsync(id, dto, userId);
        return Results.Ok(comment);
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapDelete("/api/comments/{id:int}", async (int id, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    try
    {
        await service.DeleteAsync(id, userId);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

app.MapPost("/api/comments/{id:int}/like", async (int id, HttpContext httpContext, ICommentService service) =>
{
    var userId = GetUserIdFromToken(httpContext);
    
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    try
    {
        await service.ToggleLikeAsync(id, userId);
        var isLiked = await service.IsLikedByUserAsync(id, userId);
        var comment = await service.GetByIdAsync(id, userId);
        return Results.Ok(new { isLiked, likeCount = comment?.LikeCount ?? 0 });
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
}).RequireAuthorization();

// Kullanıcı istatistikleri endpoint'i
app.MapGet("/api/users/statistics", async (HttpContext httpContext, IUnitOfWork unitOfWork) =>
{
    var userId = GetUserIdFromToken(httpContext);
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var likeRepository = unitOfWork.Likes;
    var collectionRepository = unitOfWork.Collections;
    var commentRepository = unitOfWork.Comments;
    var ratingRepository = unitOfWork.Ratings;

    // Beğenilen tarif sayısı
    var likedRecipesCount = await likeRepository.GetByUserIdAsync(userId);
    var likedCount = likedRecipesCount.Count;

    // Koleksiyon sayısı
    var collections = await collectionRepository.GetByUserIdAsync(userId);
    var collectionCount = collections.Count;

    // Yorum sayısı
    var comments = await commentRepository.GetByUserIdAsync(userId);
    var commentCount = comments.Count;

    // Puan verilen tarif sayısı ve ortalama verilen puan
    // RatingRepository'de GetByUserIdAsync yok, bu yüzden direkt context'ten alalım
    var dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();
    var userRatings = await dbContext.RecipeRatings
        .Where(r => r.UserId == userId)
        .ToListAsync();
    
    var ratedRecipesCount = userRatings
        .Select(r => r.RecipeId)
        .Distinct()
        .Count();
    
    var averageGivenRating = userRatings.Any() ? userRatings.Average(r => r.Rating) : (double?)null;

    var statistics = new
    {
        likedRecipesCount = likedCount,
        collectionCount = collectionCount,
        commentCount = commentCount,
        ratedRecipesCount = ratedRecipesCount,
        averageGivenRating = averageGivenRating
    };

    return Results.Ok(statistics);
}).RequireAuthorization();

app.Run();
