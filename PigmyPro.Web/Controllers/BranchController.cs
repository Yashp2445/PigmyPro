using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Branch;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.BankAdmin)]
    public class BranchController : BaseController
    {
        private readonly IBranchRepository _repo;
        private readonly IBankRepository _bankRepo;

        public BranchController(IBranchRepository repo, IBankRepository bankRepo)
        {
            _repo = repo;
            _bankRepo = bankRepo;
        }

        public async Task<IActionResult> Index(int? bankId)
        {
            bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

            IEnumerable<Branch> data;

            if (isSuperAdmin)
            {
                data = bankId.HasValue && bankId > 0
                    ? await _repo.GetAllByBankIdAsync(bankId.Value)
                    : Enumerable.Empty<Branch>(); 
            }
            else
            {
                data = await _repo.GetAllByBankIdAsync(CurrentBankID);
            }

            var vm = new List<BranchListVM>();
            foreach (var x in data)
            {
                var deps = await _repo.GetDependentRecordCountsAsync(x.BankID, x.BranchID);
                var hasDependents = deps.AgentCount > 0 || deps.UserCount > 0 || deps.AccountCount > 0 || deps.TransactionCount > 0;
                
                var msgParts = new List<string>();
                if (deps.AgentCount > 0) msgParts.Add($"{deps.AgentCount} agents");
                if (deps.UserCount > 0) msgParts.Add($"{deps.UserCount} users");
                if (deps.AccountCount > 0) msgParts.Add($"{deps.AccountCount} accounts");
                if (deps.TransactionCount > 0) msgParts.Add($"{deps.TransactionCount} transactions");

                string msg = hasDependents 
                    ? $"This branch cannot be deleted because it still has: {string.Join(", ", msgParts)}." 
                    : "";

                vm.Add(new BranchListVM
                {
                    BranchID = x.BranchID,
                    BankID = x.BankID,
                    Name = x.Name,
                    IsActive = x.Active == "Y" ? "Active" : "Inactive",
                    EntryDate = x.EntryDate,
                    CanDelete = !hasDependents,
                    DependencyMessage = msg
                });
            }

            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.SelectedBankID = bankId ?? 0;

            if (isSuperAdmin)
            {
                var banks = await _bankRepo.GetAllAsync();

                ViewBag.BankList = banks.Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name,
                    Selected = b.BankID == bankId
                }).ToList();
            }
            else
            {
                var banks = await _bankRepo.GetAllAsync();
                ViewBag.BankName = banks.FirstOrDefault(b => b.BankID == CurrentBankID)?.Name;
            }

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);
            var vm = new BranchCreateEditVM
            {
                Active = "Y",
                IsSuperAdmin = isSuperAdmin
            };

            if (isSuperAdmin)
            {
                var banks = await _bankRepo.GetAllAsync();
                vm.BankList = banks.Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name
                }).ToList();
            }
            else
            {
                vm.BankID = CurrentBankID;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchCreateEditVM vm)
        {
            if (!User.IsInRole(AppRoles.SuperAdmin))
            {
                vm.BankID = CurrentBankID;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole(AppRoles.SuperAdmin))
                {
                    var banks = await _bankRepo.GetAllAsync();
                    vm.BankList = banks.Select(b => new SelectListItem
                    {
                        Value = b.BankID.ToString(),
                        Text = b.Name
                    }).ToList();
                }
                return View(vm);
            }

            var entity = new Branch
            {
                BankID = vm.BankID,
                Name = vm.Name,
                Active = vm.Active
            };

            await _repo.AddAsync(entity);
            TempData["Success"] = "Branch created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, int bankId)
        {
            bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

            int targetBankId = isSuperAdmin ? bankId : CurrentBankID;

            var branch = await _repo.GetByIdAndBankIdAsync(id, targetBankId);
            if (branch == null) return NotFound();

            var vm = new BranchCreateEditVM
            {
                BranchID = branch.BranchID,
                BankID = branch.BankID,
                Name = branch.Name,
                Active = branch.Active ?? "Y",
                IsSuperAdmin = isSuperAdmin
            };

            if (isSuperAdmin)
            {
                var banks = await _bankRepo.GetAllAsync();
                vm.BankList = banks.Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name,
                    Selected = b.BankID == branch.BankID
                }).ToList();
            }

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BranchCreateEditVM vm)
        {
            if (!User.IsInRole(AppRoles.SuperAdmin))
            {
                vm.BankID = CurrentBankID;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole(AppRoles.SuperAdmin))
                {
                    var banks = await _bankRepo.GetAllAsync();
                    vm.BankList = banks.Select(b => new SelectListItem
                    {
                        Value = b.BankID.ToString(),
                        Text = b.Name
                    }).ToList();
                }
                return View("Create", vm);
            }

            var entity = new Branch
            {
                BranchID = vm.BranchID,
                BankID = vm.BankID,
                Name = vm.Name,
                Active = vm.Active
            };

            await _repo.UpdateAsync(entity);
            TempData["Success"] = "Branch updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int bankId)
        {
            int targetBankId = User.IsInRole(AppRoles.SuperAdmin) ? bankId : CurrentBankID;

            var deps = await _repo.GetDependentRecordCountsAsync(targetBankId, id);
            if (deps.AgentCount > 0 || deps.UserCount > 0 || deps.AccountCount > 0 || deps.TransactionCount > 0)
            {
                TempData["Error"] = "Cannot delete this branch because it has dependent agents, users, accounts, or transactions.";
                return RedirectToAction(nameof(Index));
            }

            await _repo.DeleteAsync(id, targetBankId);
            TempData["Success"] = "Branch deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckDependencies(int id, int bankId)
        {
            int targetBankId = User.IsInRole(AppRoles.SuperAdmin) ? bankId : CurrentBankID;
            
            var deps = await _repo.GetDependentRecordCountsAsync(targetBankId, id);
            bool hasDependents = deps.AgentCount > 0 || deps.UserCount > 0 || deps.AccountCount > 0 || deps.TransactionCount > 0;
            
            if (hasDependents)
            {
                var msgParts = new List<string>();
                if (deps.AgentCount > 0) msgParts.Add($"{deps.AgentCount} agents");
                if (deps.UserCount > 0) msgParts.Add($"{deps.UserCount} users");
                if (deps.AccountCount > 0) msgParts.Add($"{deps.AccountCount} accounts");
                if (deps.TransactionCount > 0) msgParts.Add($"{deps.TransactionCount} transactions");

                string msg = $"This branch cannot be deleted because it still has: {string.Join(", ", msgParts)}.";
                
                return Json(new { 
                    canDelete = false, 
                    agentCount = deps.AgentCount,
                    userCount = deps.UserCount,
                    accountCount = deps.AccountCount,
                    transactionCount = deps.TransactionCount,
                    message = msg 
                });
            }

            return Json(new { canDelete = true, message = "" });
        }

        public async Task<JsonResult> GetByBankId(int bankId)
        {
            var branches = await _repo.GetAllByBankIdAsync(bankId);

            return Json(branches.Select(b => new
            {
                value = b.BranchID,
                text = b.Name
            }));
        }
    }
}