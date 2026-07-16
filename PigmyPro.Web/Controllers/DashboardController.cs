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
        private readonly IBankRepository _bankRepo;
        private readonly IBranchRepository _branchRepo;

        public DashboardController(IDashboardRepository repo, IBankRepository bankRepo, IBranchRepository branchRepo)
        {
            _repo = repo;
            _bankRepo = bankRepo;
            _branchRepo = branchRepo;
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

        public async Task<IActionResult> SuperAdmin(int? bankId)
        {
            if (CurrentUserRole != AppRoles.SuperAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var summary = await _repo.GetSuperAdminSummaryAsync(bankId);
            var bankWise = await _repo.GetBankWiseSummaryAsync(bankId);
            var acMasterData = await _repo.GetAcMasterSummaryAsync(bankId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(bankId);

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
                FilterBankID = bankId,
                Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> BankAdmin(int? branchId)
        {
            if (CurrentUserRole != AppRoles.BankAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var bankId = CurrentBankID;
            var summary = await _repo.GetBankAdminSummaryAsync(bankId);
            var branchWise = await _repo.GetBranchWiseSummaryAsync(bankId, branchId);
            var topAgents = await _repo.GetTopAgentCollectionsAsync(bankId, 10, branchId);
            var acMasterData = await _repo.GetAcMasterSummaryAsync(bankId, branchId);
            var agentOverview = await _repo.GetAgentOverviewAsync(bankId, branchId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(bankId, branchId);
            var atRiskAgents = (await _repo.GetAtRiskAgentsAsync(bankId, 5, branchId)).ToList();
            
            var collectionHeld = await _repo.GetCollectionHeldWithAgentsAsync(bankId, branchId);
            var collectionDeposited = await _repo.GetTodayDepositedCollectionAsync(bankId, branchId);

            var bank = await _bankRepo.GetByIdAsync(bankId);
            string bankName = bank?.Name ?? "Bank Overview";
            bool hasBankLogo = !string.IsNullOrEmpty(bank?.LogoFileName);
            string branchName = "";
            if (branchId.HasValue) 
            {
                var branchObj = await _branchRepo.GetByIdAndBankIdAsync(branchId.Value, bankId);
                if (branchObj != null) branchName = branchObj.Name ?? "";
            }

            // Populate branch dropdown
            var branches = await _repo.GetBranchDropdownAsync(bankId);

            var vm = new BankAdminDashboardVM
            {
                BankID = bankId,
                BankName = bankName,
                BranchName = branchName,
                HasBankLogo = hasBankLogo,
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
                CollectionHeld = collectionHeld,
                CollectionDeposited = collectionDeposited,
                LastUpdated = DateTime.Now,
                FilterBranchID = branchId,
                Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> BranchAdmin()
        {
            if (CurrentUserRole != AppRoles.BranchAdmin)
                return RedirectToAction(nameof(Index));

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            var bankId = CurrentBankID;
            var branchId = CurrentBranchID;
            var summary = await _repo.GetBranchAdminSummaryAsync(bankId, branchId);
            var agents = await _repo.GetAgentCollectionsByBranchAsync(bankId, branchId);
            var agentOverview = await _repo.GetAgentOverviewAsync(bankId, branchId);
            var trendData = await _repo.GetDailyCollectionTrendAsync(bankId, branchId);
            var atRiskAgents = (await _repo.GetAtRiskAgentsAsync(bankId, 5, branchId)).ToList();

            var collectionHeld = await _repo.GetCollectionHeldWithAgentsAsync(bankId, branchId);
            var collectionDeposited = await _repo.GetTodayDepositedCollectionAsync(bankId, branchId);

            var bank = await _bankRepo.GetByIdAsync(bankId);
            string bankName = bank?.Name ?? "Bank Overview";
            bool hasBankLogo = !string.IsNullOrEmpty(bank?.LogoFileName);
            
            var branchObj = await _branchRepo.GetByIdAndBankIdAsync(branchId, bankId);
            string branchName = branchObj?.Name ?? "Branch Overview";

            var agentsReadyForUpload = await _repo.GetAgentsReadyForUploadAsync(bankId, branchId);

            var vm = new BranchAdminDashboardVM
            {
                BankID = bankId,
                BankName = bankName,
                BranchName = branchName,
                HasBankLogo = hasBankLogo,
                TotalAgents = summary.TotalAgents,
                TotalAccounts = summary.TotalAccounts,
                TodayCollection = summary.TodayCollection,
                AccountsCollectedToday = summary.AccountsCollectedToday,
                AgentData = agents.ToList(),
                AgentOverview = agentOverview.ToList(),
                DailyTrendData = trendData.ToList(),
                AtRiskAgents = atRiskAgents,
                AgentsReadyForUpload = agentsReadyForUpload.ToList(),
                CollectionHeld = collectionHeld,
                CollectionDeposited = collectionDeposited,
                LastUpdated = DateTime.Now
            };

            return View(vm);
        }
    }
}

