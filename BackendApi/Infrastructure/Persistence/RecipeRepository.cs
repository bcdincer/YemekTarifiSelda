using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class RecipeRepository(AppDbContext context) : IRecipeRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Recipe>> GetAllAsync()
        => await _context.Recipes
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<(List<Recipe> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.Recipes.Include(r => r.Category).OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<Recipe?> GetByIdAsync(int id)
        => await _context.Recipes
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(Recipe recipe)
    {
        await _context.Recipes.AddAsync(recipe);
    }

    public Task UpdateAsync(Recipe recipe)
    {
        _context.Recipes.Update(recipe);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Recipe recipe)
    {
        _context.Recipes.Remove(recipe);
        return Task.CompletedTask;
    }

    public async Task<List<Recipe>> GetFeaturedAsync(int count = 6)
        => await _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.IsFeatured)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<Recipe>> GetPopularAsync(int count = 6)
        => await _context.Recipes
            .Include(r => r.Category)
            .OrderByDescending(r => r.ViewCount)
            .ThenByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<Recipe>> GetByCategoryAsync(int categoryId)
        => await _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<(List<Recipe> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, int pageNumber, int pageSize)
    {
        var query = _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId)
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<Recipe>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _context.Recipes
            .Include(r => r.Category)
            .Where(r => 
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.Ingredients.ToLower().Contains(term) ||
                r.Steps.ToLower().Contains(term) ||
                (r.Tips != null && r.Tips.ToLower().Contains(term)) ||
                (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(term)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(term)))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<Recipe> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var term = searchTerm.ToLower();
        var query = _context.Recipes
            .Include(r => r.Category)
            .Where(r => 
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.Ingredients.ToLower().Contains(term) ||
                r.Steps.ToLower().Contains(term) ||
                (r.Tips != null && r.Tips.ToLower().Contains(term)) ||
                (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(term)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(term)))
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<(List<Recipe> Items, int TotalCount)> SearchWithFiltersAsync(
        string? searchTerm,
        int? categoryId,
        Domain.Entities.DifficultyLevel? difficulty,
        int? maxPrepTime,
        int? maxCookingTime,
        int? maxTotalTime,
        int? minServings,
        int? maxServings,
        string? ingredient,
        List<string>? ingredients,
        List<string>? excludedIngredients,
        string? dietType,
        bool? isFeatured,
        double? minRating,
        int? minRatingCount,
        string? sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize)
    {
        IQueryable<Recipe> query = _context.Recipes.Include(r => r.Category);

        // Arama terimi filtresi
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.Ingredients.ToLower().Contains(term) ||
                r.Steps.ToLower().Contains(term) ||
                (r.Tips != null && r.Tips.ToLower().Contains(term)) ||
                (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(term)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(term)));
        }

        // Kategori filtresi
        if (categoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == categoryId.Value);
        }

        // Zorluk seviyesi filtresi
        if (difficulty.HasValue)
        {
            query = query.Where(r => r.Difficulty == difficulty.Value);
        }

        // Hazırlık süresi filtresi
        if (maxPrepTime.HasValue)
        {
            query = query.Where(r => r.PrepTimeMinutes <= maxPrepTime.Value);
        }

        // Pişirme süresi filtresi
        if (maxCookingTime.HasValue)
        {
            query = query.Where(r => r.CookingTimeMinutes <= maxCookingTime.Value);
        }

        // Toplam süre filtresi (hazırlık + pişirme)
        if (maxTotalTime.HasValue)
        {
            query = query.Where(r => (r.PrepTimeMinutes + r.CookingTimeMinutes) <= maxTotalTime.Value);
        }

        // Porsiyon sayısı filtresi
        if (minServings.HasValue)
        {
            query = query.Where(r => r.Servings >= minServings.Value);
        }
        if (maxServings.HasValue)
        {
            query = query.Where(r => r.Servings <= maxServings.Value);
        }

        // Malzeme filtresi (tek malzeme)
        if (!string.IsNullOrWhiteSpace(ingredient))
        {
            var ingredientLower = ingredient.ToLower();
            query = query.Where(r => r.Ingredients.ToLower().Contains(ingredientLower) ||
                                    (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(ingredientLower)));
        }

        // Malzeme filtresi (birden fazla - hepsi içinde olmalı)
        if (ingredients != null && ingredients.Any())
        {
            foreach (var ing in ingredients)
            {
                if (!string.IsNullOrWhiteSpace(ing))
                {
                    var ingredientLower = ing.ToLower();
                    query = query.Where(r => r.Ingredients.ToLower().Contains(ingredientLower) ||
                                            (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(ingredientLower)));
                }
            }
        }

        // Hariç tutulacak malzemeler (alerjenler) filtresi
        if (excludedIngredients != null && excludedIngredients.Any())
        {
            foreach (var excludedIng in excludedIngredients)
            {
                if (!string.IsNullOrWhiteSpace(excludedIng))
                {
                    var excludedLower = excludedIng.ToLower();
                    query = query.Where(r => !r.Ingredients.ToLower().Contains(excludedLower) &&
                                            (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(excludedLower)));
                }
            }
        }

        // Diyet tipi filtresi (malzeme bazlı kontrol)
        if (!string.IsNullOrWhiteSpace(dietType))
        {
            var diet = dietType.ToLower();
            switch (diet)
            {
                case "vegan":
                    // Vegan: et, balık, süt, yumurta, peynir, bal, yoğurt içermemeli
                    var veganExcluded = new[] { "et", "balık", "tavuk", "kırmızı et", "beyaz et", "süt", "yumurta", "peynir", "bal", "yoğurt", "tereyağı", "krema", "mayonez" };
                    foreach (var ex in veganExcluded)
                    {
                        query = query.Where(r => !r.Ingredients.ToLower().Contains(ex) &&
                                                (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(ex)));
                    }
                    break;
                case "vegetarian":
                    // Vejetaryen: et, balık, tavuk içermemeli
                    var vegExcluded = new[] { "et", "balık", "tavuk", "kırmızı et", "beyaz et", "hamburger", "sosis", "salam" };
                    foreach (var ex in vegExcluded)
                    {
                        query = query.Where(r => !r.Ingredients.ToLower().Contains(ex) &&
                                                (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(ex)));
                    }
                    break;
                case "gluten-free":
                    // Glütensiz: buğday, arpa, çavdar, yulaf içermemeli
                    var glutenExcluded = new[] { "buğday", "arpa", "çavdar", "yulaf", "un", "ekmek", "makarna", "bulgur", "irmik" };
                    foreach (var ex in glutenExcluded)
                    {
                        query = query.Where(r => !r.Ingredients.ToLower().Contains(ex) &&
                                                (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(ex)));
                    }
                    break;
                case "dairy-free":
                    // Süt ürünleri yok: süt, peynir, yoğurt, tereyağı, krema
                    var dairyExcluded = new[] { "süt", "peynir", "yoğurt", "tereyağı", "krema", "kaymak", "lor", "labne" };
                    foreach (var ex in dairyExcluded)
                    {
                        query = query.Where(r => !r.Ingredients.ToLower().Contains(ex) &&
                                                (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(ex)));
                    }
                    break;
                case "nut-free":
                    // Kuruyemiş yok: fındık, fıstık, ceviz, badem, kaju
                    var nutExcluded = new[] { "fındık", "fıstık", "ceviz", "badem", "kaju", "antep fıstığı", "fıstık ezmesi" };
                    foreach (var ex in nutExcluded)
                    {
                        query = query.Where(r => !r.Ingredients.ToLower().Contains(ex) &&
                                                (r.AlternativeIngredients == null || !r.AlternativeIngredients.ToLower().Contains(ex)));
                    }
                    break;
            }
        }

        // Öne çıkan tarifler filtresi
        if (isFeatured.HasValue)
        {
            query = query.Where(r => r.IsFeatured == isFeatured.Value);
        }

        // Minimum puan filtresi
        if (minRating.HasValue)
        {
            query = query.Where(r => r.AverageRating.HasValue && r.AverageRating >= minRating.Value);
        }

        // Minimum puan sayısı filtresi
        if (minRatingCount.HasValue)
        {
            query = query.Where(r => r.RatingCount >= minRatingCount.Value);
        }

        // Sıralama
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDescending
                ? query.OrderByDescending(r => r.AverageRating).ThenByDescending(r => r.RatingCount)
                : query.OrderBy(r => r.AverageRating).ThenBy(r => r.RatingCount),
            "likes" => sortDescending
                ? query.OrderByDescending(r => r.LikeCount)
                : query.OrderBy(r => r.LikeCount),
            "views" => sortDescending
                ? query.OrderByDescending(r => r.ViewCount)
                : query.OrderBy(r => r.ViewCount),
            "preptime" => sortDescending
                ? query.OrderByDescending(r => r.PrepTimeMinutes)
                : query.OrderBy(r => r.PrepTimeMinutes),
            "cookingtime" => sortDescending
                ? query.OrderByDescending(r => r.CookingTimeMinutes)
                : query.OrderBy(r => r.CookingTimeMinutes),
            "totaltime" => sortDescending
                ? query.OrderByDescending(r => r.PrepTimeMinutes + r.CookingTimeMinutes)
                : query.OrderBy(r => r.PrepTimeMinutes + r.CookingTimeMinutes),
            _ => sortDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


