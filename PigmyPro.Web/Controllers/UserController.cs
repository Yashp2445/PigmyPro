using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.User;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.BankAdmin)]
    public class UserController : BaseController
    {
        private readonly IUserRepository _userRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IBankRepository _bankRepo;

        public UserController(IUserRepository userRepo, IBranchRepository branchRepo, IBankRepository bankRepo)
        {
            _userRepo = userRepo;
            _branchRepo = branchRepo;
            _bankRepo = bankRepo;
        }

        public async Task<IActionResult> Index(int? bankId)
        {
            bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

            IEnumerable<User> users;

            if (isSuperAdmin)
            {
                users = bankId.HasValue && bankId.Value > 0
                    ? await _userRepo.GetAllByBankIdAsync(bankId.Value)
                    : Enumerable.Empty<User>(); 
            }
            else
            {
                users = await _userRepo.GetAllByBankIdAsync(CurrentBankID);
            }

            IEnumerable<PigmyPro.Domain.Entities.Branch> allBranches;

            if (isSuperAdmin && bankId.HasValue && bankId.Value > 0)
                allBranches = await _branchRepo.GetAllByBankIdAsync(bankId.Value);
            else if (!isSuperAdmin)
                allBranches = await _branchRepo.GetAllByBankIdAsync(CurrentBankID);
            else
                allBranches = await _branchRepo.GetAllAsync();

            var vm = users.Select(u => new UserListVM
            {
                UserID = u.UserID,
                Username = u.Username,
                Name = u.Name,
                Role = u.Role,
                BranchName = allBranches.FirstOrDefault(b => b.BranchID == u.BranchID)?.Name,
                IsActive = u.IsActive
            }).ToList();

            ViewBag.IsSuperAdmin = isSuperAdmin;

            if (isSuperAdmin)
            {
                var banks = await _bankRepo.GetAllAsync();

                ViewBag.BankList = banks.Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name
                }).ToList();
            }
            else
            {
                var banks = await _bankRepo.GetAllAsync();
                var bank = banks.FirstOrDefault(b => b.BankID == CurrentBankID);
                ViewBag.BankName = bank?.Name;
            }

            return View(vm);
        }

        // ================= CREATE GET =================
        public async Task<IActionResult> Create()
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;

            var vm = new UserCreateEditVM
            {
                IsSuperAdmin = isSuperAdmin
            };

            if (isSuperAdmin)
            {
                vm.BankList = (await _bankRepo.GetAllAsync())
                    .Select(b => new SelectListItem
                    {
                        Value = b.BankID.ToString(),
                        Text = b.Name
                    });
            }
            else
            {
                vm.BranchList = await GetBranchList(CurrentBankID);
            }

            return View(vm);
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            vm.IsSuperAdmin = isSuperAdmin;

            int bankId;

            if (isSuperAdmin)
            {
                if (!vm.SelectedBankID.HasValue)
                    ModelState.AddModelError("SelectedBankID", "Please select a bank.");

                bankId = vm.SelectedBankID ?? 0;

                vm.BankList = (await _bankRepo.GetAllAsync())
                    .Select(b => new SelectListItem
                    {
                        Value = b.BankID.ToString(),
                        Text = b.Name
                    });
            }
            else
            {
                bankId = CurrentBankID;
            }

            if (vm.Role != AppRoles.SuperAdmin && !vm.BranchID.HasValue)
                ModelState.AddModelError("BranchID", "Branch is required.");

            if (!ModelState.IsValid)
            {
                vm.BranchList = await GetBranchList(bankId);
                return View(vm);
            }

            var user = new User
            {
                BankID = bankId,
                BranchID = vm.Role == AppRoles.SuperAdmin ? null : vm.BranchID,
                Username = vm.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password ?? ""),
                Role = vm.Role,
                Code = vm.Code,
                Name = vm.Name,
                IsActive = vm.IsActive
            };

            await _userRepo.AddAsync(user);

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(int id)
        {
            User? user;
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;

            if (isSuperAdmin)
                user = await _userRepo.GetByIdAsync(id);
            else
                user = await _userRepo.GetByIdAndBankIdAsync(id, CurrentBankID);

            if (user == null) return NotFound();

            var vm = new UserCreateEditVM
            {
                UserID = user.UserID,
                SelectedBankID = user.BankID,
                Username = user.Username,
                Role = user.Role,
                Code = user.Code,
                Name = user.Name,
                IsActive = user.IsActive,
                BranchID = user.BranchID,
                IsSuperAdmin = isSuperAdmin
            };

            if (isSuperAdmin)
            {
                vm.BankList = (await _bankRepo.GetAllAsync())
                    .Select(b => new SelectListItem { Value = b.BankID.ToString(), Text = b.Name, Selected = b.BankID == user.BankID });
                vm.BranchList = await GetBranchList(user.BankID);
            }
            else
            {
                vm.BranchList = await GetBranchList(CurrentBankID);
            }

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            vm.IsSuperAdmin = isSuperAdmin;
            int bankId = isSuperAdmin ? (vm.SelectedBankID ?? 0) : CurrentBankID;

            if (vm.Role != AppRoles.SuperAdmin && !vm.BranchID.HasValue)
                ModelState.AddModelError("BranchID", "Branch is required.");

            if (!ModelState.IsValid)
            {
                if (isSuperAdmin)
                    vm.BankList = (await _bankRepo.GetAllAsync()).Select(b => new SelectListItem { Value = b.BankID.ToString(), Text = b.Name });
                vm.BranchList = await GetBranchList(bankId);
                return View("Create", vm);
            }

            var user = await _userRepo.GetByIdAsync(vm.UserID);
            if (user == null) return NotFound();

            // Security check for BankAdmin
            if (!isSuperAdmin && user.BankID != CurrentBankID) return Forbid();

            user.BankID = bankId;
            user.Username = vm.Username;
            user.Role = vm.Role;
            user.Code = vm.Code;
            user.Name = vm.Name;
            user.IsActive = vm.IsActive;
            user.BranchID = vm.Role == AppRoles.SuperAdmin ? null : vm.BranchID;

            if (!string.IsNullOrEmpty(vm.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);

            await _userRepo.UpdateAsync(user);

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (!isSuperAdmin && user.BankID != CurrentBankID) return Forbid();

            await _userRepo.DeleteAsync(id, user.BankID);
            TempData["Success"] = "User account removed.";
            return RedirectToAction(nameof(Index));
        }

        // ================= HELPER =================
        private async Task<IEnumerable<SelectListItem>> GetBranchList(int bankId)
        {
            var branches = await _branchRepo.GetAllByBankIdAsync(bankId);

            return branches.Select(b => new SelectListItem
            {
                Value = b.BranchID.ToString(),
                Text = b.Name
            });
        }
    }
}