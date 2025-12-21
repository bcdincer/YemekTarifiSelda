using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace BackendApi.Application.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMealPlanService _mealPlanService;
    private IShoppingListRepository ShoppingListRepository => _unitOfWork.ShoppingLists;
    private IShoppingListItemRepository ShoppingListItemRepository => _unitOfWork.ShoppingListItems;
    private IRecipeRepository RecipeRepository => _unitOfWork.Recipes;

    public ShoppingListService(IUnitOfWork unitOfWork, IMealPlanService mealPlanService)
    {
        _unitOfWork = unitOfWork;
        _mealPlanService = mealPlanService;
    }

    public async Task<ShoppingListResponseDto?> GetByIdAsync(int id, string userId)
    {
        var shoppingList = await ShoppingListRepository.GetByIdAsync(id);
        if (shoppingList == null || shoppingList.UserId != userId)
            return null;

        return shoppingList.ToDto();
    }

    public async Task<List<ShoppingListResponseDto>> GetByUserIdAsync(string userId)
    {
        var shoppingLists = await ShoppingListRepository.GetByUserIdAsync(userId);
        return shoppingLists.Select(sl => sl.ToDto()).ToList();
    }

    public async Task<ShoppingListResponseDto> CreateAsync(CreateShoppingListDto dto, string userId)
    {
        var shoppingList = new ShoppingList
        {
            UserId = userId,
            Name = dto.Name,
            MealPlanId = dto.MealPlanId,
            CreatedAt = DateTime.UtcNow
        };

        await ShoppingListRepository.AddAsync(shoppingList);
        await _unitOfWork.SaveChangesAsync();

        // Items ekle
        var displayOrder = 0;
        foreach (var itemDto in dto.Items)
        {
            var item = new ShoppingListItem
            {
                ShoppingListId = shoppingList.Id,
                Ingredient = itemDto.Ingredient,
                Quantity = itemDto.Quantity,
                Unit = itemDto.Unit,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            await ShoppingListItemRepository.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        var created = await ShoppingListRepository.GetByIdAsync(shoppingList.Id);
        return created!.ToDto();
    }

    public async Task<ShoppingListResponseDto> CreateFromMealPlanAsync(int mealPlanId, string userId)
    {
        var mealPlan = await _mealPlanService.GetByIdAsync(mealPlanId, userId);
        if (mealPlan == null)
            throw new ArgumentException("Meal plan not found");

        // Mevcut shopping list varsa döndür
        var existing = await ShoppingListRepository.GetByMealPlanIdAsync(mealPlanId);
        if (existing != null && existing.UserId == userId)
        {
            return existing.ToDto();
        }

        // Tariflerden malzemeleri topla
        var allIngredients = new Dictionary<string, IngredientInfo>();

        foreach (var item in mealPlan.Items)
        {
            if (item.Recipe == null)
                continue;

            var recipe = await RecipeRepository.GetByIdAsync(item.RecipeId);
            if (recipe == null)
                continue;

            // Servings oranını hesapla
            var servingsRatio = item.Servings / (double)recipe.Servings;

            // Malzemeleri parse et ve birleştir
            var ingredients = ParseIngredients(recipe.Ingredients);
            foreach (var ing in ingredients)
            {
                var key = ing.Name.ToLowerInvariant();
                if (allIngredients.ContainsKey(key))
                {
                    // Miktarı birleştir
                    allIngredients[key].TotalQuantity += ing.Quantity * servingsRatio;
                }
                else
                {
                    allIngredients[key] = new IngredientInfo
                    {
                        Name = ing.Name,
                        TotalQuantity = ing.Quantity * servingsRatio,
                        Unit = ing.Unit
                    };
                }
            }
        }

        // Shopping list oluştur
        var shoppingList = new ShoppingList
        {
            UserId = userId,
            Name = $"Alışveriş Listesi - {mealPlan.Name}",
            MealPlanId = mealPlanId,
            CreatedAt = DateTime.UtcNow
        };

        await ShoppingListRepository.AddAsync(shoppingList);
        await _unitOfWork.SaveChangesAsync();

        // Items ekle
        var displayOrder = 0;
        foreach (var ingredient in allIngredients.Values.OrderBy(i => i.Name))
        {
            var item = new ShoppingListItem
            {
                ShoppingListId = shoppingList.Id,
                Ingredient = ingredient.Name,
                Quantity = ingredient.TotalQuantity > 0 ? Math.Round(ingredient.TotalQuantity, 2).ToString("0.##") : null,
                Unit = ingredient.Unit,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            await ShoppingListItemRepository.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        var created = await ShoppingListRepository.GetByIdAsync(shoppingList.Id);
        return created!.ToDto();
    }

    public async Task<bool> UpdateAsync(int id, CreateShoppingListDto dto, string userId)
    {
        var existing = await ShoppingListRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
            return false;

        existing.Name = dto.Name;
        existing.UpdatedAt = DateTime.UtcNow;

        // Mevcut items'ı sil
        await ShoppingListItemRepository.DeleteByShoppingListIdAsync(id);
        await _unitOfWork.SaveChangesAsync();

        // Yeni items ekle
        var displayOrder = 0;
        foreach (var itemDto in dto.Items)
        {
            var item = new ShoppingListItem
            {
                ShoppingListId = id,
                Ingredient = itemDto.Ingredient,
                Quantity = itemDto.Quantity,
                Unit = itemDto.Unit,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            await ShoppingListItemRepository.AddAsync(item);
        }

        await ShoppingListRepository.UpdateAsync(existing);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateItemCheckedAsync(int listId, int itemId, bool isChecked, string userId)
    {
        var shoppingList = await ShoppingListRepository.GetByIdAsync(listId);
        if (shoppingList == null || shoppingList.UserId != userId)
            return false;

        var item = await ShoppingListItemRepository.GetByIdAsync(itemId);
        if (item == null || item.ShoppingListId != listId)
            return false;

        item.IsChecked = isChecked;
        await ShoppingListItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var shoppingList = await ShoppingListRepository.GetByIdAsync(id);
        if (shoppingList == null || shoppingList.UserId != userId)
            return false;

        await ShoppingListRepository.DeleteAsync(shoppingList);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private List<ParsedIngredient> ParseIngredients(string ingredientsText)
    {
        var result = new List<ParsedIngredient>();

        if (string.IsNullOrWhiteSpace(ingredientsText))
            return result;

        // Virgül veya satır sonu ile ayır
        var lines = ingredientsText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Basit parse: "2 adet domates" veya "500g un" gibi formatları destekle
            var match = Regex.Match(trimmed, @"^(\d+(?:[.,]\d+)?)\s*(\w+)?\s*(.+)$");
            if (match.Success)
            {
                var quantity = double.Parse(match.Groups[1].Value.Replace(',', '.'));
                var unit = match.Groups[2].Value.Trim();
                var name = match.Groups[3].Value.Trim();

                result.Add(new ParsedIngredient
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = string.IsNullOrWhiteSpace(unit) ? null : unit
                });
            }
            else
            {
                // Parse edilemezse direkt isim olarak ekle
                result.Add(new ParsedIngredient
                {
                    Name = trimmed,
                    Quantity = 1,
                    Unit = null
                });
            }
        }

        return result;
    }

    private class ParsedIngredient
    {
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string? Unit { get; set; }
    }

    private class IngredientInfo
    {
        public string Name { get; set; } = string.Empty;
        public double TotalQuantity { get; set; }
        public string? Unit { get; set; }
    }
}

