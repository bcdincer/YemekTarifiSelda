using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class CollectionService(
    IUnitOfWork unitOfWork,
    ILogger<CollectionService> logger,
    IRatingService ratingService,
    ILikeService likeService) : ICollectionService
{
    private ICollectionRepository CollectionRepository => unitOfWork.Collections;
    private ICollectionRecipeRepository CollectionRecipeRepository => unitOfWork.CollectionRecipes;
    private IRecipeRepository RecipeRepository => unitOfWork.Recipes;

    public async Task<CollectionResponseDto> CreateAsync(CreateCollectionDto dto, string userId)
    {
        // Aynı isimde koleksiyon var mı kontrol et
        if (await CollectionRepository.NameExistsForUserAsync(dto.Name, userId))
        {
            throw new InvalidOperationException($"'{dto.Name}' adında bir koleksiyon zaten mevcut.");
        }

        var collection = new Collection
        {
            Name = dto.Name,
            Description = dto.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await CollectionRepository.AddAsync(collection);
        await CollectionRepository.SaveChangesAsync();

        logger.LogInformation("Collection created: {CollectionId} by user {UserId}", collection.Id, userId);

        return new CollectionResponseDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            RecipeCount = 0,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task<CollectionResponseDto?> GetByIdAsync(int id, string userId)
    {
        var collection = await CollectionRepository.GetByIdAsync(id);
        
        if (collection == null || collection.UserId != userId)
        {
            return null;
        }

        return new CollectionResponseDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            RecipeCount = collection.CollectionRecipes.Count,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task<List<CollectionResponseDto>> GetByUserIdAsync(string userId)
    {
        var collections = await CollectionRepository.GetByUserIdAsync(userId);
        
        return collections.Select(c => new CollectionResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            RecipeCount = c.CollectionRecipes.Count,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    public async Task<CollectionDetailDto?> GetDetailByIdAsync(int id, string userId)
    {
        var collection = await CollectionRepository.GetByIdAsync(id);
        
        if (collection == null || collection.UserId != userId)
        {
            return null;
        }

        var recipes = new List<RecipeResponseDto>();

        if (collection.CollectionRecipes.Any())
        {
            var recipeEntities = collection.CollectionRecipes
                .Where(cr => cr.Recipe != null)
                .Select(cr => cr.Recipe!)
                .ToList();
            recipes = await MapRecipesWithRealTimeRatingsAsync(recipeEntities);
        }

        return new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            Recipes = recipes
        };
    }

    public async Task<CollectionResponseDto> UpdateAsync(int id, UpdateCollectionDto dto, string userId)
    {
        var collection = await CollectionRepository.GetByIdAsync(id);
        
        if (collection == null || collection.UserId != userId)
        {
            throw new KeyNotFoundException("Koleksiyon bulunamadı veya erişim yetkiniz yok.");
        }

        // Aynı isimde başka bir koleksiyon var mı kontrol et (mevcut koleksiyon hariç)
        if (await CollectionRepository.NameExistsForUserAsync(dto.Name, userId, id))
        {
            throw new InvalidOperationException($"'{dto.Name}' adında bir koleksiyon zaten mevcut.");
        }

        collection.Name = dto.Name;
        collection.Description = dto.Description;
        collection.UpdatedAt = DateTime.UtcNow;

        await CollectionRepository.UpdateAsync(collection);
        await CollectionRepository.SaveChangesAsync();

        logger.LogInformation("Collection updated: {CollectionId} by user {UserId}", id, userId);

        return new CollectionResponseDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            RecipeCount = collection.CollectionRecipes.Count,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var collection = await CollectionRepository.GetByIdAsync(id);
        
        if (collection == null || collection.UserId != userId)
        {
            throw new KeyNotFoundException("Koleksiyon bulunamadı veya erişim yetkiniz yok.");
        }

        await CollectionRepository.DeleteAsync(collection);
        await CollectionRepository.SaveChangesAsync();

        logger.LogInformation("Collection deleted: {CollectionId} by user {UserId}", id, userId);
    }

    public async Task AddRecipeToCollectionAsync(int collectionId, int recipeId, string userId)
    {
        // Koleksiyon kontrolü
        var collection = await CollectionRepository.GetByIdAsync(collectionId);
        if (collection == null || collection.UserId != userId)
        {
            throw new KeyNotFoundException("Koleksiyon bulunamadı veya erişim yetkiniz yok.");
        }

        // Tarif kontrolü
        var recipe = await RecipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new KeyNotFoundException("Tarif bulunamadı.");
        }

        // Zaten ekli mi kontrol et
        if (await CollectionRecipeRepository.ExistsAsync(collectionId, recipeId))
        {
            throw new InvalidOperationException("Bu tarif zaten koleksiyonda mevcut.");
        }

        var collectionRecipe = new CollectionRecipe
        {
            CollectionId = collectionId,
            RecipeId = recipeId,
            AddedAt = DateTime.UtcNow
        };

        await CollectionRecipeRepository.AddAsync(collectionRecipe);
        await CollectionRecipeRepository.SaveChangesAsync();

        // Koleksiyon güncelleme tarihini güncelle
        collection.UpdatedAt = DateTime.UtcNow;
        await CollectionRepository.UpdateAsync(collection);
        await CollectionRepository.SaveChangesAsync();

        logger.LogInformation("Recipe {RecipeId} added to collection {CollectionId} by user {UserId}", recipeId, collectionId, userId);
    }

    public async Task RemoveRecipeFromCollectionAsync(int collectionId, int recipeId, string userId)
    {
        // Koleksiyon kontrolü
        var collection = await CollectionRepository.GetByIdAsync(collectionId);
        if (collection == null || collection.UserId != userId)
        {
            throw new KeyNotFoundException("Koleksiyon bulunamadı veya erişim yetkiniz yok.");
        }

        var collectionRecipe = await CollectionRecipeRepository.GetByCollectionAndRecipeAsync(collectionId, recipeId);
        if (collectionRecipe == null)
        {
            throw new KeyNotFoundException("Tarif koleksiyonda bulunamadı.");
        }

        await CollectionRecipeRepository.DeleteAsync(collectionRecipe);
        await CollectionRecipeRepository.SaveChangesAsync();

        // Koleksiyon güncelleme tarihini güncelle
        collection.UpdatedAt = DateTime.UtcNow;
        await CollectionRepository.UpdateAsync(collection);
        await CollectionRepository.SaveChangesAsync();

        logger.LogInformation("Recipe {RecipeId} removed from collection {CollectionId} by user {UserId}", recipeId, collectionId, userId);
    }

    public async Task<List<int>> GetCollectionIdsForRecipeAsync(int recipeId, string userId)
    {
        var collectionRecipes = await CollectionRecipeRepository.GetByRecipeIdAsync(recipeId);
        
        if (!collectionRecipes.Any())
            return new List<int>();

        // Collection ID'lerini topla
        var collectionIds = collectionRecipes.Select(cr => cr.CollectionId).Distinct().ToList();
        
        // Kullanıcının koleksiyonlarını tek sorguda al (N+1 problemini çöz)
        var userCollections = await CollectionRepository.GetByUserIdAsync(userId);
        var userCollectionIdsSet = userCollections.Select(c => c.Id).ToHashSet();
        
        // Sadece kullanıcının koleksiyonlarını filtrele
        return collectionIds.Where(id => userCollectionIdsSet.Contains(id)).ToList();
    }

    public async Task<List<int>> GetCollectionsForRecipeAsync(int recipeId, string userId)
    {
        return await GetCollectionIdsForRecipeAsync(recipeId, userId);
    }

    public async Task ToggleRecipeInCollectionAsync(int collectionId, int recipeId, string userId)
    {
        // Koleksiyon kontrolü
        var collection = await CollectionRepository.GetByIdAsync(collectionId);
        if (collection == null || collection.UserId != userId)
        {
            throw new KeyNotFoundException("Koleksiyon bulunamadı veya erişim yetkiniz yok.");
        }

        // Tarif kontrolü
        var recipe = await RecipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new KeyNotFoundException("Tarif bulunamadı.");
        }

        // Zaten ekli mi kontrol et
        var exists = await CollectionRecipeRepository.ExistsAsync(collectionId, recipeId);
        
        if (exists)
        {
            // Kaldır
            var collectionRecipe = await CollectionRecipeRepository.GetByCollectionAndRecipeAsync(collectionId, recipeId);
            if (collectionRecipe != null)
            {
                await CollectionRecipeRepository.DeleteAsync(collectionRecipe);
                await CollectionRecipeRepository.SaveChangesAsync();
                
                // Koleksiyon güncelleme tarihini güncelle
                collection.UpdatedAt = DateTime.UtcNow;
                await CollectionRepository.UpdateAsync(collection);
                await CollectionRepository.SaveChangesAsync();
                
                logger.LogInformation("Recipe {RecipeId} removed from collection {CollectionId} by user {UserId}", recipeId, collectionId, userId);
            }
        }
        else
        {
            // Ekle
            var collectionRecipe = new CollectionRecipe
            {
                CollectionId = collectionId,
                RecipeId = recipeId,
                AddedAt = DateTime.UtcNow
            };

            await CollectionRecipeRepository.AddAsync(collectionRecipe);
            await CollectionRecipeRepository.SaveChangesAsync();

            // Koleksiyon güncelleme tarihini güncelle
            collection.UpdatedAt = DateTime.UtcNow;
            await CollectionRepository.UpdateAsync(collection);
            await CollectionRepository.SaveChangesAsync();
            
            logger.LogInformation("Recipe {RecipeId} added to collection {CollectionId} by user {UserId}", recipeId, collectionId, userId);
        }
    }

    private async Task<List<RecipeResponseDto>> MapRecipesWithRealTimeRatingsAsync(List<Recipe> recipes)
    {
        if (recipes == null || !recipes.Any())
            return new List<RecipeResponseDto>();

        var dtos = new List<RecipeResponseDto>();
        var recipeIds = recipes.Select(r => r.Id).ToList();
        
        // Rating ve like bilgilerini toplu olarak al
        var averageRatings = await ratingService.GetAverageRatingsAsync(recipeIds);
        var ratingCounts = await ratingService.GetRatingCountsAsync(recipeIds);
        var likeCounts = await likeService.GetLikeCountsAsync(recipeIds);
        
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

