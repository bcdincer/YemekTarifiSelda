using System.Net;
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
        Converters = { 
            new FrontendMvc.Models.Recipes.DifficultyJsonConverter(),
            new FrontendMvc.Models.Recipes.IngredientsListConverter(),
            new FrontendMvc.Models.Recipes.StepsListConverter()
        }
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
        
        // Token'ı header'a ekle (localStorage'dan JavaScript ile alınacak, şimdilik boş)
        // Not: Server-side'da token yok, bu yüzden client-side API çağrıları kullanılmalı
        // Şimdilik bu action sadece view'i render ediyor, gerçek veri JavaScript ile yüklenecek
        
        List<RecipeViewModel> likedRecipes = new List<RecipeViewModel>(); // Boş liste, JavaScript ile yüklenecek
        
        ViewBag.SearchTerm = search;
        
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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "Tarif eklemek için önce giriş yapmanız gerekiyor.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Recipes") });
        }

        // Admin kontrolü - Admin'ler başkalarının tariflerini düzenleyebilir ama tarif eklemek için yine de yazar olmalılar
        bool isAdmin = User.IsInRole("Admin");

        // Kullanıcının yazar olup olmadığını kontrol et
        var client = CreateApiClient();
        AuthorViewModel? author = null;
        try
        {
            var response = await client.GetAsync($"/api/authors/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                author = await response.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
            }
        }
        catch (HttpRequestException)
        {
            // 404 veya başka bir hata - yazar değil
            author = null;
        }

        // Hem Admin hem de normal kullanıcılar için yazar olmak zorunlu (Admin'ler de yazar olarak tarif eklemeli)
        if (author == null || !author.IsActive)
        {
            TempData["InfoMessage"] = "Tarif eklemek için önce yazar olmanız gerekiyor.";
            return RedirectToAction("BecomeAuthor", "Author");
        }

        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;
        ViewBag.IsAdmin = isAdmin;
        ViewBag.AuthorId = author?.Id; // Admin değilse author.Id'yi kullan, Admin ise null (kendi author'ını oluşturmalı)
        return View(new RecipeViewModel { Difficulty = "Orta", Servings = 4 });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeViewModel model, IFormFile? imageFile, IFormFile[]? imageFiles, string? imageUrlsJson, int? primaryImageIndex)
    {
        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kullanıcının yazar olup olmadığını kontrol et
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Admin kontrolü - Admin'ler başkalarının tariflerini düzenleyebilir ama tarif eklemek için yine de yazar olmalılar
        bool isAdmin = User.IsInRole("Admin");

        AuthorViewModel? author = null;
        try
        {
            var authorResponse = await client.GetAsync($"/api/authors/user/{userId}");
            if (authorResponse.IsSuccessStatusCode)
            {
                author = await authorResponse.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
            }
        }
        catch (HttpRequestException)
        {
            // 404 veya başka bir hata - yazar değil
            author = null;
        }

        // Hem Admin hem de normal kullanıcılar için yazar olmak zorunlu (Admin'ler de yazar olarak tarif eklemeli)
        if (author == null || !author.IsActive)
        {
            TempData["InfoMessage"] = "Tarif eklemek için önce yazar olmanız gerekiyor.";
            return RedirectToAction("BecomeAuthor", "Author");
        }

        // Çoklu fotoğraf yükleme - S3'e yükle
        var imageUrls = new List<string>();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Eski tek dosya desteği (backward compatibility) - S3'e yükle
        if (imageFile != null && imageFile.Length > 0)
        {
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
                            imageUrls.Add(uploadedUrl);
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

        // Yeni çoklu dosya desteği - S3'e yükle
        if (imageFiles != null && imageFiles.Length > 0)
        {
            foreach (var file in imageFiles)
            {
                if (file.Length == 0) continue;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFiles", $"{file.FileName} sadece JPG, PNG, GIF veya WebP formatında olabilir.");
                    continue;
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("ImageFiles", $"{file.FileName} 5MB'dan küçük olmalıdır.");
                    continue;
                }

                // S3'e yükle
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = file.OpenReadStream();
                    content.Add(new StreamContent(fileStream), "file", file.FileName);
                    
                    var uploadResponse = await client.PostAsync("/api/upload/image", content);
                    if (uploadResponse.IsSuccessStatusCode)
                    {
                        var result = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
                        if (result.TryGetProperty("url", out var urlElement))
                        {
                            var url = urlElement.GetString();
                            if (!string.IsNullOrWhiteSpace(url) && !imageUrls.Contains(url))
                            {
                                imageUrls.Add(url);
                            }
                            else if (string.IsNullOrWhiteSpace(url))
                            {
                                ModelState.AddModelError("ImageFiles", $"{file.FileName} yüklendi ancak URL alınamadı.");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("ImageFiles", $"{file.FileName} yüklendi ancak yanıt formatı hatalı.");
                        }
                    }
                    else
                    {
                        var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                        var statusCode = uploadResponse.StatusCode;
                        ModelState.AddModelError("ImageFiles", $"{file.FileName} yüklenirken hata (Status: {statusCode}): {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFiles", $"{file.FileName} yüklenirken hata: {ex.Message}");
                }
            }
        }

        // JSON'dan URL'leri ekle (zaten S3 URL'leri olabilir veya external URL'ler)
        if (!string.IsNullOrWhiteSpace(imageUrlsJson))
        {
            try
            {
                var urlsFromJson = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imageUrlsJson);
                if (urlsFromJson != null)
                {
                    foreach (var url in urlsFromJson)
                    {
                        if (!string.IsNullOrWhiteSpace(url) && !imageUrls.Contains(url))
                        {
                            // Eğer base64 data URL ise, S3'e yükle
                            if (url.StartsWith("data:image"))
                            {
                                try
                                {
                                    var base64Data = url.Split(',')[1];
                                    var imageBytes = Convert.FromBase64String(base64Data);
                                    var mimeType = url.Split(';')[0].Split(':')[1];
                                    var extension = mimeType.Split('/')[1];
                                    
                                    using var ms = new MemoryStream(imageBytes);
                                    using var content = new MultipartFormDataContent();
                                    content.Add(new StreamContent(ms), "file", $"image.{extension}");
                                    
                                    var uploadResponse = await client.PostAsync("/api/upload/image", content);
                                    if (uploadResponse.IsSuccessStatusCode)
                                    {
                                        var result = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
                                        if (result.TryGetProperty("url", out var urlElement))
                                        {
                                            imageUrls.Add(urlElement.GetString() ?? string.Empty);
                                        }
                                    }
                                }
                                catch
                                {
                                    // Base64 yükleme hatası, atla
                                }
                            }
                            else
                            {
                                // Zaten bir URL (S3 veya external)
                                imageUrls.Add(url);
                            }
                        }
                    }
                }
            }
            catch
            {
                // JSON parse hatası, devam et
            }
        }

        // Ana fotoğrafı belirle
        var primaryIdx = primaryImageIndex ?? 0;
        if (primaryIdx < 0 || primaryIdx >= imageUrls.Count)
            primaryIdx = 0;
        
        // Eğer hiç fotoğraf yoksa, eski ImageUrl'i kullan (backward compatibility)
        if (imageUrls.Count == 0 && !string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            imageUrls.Add(model.ImageUrl);
        }

        // RecipeViewModel'den CreateRecipeDto formatına mapping yap
        // Backend API PascalCase bekliyor (PropertyNamingPolicy = null)
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

        // AuthorId'yi ekle
        // Hem Admin hem de normal kullanıcılar için author zorunlu (Admin'ler de yazar olarak tarif eklemeli)
        int? authorId = author?.Id;
        if (!authorId.HasValue)
        {
            TempData["InfoMessage"] = "Tarif eklemek için önce yazar olmanız gerekiyor.";
            return RedirectToAction("BecomeAuthor", "Author");
        }

        // Anonymous object kullanarak JSON serialization otomatik olarak PascalCase kullanır (PropertyNamingPolicy = null ayarı sayesinde)
        var createDto = new
        {
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            Ingredients = ingredientsList,
            Steps = stepsList,
            PrepTimeMinutes = model.PrepTimeMinutes,
            CookingTimeMinutes = model.CookingTimeMinutes,
            Servings = model.Servings,
            Difficulty = model.Difficulty ?? "Orta",
            ImageUrl = imageUrls.Count > 0 ? imageUrls[primaryIdx] : (string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl), // Ana fotoğraf
            ImageUrls = imageUrls.Count > 0 ? imageUrls : null, // Çoklu fotoğraflar
            PrimaryImageIndex = imageUrls.Count > 0 ? (int?)primaryIdx : null, // Ana fotoğraf index'i
            Tips = string.IsNullOrWhiteSpace(model.Tips) ? null : model.Tips,
            AlternativeIngredients = string.IsNullOrWhiteSpace(model.AlternativeIngredients) ? null : model.AlternativeIngredients,
            NutritionInfo = string.IsNullOrWhiteSpace(model.NutritionInfo) ? null : model.NutritionInfo,
            CategoryId = model.CategoryId, // Backend validator'da nullable, bu yüzden null gönderebiliriz
            AuthorId = authorId,
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

        // Yazar ise tariflerim sayfasına, Admin ise Admin panelindeki tarifler listesine, değilse ana sayfaya yönlendir
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Recipes", "Admin");
        }
        else if (authorId.HasValue)
        {
            return RedirectToAction("MyRecipes", "Author", new { authorId = authorId.Value });
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

    [Authorize]
    public async Task<IActionResult> Collections()
    {
        try
        {
            var client = CreateApiClient();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var response = await client.GetAsync("/api/users/collections");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Koleksiyonlar yüklenirken bir hata oluştu. (Status: {(int)response.StatusCode})";
                return View(new List<CollectionViewModel>());
            }
            
            var collections = await response.Content.ReadFromJsonAsync<List<CollectionViewModel>>(JsonOptions) 
                             ?? new List<CollectionViewModel>();
            
            return View(collections);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Koleksiyonlar yüklenirken bir hata oluştu.";
            return View(new List<CollectionViewModel>());
        }
    }

    [Authorize]
    public async Task<IActionResult> CollectionDetail(int id)
    {
        var client = CreateApiClient();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        // Token'ı header'a ekle
        var token = HttpContext.Request.Cookies["authToken"] ?? 
                    HttpContext.Session.GetString("authToken");
        
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        var collection = await client.GetFromJsonAsync<CollectionDetailViewModel>($"/api/users/collections/{id}/detail", JsonOptions);
        
        if (collection == null)
        {
            return NotFound();
        }
        
        return View(collection);
    }

    // Tarif Düzenle - GET
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var client = CreateApiClient();
        var recipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);

        if (recipe == null)
        {
            return NotFound();
        }

        // Admin kontrolü - Admin'ler başkalarının tariflerini de düzenleyebilir
        bool isAdmin = User.IsInRole("Admin");

        // Kullanıcının yazar olup olmadığını ve bu tarifin sahibi olup olmadığını kontrol et
        AuthorViewModel? author = null;
        try
        {
            var authorResponse = await client.GetAsync($"/api/authors/user/{userId}");
            if (authorResponse.IsSuccessStatusCode)
            {
                author = await authorResponse.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
            }
        }
        catch (HttpRequestException)
        {
            author = null;
        }

        // Admin değilse, yazar kontrolü yap
        if (!isAdmin)
        {
            if (author == null || !author.IsActive)
            {
                TempData["ErrorMessage"] = "Tarif düzenlemek için yazar olmanız gerekiyor.";
                return RedirectToAction("BecomeAuthor", "Author");
            }

            // Tarifin bu yazara ait olup olmadığını kontrol et
            if (recipe.AuthorId != author.Id)
            {
                TempData["ErrorMessage"] = "Sadece kendi tariflerinizi düzenleyebilirsiniz.";
                return RedirectToAction("MyRecipes", "Author", new { authorId = author.Id });
            }
        }

        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;
        ViewBag.ReturnUrl = returnUrl;

        return View(recipe);
    }

    // Tarif Düzenle - POST
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RecipeViewModel model, IFormFile? imageFile, IFormFile[]? imageFiles, string? imageUrlsJson, string? removedImageIds, int? primaryImageIndex, string? returnUrl = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var client = CreateApiClient();
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;

        // Mevcut tarifi al
        var existingRecipe = await client.GetFromJsonAsync<RecipeViewModel>($"/api/recipes/{id}", JsonOptions);
        if (existingRecipe == null)
        {
            return NotFound();
        }

        // Admin kontrolü - Admin'ler başkalarının tariflerini de düzenleyebilir
        bool isAdmin = User.IsInRole("Admin");

        // Kullanıcının yazar olup olmadığını ve bu tarifin sahibi olup olmadığını kontrol et
        AuthorViewModel? author = null;
        try
        {
            var authorResponse = await client.GetAsync($"/api/authors/user/{userId}");
            if (authorResponse.IsSuccessStatusCode)
            {
                author = await authorResponse.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
            }
        }
        catch (HttpRequestException)
        {
            author = null;
        }

        // Admin değilse, yazar kontrolü yap
        if (!isAdmin)
        {
            if (author == null || !author.IsActive)
            {
                TempData["ErrorMessage"] = "Tarif düzenlemek için yazar olmanız gerekiyor.";
                return RedirectToAction("BecomeAuthor", "Author");
            }

            // Tarifin bu yazara ait olup olmadığını kontrol et
            if (existingRecipe.AuthorId != author.Id)
            {
                TempData["ErrorMessage"] = "Sadece kendi tariflerinizi düzenleyebilirsiniz.";
                return RedirectToAction("MyRecipes", "Author", new { authorId = author.Id });
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // AdminController'daki EditRecipe mantığını kullan
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var imageUrls = new List<string>();

        // Eski tek dosya desteği (backward compatibility) - S3'e yükle
        if (imageFile != null && imageFile.Length > 0)
        {
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
                            imageUrls.Add(uploadedUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ImageFile", $"Fotoğraf yüklenirken hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // Çoklu dosya desteği - S3'e yükle
        if (imageFiles != null && imageFiles.Length > 0)
        {
            foreach (var file in imageFiles)
            {
                if (file.Length == 0) continue;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFiles", $"{file.FileName} sadece JPG, PNG, GIF veya WebP formatında olabilir.");
                    continue;
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("ImageFiles", $"{file.FileName} 5MB'dan küçük olmalıdır.");
                    continue;
                }

                // S3'e yükle
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = file.OpenReadStream();
                    content.Add(new StreamContent(fileStream), "file", file.FileName);
                    
                    var uploadResponse = await client.PostAsync("/api/upload/image", content);
                    if (uploadResponse.IsSuccessStatusCode)
                    {
                        var result = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOptions);
                        if (result.TryGetProperty("url", out var urlElement))
                        {
                            var url = urlElement.GetString();
                            if (!string.IsNullOrWhiteSpace(url) && !imageUrls.Contains(url))
                            {
                                imageUrls.Add(url);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFiles", $"Fotoğraf yüklenirken hata oluştu: {ex.Message}");
                    continue;
                }
            }
        }

        // Mevcut görselleri koru (imageUrlsJson'dan)
        if (!string.IsNullOrWhiteSpace(imageUrlsJson))
        {
            try
            {
                var urlsFromJson = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imageUrlsJson);
                if (urlsFromJson != null)
                {
                    foreach (var url in urlsFromJson)
                    {
                        if (!string.IsNullOrWhiteSpace(url) && !imageUrls.Contains(url))
                        {
                            imageUrls.Add(url);
                        }
                    }
                }
            }
            catch
            {
                // JSON parse error, continue
            }
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

        var primaryIdx = primaryImageIndex ?? 0;
        if (primaryIdx < 0 || primaryIdx >= imageUrls.Count)
            primaryIdx = imageUrls.Count > 0 ? 0 : -1;

        // RecipeViewModel'den CreateRecipeDto formatına mapping yap
        // ÖNEMLİ: AuthorId'yi mevcut tariften al (güncellemede değiştirilmemeli)
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
            ImageUrl = imageUrls.Count > 0 && primaryIdx >= 0 ? imageUrls[primaryIdx] : (string.IsNullOrWhiteSpace(model.ImageUrl) ? existingRecipe.ImageUrl : model.ImageUrl),
            ImageUrls = imageUrls.Count > 0 ? imageUrls : null,
            PrimaryImageIndex = imageUrls.Count > 0 && primaryIdx >= 0 ? (int?)primaryIdx : null,
            Tips = string.IsNullOrWhiteSpace(model.Tips) ? null : model.Tips,
            AlternativeIngredients = string.IsNullOrWhiteSpace(model.AlternativeIngredients) ? null : model.AlternativeIngredients,
            NutritionInfo = string.IsNullOrWhiteSpace(model.NutritionInfo) ? null : model.NutritionInfo,
            CategoryId = model.CategoryId,
            AuthorId = existingRecipe.AuthorId, // Mevcut AuthorId'yi koru
            IsFeatured = model.IsFeatured
        };

        // Backend'e PUT isteği gönder
        HttpResponseMessage updateResponse;
        try
        {
            updateResponse = await client.PutAsJsonAsync($"/api/recipes/{id}", updateDto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Tarif güncellenirken bir hata oluştu: {ex.Message}");
            return View(model);
        }
        
        if ((int)updateResponse.StatusCode >= 400)
        {
            var errorContent = await updateResponse.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Tarif güncellenirken bir hata oluştu: {errorContent}");
            return View(model);
        }

        TempData["SuccessMessage"] = "Tarif başarıyla güncellendi.";
        
        // returnUrl varsa oraya yönlendir
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        
        // Admin ise tarifler listesine, değilse kendi tariflerine yönlendir
        if (isAdmin)
        {
            return RedirectToAction("Recipes", "Admin");
        }
        else
        {
            return RedirectToAction("MyRecipes", "Author", new { authorId = author!.Id });
        }
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        
        // Token'ı cookie'den al
        var token = HttpContext.Request.Cookies["authToken"];
        
        // Session yapılandırılmışsa ve cookie'de token yoksa session'dan al
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                if (HttpContext.Session != null && HttpContext.Session.IsAvailable)
                {
                    token = HttpContext.Session.GetString("authToken");
                }
            }
            catch
            {
                // Session yapılandırılmamış, devam et
            }
        }
        
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }
}


