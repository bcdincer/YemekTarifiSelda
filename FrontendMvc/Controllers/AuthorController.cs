using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize]
public class AuthorController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthorController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> BecomeAuthor()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Kullanıcı zaten yazar mı kontrol et
        var client = CreateApiClient();
        AuthorViewModel? existingAuthor = null;
        try
        {
            var response = await client.GetAsync($"/api/authors/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                existingAuthor = await response.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
            }
        }
        catch (HttpRequestException)
        {
            // 404 veya başka bir hata - yazar değil, devam et
            existingAuthor = null;
        }
        
        if (existingAuthor != null)
        {
            // Zaten yazar, tariflerim sayfasına yönlendir
            return RedirectToAction("MyRecipes", new { authorId = existingAuthor.Id });
        }

        return View(new BecomeAuthorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BecomeAuthor(BecomeAuthorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var client = CreateApiClient();
        var createDto = new
        {
            UserId = userId,
            DisplayName = model.DisplayName,
            Bio = model.Bio,
            ProfileImageUrl = model.ProfileImageUrl
        };

        var response = await client.PostAsJsonAsync("/api/authors/become-author", createDto);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Yazar olurken bir hata oluştu: {errorContent}");
            return View(model);
        }

        var author = await response.Content.ReadFromJsonAsync<AuthorViewModel>(JsonOptions);
        if (author != null)
        {
            TempData["SuccessMessage"] = "Tebrikler! Artık bir yazarsınız. Tariflerinizi paylaşmaya başlayabilirsiniz.";
            return RedirectToAction("MyRecipes", new { authorId = author.Id });
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> MyRecipes(int? authorId, int pageNumber = 1, int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var client = CreateApiClient();
        
        // Eğer authorId verilmemişse, kullanıcının author bilgisini al
        if (!authorId.HasValue)
        {
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
            
            if (author == null)
            {
                TempData["InfoMessage"] = "Henüz yazar değilsiniz. Yazar olmak için lütfen kayıt olun.";
                return RedirectToAction("BecomeAuthor");
            }
            authorId = author.Id;
        }
        else
        {
            // Başka bir yazarın tariflerine bakılıyor mu kontrol et
            var author = await client.GetFromJsonAsync<AuthorViewModel>($"/api/authors/{authorId.Value}", JsonOptions);
            if (author == null)
            {
                return NotFound();
            }
            // Eğer kendi tarifleri değilse, sadece görüntüleme modunda
            ViewBag.IsOwnRecipes = author.UserId == userId;
        }

        var pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>(
            $"/api/authors/{authorId.Value}/recipes?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);

        ViewBag.AuthorId = authorId.Value;
        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = pagedResult?.TotalCount ?? 0;

        return View(pagedResult?.Items ?? new List<RecipeViewModel>());
    }

    [HttpGet]
    [AllowAnonymous]
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

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> AuthorRecipes(int id, int pageNumber = 1, int pageSize = 10)
    {
        var client = CreateApiClient();
        var author = await client.GetFromJsonAsync<AuthorViewModel>($"/api/authors/{id}", JsonOptions);
        
        if (author == null)
        {
            return NotFound();
        }

        var pagedResult = await client.GetFromJsonAsync<PagedResult<RecipeViewModel>>(
            $"/api/authors/{id}/recipes?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);

        ViewBag.Author = author;
        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = pagedResult?.TotalCount ?? 0;

        return View(pagedResult?.Items ?? new List<RecipeViewModel>());
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        
        var token = HttpContext.Request.Cookies["authToken"];
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
                // Session yapılandırılmamış
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

