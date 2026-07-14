using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

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

        protected char CurrentBankHasCBS
        {
            get
            {
                var claimValue = User.FindFirst("HasCBS")?.Value;
                if (!string.IsNullOrEmpty(claimValue) && claimValue.Length > 0) return claimValue[0];
                var sessionValue = HttpContext.Session.GetString("HasCBS");
                if (!string.IsNullOrEmpty(sessionValue) && sessionValue.Length > 0) return sessionValue[0];
                return 'N';
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.HasCBS = CurrentBankHasCBS;

                // Load Global Bank info for the layout navbar
                int bankId = CurrentBankID;
                if (bankId > 0)
                {
                    string sessionBankName = HttpContext.Session.GetString("GlobalBankName");
                    string sessionHasLogo = HttpContext.Session.GetString("GlobalHasLogo");
                    string sessionBranchName = HttpContext.Session.GetString("GlobalBranchName");

                    if (sessionBankName == null || sessionHasLogo == null)
                    {
                        var bankRepo = context.HttpContext.RequestServices.GetService(typeof(PigmyPro.Data.Interfaces.IBankRepository)) as PigmyPro.Data.Interfaces.IBankRepository;
                        if (bankRepo != null)
                        {
                            var bank = await bankRepo.GetByIdAsync(bankId);
                            if (bank != null)
                            {
                                sessionBankName = bank.Name;
                                sessionHasLogo = (!string.IsNullOrEmpty(bank.LogoFileName)).ToString();
                                HttpContext.Session.SetString("GlobalBankName", sessionBankName);
                                HttpContext.Session.SetString("GlobalHasLogo", sessionHasLogo);
                            }
                        }
                    }

                    int branchId = CurrentBranchID;
                    if (branchId > 0 && sessionBranchName == null)
                    {
                        var branchRepo = context.HttpContext.RequestServices.GetService(typeof(PigmyPro.Data.Interfaces.IBranchRepository)) as PigmyPro.Data.Interfaces.IBranchRepository;
                        if (branchRepo != null)
                        {
                            var branch = await branchRepo.GetByIdAndBankIdAsync(branchId, bankId);
                            if (branch != null)
                            {
                                sessionBranchName = branch.Name;
                                HttpContext.Session.SetString("GlobalBranchName", sessionBranchName);
                            }
                        }
                    }

                    ViewBag.GlobalBankName = sessionBankName ?? "Bank Overview";
                    ViewBag.GlobalHasLogo = (sessionHasLogo == "True");
                    ViewBag.GlobalBankID = bankId;
                    ViewBag.GlobalBranchName = sessionBranchName;
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}