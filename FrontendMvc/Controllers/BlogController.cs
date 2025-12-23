using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models.Blog;
using FrontendMvc.Models.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PagedResult = FrontendMvc.Models.Recipes.PagedResult<FrontendMvc.Models.Blog.BlogPostViewModel>;

namespace FrontendMvc.Controllers;

public class BlogController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BlogController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 12, string? sortBy = null)
    {
        var client = CreateApiClient();
        
        // Sıralama: "newest" (varsayılan), "oldest", "popular" (görüntülenme), "featured"
        PagedResult? pagedResult = null;
        
        if (sortBy == "featured")
        {
            var featured = await client.GetFromJsonAsync<List<BlogPostViewModel>>("/api/blog/featured?count=100", JsonOptions);
            if (featured != null && featured.Any())
            {
                var totalCount = featured.Count;
                var items = featured
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                pagedResult = new PagedResult { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
            }
        }
        else
        {
            pagedResult = await client.GetFromJsonAsync<PagedResult>(
                $"/api/blog?pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);
        }

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = pagedResult?.TotalCount ?? 0;
        ViewBag.SortBy = sortBy ?? "newest";

        return View(pagedResult?.Items ?? new List<BlogPostViewModel>());
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var client = CreateApiClient();
        var blogPost = await client.GetFromJsonAsync<BlogPostViewModel>($"/api/blog/{id}", JsonOptions);

        if (blogPost == null)
        {
            return NotFound();
        }

        // Görüntülenme sayısını artır
        await client.PostAsync($"/api/blog/{id}/view", null);

        return View(blogPost);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Search(string q, int pageNumber = 1, int pageSize = 12)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return RedirectToAction("Index");
        }

        var client = CreateApiClient();
        var pagedResult = await client.GetFromJsonAsync<PagedResult>(
            $"/api/blog/search?q={Uri.EscapeDataString(q)}&pageNumber={pageNumber}&pageSize={pageSize}", JsonOptions);

        ViewBag.SearchTerm = q;
        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = pagedResult?.TotalCount ?? 0;

        return View(pagedResult?.Items ?? new List<BlogPostViewModel>());
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "Blog yazısı eklemek için önce giriş yapmanız gerekiyor.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Blog") });
        }

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
            author = null;
        }

        if (author == null || !author.IsActive)
        {
            TempData["InfoMessage"] = "Blog yazısı eklemek için önce yazar olmanız gerekiyor.";
            return RedirectToAction("BecomeAuthor", "Author");
        }

        // Kategorileri yükle
        var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                         ?? new List<CategoryViewModel>();
        ViewBag.Categories = categories;
        ViewBag.AuthorId = author.Id;

        return View(new CreateBlogPostViewModel { IsPublished = true });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBlogPostViewModel model, IFormFile? imageFile)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

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
            author = null;
        }

        if (author == null || !author.IsActive)
        {
            TempData["ErrorMessage"] = "Blog yazısı eklemek için önce yazar olmanız gerekiyor.";
            return RedirectToAction("BecomeAuthor", "Author");
        }

        if (!ModelState.IsValid)
        {
            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                             ?? new List<CategoryViewModel>();
            ViewBag.Categories = categories;
            ViewBag.AuthorId = author.Id;
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
                var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                                 ?? new List<CategoryViewModel>();
                ViewBag.Categories = categories;
                ViewBag.AuthorId = author.Id;
                return View(model);
            }

            if (imageFile.Length > 5 * 1024 * 1024) // 5MB
            {
                ModelState.AddModelError("ImageFile", "Görsel boyutu 5MB'dan küçük olmalıdır.");
                var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                                 ?? new List<CategoryViewModel>();
                ViewBag.Categories = categories;
                ViewBag.AuthorId = author.Id;
                return View(model);
            }

            // Klasörü oluştur
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "blog");
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
            model.ImageUrl = $"/images/blog/{fileName}";
        }

        var createDto = new
        {
            Title = model.Title ?? string.Empty,
            Content = model.Content ?? string.Empty,
            Excerpt = model.Excerpt ?? string.Empty,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl,
            ImageBanner = string.IsNullOrWhiteSpace(model.ImageBanner) ? null : model.ImageBanner,
            IsPublished = model.IsPublished,
            IsFeatured = model.IsFeatured,
            AuthorId = author.Id,
            CategoryId = model.CategoryId
        };

        var apiResponse = await client.PostAsJsonAsync("/api/blog", createDto);

        if (!apiResponse.IsSuccessStatusCode)
        {
            var errorContent = await apiResponse.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Blog yazısı kaydedilirken bir hata oluştu: {errorContent}");
            var categories = await client.GetFromJsonAsync<List<CategoryViewModel>>("/api/categories") 
                             ?? new List<CategoryViewModel>();
            ViewBag.Categories = categories;
            ViewBag.AuthorId = author.Id;
            return View(model);
        }

        var createdBlogPost = await apiResponse.Content.ReadFromJsonAsync<BlogPostViewModel>(JsonOptions);
        if (createdBlogPost != null)
        {
            TempData["SuccessMessage"] = "Blog yazınız başarıyla kaydedildi!";
            return RedirectToAction("Details", new { id = createdBlogPost.Id });
        }

        return RedirectToAction("Index");
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

