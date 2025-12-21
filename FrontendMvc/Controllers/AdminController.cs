using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Data;
using FrontendMvc.Models.Admin;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        IHttpClientFactory httpClientFactory, 
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _httpClientFactory = httpClientFactory;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FrontendMvc.Models.Recipes.DifficultyJsonConverter() }
    };

    // Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var client = CreateApiClient();
        
        // İstatistikleri al
        var allRecipesPaged = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>("/api/recipes", JsonOptions);
        var allRecipes = allRecipesPaged?.Items ?? new List<RecipeViewModel>();
        
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                        ?? new List<CategoryViewModel>();
        var featuredRecipes = await client.GetFromJsonAsync<List<RecipeViewModel>>("/api/recipes/featured", JsonOptions) 
                             ?? new List<RecipeViewModel>();
        
        ViewBag.TotalRecipes = allRecipesPaged?.TotalCount ?? allRecipes.Count;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.FeaturedCount = featuredRecipes.Count;
        ViewBag.TotalUsers = _userManager.Users.Count();
        ViewBag.RecentRecipes = allRecipes.OrderByDescending(r => r.CreatedAt).Take(5).ToList();
        
        return View();
    }

    // Tarif Yönetimi
    public async Task<IActionResult> Recipes(int? categoryId, string? search)
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

        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        return View(recipes);
    }

    // Tarif Düzenle
    [HttpGet]
    public async Task<IActionResult> EditRecipe(int id)
    {
        var client = CreateApiClient();
        var recipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);

        if (recipe == null)
        {
            return NotFound();
        }

        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        return View(recipe);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRecipe(int id, RecipeViewModel model, IFormFile? imageFile)
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        // Mevcut tarifi al (ImageUrl'i korumak için)
        var existingRecipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);
        if (existingRecipe == null)
        {
            return NotFound();
        }
        var currentImageUrl = existingRecipe.ImageUrl;
        
        // Yeni yüklenen dosya için ImageUrl'i sakla
        string? newImageUrl = null;

        // Dosya yüklendiyse kaydet (öncelik dosya yüklemede)
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

            // Yeni URL'i sakla
            newImageUrl = $"/images/recipes/{fileName}";
            model.ImageUrl = newImageUrl;
        }
        else if (string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            // Dosya yüklenmedi ve ImageUrl input'u boşsa, mevcut ImageUrl'i koru
            model.ImageUrl = currentImageUrl;
        }
        // Eğer model.ImageUrl doluysa (URL input'undan geliyorsa), onu kullan (zaten model'de var)

        // RecipeViewModel'den CreateRecipeDto formatına mapping yap
        var updateDto = new
        {
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            Ingredients = model.Ingredients ?? string.Empty,
            Steps = model.Steps ?? string.Empty,
            PrepTimeMinutes = model.PrepTimeMinutes,
            CookingTimeMinutes = model.CookingTimeMinutes,
            Servings = model.Servings,
            Difficulty = model.Difficulty ?? "Orta",
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl,
            Tips = string.IsNullOrWhiteSpace(model.Tips) ? null : model.Tips,
            AlternativeIngredients = string.IsNullOrWhiteSpace(model.AlternativeIngredients) ? null : model.AlternativeIngredients,
            NutritionInfo = string.IsNullOrWhiteSpace(model.NutritionInfo) ? null : model.NutritionInfo,
            CategoryId = model.CategoryId,
            IsFeatured = model.IsFeatured
        };

        // Backend'e PUT isteği gönder
        HttpResponseMessage response;
        try
        {
            response = await client.PutAsJsonAsync($"/api/recipes/{id}", updateDto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Tarif güncellenirken bir hata oluştu: {ex.Message}");
            if (!string.IsNullOrEmpty(newImageUrl))
            {
                model.ImageUrl = newImageUrl;
            }
            else if (string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                model.ImageUrl = currentImageUrl;
            }
            return View(model);
        }
        
        // Response durumunu kontrol et (400, 404, 500 gibi hata kodları için)
        if ((int)response.StatusCode >= 400)
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
                    ModelState.AddModelError(string.Empty, $"Tarif güncellenirken bir hata oluştu: {errorContent}");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, $"Tarif güncellenirken bir hata oluştu: {errorContent}");
            }
            
            // Hata durumunda, yeni yüklenen görsel varsa model'de koru
            if (!string.IsNullOrEmpty(newImageUrl))
            {
                model.ImageUrl = newImageUrl;
            }
            else if (string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                model.ImageUrl = currentImageUrl;
            }
            
            return View(model);
        }

        TempData["SuccessMessage"] = "Tarif başarıyla güncellendi.";
        return RedirectToAction(nameof(Recipes));
    }

    // Tarif Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecipe(int id)
    {
        var client = CreateApiClient();
        var response = await client.DeleteAsync($"/api/recipes/{id}");

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Tarif başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Tarif silinirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Recipes));
    }

    // Kategori Yönetimi
    public async Task<IActionResult> Categories()
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                         ?? new List<CategoryViewModel>();
        return View(categories);
    }

    // Kullanıcı Yönetimi
    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            });
        }

        return View(userViewModels);
    }

    // Rol Yönetimi
    public IActionResult Roles()
    {
        var roles = _roleManager.Roles.ToList();
        var roleViewModels = new List<RoleViewModel>();

        foreach (var role in roles)
        {
            var userCount = _userManager.GetUsersInRoleAsync(role.Name!).Result.Count;
            roleViewModels.Add(new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                UserCount = userCount
            });
        }

        return View(roleViewModels);
    }

    // Yeni Rol Oluştur - GET
    [HttpGet]
    public IActionResult CreateRole()
    {
        return View();
    }

    // Yeni Rol Oluştur - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModelState.AddModelError("", "Rol adı boş olamaz.");
            return View();
        }

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            ModelState.AddModelError("", "Bu rol zaten mevcut.");
            return View();
        }

        var role = new IdentityRole(roleName);
        var result = await _roleManager.CreateAsync(role);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Rol başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Roles));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View();
    }

    // Rol Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            TempData["ErrorMessage"] = "Rol bulunamadı.";
            return RedirectToAction(nameof(Roles));
        }

        // Admin rolünü silmeyi engelle
        if (role.Name == "Admin")
        {
            TempData["ErrorMessage"] = "Admin rolü silinemez.";
            return RedirectToAction(nameof(Roles));
        }

        // Rolün kullanıcıları var mı kontrol et
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            TempData["ErrorMessage"] = "Bu rolde kullanıcılar bulunmaktadır. Önce kullanıcıları başka rollere atayın.";
            return RedirectToAction(nameof(Roles));
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Rol başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Rol silinirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Roles));
    }

    // Kullanıcıya Rol Ata - GET
    [HttpGet]
    public async Task<IActionResult> AssignRole(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = _roleManager.Roles.Select(r => r.Name!).ToList();

        var model = new AssignRoleViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? "",
            UserFullName = user.FullName ?? "",
            AvailableRoles = allRoles,
            UserRoles = userRoles.ToList()
        };

        return View(model);
    }

    // Kullanıcıya Rol Ata - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, List<string> selectedRoles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        
        // Seçilmemiş rolleri kaldır
        var rolesToRemove = currentRoles.Except(selectedRoles ?? new List<string>());
        foreach (var role in rolesToRemove)
        {
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        // Yeni seçilen rolleri ekle
        var rolesToAdd = (selectedRoles ?? new List<string>()).Except(currentRoles);
        foreach (var role in rolesToAdd)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        TempData["SuccessMessage"] = "Kullanıcı rolleri başarıyla güncellendi.";
        return RedirectToAction(nameof(Users));
    }

    // Görsel Yönetimi
    public async Task<IActionResult> Images()
    {
        var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "recipes");
        
        if (!Directory.Exists(imagesPath))
        {
            return View(new List<ImageInfo>());
        }

        var imageFiles = Directory.GetFiles(imagesPath)
            .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".png") || f.EndsWith(".gif") || f.EndsWith(".webp"))
            .Select(f => new ImageInfo
            {
                FileName = Path.GetFileName(f),
                Url = $"/images/recipes/{Path.GetFileName(f)}",
                Size = new FileInfo(f).Length,
                CreatedAt = System.IO.File.GetCreationTime(f)
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

        return View(imageFiles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteImage(string fileName)
    {
        var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "recipes");
        var filePath = Path.Combine(imagesPath, fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            TempData["SuccessMessage"] = "Görsel başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Görsel bulunamadı.";
        }

        return RedirectToAction(nameof(Images));
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        return client;
    }
}

