using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceTracker.Controllers
{
    public class BaseController : Controller
    {
        protected int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return userId != null && int.TryParse(userId, out var id) ? id : null;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (GetUserId() == null)
            {
                context.Result = RedirectToAction("Login", "Account");
            }

            base.OnActionExecuting(context);
        }
    }
}