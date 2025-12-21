using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace FrontendMvc.Controllers;

public class LanguageController : Controller
{
    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly ILogger<LanguageController> _logger;

    public LanguageController(IStringLocalizerFactory localizerFactory, ILogger<LanguageController> logger)
    {
        _localizerFactory = localizerFactory;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        // Validate culture - use same supported cultures as Program.cs
        var supportedCultures = new[] { "tr", "en" };
        if (string.IsNullOrWhiteSpace(culture) || !supportedCultures.Contains(culture))
        {
            culture = "tr"; // Default to Turkish
        }

        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        var cookieName = CookieRequestCultureProvider.DefaultCookieName;
        
        _logger.LogInformation("Setting language cookie: {CookieName} = {CookieValue}, Culture: {Culture}", 
            cookieName, cookieValue, culture);
        
        Response.Cookies.Append(
            cookieName,
            cookieValue,
            new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            }
        );

        // Log cookie set
        _logger.LogInformation("Cookie set successfully. Redirecting to: {ReturnUrl}", returnUrl ?? "/");

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet]
    public IActionResult Debug()
    {
        var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
        var cookie = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
        
        var debugInfo = new
        {
            CurrentCulture = System.Globalization.CultureInfo.CurrentCulture.Name,
            CurrentUICulture = System.Globalization.CultureInfo.CurrentUICulture.Name,
            RequestCulture = requestCulture?.RequestCulture?.Culture?.Name,
            RequestUICulture = requestCulture?.RequestCulture?.UICulture?.Name,
            CookieValue = cookie,
            CookieName = CookieRequestCultureProvider.DefaultCookieName,
            AllCookies = Request.Cookies.Keys.ToList(),
            LocalizerTest = new
            {
                TrLocalizer = "Test TR",
                EnLocalizer = "Test EN", 
                CurrentLocalizer = "Test Current"
            }
        };

        return Json(debugInfo);
    }
}

