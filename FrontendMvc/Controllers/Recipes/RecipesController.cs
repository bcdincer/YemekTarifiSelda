using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers.Recipes;

public class RecipesController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : Controller
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    
    // Custom JSON options for deserialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FrontendMvc.Models.Recipes.DifficultyJsonConverter() }
    };

    public async Task<IActionResult> Index(int? categoryId, string? search, string? sortBy)
    {
        var client = CreateApiClient();
        List<RecipeViewModel> recipes;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/search?q={Uri.EscapeDataString(search)}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.SearchTerm = search;
        }
        else if (categoryId.HasValue)
        {
            var pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>($"/api/recipes/category/{categoryId.Value}", JsonOptions);
            recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            ViewBag.CategoryId = categoryId.Value;
        }
        else
        {
            var pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>("/api/recipes", JsonOptions);
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

        // Kategorileri yükle ve DisplayOrder'a göre sırala
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        ViewBag.Categories = categories;

        return View(recipes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = CreateApiClient();
        var recipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);

        if (recipe == null)
        {
            return NotFound();
        }

        // Görüntülenme sayısını artır
        await client.PostAsync($"/api/recipes/{id}/view", null);

        // Kategorileri yükle (detay sayfasında kullanılabilir)
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        return View(recipe);
    }

    [Authorize]
    public async Task<IActionResult> LikedRecipes(string? search)
    {
        var client = CreateApiClient();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        List<RecipeViewModel> likedRecipes;
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Arama yapılıyorsa
            var searchUrl = $"/api/users/{userId}/liked-recipes/search?q={Uri.EscapeDataString(search)}";
            likedRecipes = await client.GetFromJsonAsync<List<RecipeViewModel>>(searchUrl, JsonOptions) 
                          ?? new List<RecipeViewModel>();
            ViewBag.SearchTerm = search;
        }
        else
        {
            // Normal liste
            likedRecipes = await client.GetFromJsonAsync<List<RecipeViewModel>>($"/api/users/{userId}/liked-recipes", JsonOptions) 
                          ?? new List<RecipeViewModel>();
        }
        
        // Kategorileri yükle ve DisplayOrder'a göre sırala
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        ViewBag.Categories = categories;
        
        return View(likedRecipes);
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;
        return View(new RecipeViewModel { Difficulty = "Orta", Servings = 4 });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeViewModel model, IFormFile? imageFile)
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Dosya yüklendiyse kaydet
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ImageFile", "Sadece JPG, PNG, GIF veya WebP formatında görseller yüklenebilir.");
                return View(model);
            }

            if (imageFile.Length > 5 * 1024 * 1024) // 5MB
            {
                ModelState.AddModelError("ImageFile", "Görsel boyutu 5MB'dan küçük olmalıdır.");
                return View(model);
            }

            // Klasörü oluştur
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "recipes");
            Directory.CreateDirectory(imagesPath);

            // Benzersiz dosya adı oluştur
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(imagesPath, fileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Model'e URL'i ekle
            model.ImageUrl = $"/images/recipes/{fileName}";
        }

        // RecipeViewModel'den CreateRecipeDto formatına mapping yap
        // Backend API PascalCase bekliyor (PropertyNamingPolicy = null)
        // Anonymous object kullanarak JSON serialization otomatik olarak PascalCase kullanır (PropertyNamingPolicy = null ayarı sayesinde)
        var createDto = new
        {
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            Ingredients = model.Ingredients ?? string.Empty,
            Steps = model.Steps ?? string.Empty,
            PrepTimeMinutes = model.PrepTimeMinutes,
            CookingTimeMinutes = model.CookingTimeMinutes,
            Servings = model.Servings,
            Difficulty = model.Difficulty ?? "Orta",
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl, // Boş string yerine null gönder
            Tips = string.IsNullOrWhiteSpace(model.Tips) ? null : model.Tips,
            AlternativeIngredients = string.IsNullOrWhiteSpace(model.AlternativeIngredients) ? null : model.AlternativeIngredients,
            NutritionInfo = string.IsNullOrWhiteSpace(model.NutritionInfo) ? null : model.NutritionInfo,
            CategoryId = model.CategoryId, // Backend validator'da nullable, bu yüzden null gönderebiliriz
            IsFeatured = model.IsFeatured
        };
        
        // HttpClient PostAsJsonAsync, Program.cs'deki JsonOptions ayarlarını kullanır (PropertyNamingPolicy = null)
        var response = await client.PostAsJsonAsync("/api/recipes", createDto);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            
            // JSON error response'u parse et ve validation hatalarını göster
            try
            {
                var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
                
                if (errorJson.TryGetProperty("errors", out var errors))
                {
                    foreach (var errorProperty in errors.EnumerateObject())
                    {
                        var propertyName = errorProperty.Name;
                        if (errorProperty.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var errorMessage in errorProperty.Value.EnumerateArray())
                            {
                                ModelState.AddModelError(propertyName, errorMessage.GetString() ?? "Hata");
                            }
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Tarif kaydedilirken bir hata oluştu: {errorContent}");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, $"Tarif kaydedilirken bir hata oluştu: {errorContent}");
            }
            
            return View(model);
        }

        // Admin kullanıcı ise Admin panelindeki tarifler listesine, değilse ana sayfaya yönlendir
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Recipes", "Admin");
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Dosya seçilmedi.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Sadece JPG, PNG, GIF veya WebP formatında görseller yüklenebilir.");
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB
        {
            return BadRequest("Görsel boyutu 5MB'dan küçük olmalıdır.");
        }

        // Klasörü oluştur
        var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "recipes");
        Directory.CreateDirectory(imagesPath);

        // Benzersiz dosya adı oluştur
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(imagesPath, fileName);

        // Dosyayı kaydet
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Json(new { url = $"/images/recipes/{fileName}" });
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        return client;
    }
}


