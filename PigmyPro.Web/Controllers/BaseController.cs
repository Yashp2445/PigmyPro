using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class BaseController : Controller
    {
        protected int CurrentBankID
        {
            get
            {
                var claimValue = User.FindFirst("BankID")?.Value;
                if (int.TryParse(claimValue, out int b)) return b;
                return HttpContext.Session.GetInt32("BankID") ?? 0;
            }
        }

        protected int CurrentBranchID
        {
            get
            {
                var claimValue = User.FindFirst("BranchID")?.Value;
                if (int.TryParse(claimValue, out int b)) return b;
                return HttpContext.Session.GetInt32("BranchID") ?? 0;
            }
        }

        protected string CurrentUserRole
        {
            get
            {
                var claimValue = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(claimValue)) return claimValue;
                return HttpContext.Session.GetString("UserRole") ?? "";
            }
        }
    }
}