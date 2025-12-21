using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace FrontendMvc.ViewComponents;

public class CategoriesViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CategoriesViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                            ?? new List<CategoryViewModel>();
            return View(categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList());
        }
        catch
        {
            return View(new List<CategoryViewModel>());
        }
    }
}

