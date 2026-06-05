using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Branch;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.Controllers
{
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
            bool isSuperAdmin = User.IsInRole("SuperAdmin");

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

            var vm = data.Select(x => new BranchListVM
            {
                BranchID = x.BranchID,
                BankID = x.BankID,
                Name = x.Name,
                IsActive = x.Active == "Y" ? "Active" : "Inactive",
                EntryDate = x.EntryDate
            }).ToList();

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
            bool isSuperAdmin = User.IsInRole("SuperAdmin");
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
            if (!User.IsInRole("SuperAdmin"))
            {
                vm.BankID = CurrentBankID;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("SuperAdmin"))
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
            bool isSuperAdmin = User.IsInRole("SuperAdmin");

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
            if (!User.IsInRole("SuperAdmin"))
            {
                vm.BankID = CurrentBankID;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("SuperAdmin"))
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
            int targetBankId = User.IsInRole("SuperAdmin") ? bankId : CurrentBankID;

            await _repo.DeleteAsync(id, targetBankId);
            TempData["Success"] = "Branch deleted.";
            return RedirectToAction(nameof(Index));
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