using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models.MealPlan;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FrontendMvc.Controllers;

[Authorize]
public class MealPlanController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MealPlanController> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MealPlanController(IHttpClientFactory httpClientFactory, ILogger<MealPlanController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        
        // Önce cookie'den token al
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

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateApiClient();
            var response = await client.GetAsync("/api/meal-plans");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Yemek planları yüklenirken bir hata oluştu.";
                return View(new List<MealPlanViewModel>());
            }
            
            var mealPlans = await response.Content.ReadFromJsonAsync<List<MealPlanViewModel>>(JsonOptions) 
                            ?? new List<MealPlanViewModel>();
            
            return View(mealPlans);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Yemek planları yüklenirken bir hata oluştu.";
            return View(new List<MealPlanViewModel>());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = CreateApiClient();
            var response = await client.GetAsync($"/api/meal-plans/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Yemek planı yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
            
            var mealPlan = await response.Content.ReadFromJsonAsync<MealPlanViewModel>(JsonOptions);
            
            if (mealPlan == null)
            {
                return NotFound();
            }
            
            return View(mealPlan);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Yemek planı yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(DateTime? startDate)
    {
        try
        {
            var client = CreateApiClient();
            
            // Tarifleri getir (dropdown için) - PagedResult döndürüyor
            var response = await client.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            List<RecipeViewModel> recipes = new List<RecipeViewModel>();
            if (response.IsSuccessStatusCode)
            {
                var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            }
            
            ViewBag.Recipes = recipes;
            
            var model = new CreateMealPlanViewModel
            {
                StartDate = startDate ?? DateTime.Now.Date,
                EndDate = (startDate ?? DateTime.Now.Date).AddDays(6)
            };
            
            return View(model);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Tarifler yüklenirken bir hata oluştu.";
            ViewBag.Recipes = new List<RecipeViewModel>();
            var model = new CreateMealPlanViewModel
            {
                StartDate = startDate ?? DateTime.Now.Date,
                EndDate = (startDate ?? DateTime.Now.Date).AddDays(6)
            };
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMealPlanViewModel model)
    {
        // Items kontrolü
        if (model.Items == null || !model.Items.Any())
        {
            ModelState.AddModelError("Items", "En az bir öğün eklemelisiniz.");
        }
        
        if (!ModelState.IsValid)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
                
                List<RecipeViewModel> recipes = new List<RecipeViewModel>();
                if (response.IsSuccessStatusCode)
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                    recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
                }
                
                ViewBag.Recipes = recipes;
            }
            catch
            {
                ViewBag.Recipes = new List<RecipeViewModel>();
            }
            return View(model);
        }

        var apiClient = CreateApiClient();
        
        var items = (model.Items ?? new List<CreateMealPlanItemViewModel>())
            .Select(i => new
            {
                recipeId = i.RecipeId,
                date = i.Date,
                mealType = i.MealType,
                servings = i.Servings
            }).ToList();
        
        var createDto = new
        {
            name = model.Name,
            startDate = model.StartDate,
            endDate = model.EndDate,
            items = items
        };
        
        var createResponse = await apiClient.PostAsJsonAsync("/api/meal-plans", createDto);
        
        if (createResponse.IsSuccessStatusCode)
        {
            var created = await createResponse.Content.ReadFromJsonAsync<MealPlanViewModel>(JsonOptions);
            return RedirectToAction(nameof(Details), new { id = created?.Id });
        }
        
        // Hata durumunda detayları logla
        var errorContent = await createResponse.Content.ReadAsStringAsync();
        _logger.LogError("Meal plan creation failed. Status: {StatusCode}, Response: {ErrorContent}", createResponse.StatusCode, errorContent);
        
        // Hata mesajını kullanıcıya göster
        try
        {
            var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
            if (errorJson.TryGetProperty("detail", out var detail))
            {
                ModelState.AddModelError("", $"Hata: {detail.GetString()}");
            }
            else
            {
                ModelState.AddModelError("", $"Yemek planı oluşturulurken bir hata oluştu. (Status: {(int)createResponse.StatusCode})");
            }
        }
        catch
        {
            ModelState.AddModelError("", $"Yemek planı oluşturulurken bir hata oluştu. (Status: {(int)createResponse.StatusCode})");
        }
        try
        {
            var client2 = CreateApiClient();
            var recipesResponse = await client2.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
            
            List<RecipeViewModel> recipes2 = new List<RecipeViewModel>();
            if (recipesResponse.IsSuccessStatusCode)
            {
                var pagedResult = await recipesResponse.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                recipes2 = pagedResult?.Items ?? new List<RecipeViewModel>();
            }
            
            ViewBag.Recipes = recipes2;
        }
        catch
        {
            ViewBag.Recipes = new List<RecipeViewModel>();
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var client = CreateApiClient();
            
            // Mevcut meal plan'ı getir
            var mealPlanResponse = await client.GetAsync($"/api/meal-plans/{id}");
            
            if (mealPlanResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            if (!mealPlanResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Yemek planı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            
            var mealPlan = await mealPlanResponse.Content.ReadFromJsonAsync<MealPlanViewModel>(JsonOptions);
            if (mealPlan == null)
            {
                TempData["ErrorMessage"] = "Yemek planı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            
            // Tarifleri getir (dropdown için)
            var recipesResponse = await client.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
            
            List<RecipeViewModel> recipes = new List<RecipeViewModel>();
            if (recipesResponse.IsSuccessStatusCode)
            {
                var pagedResult = await recipesResponse.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
            }
            
            ViewBag.Recipes = recipes;
            
            // MealPlanViewModel'i CreateMealPlanViewModel'e çevir
            var model = new CreateMealPlanViewModel
            {
                Name = mealPlan.Name,
                StartDate = mealPlan.StartDate,
                EndDate = mealPlan.EndDate,
                Items = mealPlan.Items?.Select(item => new CreateMealPlanItemViewModel
                {
                    RecipeId = item.RecipeId,
                    Date = item.Date,
                    MealType = item.MealType.ToString(),
                    Servings = item.Servings
                }).ToList() ?? new List<CreateMealPlanItemViewModel>()
            };
            
            ViewBag.MealPlanId = id;
            return View("Create", model);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Yemek planı yüklenirken bir hata oluştu.";
            _logger.LogError(ex, "Error loading meal plan for edit");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateMealPlanViewModel model)
    {
        // Items kontrolü
        if (model.Items == null || !model.Items.Any())
        {
            ModelState.AddModelError("Items", "En az bir öğün eklemelisiniz.");
        }
        
        if (!ModelState.IsValid)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
                
                List<RecipeViewModel> recipes = new List<RecipeViewModel>();
                if (response.IsSuccessStatusCode)
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                    recipes = pagedResult?.Items ?? new List<RecipeViewModel>();
                }
                
                ViewBag.Recipes = recipes;
                ViewBag.MealPlanId = id;
            }
            catch
            {
                ViewBag.Recipes = new List<RecipeViewModel>();
            }
            return View("Create", model);
        }

        var apiClient = CreateApiClient();
        
        var items = (model.Items ?? new List<CreateMealPlanItemViewModel>())
            .Select(i => new
            {
                recipeId = i.RecipeId,
                date = i.Date,
                mealType = i.MealType,
                servings = i.Servings
            }).ToList();
        
        var updateDto = new
        {
            name = model.Name,
            startDate = model.StartDate,
            endDate = model.EndDate,
            items = items
        };
        
        var updateResponse = await apiClient.PutAsJsonAsync($"/api/meal-plans/{id}", updateDto);
        
        if (updateResponse.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Details), new { id = id });
        }
        
        // Hata durumunda detayları logla
        var errorContent = await updateResponse.Content.ReadAsStringAsync();
        _logger.LogError("Meal plan update failed. Status: {StatusCode}, Response: {ErrorContent}", updateResponse.StatusCode, errorContent);
        
        // Hata mesajını kullanıcıya göster
        try
        {
            var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
            if (errorJson.TryGetProperty("detail", out var detail))
            {
                ModelState.AddModelError("", $"Hata: {detail.GetString()}");
            }
            else
            {
                ModelState.AddModelError("", $"Yemek planı güncellenirken bir hata oluştu. (Status: {(int)updateResponse.StatusCode})");
            }
        }
        catch
        {
            ModelState.AddModelError("", $"Yemek planı güncellenirken bir hata oluştu. (Status: {(int)updateResponse.StatusCode})");
        }
        try
        {
            var client2 = CreateApiClient();
            var recipesResponse = await client2.GetAsync("/api/recipes?pageNumber=1&pageSize=100");
            
            List<RecipeViewModel> recipes2 = new List<RecipeViewModel>();
            if (recipesResponse.IsSuccessStatusCode)
            {
                var pagedResult = await recipesResponse.Content.ReadFromJsonAsync<PagedResult<RecipeViewModel>>(JsonOptions);
                recipes2 = pagedResult?.Items ?? new List<RecipeViewModel>();
            }
            
            ViewBag.Recipes = recipes2;
            ViewBag.MealPlanId = id;
        }
        catch
        {
            ViewBag.Recipes = new List<RecipeViewModel>();
        }
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateApiClient();
        var response = await client.DeleteAsync($"/api/meal-plans/{id}");
        
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }
        
        return NotFound();
    }
}

