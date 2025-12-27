using FrontendMvc.Data;
using FrontendMvc.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace FrontendMvc.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        // returnUrl'i query string'den veya form'dan al
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = Request.Query["returnUrl"].ToString();
        }
        
        ViewData["ReturnUrl"] = returnUrl;
        _logger.LogInformation("Login attempt with returnUrl: {ReturnUrl}", returnUrl);
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in.");
            
            // Kullanıcı bilgilerini al
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Backend API'den JWT token al
                try
                {
                    var backendApiUrl = _configuration["BackendApi:BaseUrl"] ?? "https://localhost:7016";
                    var client = _httpClientFactory.CreateClient();
                    client.BaseAddress = new Uri(backendApiUrl);
                    
                    var loginRequest = new
                    {
                        userId = user.Id,
                        userName = user.UserName ?? user.Email,
                        email = user.Email
                    };
                    
                    var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        var loginResponse = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                        if (loginResponse.TryGetProperty("token", out var tokenElement))
                        {
                            var token = tokenElement.GetString();
                            if (!string.IsNullOrEmpty(token))
                            {
                                // Önce eski token cookie'sini temizle
                                Response.Cookies.Delete("authToken");
                                
                                // Yeni token'ı cookie'ye kaydet (HttpOnly = false çünkü JavaScript'ten okunması gerekiyor)
                                var cookieOptions = new CookieOptions
                                {
                                    HttpOnly = false, // JavaScript'ten okunabilir olmalı
                                    Secure = Request.IsHttps,
                                    SameSite = SameSiteMode.Lax,
                                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                                };
                                Response.Cookies.Append("authToken", token, cookieOptions);
                                
                                _logger.LogInformation("Auth token cookie set for user: {UserId}", user.Id);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Backend API'den token alınamadı. Status: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backend API'den token alınamadı, devam ediliyor.");
                    // Token alınamazsa da devam et, kullanıcı giriş yapmış
                }
            }
            
            // Hoşgeldiniz mesajı
            if (user != null)
            {
                var userName = user.UserName ?? user.Email?.Split('@')[0] ?? "Kullanıcı";
                TempData["SuccessMessage"] = $"Hoş geldiniz, {userName}!";
            }
            
            // returnUrl'i decode et (URL encoding'den kaynaklanan sorunları önlemek için)
            string? decodedReturnUrl = null;
            if (!string.IsNullOrEmpty(returnUrl))
            {
                try
                {
                    decodedReturnUrl = Uri.UnescapeDataString(returnUrl);
                    _logger.LogInformation("Decoded returnUrl: {DecodedReturnUrl}", decodedReturnUrl);
                }
                catch
                {
                    decodedReturnUrl = returnUrl;
                }
            }
            
            // returnUrl varsa ona yönlendir, yoksa ana sayfaya yönlendir
            _logger.LogInformation("Redirecting to: {ReturnUrl}", decodedReturnUrl ?? "Home/Index");
            return RedirectToLocal(decodedReturnUrl);
        }
        
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            ModelState.AddModelError(string.Empty, "Hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.");
            return View(model);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Yeni kullanıcıları User rolüne ekle
            await _userManager.AddToRoleAsync(user, "User");
            
            _logger.LogInformation("User created a new account with password.");
            
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            // Backend API'den JWT token al
            try
            {
                var backendApiUrl = _configuration["BackendApi:BaseUrl"] ?? "https://localhost:7016";
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(backendApiUrl);
                
                var loginRequest = new
                {
                    userId = user.Id,
                    userName = user.UserName ?? user.Email,
                    email = user.Email
                };
                
                var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (loginResponse.TryGetProperty("token", out var tokenElement))
                    {
                        var token = tokenElement.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            // Önce eski token cookie'sini temizle
                            Response.Cookies.Delete("authToken");
                            
                            // Yeni token'ı cookie'ye kaydet (HttpOnly = false çünkü JavaScript'ten okunması gerekiyor)
                            var cookieOptions = new CookieOptions
                            {
                                HttpOnly = false, // JavaScript'ten okunabilir olmalı
                                Secure = Request.IsHttps,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.UtcNow.AddDays(30)
                            };
                            Response.Cookies.Append("authToken", token, cookieOptions);
                            
                            _logger.LogInformation("Auth token cookie set for new user: {UserId}", user.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend API'den token alınamadı, devam ediliyor.");
            }
            
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        // Token cookie'sini temizle
        Response.Cookies.Delete("authToken");
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        // Token cookie'sini temizle
        Response.Cookies.Delete("authToken");
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult Profile()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }
}

