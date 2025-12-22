using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using FrontendMvc.Models;
using FrontendMvc.Models.Recipes;

namespace FrontendMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    
    // Custom JSON options for deserialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { 
            new FrontendMvc.Models.Recipes.DifficultyJsonConverter(),
            new FrontendMvc.Models.Recipes.IngredientsListConverter(),
            new FrontendMvc.Models.Recipes.StepsListConverter()
        }
    };

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(
        int? categoryId, 
        string? search, 
        string? sortBy,
        string? difficulty,
        int? maxPrepTime,
        int? maxCookingTime,
        int? maxTotalTime,
        int? minServings,
        int? maxServings,
        string? ingredient,
        string? excludedIngredients,
        string? dietType,
        bool? isFeatured,
        double? minRating,
        int? minRatingCount,
        int pageNumber = 1, 
        int pageSize = 10)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        List<RecipeViewModel> recipes;
        PagedResult<RecipeViewModel>? pagedResult = null;

        // ExcludedIngredients string'i list'e çevir
        List<string>? excludedIngredientsList = null;
        if (!string.IsNullOrWhiteSpace(excludedIngredients))
        {
            excludedIngredientsList = excludedIngredients
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        // Gelişmiş filtreleme kullanılacak mı kontrol et
        // sortBy parametresi varsa mutlaka gelişmiş filtreleme kullan (gerçek zamanlı sıralama için)
        bool useAdvancedFilters = !string.IsNullOrWhiteSpace(sortBy) ||
                                  categoryId.HasValue || 
                                  !string.IsNullOrWhiteSpace(difficulty) ||
                                  maxPrepTime.HasValue ||
                                  maxCookingTime.HasValue ||
                                  maxTotalTime.HasValue ||
                                  minServings.HasValue ||
                                  maxServings.HasValue ||
                                  !string.IsNullOrWhiteSpace(ingredient) ||
                                  (excludedIngredientsList != null && excludedIngredientsList.Any()) ||
                                  !string.IsNullOrWhiteSpace(dietType) ||
                                  isFeatured.HasValue ||
                                  minRating.HasValue ||
                                  minRatingCount.HasValue;

        if (useAdvancedFilters || (!string.IsNullOrWhiteSpace(search) && (categoryId.HasValue || !string.IsNullOrWhiteSpace(difficulty))))
        {
            // Gelişmiş filtreleme endpoint'ini kullan
            var filterDto = new
            {
                searchTerm = search,
                categoryId = categoryId,
                difficulty = !string.IsNullOrWhiteSpace(difficulty) ? MapDifficultyToEnum(difficulty) : (int?)null,
                maxPrepTime = maxPrepTime,
                maxCookingTime = maxCookingTime,
                maxTotalTime = maxTotalTime,
                minServings = minServings,
                maxServings = maxServings,
                ingredient = ingredient,
                excludedIngredients = excludedIngredientsList,
                dietType = dietType,
                isFeatured = isFeatured,
                minRating = minRating,
                minRatingCount = minRatingCount,
                sortBy = sortBy, // null ise backend'de "smart" sıralaması yapılır
                sortDescending = true
            };

            var response = await client.PostAsJsonAsync($"/api/recipes/filter?pageNumber={pageNumber}&pageSize={pageSize}", filterDto);
            if (response.IsSuccessStatusCode)
            {
                pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            }
            else
            {
                recipes = new List<RecipeViewModel>();
            }
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            // Basit arama
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/search?q={Uri.EscapeDataString(search)}&pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.SearchTerm = search;
        }
        else if (categoryId.HasValue)
        {
            // Kategoriye göre
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/category/{categoryId.Value}?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.CategoryId = categoryId.Value;
        }
        else
        {
            // Tüm tarifler
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
        }

        // ViewBag'e filtre değerlerini aktar
        ViewBag.SearchTerm = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.SortBy = sortBy;
        ViewBag.Difficulty = difficulty;
        ViewBag.MaxPrepTime = maxPrepTime;
        ViewBag.MaxCookingTime = maxCookingTime;
        ViewBag.MaxTotalTime = maxTotalTime;
        ViewBag.MinServings = minServings;
        ViewBag.MaxServings = maxServings;
        ViewBag.Ingredient = ingredient;
        ViewBag.ExcludedIngredients = excludedIngredients;
        ViewBag.DietType = dietType;
        ViewBag.IsFeatured = isFeatured;
        ViewBag.MinRating = minRating;
        ViewBag.MinRatingCount = minRatingCount;

        // Kategorileri yükle ve DisplayOrder'a göre sırala
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        ViewBag.Categories = categories;
        ViewBag.PagedResult = pagedResult;

        return View(recipes);
    }

    private int? MapDifficultyToEnum(string difficulty)
    {
        // Backend DifficultyLevel enum: Kolay=1, Orta=2, Zor=3
        // JsonStringEnumConverter kullanıldığı için string olarak gönderebiliriz, ama integer da kabul edilir
        return difficulty?.ToLower() switch
        {
            "kolay" => 1,
            "orta" => 2,
            "zor" => 3,
            _ => null
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
