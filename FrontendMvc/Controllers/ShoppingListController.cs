using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models.MealPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize]
public class ShoppingListController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ShoppingListController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
            var response = await client.GetAsync("/api/shopping-lists");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Alışveriş listeleri yüklenirken bir hata oluştu.";
                return View(new List<ShoppingListViewModel>());
            }
            
            var shoppingLists = await response.Content.ReadFromJsonAsync<List<ShoppingListViewModel>>(JsonOptions) 
                                ?? new List<ShoppingListViewModel>();
            
            return View(shoppingLists);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Alışveriş listeleri yüklenirken bir hata oluştu.";
            return View(new List<ShoppingListViewModel>());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = CreateApiClient();
            var response = await client.GetAsync($"/api/shopping-lists/{id}");
            
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
                TempData["ErrorMessage"] = "Alışveriş listesi yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
            
            var shoppingList = await response.Content.ReadFromJsonAsync<ShoppingListViewModel>(JsonOptions);
            
            if (shoppingList == null)
            {
                return NotFound();
            }
            
            return View(shoppingList);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            TempData["ErrorMessage"] = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Alışveriş listesi yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromMealPlan(int mealPlanId)
    {
        var client = CreateApiClient();
        var response = await client.PostAsync($"/api/shopping-lists/from-meal-plan/{mealPlanId}", null);
        
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<ShoppingListViewModel>(JsonOptions);
            return RedirectToAction(nameof(Details), new { id = created?.Id });
        }
        
        return BadRequest("Alışveriş listesi oluşturulurken bir hata oluştu.");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleItemChecked(int listId, int itemId, bool isChecked)
    {
        var client = CreateApiClient();
        var response = await client.PutAsJsonAsync($"/api/shopping-lists/{listId}/items/{itemId}/checked?isChecked={isChecked}", new { });
        
        if (response.IsSuccessStatusCode)
        {
            return Ok();
        }
        
        return BadRequest();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateApiClient();
        var response = await client.DeleteAsync($"/api/shopping-lists/{id}");
        
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }
        
        return NotFound();
    }
}

