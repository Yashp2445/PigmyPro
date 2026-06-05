using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PigmyPro.Web.Controllers
{
    public class BaseController : Controller
    {
        protected int CurrentBankID
        {
            get
            {
                return HttpContext.Session.GetInt32("BankID") ?? 0;
            }
        }

        protected int CurrentBranchID
        {
            get
            {
                return HttpContext.Session.GetInt32("BranchID") ?? 0;
            }
        }

        protected string CurrentUserRole
        {
            get
            {
                return HttpContext.Session.GetString("UserRole") ?? "";
            }
        }

        protected string CurrentUserRoleFromClaims
        {
            get
            {
                return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            }
        }
    }
}