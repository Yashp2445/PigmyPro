using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Audit;
using System;
using System.Threading.Tasks;
using PigmyPro.Domain;
using PigmyPro.Web.Controllers;

namespace PigmyPro.Web.Controllers
{
    public class AuditController : BaseController
    {
        private readonly IAuditRepository _auditRepo;

        public AuditController(IAuditRepository auditRepo)
        {
            _auditRepo = auditRepo;
        }

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Audit Log</li>";
            ViewData["Title"] = "Audit Log";

            var from = dateFrom ?? DateTime.Today.AddDays(-7);
            var to = dateTo ?? DateTime.Today;

            var bankId = CurrentBankID;
            decimal? branchId = null;

            if (CurrentUserRole == AppRoles.BranchAdmin)
            {
                branchId = CurrentBranchID;
            }

            var logs = await _auditRepo.GetRecentActivityAsync(bankId, branchId, from, to);

            var vm = new AuditIndexVM
            {
                DateFrom = from,
                DateTo = to,
                Logs = logs
            };

            return View(vm);
        }
    }
}
