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
        Converters = { 
            new FrontendMvc.Models.Recipes.DifficultyJsonConverter(),
            new FrontendMvc.Models.Recipes.IngredientsListConverter(),
            new FrontendMvc.Models.Recipes.StepsListConverter()
        }
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
    public async Task<IActionResult> EditRecipe(int id, RecipeViewModel model, IFormFile? imageFile, string? imageUrlsJson, string? removedImageIds, int? primaryImageIndex)
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories", JsonOptions) 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        // Mevcut tarifi al
        var existingRecipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);
        if (existingRecipe == null)
        {
            return NotFound();
        }
        
        // Eski tek dosya desteği (backward compatibility) - S3'e yükle
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

            // S3'e yükle
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = imageFile.OpenReadStream();
                content.Add(new StreamContent(fileStream), "file", imageFile.FileName);
                
                var uploadResponse = await client.PostAsync("/api/upload/image", content);
                if (uploadResponse.IsSuccessStatusCode)
                {
                    var result = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
                    if (result.TryGetProperty("url", out var urlElement))
                    {
                        var uploadedUrl = urlElement.GetString();
                        if (!string.IsNullOrWhiteSpace(uploadedUrl))
                        {
                            model.ImageUrl = uploadedUrl;
                        }
                        else
                        {
                            ModelState.AddModelError("ImageFile", "Fotoğraf yüklendi ancak URL alınamadı.");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ImageFile", "Fotoğraf yüklendi ancak yanıt formatı hatalı.");
                        return View(model);
                    }
                }
                else
                {
                    var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                    var statusCode = uploadResponse.StatusCode;
                    ModelState.AddModelError("ImageFile", $"Fotoğraf yüklenirken hata oluştu (Status: {statusCode}): {errorContent}");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ImageFile", $"Fotoğraf yüklenirken hata oluştu: {ex.Message}");
                return View(model);
            }
        }
        else if (string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            // Dosya yüklenmedi ve ImageUrl input'u boşsa, mevcut ImageUrl'i koru
            model.ImageUrl = existingRecipe.ImageUrl;
        }

        // Malzemeleri ve adımları string'den listeye çevir
        var ingredientsList = string.IsNullOrWhiteSpace(model.Ingredients)
            ? new List<string>()
            : model.Ingredients.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

        var stepsList = string.IsNullOrWhiteSpace(model.Steps)
            ? new List<string>()
            : model.Steps.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

        // Çoklu fotoğraf desteği - imageUrlsJson'dan URL'leri al
        List<string>? imageUrls = null;
        if (!string.IsNullOrWhiteSpace(imageUrlsJson))
        {
            try
            {
                imageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imageUrlsJson, JsonOptions);
            }
            catch
            {
                // JSON parse hatası - görmezden gel
            }
        }
        
        // Silinen fotoğraf ID'lerini al
        List<int>? removedImageIdsList = null;
        if (!string.IsNullOrWhiteSpace(removedImageIds))
        {
            try
            {
                removedImageIdsList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(removedImageIds, JsonOptions);
            }
            catch
            {
                // JSON parse hatası - görmezden gel
            }
        }
        
        // Silinen fotoğrafları S3'ten sil (eğer S3 URL'leri ise)
        if (removedImageIdsList != null && removedImageIdsList.Any() && existingRecipe.Images != null)
        {
            var imagesToDelete = existingRecipe.Images
                .Where(img => removedImageIdsList.Contains(img.Id))
                .ToList();
            
            foreach (var image in imagesToDelete)
            {
                // S3 URL'si ise S3'ten sil
                if (!string.IsNullOrWhiteSpace(image.ImageUrl) && 
                    (image.ImageUrl.StartsWith("http://") || image.ImageUrl.StartsWith("https://")))
                {
                    try
                    {
                        var deleteUrl = $"/api/upload/image?url={Uri.EscapeDataString(image.ImageUrl)}";
                        var deleteResponse = await client.DeleteAsync(deleteUrl);
                        if (deleteResponse.IsSuccessStatusCode)
                        {
                            // Image deleted successfully
                        }
                        else
                        {
                            // Failed to delete - log but continue
                        }
                    }
                    catch
                    {
                        // S3'ten silme hatası olsa bile devam et
                        // Log error but don't throw
                    }
                }
            }
        }
        
        // RecipeViewModel'den CreateRecipeDto formatına mapping yap
        var updateDto = new
        {
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            Ingredients = ingredientsList,
            Steps = stepsList,
            PrepTimeMinutes = model.PrepTimeMinutes,
            CookingTimeMinutes = model.CookingTimeMinutes,
            Servings = model.Servings,
            Difficulty = model.Difficulty ?? "Orta",
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl, // Backward compatibility
            ImageUrls = imageUrls, // Çoklu fotoğraf URL'leri
            PrimaryImageIndex = primaryImageIndex, // Ana fotoğraf index'i
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
            if (string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                model.ImageUrl = existingRecipe.ImageUrl;
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
            
            // Hata durumunda, mevcut ImageUrl'i koru
            if (string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                model.ImageUrl = existingRecipe.ImageUrl;
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

    // Yazar Yönetimi
    public async Task<IActionResult> Authors(int pageNumber = 1, int pageSize = 20)
    {
        var client = CreateApiClient();
        var pagedResult = await client.GetFromJsonAsync<PagedResult<AuthorViewModel>>(
            $"/api/authors?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = pagedResult?.TotalCount ?? 0;

        return View(pagedResult?.Items ?? new List<AuthorViewModel>());
    }

    // Yazar Düzenle - GET
    [HttpGet]
    public async Task<IActionResult> EditAuthor(int id)
    {
        var client = CreateApiClient();
        var author = await client.GetFromJsonAsync<AuthorViewModel>($"/api/authors/{id}", JsonOptions);

        if (author == null)
        {
            return NotFound();
        }

        return View(author);
    }

    // Yazar Düzenle - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAuthor(int id, AuthorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = CreateApiClient();
        
        var updateDto = new
        {
            DisplayName = model.DisplayName ?? string.Empty,
            Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio,
            ProfileImageUrl = string.IsNullOrWhiteSpace(model.ProfileImageUrl) ? null : model.ProfileImageUrl,
            IsActive = model.IsActive
        };

        var response = await client.PutAsJsonAsync($"/api/authors/{id}/admin", updateDto);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Yazar başarıyla güncellendi.";
            return RedirectToAction(nameof(Authors));
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Yazar güncellenirken bir hata oluştu: {errorContent}");
        
        return View(model);
    }

    // Yazar Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAuthor(int id)
    {
        var client = CreateApiClient();
        // Backend'de delete endpoint'i yok, şimdilik sadece IsActive = false yapabiliriz
        var author = await client.GetFromJsonAsync<AuthorViewModel>($"/api/authors/{id}", JsonOptions);
        
        if (author == null)
        {
            TempData["ErrorMessage"] = "Yazar bulunamadı.";
            return RedirectToAction(nameof(Authors));
        }

        var updateDto = new
        {
            DisplayName = author.DisplayName,
            Bio = author.Bio,
            ProfileImageUrl = author.ProfileImageUrl,
            IsActive = false
        };

        var response = await client.PutAsJsonAsync($"/api/authors/{id}/admin", updateDto);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Yazar pasif hale getirildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Yazar güncellenirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Authors));
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        return client;
    }
}

