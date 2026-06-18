using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Controllers
{
    public class LanguageController : Controller
    {
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                }
            );

            // DEBUG dulu
            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return LocalRedirect(returnUrl);
        }
    }
}