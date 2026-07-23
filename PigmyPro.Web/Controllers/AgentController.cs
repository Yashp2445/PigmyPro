using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Agent;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    public class AgentController : BaseController
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IBankRepository _bankRepo;

        public AgentController(IAgentRepository agentRepo, IBranchRepository branchRepo, IBankRepository bankRepo)
        {
            _agentRepo = agentRepo;
            _branchRepo = branchRepo;
            _bankRepo = bankRepo;
        }

        public async Task<IActionResult> Index(int? filterBankID, decimal? filterBranchCode, int page = 1)
        {
            int pageSize = 25;
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;

            PigmyPro.Data.PagedResult<Agent> agentsResult;

            if (isSuperAdmin)
            {
                if (filterBankID.HasValue && filterBranchCode.HasValue)
                    agentsResult = await _agentRepo.GetAllByBankAndBranchAsync(filterBankID.Value, filterBranchCode.Value, page, pageSize);
                else if (filterBankID.HasValue)
                    agentsResult = await _agentRepo.GetAllByBankAsync(filterBankID.Value, page, pageSize);
                else
                    agentsResult = new PigmyPro.Data.PagedResult<Agent> { Items = Enumerable.Empty<Agent>(), TotalCount = 0, PageNumber = page, PageSize = pageSize };
            }
            else if (isBankAdmin)
            {
                agentsResult = filterBranchCode.HasValue
                    ? await _agentRepo.GetAllByBankAndBranchAsync(CurrentBankID, filterBranchCode.Value, page, pageSize)
                    : await _agentRepo.GetAllByBankAsync(CurrentBankID, page, pageSize);
            }
            else
            {
                agentsResult = await _agentRepo.GetAllByBankAndBranchAsync(CurrentBankID, CurrentBranchID, page, pageSize);
            }

            IEnumerable<PigmyPro.Domain.Entities.Branch> scopedBranches;
            if (isSuperAdmin && filterBankID.HasValue)
                scopedBranches = await _branchRepo.GetActiveByBankIdAsync(filterBankID.Value);
            else if (!isSuperAdmin)
                scopedBranches = await _branchRepo.GetActiveByBankIdAsync(CurrentBankID);
            else
                scopedBranches = await _branchRepo.GetAllAsync();

            var allBanks = isSuperAdmin ? await _bankRepo.GetActiveAsync() : null;

            var agentList = agentsResult.Items.Select(a => new AgentListVM
            {
                BankID = a.BankID,
                BranchCode = a.brnc_code,
                Code = a.code,
                Name = a.NAME ?? "N/A",
                MobileNo = a.MobileNo,
                BranchName = scopedBranches.FirstOrDefault(b => b.BranchID == (int)a.brnc_code)?.Name ?? "Unknown",
                BankName = allBanks?.FirstOrDefault(b => b.BankID == a.BankID)?.Name ?? string.Empty,
                IsBlocked = a.Block ?? false,
                Holidays = a.NoOfHolidays ?? 0,
                ReadyToCash = a.RadyToCash == "Y",
                EntryDate = a.EntryDate
            }).ToList();

            var vm = new AgentIndexVM
            {
                Agents = new PigmyPro.Data.PagedResult<AgentListVM>
                {
                    Items = agentList,
                    TotalCount = agentsResult.TotalCount,
                    PageNumber = agentsResult.PageNumber,
                    PageSize = agentsResult.PageSize
                },
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin,
                FilterBankID = filterBankID,
                FilterBranchCode = filterBranchCode
            };

            if (isSuperAdmin)
            {
                vm.BankList = allBanks!.Select(b => new SelectListItem
                {
                    Value = b.BankID.ToString(),
                    Text = b.Name,
                    Selected = b.BankID == filterBankID
                });

                if (filterBankID.HasValue)
                {
                    vm.BranchList = (await _branchRepo.GetActiveByBankIdAsync(filterBankID.Value))
                        .Select(b => new SelectListItem
                        {
                            Value = b.BranchID.ToString(),
                            Text = b.Name,
                            Selected = filterBranchCode.HasValue && b.BranchID == (int)filterBranchCode.Value
                        });
                }
            }
            else if (isBankAdmin)
            {
                vm.BranchList = (await _branchRepo.GetActiveByBankIdAsync(CurrentBankID))
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchID.ToString(),
                        Text = b.Name,
                        Selected = filterBranchCode.HasValue && b.BranchID == (int)filterBranchCode.Value
                    });
            }

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;

            var vm = new AgentCreateEditVM
            {
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin
            };

            if (isSuperAdmin)
            {
                vm.BankList = await GetBankList();
            }
            else if (isBankAdmin)
            {
                vm.SelectedBankID = CurrentBankID;
                vm.BranchList = await GetBranchList(CurrentBankID);
            }
            else
            {
                vm.BranchCode = (decimal)CurrentBranchID;
                vm.Code = await _agentRepo.GetNextAgentCodeAsync(CurrentBankID, (decimal)CurrentBranchID);
            }

            var bank = await _bankRepo.GetByIdAsync(CurrentBankID);
            vm.AppLoginPrefix = bank?.AppLoginPrefix;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AgentCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            vm.IsSuperAdmin = isSuperAdmin;
            vm.IsBankAdmin = isBankAdmin;

            int bankId;
            decimal branchCode;

            if (isSuperAdmin)
            {
                if (!vm.SelectedBankID.HasValue)
                    ModelState.AddModelError("SelectedBankID", "Please select a bank.");
                if (vm.BranchCode == 0)
                    ModelState.AddModelError("BranchCode", "Please select a branch.");

                bankId = vm.SelectedBankID ?? 0;
                branchCode = vm.BranchCode;
            }
            else if (isBankAdmin)
            {
                if (vm.BranchCode == 0)
                    ModelState.AddModelError("BranchCode", "Please select a branch.");

                bankId = CurrentBankID;
                branchCode = vm.BranchCode;
            }
            else
            {
                bankId = CurrentBankID;
                branchCode = (decimal)CurrentBranchID;
            }

            if (!string.IsNullOrWhiteSpace(vm.MobileNo) && await _agentRepo.IsMobileNumberInUseAsync(bankId, vm.MobileNo))
            {
                ModelState.AddModelError("MobileNo", "This mobile number is already registered to another agent in this bank.");
            }

            if (vm.ReceiptNoPerAc < 1 || vm.ReceiptNoPerAc > 3)
            {
                ModelState.AddModelError("ReceiptNoPerAc", "Receipts per day must be between 1 and 3.");
            }

            var bankInfo = await _bankRepo.GetByIdAsync(bankId);
            if (vm.NoOfHolidays.HasValue && bankInfo != null && vm.NoOfHolidays.Value > (bankInfo.No_of_Holidays ?? 0))
            {
                ModelState.AddModelError("NoOfHolidays", "Max. Collection Days should not be more than that agent's bank's No_of_Holidays value");
            }

            if (!ModelState.IsValid)
            {
                if (isSuperAdmin) vm.BankList = await GetBankList();
                if (isSuperAdmin || isBankAdmin) vm.BranchList = await GetBranchList(bankId);
                vm.AppLoginPrefix = bankInfo?.AppLoginPrefix;
                return View(vm);
            }

            if (!vm.Code.HasValue)
            {
                vm.Code = await _agentRepo.GetNextAgentCodeAsync(bankId, branchCode);
            }

            var agent = new Agent
            {
                BankID = bankId,
                brnc_code = branchCode,
                code = vm.Code.Value,
                NAME = vm.NAME?.ToUpper(),
                MobileNo = vm.MobileNo,
                Block = vm.Block,
                NoOfHolidays = vm.NoOfHolidays ?? 0,
                ReceiptNoPerAc = vm.ReceiptNoPerAc,
                RadyToCash = vm.ReadyToCash ? "Y" : "N"
            };

            await _agentRepo.AddAsync(agent, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Field Agent registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(decimal code, int bankId, decimal branchCode)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;

            int resolvedBankId = isSuperAdmin ? bankId : CurrentBankID;
            decimal resolvedBranchCode = (isSuperAdmin || isBankAdmin) ? branchCode : (decimal)CurrentBranchID;

            var agent = await _agentRepo.GetByCodeAsync(resolvedBankId, resolvedBranchCode, code);
            if (agent == null) return NotFound();

            var vm = new AgentCreateEditVM
            {
                IsEdit = true,
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin,
                Code = agent.code,
                NAME = agent.NAME ?? string.Empty,
                MobileNo = agent.MobileNo,
                BranchCode = agent.brnc_code,
                SelectedBankID = agent.BankID,
                Block = agent.Block ?? false,
                NoOfHolidays = agent.NoOfHolidays ?? 0,
                ReceiptNoPerAc = agent.ReceiptNoPerAc,
                ReadyToCash = agent.RadyToCash == "Y"
            };

            if (isSuperAdmin)
            {
                vm.BankList = await GetBankList(agent.BankID);
                vm.BranchList = await GetBranchList(agent.BankID, agent.brnc_code);
            }
            else if (isBankAdmin)
            {
                vm.BranchList = await GetBranchList(CurrentBankID, agent.brnc_code);
            }

            var bank = await _bankRepo.GetByIdAsync(CurrentBankID);
            vm.AppLoginPrefix = bank?.AppLoginPrefix;

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AgentCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            bool isBankAdmin = CurrentUserRole == AppRoles.BankAdmin;
            vm.IsSuperAdmin = isSuperAdmin;
            vm.IsBankAdmin = isBankAdmin;

            int bankId = isSuperAdmin ? (vm.SelectedBankID ?? CurrentBankID) : CurrentBankID;
            decimal branchCode = (isSuperAdmin || isBankAdmin) ? vm.BranchCode : (decimal)CurrentBranchID;

            if (!string.IsNullOrWhiteSpace(vm.MobileNo) && await _agentRepo.IsMobileNumberInUseAsync(bankId, vm.MobileNo, vm.Code))
            {
                ModelState.AddModelError("MobileNo", "This mobile number is already registered to another agent in this bank.");
            }

            if (vm.ReceiptNoPerAc < 1 || vm.ReceiptNoPerAc > 3)
            {
                ModelState.AddModelError("ReceiptNoPerAc", "Receipts per day must be between 1 and 3.");
            }

            var bankInfo = await _bankRepo.GetByIdAsync(bankId);
            if (vm.NoOfHolidays.HasValue && bankInfo != null && vm.NoOfHolidays.Value > (bankInfo.No_of_Holidays ?? 0))
            {
                ModelState.AddModelError("NoOfHolidays", "Max. Collection Days should not be more than that agent's bank's No_of_Holidays value");
            }

            if (!ModelState.IsValid)
            {
                if (isSuperAdmin) vm.BankList = await GetBankList(bankId);
                if (isSuperAdmin || isBankAdmin) vm.BranchList = await GetBranchList(bankId, branchCode);
                vm.AppLoginPrefix = bankInfo?.AppLoginPrefix;
                return View("Create", vm);
            }

            var agent = new Agent
            {
                BankID = bankId,
                brnc_code = branchCode,
                code = vm.Code ?? 0,
                NAME = vm.NAME?.ToUpper(),
                MobileNo = vm.MobileNo,
                NoOfHolidays = vm.NoOfHolidays ?? 0,
                ReceiptNoPerAc = vm.ReceiptNoPerAc,
                RadyToCash = vm.ReadyToCash ? "Y" : "N"
                // Block is intentionally NOT set here — Block/Unblock is
                // driven by vm.Block via the dedicated flag below, applied
                // by the SP's Block branch, not the base-field update.
            };

            await _agentRepo.UpdateAsync(
                agent,
                resetAgent: vm.ResetAgent,
                resetRemark: vm.ResetRemark,
                blockChecked: vm.Block,
                blockRemark: vm.BlockRemark,
                changedBy: User.Identity?.Name,
                changeIp: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Agent profile updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(decimal code, int bankId, decimal branchCode)
        {
            bool isSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;

            int resolvedBankId = isSuperAdmin ? bankId : CurrentBankID;
            decimal resolvedBranchCode = (isSuperAdmin || CurrentUserRole == AppRoles.BankAdmin) ? branchCode : (decimal)CurrentBranchID;

            await _agentRepo.DeleteAsync(
                resolvedBankId,
                resolvedBranchCode,
                code,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Agent deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _branchRepo.GetActiveByBankIdAsync(bankId);
            return Json(branches.Select(b => new { value = b.BranchID.ToString(), text = b.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> GetNextCode(int bankId, decimal branchCode)
        {
            var next = await _agentRepo.GetNextAgentCodeAsync(bankId, branchCode);
            return Json(new { code = next });
        }

        [HttpGet]
        public async Task<IActionResult> GetBankHolidays(int bankId)
        {
            var bank = await _bankRepo.GetByIdAsync(bankId);
            return Json(new { holidays = bank?.No_of_Holidays ?? 0, appLoginPrefix = bank?.AppLoginPrefix ?? "" });
        }

        [HttpGet]
        public async Task<IActionResult> CheckCodeExists(int bankId, decimal branchCode, decimal code)
        {
            var agent = await _agentRepo.GetByCodeAsync(bankId, branchCode, code);
            return Json(new { exists = agent != null });
        }

        private async Task<IEnumerable<SelectListItem>> GetBankList(int? selectedBankId = null)
        {
            var banks = await _bankRepo.GetActiveAsync();
            return banks.Select(b => new SelectListItem
            {
                Value = b.BankID.ToString(),
                Text = b.Name,
                Selected = b.BankID == selectedBankId
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetBranchList(int bankId, decimal? selectedBranchCode = null)
        {
            var branches = await _branchRepo.GetActiveByBankIdAsync(bankId);
            return branches.Select(b => new SelectListItem
            {
                Value = b.BranchID.ToString(),
                Text = b.Name,
                Selected = selectedBranchCode.HasValue && b.BranchID == (int)selectedBranchCode.Value
            });
        }
    }
}