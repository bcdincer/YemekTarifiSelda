using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Events;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class RecipeService(IUnitOfWork unitOfWork, ILogger<RecipeService> logger, IEventPublisher eventPublisher, IRatingService ratingService, ILikeService likeService) : IRecipeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<RecipeService> _logger = logger;
    private readonly IEventPublisher _eventPublisher = eventPublisher;
    private readonly IRatingService _ratingService = ratingService;
    private readonly ILikeService _likeService = likeService;
    private IRecipeRepository Repository => _unitOfWork.Recipes;

    public async Task<List<RecipeResponseDto>> GetAllAsync()
    {
        var recipes = await Repository.GetAllAsync();
        return await MapRecipesWithRealTimeRatingsAsync(recipes);
    }

    public async Task<PagedResult<RecipeResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetAllPagedAsync(pageNumber, pageSize);
        var dtoItems = await MapRecipesWithRealTimeRatingsAsync(items);
        return new PagedResult<RecipeResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<RecipeResponseDto?> GetByIdAsync(int id)
    {
        var recipe = await Repository.GetByIdAsync(id);
        if (recipe == null) return null;
        return await MapRecipeWithRealTimeRatingAsync(recipe);
    }

    public async Task<List<RecipeResponseDto>> GetFeaturedAsync(int count = 6)
    {
        var recipes = await Repository.GetFeaturedAsync(count);
        return await MapRecipesWithRealTimeRatingsAsync(recipes);
    }

    public async Task<List<RecipeResponseDto>> GetPopularAsync(int count = 6)
    {
        var recipes = await Repository.GetPopularAsync(count);
        return await MapRecipesWithRealTimeRatingsAsync(recipes);
    }

    public async Task<List<RecipeResponseDto>> GetByCategoryAsync(int categoryId)
    {
        var recipes = await Repository.GetByCategoryAsync(categoryId);
        return await MapRecipesWithRealTimeRatingsAsync(recipes);
    }

    public async Task<PagedResult<RecipeResponseDto>> GetByCategoryPagedAsync(int categoryId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetByCategoryPagedAsync(categoryId, pageNumber, pageSize);
        var dtoItems = await MapRecipesWithRealTimeRatingsAsync(items);
        return new PagedResult<RecipeResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<List<RecipeResponseDto>> SearchAsync(string searchTerm)
    {
        var recipes = await Repository.SearchAsync(searchTerm);
        return await MapRecipesWithRealTimeRatingsAsync(recipes);
    }

    public async Task<PagedResult<RecipeResponseDto>> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.SearchPagedAsync(searchTerm, pageNumber, pageSize);
        var dtoItems = await MapRecipesWithRealTimeRatingsAsync(items);
        return new PagedResult<RecipeResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<RecipeResponseDto>> SearchWithFiltersAsync(DTOs.RecipeFilterDto filter, int pageNumber, int pageSize)
    {
        // Rating ve likes sıralamaları için gerçek zamanlı değerlere ihtiyacımız var
        // Bu yüzden sayfalama yapmadan tüm sonuçları alıp, gerçek zamanlı değerlerle sıralayıp sonra sayfalama yapıyoruz
        bool needsRealtimeSort = string.IsNullOrWhiteSpace(filter.SortBy) || 
                                  filter.SortBy.ToLower() == "rating" || 
                                  filter.SortBy.ToLower() == "likes";
        
        // Rating/likes için tüm sonuçları al (maksimum 10000 kayıt limiti)
        int fetchPageSize = needsRealtimeSort ? 10000 : pageSize;
        int fetchPageNumber = needsRealtimeSort ? 1 : pageNumber;
        
        var (items, totalCount) = await Repository.SearchWithFiltersAsync(
            filter.SearchTerm,
            filter.CategoryId,
            filter.Difficulty,
            filter.MaxPrepTime,
            filter.MaxCookingTime,
            filter.MaxTotalTime,
            filter.MinServings,
            filter.MaxServings,
            filter.Ingredient,
            filter.Ingredients,
            filter.ExcludedIngredients,
            filter.DietType,
            filter.IsFeatured,
            filter.MinRating,
            filter.MinRatingCount,
            needsRealtimeSort ? null : filter.SortBy, // Rating/likes için repository sıralamasını devre dışı bırak
            filter.SortDescending,
            fetchPageNumber,
            fetchPageSize);
        
        var dtoItems = await MapRecipesWithRealTimeRatingsAsync(items);
        
        // Gerçek zamanlı rating ve like count'lara göre sıralama yap
        if (needsRealtimeSort)
        {
            // Repository'den gelen totalCount'u kullan (tüm sonuçlar için)
            // Eğer fetchPageSize limiti nedeniyle tüm sonuçları alamadıysak,
            // repository'den gelen totalCount doğru olacaktır
            // Aksi halde items.Count gerçek sayıdır
            if (items.Count < fetchPageSize)
            {
                // Tüm sonuçları aldık, gerçek sayı items.Count
                totalCount = items.Count;
            }
            // Aksi halde repository'den gelen totalCount zaten doğru
            
            // Sıralamayı yap
            dtoItems = SortDtoItems(dtoItems, filter.SortBy, filter.SortDescending);
            
            // Sayfalama yap
            dtoItems = dtoItems
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        
        return new PagedResult<RecipeResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }
    
    private List<RecipeResponseDto> SortDtoItems(List<RecipeResponseDto> items, string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Akıllı sıralama: önce featured, sonra rating, sonra like count
            return items
                .OrderByDescending(r => r.IsFeatured)
                .ThenByDescending(r => r.AverageRating.HasValue ? r.AverageRating.Value : double.MinValue)
                .ThenByDescending(r => r.LikeCount)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
        }
        
        return sortBy.ToLower() switch
        {
            "rating" => sortDescending
                ? items.Where(r => r.AverageRating.HasValue)
                      .OrderByDescending(r => r.AverageRating!.Value)
                      .ThenByDescending(r => r.RatingCount)
                      .ThenByDescending(r => r.LikeCount)
                      .Concat(items.Where(r => !r.AverageRating.HasValue))
                      .ToList()
                : items.Where(r => r.AverageRating.HasValue)
                      .OrderBy(r => r.AverageRating!.Value)
                      .ThenBy(r => r.RatingCount)
                      .ThenBy(r => r.LikeCount)
                      .Concat(items.Where(r => !r.AverageRating.HasValue))
                      .ToList(),
            "likes" => sortDescending
                ? items.OrderByDescending(r => r.LikeCount).ToList()
                : items.OrderBy(r => r.LikeCount).ToList(),
            "views" => sortDescending
                ? items.OrderByDescending(r => r.ViewCount).ToList()
                : items.OrderBy(r => r.ViewCount).ToList(),
            "newest" => sortDescending
                ? items.OrderByDescending(r => r.CreatedAt).ToList()
                : items.OrderBy(r => r.CreatedAt).ToList(),
            _ => sortDescending
                ? items.OrderByDescending(r => r.CreatedAt).ToList()
                : items.OrderBy(r => r.CreatedAt).ToList()
        };
    }

    public async Task<RecipeResponseDto> CreateAsync(Recipe recipe)
    {
        try
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            recipe.CreatedAt = DateTime.UtcNow;
            await Repository.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe '{RecipeTitle}' created with id {RecipeId}", recipe.Title, recipe.Id);

            // Category'yi yükle ve DTO'ya çevir
            var createdRecipe = await Repository.GetByIdAsync(recipe.Id);
            if (createdRecipe == null)
                throw new InvalidOperationException("Recipe was created but could not be retrieved");

            // Publish domain event (async - email notification will be sent via Hangfire)
            var recipeCreatedEvent = new RecipeCreatedEvent(
                createdRecipe.Id,
                createdRecipe.Title,
                null, // TODO: Get from current user context
                createdRecipe.CreatedAt
            );
            await _eventPublisher.PublishAsync(recipeCreatedEvent);
            
            return await MapRecipeWithRealTimeRatingAsync(createdRecipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe '{RecipeTitle}'", recipe?.Title);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, Recipe updated)
    {
        try
        {
            var existing = await Repository.GetByIdAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Recipe with id {RecipeId} not found for update", id);
                return false;
            }

            // Update properties using DTO mapping (SRP: Mapping logic separated)
            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Ingredients = updated.Ingredients;
            existing.Steps = updated.Steps;
            existing.PrepTimeMinutes = updated.PrepTimeMinutes;
            existing.CookingTimeMinutes = updated.CookingTimeMinutes;
            existing.Servings = updated.Servings;
            existing.Difficulty = updated.Difficulty;
            existing.ImageUrl = updated.ImageUrl;
            existing.Tips = updated.Tips;
            existing.AlternativeIngredients = updated.AlternativeIngredients;
            existing.NutritionInfo = updated.NutritionInfo;
            existing.CategoryId = updated.CategoryId;
            existing.IsFeatured = updated.IsFeatured;
            existing.UpdatedAt = DateTime.UtcNow;

            await Repository.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe {RecipeId} updated successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating recipe {RecipeId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var existing = await Repository.GetByIdAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Recipe with id {RecipeId} not found for deletion", id);
                return false;
            }

            await Repository.DeleteAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe {RecipeId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recipe {RecipeId}", id);
            throw;
        }
    }

    public async Task IncrementViewCountAsync(int id)
    {
        try
        {
            var recipe = await Repository.GetByIdAsync(id);
            if (recipe != null)
            {
                recipe.ViewCount++;
                await Repository.UpdateAsync(recipe);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogDebug("View count incremented for recipe {RecipeId}", id);
            }
            else
            {
                _logger.LogWarning("Recipe with id {RecipeId} not found for view count increment", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for recipe {RecipeId}", id);
            // Don't throw - view count increment is not critical
        }
    }

    private async Task<RecipeResponseDto> MapRecipeWithRealTimeRatingAsync(Recipe recipe)
    {
        var dto = recipe.ToDto();
        // Gerçek zamanlı rating ve like count hesapla
        dto.AverageRating = await _ratingService.GetAverageRatingAsync(recipe.Id);
        dto.RatingCount = await _ratingService.GetRatingCountAsync(recipe.Id);
        dto.LikeCount = await _likeService.GetLikeCountAsync(recipe.Id);
        return dto;
    }

    private async Task<List<RecipeResponseDto>> MapRecipesWithRealTimeRatingsAsync(List<Recipe> recipes)
    {
        if (!recipes.Any())
            return new List<RecipeResponseDto>();

        var dtos = new List<RecipeResponseDto>();
        
        // Tüm recipe ID'lerini topla
        var recipeIds = recipes.Select(r => r.Id).ToList();
        
        // Tüm rating'leri ve like count'ları tek sorguda toplu olarak hesapla (DbContext concurrency sorununu önlemek için)
        var averageRatings = await _ratingService.GetAverageRatingsAsync(recipeIds);
        var ratingCounts = await _ratingService.GetRatingCountsAsync(recipeIds);
        var likeCounts = await _likeService.GetLikeCountsAsync(recipeIds);
        
        // DTO'ları oluştur ve rating'leri ve like count'ları ekle
        foreach (var recipe in recipes)
        {
            var dto = recipe.ToDto();
            if (averageRatings.TryGetValue(recipe.Id, out var avgRating))
            {
                dto.AverageRating = avgRating;
            }
            if (ratingCounts.TryGetValue(recipe.Id, out var count))
            {
                dto.RatingCount = count;
            }
            if (likeCounts.TryGetValue(recipe.Id, out var likeCount))
            {
                dto.LikeCount = likeCount;
            }
            dtos.Add(dto);
        }
        
        return dtos;
    }
}


