using System.Net.Http.Json;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriesController(IHttpClientFactory httpClientFactory) : Controller
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    // JSON endpoint for categories (used by header dropdown - no auth required)
    [HttpGet]
    [Route("/Categories/GetAll")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllJson()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories");
            return Json(categories ?? new List<CategoryViewModel>());
        }
        catch
        {
            return Json(new List<CategoryViewModel>());
        }
    }

    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        return View(categories);
    }

    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        var response = await client.PostAsJsonAsync("/api/categories", model);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Kategori kaydedilirken bir hata oluştu: {errorContent}");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        var category = await client.GetFromJsonAsync<CategoryViewModel>($"/api/categories/{id}");

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        var response = await client.PutAsJsonAsync($"/api/categories/{id}", model);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Kategori güncellenirken bir hata oluştu: {errorContent}");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        var response = await client.DeleteAsync($"/api/categories/{id}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Kategori silinirken bir hata oluştu.";
        }
        else
        {
            TempData["Success"] = "Kategori başarıyla silindi.";
        }

        return RedirectToAction(nameof(Index));
    }
}

