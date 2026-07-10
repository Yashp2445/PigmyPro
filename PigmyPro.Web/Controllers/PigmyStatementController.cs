using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Report;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    public class PigmyStatementController : BaseController
    {
        private readonly IPigmyStatementRepository _repo;

        public PigmyStatementController(IPigmyStatementRepository repo)
        {
            _repo = repo;
        }

        // ============================================================
        // PIGMY ACCOUNT STATEMENT REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/PigmyStatement/Index'>Reports</a></li><li class='breadcrumb-item active'>Pigmy Account Statement</li>";

            var vm = new PigmyStatementReportVM
            {
                UserRole = CurrentUserRole,
                HasSearched = false
            };

            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PigmyStatementReportVM vm)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/PigmyStatement/Index'>Reports</a></li><li class='breadcrumb-item active'>Pigmy Account Statement</li>";

            vm.UserRole = CurrentUserRole;

            // Enforce role scoping — never trust posted values
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == AppRoles.BranchAdmin ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetPigmyStatementAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            vm.Rows = rows.ToList();
            vm.HasSearched = true;

            await PopulateDropdowns(vm);
            return View(vm);
        }

        // ============================================================
        // PRINT VIEW
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Print(PigmyStatementReportVM vm)
        {
            vm.UserRole = CurrentUserRole;

            // Enforce role scoping
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == AppRoles.BranchAdmin ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetPigmyStatementAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            vm.Rows = rows.ToList();

            // Resolve bank name for the print header
            int headerBankId = bankId ?? CurrentBankID;
            if (headerBankId > 0)
                vm.BankName = await _repo.GetBankNameAsync(headerBankId);
            else
                vm.BankName = "";

            return View(vm);
        }

        // ============================================================
        // AJAX ENDPOINTS
        // ============================================================

        // Populate branches when bank changes (SuperAdmin)
        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _repo.GetBranchDropdownAsync(bankId);
            return Json(branches);
        }

        // Populate agents when branch changes
        [HttpGet]
        public async Task<IActionResult> GetAgents(int bankId, int branchId)
        {
            int safeBankId = CurrentUserRole == AppRoles.SuperAdmin ? bankId : CurrentBankID;
            var agents = await _repo.GetAgentDropdownAsync(safeBankId, branchId);
            return Json(agents);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private async Task PopulateDropdowns(PigmyStatementReportVM vm)
        {
            if (CurrentUserRole == AppRoles.SuperAdmin)
            {
                var banks = await _repo.GetBankDropdownAsync();
                vm.Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList();
            }

            if (CurrentUserRole != AppRoles.BranchAdmin)
            {
                int bId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
                if (bId > 0)
                {
                    var branches = await _repo.GetBranchDropdownAsync(bId);
                    vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
                }
            }

            int abId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
            int abrId = (CurrentUserRole == AppRoles.BranchAdmin) ? CurrentBranchID : (vm.FilterBranchID ?? 0);
            if (abId > 0 && abrId > 0)
            {
                var agents = await _repo.GetAgentDropdownAsync(abId, abrId);
                vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();
            }
        }
    }
}
