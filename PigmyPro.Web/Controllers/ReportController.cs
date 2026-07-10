using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Report;
using PigmyPro.Domain;
using OfficeOpenXml;
using System.Text;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly IReportRepository _repo;
        private readonly PigmyPro.Web.Services.IReportExportService _exportService;

        public ReportController(IReportRepository repo, PigmyPro.Web.Services.IReportExportService exportService)
        {
            _repo = repo;
            _exportService = exportService;
        }

        // ============================================================
        // DAILY COLLECTION REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DailyCollection()
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Daily Collection</li>";

            var vm = new DailyCollectionReportVM
            {
                UserRole = CurrentUserRole,
                HasSearched = false
            };

            await PopulateDailyCollectionDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollection(DailyCollectionReportVM vm)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Daily Collection</li>";

            vm.UserRole = CurrentUserRole;

            // Enforce role scoping — never trust posted values
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            vm.Rows = rows.ToList();
            vm.HasSearched = true;

            await PopulateDailyCollectionDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollectionExcel(DailyCollectionReportVM vm)
        {
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            var bytes = _exportService.GenerateDailyCollectionExcel(rows, CurrentUserRole, vm.DateFrom, vm.DateTo);
            var fileName = $"DailyCollection_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollectionText(DailyCollectionReportVM vm)
        {
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            string bankName = "ALL BANKS";
            int headerBankId = bankId ?? CurrentBankID;
            if (headerBankId > 0)
                bankName = await _repo.GetBankNameAsync(headerBankId);

            var bytes = _exportService.GenerateDailyCollectionText(rows, bankName, vm.DateFrom, vm.DateTo);
            var fileName = $"DailyCollection_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.txt";
            return File(bytes, "text/plain", fileName);
        }

        // ============================================================
        // AGENT SUMMARY REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AgentSummary()
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Agent Summary</li>";
            var vm = new AgentSummaryReportVM { UserRole = CurrentUserRole, HasSearched = false };
            await PopulateAgentSummaryDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummary(AgentSummaryReportVM vm)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Agent Summary</li>";
            vm.UserRole = CurrentUserRole;
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetAgentSummaryAsync(vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode);
            vm.Rows = rows.ToList();
            vm.HasSearched = true;

            await PopulateAgentSummaryDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummaryExcel(AgentSummaryReportVM vm)
        {
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetAgentSummaryAsync(
                vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode);

            var bytes = _exportService.GenerateAgentSummaryExcel(rows, vm.DateFrom, vm.DateTo);
            var fileName = $"AgentSummary_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummaryText(AgentSummaryReportVM vm)
        {
            int? bankId = CurrentUserRole == AppRoles.SuperAdmin ? vm.FilterBankID : CurrentBankID;
            int? branchId = (CurrentUserRole == AppRoles.SuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? vm.FilterBranchID : CurrentBranchID;

            var rows = await _repo.GetAgentSummaryAsync(
                vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode);

            string bankName = "ALL BANKS";
            int headerBankId = bankId ?? CurrentBankID;
            if (headerBankId > 0)
                bankName = await _repo.GetBankNameAsync(headerBankId);

            var bytes = _exportService.GenerateAgentSummaryText(rows, bankName, vm.DateFrom, vm.DateTo);
            var fileName = $"AgentSummary_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.txt";
            return File(bytes, "text/plain", fileName);
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private async Task PopulateDailyCollectionDropdowns(DailyCollectionReportVM vm)
        {
            if (CurrentUserRole == AppRoles.SuperAdmin) {
                var banks = await _repo.GetBankDropdownAsync();
                vm.Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList();
            }
            if (CurrentUserRole != AppRoles.BranchAdmin) {
                int bId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
                if (bId > 0) {
                    var branches = await _repo.GetBranchDropdownAsync(bId);
                    vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
                }
            }
            int abId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
            int abrId = (CurrentUserRole == AppRoles.BranchAdmin) ? CurrentBranchID : (vm.FilterBranchID ?? 0);
            if (abId > 0 && abrId > 0) {
                var agents = await _repo.GetAgentDropdownAsync(abId, abrId);
                vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();
            }
        }

        private async Task PopulateAgentSummaryDropdowns(AgentSummaryReportVM vm)
        {
            if (CurrentUserRole == AppRoles.SuperAdmin) {
                var banks = await _repo.GetBankDropdownAsync();
                vm.Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList();
            }
            if (CurrentUserRole != AppRoles.BranchAdmin) {
                int bId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
                if (bId > 0) {
                    var branches = await _repo.GetBranchDropdownAsync(bId);
                    vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
                }
            }
            int abId = (CurrentUserRole == AppRoles.SuperAdmin) ? (vm.FilterBankID ?? 0) : CurrentBankID;
            int abrId = (CurrentUserRole == AppRoles.BranchAdmin) ? CurrentBranchID : (vm.FilterBranchID ?? 0);
            if (abId > 0 && abrId > 0) {
                var agents = await _repo.GetAgentDropdownAsync(abId, abrId);
                vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();
            }
        }



        // AJAX endpoint: populate branches when bank changes (SuperAdmin)
        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _repo.GetBranchDropdownAsync(bankId);
            return Json(branches);
        }

        // AJAX endpoint: populate agents when branch changes
        [HttpGet]
        public async Task<IActionResult> GetAgents(int bankId, int branchId)
        {
            int safeBankId = CurrentUserRole == AppRoles.SuperAdmin ? bankId : CurrentBankID;
            var agents = await _repo.GetAgentDropdownAsync(safeBankId, branchId);
            return Json(agents);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        // ── Reconciliation Report ───────────────────────────────────

        public async Task<IActionResult> Reconciliation(DateTime? dateFrom, DateTime? dateTo, int? branchId)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Dashboard/Index'>Reports</a></li><li class='breadcrumb-item active'>Reconciliation</li>";
            ViewData["Title"] = "Reconciliation Report";

            var bankId = CurrentBankID;
            var from = dateFrom ?? DateTime.Today.AddDays(-7);
            var to = dateTo ?? DateTime.Today;

            if (CurrentUserRole == AppRoles.BranchAdmin)
            {
                branchId = CurrentBranchID;
            }

            var branches = await _repo.GetBranchDropdownAsync(bankId);
            var results = await _repo.GetReconciliationReportAsync(from, to, bankId, branchId);

            var vm = new ReconciliationVM
            {
                DateFrom = from,
                DateTo = to,
                FilterBranchID = branchId,
                Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList(),
                Results = results
            };

            return View(vm);
        }
    }
}
