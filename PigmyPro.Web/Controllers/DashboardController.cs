using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Dashboard;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardRepository _repo;

        public DashboardController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            return CurrentUserRole switch
            {
                AppRoles.SuperAdmin => RedirectToAction(nameof(SuperAdmin)),
                AppRoles.BankAdmin => RedirectToAction(nameof(BankAdmin)),
                AppRoles.BranchAdmin => RedirectToAction(nameof(BranchAdmin)),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        public async Task<IActionResult> SuperAdmin(DateTime? dateFrom, DateTime? dateTo, int? bankId)
        {
            if (CurrentUserRole != AppRoles.SuperAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var from = dateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = dateTo ?? DateTime.Today;

            var summary = await _repo.GetSuperAdminSummaryAsync(from, to, bankId);
            var bankWise = await _repo.GetBankWiseSummaryAsync(from, to, bankId);
            var acMasterData = await _repo.GetAcMasterSummaryAsync(from, to, bankId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(from, to, bankId);

            // Populate bank dropdown
            var banks = await _repo.GetBankDropdownAsync();

            var vm = new SuperAdminDashboardVM
            {
                TotalBanks = summary.TotalBanks,
                TotalBranches = summary.TotalBranches,
                TotalAgents = summary.TotalAgents,
                TotalAccounts = summary.TotalAccounts,
                TodayCollection = summary.TodayCollection,
                BankWiseData = bankWise.ToList(),
                AcMasterData = acMasterData,
                DailyTrendData = trendData.ToList(),
                LastUpdated = DateTime.Now,
                DateFrom = from,
                DateTo = to,
                FilterBankID = bankId,
                Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> BankAdmin(DateTime? dateFrom, DateTime? dateTo, int? branchId)
        {
            if (CurrentUserRole != AppRoles.BankAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var bankId = CurrentBankID;
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = dateTo ?? DateTime.Today;

            var summary = await _repo.GetBankAdminSummaryAsync(bankId, from, to);
            var branchWise = await _repo.GetBranchWiseSummaryAsync(bankId, from, to, branchId);
            var topAgents = await _repo.GetTopAgentCollectionsAsync(bankId, 10, from, to, branchId);
            var acMasterData = await _repo.GetAcMasterSummaryAsync(from, to, bankId, branchId);
            var agentOverview = await _repo.GetAgentOverviewAsync(bankId, from, to, branchId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(from, to, bankId, branchId);
            var atRiskAgents = (await _repo.GetAtRiskAgentsAsync(bankId, from, to, 5, branchId)).ToList();

            // Populate branch dropdown
            var branches = await _repo.GetBranchDropdownAsync(bankId);

            var vm = new BankAdminDashboardVM
            {
                TotalBranches = summary.TotalBranches,
                TotalAgents = summary.TotalAgents,
                TotalAccounts = summary.TotalAccounts,
                TodayCollection = summary.TodayCollection,
                BranchWiseData = branchWise.ToList(),
                TopAgents = topAgents.ToList(),
                AcMasterData = acMasterData,
                AgentOverview = agentOverview.ToList(),
                DailyTrendData = trendData.ToList(),
                AtRiskAgents = atRiskAgents,
                LastUpdated = DateTime.Now,
                DateFrom = from,
                DateTo = to,
                FilterBranchID = branchId,
                Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> BranchAdmin(DateTime? dateFrom, DateTime? dateTo)
        {
            if (CurrentUserRole != AppRoles.BranchAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var bankId = CurrentBankID;
            var branchId = CurrentBranchID;
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = dateTo ?? DateTime.Today;

            var summary = await _repo.GetBranchAdminSummaryAsync(bankId, branchId, from, to);
            var agents = await _repo.GetAgentCollectionsByBranchAsync(bankId, branchId, from, to);
            var agentOverview = await _repo.GetAgentOverviewAsync(bankId, from, to, branchId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(from, to, bankId, branchId);
            var atRiskAgents = (await _repo.GetAtRiskAgentsAsync(bankId, from, to, 5, branchId)).ToList();

            var vm = new BranchAdminDashboardVM
            {
                TotalAgents = summary.TotalAgents,
                TotalAccounts = summary.TotalAccounts,
                TodayCollection = summary.TodayCollection,
                AccountsCollectedToday = summary.AccountsCollectedToday,
                AgentData = agents.ToList(),
                AgentOverview = agentOverview.ToList(),
                DailyTrendData = trendData.ToList(),
                AtRiskAgents = atRiskAgents,
                LastUpdated = DateTime.Now,
                DateFrom = from,
                DateTo = to
            };

            return View(vm);
        }
    }
}
