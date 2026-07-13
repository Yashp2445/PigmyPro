using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Account;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.BankAdmin + "," + AppRoles.BranchAdmin)]
    public class AccountController : BaseController
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IBankRepository _bankRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IAgentRepository _agentRepo;

        public AccountController(
            IAccountRepository accountRepo,
            IBankRepository bankRepo,
            IBranchRepository branchRepo,
            IAgentRepository agentRepo)
        {
            _accountRepo = accountRepo;
            _bankRepo = bankRepo;
            _branchRepo = branchRepo;
            _agentRepo = agentRepo;
        }

        public async Task<IActionResult> Index(int? filterBankID, decimal? filterBranchCode, decimal? filterCode1, int page = 1)
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            int pageSize = 25;
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;

            PigmyPro.Data.PagedResult<CustomerAccount> dataResult;

            if (isSuperAdmin)
            {
                if (filterBankID.HasValue && filterBranchCode.HasValue)
                    dataResult = await _accountRepo.GetAllByBankAndBranchAsync(filterBankID.Value, filterBranchCode.Value, page, pageSize);
                else if (filterBankID.HasValue)
                    dataResult = await _accountRepo.GetAllByBankAsync(filterBankID.Value, page, pageSize);
                else
                    dataResult = new PigmyPro.Data.PagedResult<CustomerAccount> { Items = Enumerable.Empty<CustomerAccount>(), TotalCount = 0, PageNumber = page, PageSize = pageSize };
            }
            else if (isBankAdmin)
            {
                dataResult = filterBranchCode.HasValue
                    ? await _accountRepo.GetAllByBankAndBranchAsync(CurrentBankID, filterBranchCode.Value, page, pageSize)
                    : await _accountRepo.GetAllByBankAsync(CurrentBankID, page, pageSize);
            }
            else
            {
                dataResult = await _accountRepo.GetAllByBankAndBranchAsync(CurrentBankID, CurrentBranchID, page, pageSize);
            }

            var data = dataResult.Items;
            if (filterCode1.HasValue && filterCode1.Value > 0)
            {
                // In-memory filter on current page (acceptable for now, to preserve existing logic)
                data = data.Where(d => d.CODE1 == filterCode1.Value);
            }

            var accountTypeList = await GetAccountTypeListForBank(
                isSuperAdmin ? filterBankID : CurrentBankID);

            var accounts = data.Select(a => new AccountListVM
            {
                BankID = a.BankID,
                Code1 = a.CODE1,
                BrncCode = a.brnc_code,
                Code2 = a.CODE2,
                Name = a.name ?? "N/A",
                Address = a.ADDR,
                Balance = a.BALANCE ?? 0,
                OpenDate = a.OPN_DATE,
                AgnCode = a.AgnCode,
                MobileNo = a.Mobile_No ?? "N/A",
                TypeName = GetTypeName(a.CODE1)
            }).ToList();

            var vm = new AccountIndexVM
            {
                Accounts = new PigmyPro.Data.PagedResult<AccountListVM>
                {
                    Items = accounts,
                    TotalCount = dataResult.TotalCount,
                    PageNumber = dataResult.PageNumber,
                    PageSize = dataResult.PageSize
                },
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin,
                FilterBankID = filterBankID,
                FilterBranchCode = filterBranchCode,
                FilterCode1 = filterCode1,
                AccountTypeList = accountTypeList
            };

            if (isSuperAdmin)
            {
                vm.BankList = (await _bankRepo.GetAllAsync()).Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name,
                    Selected = b.BankID == filterBankID
                });

                if (filterBankID.HasValue)
                {
                    vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(filterBankID.Value))
                        .Select(b => new SelectListItem
                        {
                            Value = b.BranchID.ToString(),
                            Text = b.Name,
                            Selected = b.BranchID == (int?)filterBranchCode
                        });
                }
            }
            else if (isBankAdmin)
            {
                vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(CurrentBankID))
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchID.ToString(),
                        Text = b.Name,
                        Selected = b.BranchID == (int?)filterBranchCode
                    });
            }

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;

            var vm = new AccountCreateEditVM
            {
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin,
                AccountTypeList = isSuperAdmin
                    ? Enumerable.Empty<SelectListItem>() 
                    : await GetAccountTypeListForBank(CurrentBankID)
            };

            if (isSuperAdmin)
                vm.BankList = (await _bankRepo.GetAllAsync())
                    .Select(b => new SelectListItem { Value = b.BankID.ToString(), Text = b.Name });
            else if (isBankAdmin)
            {
                vm.SelectedBankID = CurrentBankID;
                vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(CurrentBankID))
                    .Select(b => new SelectListItem { Value = b.BranchID.ToString(), Text = b.Name });
            }
            else
            {
                vm.SelectedBankID = CurrentBankID;
                vm.SelectedBranchCode = (decimal)CurrentBranchID;
                vm.AgentList = await GetAgentList(CurrentBankID, (decimal)CurrentBranchID);
            }

            return View(vm);
        }

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateEditVM vm)
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            int bankId = isSuperAdmin ? (vm.SelectedBankID ?? 0) : CurrentBankID;
            decimal branchCode = (isSuperAdmin || isBankAdmin) ? (vm.SelectedBranchCode ?? 0) : (decimal)CurrentBranchID;

            if (isSuperAdmin && vm.SelectedBankID == null)
                ModelState.AddModelError("SelectedBankID", "Bank selection is required.");
            if ((isSuperAdmin || isBankAdmin) && branchCode == 0)
                ModelState.AddModelError("SelectedBranchCode", "Branch selection is required.");

            if (ModelState.IsValid && vm.Code1 > 0 && vm.Code2.HasValue && vm.Code2.Value > 0 && bankId > 0 && branchCode > 0)
            {
                var exists = await _accountRepo.ExistsAsync(bankId, vm.Code1, branchCode, vm.Code2.Value);
                if (exists)
                    ModelState.AddModelError("Code2",
                        $"Account number {vm.Code2.Value} already exists for this account type and branch.");
            }

            if (!ModelState.IsValid)
            {
                vm.IsSuperAdmin = isSuperAdmin;
                vm.IsBankAdmin = isBankAdmin;
                vm.AccountTypeList = await GetAccountTypeListForBank(bankId > 0 ? bankId : (int?)null);
                if (isSuperAdmin)
                    vm.BankList = (await _bankRepo.GetAllAsync())
                        .Select(b => new SelectListItem { Value = b.BankID.ToString(), Text = b.Name });
                if (isSuperAdmin || isBankAdmin)
                    vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(bankId))
                        .Select(b => new SelectListItem { Value = b.BranchID.ToString(), Text = b.Name });
                vm.AgentList = await GetAgentList(bankId, branchCode);
                return View(vm);
            }

            var account = new CustomerAccount
            {
                BankID = bankId,
                brnc_code = branchCode,
                CODE1 = vm.Code1,
                CODE2 = vm.Code2 ?? 0,
                name = vm.Name,
                ADDR = vm.Address,
                BALANCE = vm.Balance,
                OPN_DATE = vm.OpenDate,
                AgnCode = vm.AgnCode,
                Mobile_No = vm.MobileNo
            };

            await _accountRepo.AddAsync(account,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Customer account opened successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(decimal code1, decimal branchCode, decimal code2, int bankId)
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            int targetBankId = isSuperAdmin ? bankId : CurrentBankID;
            decimal targetBranchCode = (isSuperAdmin || isBankAdmin) ? branchCode : (decimal)CurrentBranchID;

            var account = await _accountRepo.GetByFullCodeAsync(targetBankId, code1, targetBranchCode, code2);
            if (account == null) return NotFound();

            var vm = new AccountCreateEditVM
            {
                IsEdit = true,
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin,
                SelectedBankID = account.BankID,
                SelectedBranchCode = account.brnc_code,
                Code1 = account.CODE1,
                Code2 = account.CODE2,
                Name = account.name,
                Address = account.ADDR,
                Balance = account.BALANCE ?? 0,
                OpenDate = account.OPN_DATE ?? DateTime.Today,
                AgnCode = account.AgnCode,
                MobileNo = account.Mobile_No,
                AccountTypeList = await GetAccountTypeListForBank(targetBankId),
                AgentList = await GetAgentList(targetBankId, targetBranchCode)
            };

            if (isSuperAdmin)
            {
                vm.BankList = (await _bankRepo.GetAllAsync())
                    .Select(b => new SelectListItem
                    {
                        Value = b.BankID.ToString(),
                        Text = b.Name,
                        Selected = b.BankID == targetBankId
                    });
                vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(targetBankId))
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchID.ToString(),
                        Text = b.Name,
                        Selected = b.BranchID == (int)targetBranchCode
                    });
            }
            else if (isBankAdmin)
            {
                vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(CurrentBankID))
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchID.ToString(),
                        Text = b.Name,
                        Selected = b.BranchID == (int)targetBranchCode
                    });
            }

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AccountCreateEditVM vm)
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            int bankId = isSuperAdmin ? (vm.SelectedBankID ?? 0) : CurrentBankID;
            decimal branchCode = (isSuperAdmin || isBankAdmin) ? (vm.SelectedBranchCode ?? 0) : (decimal)CurrentBranchID;

            if (!ModelState.IsValid)
            {
                vm.IsSuperAdmin = isSuperAdmin;
                vm.IsBankAdmin = isBankAdmin;
                vm.AccountTypeList = await GetAccountTypeListForBank(bankId > 0 ? bankId : (int?)null);
                if (isSuperAdmin)
                    vm.BankList = (await _bankRepo.GetAllAsync())
                        .Select(b => new SelectListItem { Value = b.BankID.ToString(), Text = b.Name });
                if (isSuperAdmin || isBankAdmin)
                    vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(bankId))
                        .Select(b => new SelectListItem { Value = b.BranchID.ToString(), Text = b.Name });
                vm.AgentList = await GetAgentList(bankId, branchCode);
                return View("Create", vm);
            }

            var account = new CustomerAccount
            {
                BankID = bankId,
                brnc_code = branchCode,
                CODE1 = vm.Code1,
                CODE2 = vm.Code2 ?? 0,
                name = vm.Name,
                ADDR = vm.Address,
                BALANCE = vm.Balance,
                OPN_DATE = vm.OpenDate,
                AgnCode = vm.AgnCode,
                Mobile_No = vm.MobileNo
            };

            await _accountRepo.UpdateAsync(account,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Account details updated.";
            return RedirectToAction(nameof(Index));
        }

     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(decimal code1, decimal branchCode, decimal code2, int bankId)
        {
            if (CurrentBankHasCBS == 'Y')
            {
                TempData["Error"] = "Customer accounts are managed via the external Core Banking System (CBS).";
                return RedirectToAction("Index", "Dashboard");
            }

            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            int targetBankId = isSuperAdmin ? bankId : CurrentBankID;
            decimal targetBranchCode = (isSuperAdmin || isBankAdmin) ? branchCode : (decimal)CurrentBranchID;

            await _accountRepo.DeleteAsync(targetBankId, code1, targetBranchCode, code2);
            TempData["Success"] = "Customer account closed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _branchRepo.GetAllByBankIdAsync(bankId);
            return Json(branches.Select(b => new { value = b.BranchID.ToString(), text = b.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents(int bankId, decimal branchCode)
        {
            // If bankId is not provided and user is not SuperAdmin, use CurrentBankID
            int targetBankId = (bankId > 0) ? bankId : CurrentBankID;

            var agents = await _agentRepo.GetAgentsAsync(targetBankId, branchCode);
            return Json(agents.Select(a => new { value = a.code.ToString(), text = a.NAME }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAccountTypes(int bankId)
        {
            var types = await GetAccountTypeListForBank(bankId);
            return Json(types.Select(t => new { value = t.Value, text = t.Text }));
        }

        private async Task<IEnumerable<SelectListItem>> GetAgentList(int bankId, decimal branchCode)
        {
            var agents = await _agentRepo.GetAgentsAsync(bankId, branchCode);

            return agents.Select(a => new SelectListItem
            {
                Value = a.code.ToString(),
                Text = a.NAME ?? "Unnamed Agent"
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetAccountTypeListForBank(int? bankId)
        {
            if (!bankId.HasValue || bankId.Value <= 0)
                return Enumerable.Empty<SelectListItem>();

            var glCode = await _accountRepo.GetCollectionGLCodeAsync(bankId.Value);

            var all = new[]
            {
                new SelectListItem { Value = ((int)PigmyPro.Domain.Enums.AccountType.Pigmy).ToString(), Text = PigmyPro.Domain.Enums.AccountTypeExtensions.GetDisplayName((int)PigmyPro.Domain.Enums.AccountType.Pigmy) },
                new SelectListItem { Value = ((int)PigmyPro.Domain.Enums.AccountType.Loan).ToString(), Text = PigmyPro.Domain.Enums.AccountTypeExtensions.GetDisplayName((int)PigmyPro.Domain.Enums.AccountType.Loan) },
                new SelectListItem { Value = ((int)PigmyPro.Domain.Enums.AccountType.Recurring).ToString(), Text = PigmyPro.Domain.Enums.AccountTypeExtensions.GetDisplayName((int)PigmyPro.Domain.Enums.AccountType.Recurring) }
            };

            return all.Where(t => int.Parse(t.Value) <= glCode);
        }

        private string GetTypeName(decimal code1)
        {
            return PigmyPro.Domain.Enums.AccountTypeExtensions.GetDisplayName((int)code1);
        }

        [HttpGet]
        public async Task<IActionResult> CheckDuplicate(int bankId, decimal code1, decimal branchCode, decimal code2)
        {
            var exists = await _accountRepo.ExistsAsync(bankId, code1, branchCode, code2);
            return Json(new { exists });
        }


    }
}