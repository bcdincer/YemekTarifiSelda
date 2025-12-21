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
        Converters = { new FrontendMvc.Models.Recipes.DifficultyJsonConverter() }
    };

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(int? categoryId, string? search, string? sortBy, int pageNumber = 1, int pageSize = 10)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        List<RecipeViewModel> recipes;
        PagedResult<RecipeViewModel>? pagedResult = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/search?q={Uri.EscapeDataString(search)}&pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.SearchTerm = search;
        }
        else if (categoryId.HasValue)
        {
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/category/{categoryId.Value}?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.CategoryId = categoryId.Value;
        }
        else
        {
            pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
        }

        // Sıralama
        ViewBag.SortBy = sortBy;
        if (sortBy == "rating")
        {
            recipes = recipes
                .Where(r => r.AverageRating.HasValue)
                .OrderByDescending(r => r.AverageRating)
                .ThenByDescending(r => r.RatingCount)
                .ToList();
        }
        else if (sortBy == "likes")
        {
            recipes = recipes.OrderByDescending(r => r.LikeCount).ToList();
        }
        else if (sortBy == "newest")
        {
            recipes = recipes.OrderByDescending(r => r.CreatedAt).ToList();
        }

        // Kategorileri yükle ve DisplayOrder'a göre sırala
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        ViewBag.Categories = categories;
        ViewBag.PagedResult = pagedResult;

        return View(recipes);
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
