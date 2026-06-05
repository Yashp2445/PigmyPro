using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Agent;

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

        // ═══════════════════════════════════════════════
        // INDEX
        // ═══════════════════════════════════════════════
        public async Task<IActionResult> Index(int? filterBankID, decimal? filterBranchCode)
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";
            bool isBankAdmin = CurrentUserRole == "BankAdmin";

            IEnumerable<Agent> agents;

            if (isSuperAdmin)
            {
                // Show nothing until at least a bank is chosen
                if (filterBankID.HasValue && filterBranchCode.HasValue)
                    agents = await _agentRepo.GetAllByBankAndBranchAsync(filterBankID.Value, filterBranchCode.Value);
                else if (filterBankID.HasValue)
                    agents = await _agentRepo.GetAllByBankAsync(filterBankID.Value);
                else
                    agents = Enumerable.Empty<Agent>();
            }
            else if (isBankAdmin)
            {
                // Always show all agents of this bank; optionally filter by branch
                agents = filterBranchCode.HasValue
                    ? await _agentRepo.GetAllByBankAndBranchAsync(CurrentBankID, filterBranchCode.Value)
                    : await _agentRepo.GetAllByBankAsync(CurrentBankID);
            }
            else
            {
                // BranchAdmin — own branch only, no filter needed
                agents = await _agentRepo.GetAllByBankAndBranchAsync(CurrentBankID, CurrentBranchID);
            }

            // ── Branch lookup scoped correctly to avoid cross-bank name collisions ──
            IEnumerable<PigmyPro.Domain.Entities.Branch> scopedBranches;
            if (isSuperAdmin && filterBankID.HasValue)
                scopedBranches = await _branchRepo.GetAllByBankIdAsync(filterBankID.Value);
            else if (!isSuperAdmin)
                scopedBranches = await _branchRepo.GetAllByBankIdAsync(CurrentBankID);
            else
                scopedBranches = await _branchRepo.GetAllAsync();

            var allBanks = isSuperAdmin ? await _bankRepo.GetAllAsync() : null;

            var agentList = agents.Select(a => new AgentListVM
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
                Agents = agentList,
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
                    vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(filterBankID.Value))
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
                vm.BranchList = (await _branchRepo.GetAllByBankIdAsync(CurrentBankID))
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchID.ToString(),
                        Text = b.Name,
                        Selected = filterBranchCode.HasValue && b.BranchID == (int)filterBranchCode.Value
                    });
            }

            return View(vm);
        }

        // ═══════════════════════════════════════════════
        // CREATE GET
        // ═══════════════════════════════════════════════
        public async Task<IActionResult> Create()
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";
            bool isBankAdmin = CurrentUserRole == "BankAdmin";

            var vm = new AgentCreateEditVM
            {
                IsSuperAdmin = isSuperAdmin,
                IsBankAdmin = isBankAdmin
            };

            if (isSuperAdmin)
                vm.BankList = await GetBankList();
            else if (isBankAdmin)
                vm.BranchList = await GetBranchList(CurrentBankID);

            return View(vm);
        }

        // ═══════════════════════════════════════════════
        // CREATE POST
        // ═══════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AgentCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";
            bool isBankAdmin = CurrentUserRole == "BankAdmin";
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

            if (!ModelState.IsValid)
            {
                if (isSuperAdmin) vm.BankList = await GetBankList();
                if (isSuperAdmin || isBankAdmin) vm.BranchList = await GetBranchList(bankId);
                return View(vm);
            }

            var agent = new Agent
            {
                BankID = bankId,
                brnc_code = branchCode,
                code = vm.Code,
                NAME = vm.NAME,
                MobileNo = vm.MobileNo,
                Block = vm.Block,
                NoOfHolidays = vm.NoOfHolidays,
                RadyToCash = vm.ReadyToCash ? "Y" : "N"
            };

            await _agentRepo.AddAsync(agent, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Field Agent registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════
        // EDIT GET  ← ROOT FIX: pass bankId + branchCode from query string
        // ═══════════════════════════════════════════════
        public async Task<IActionResult> Edit(decimal code, int bankId, decimal branchCode)
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";
            bool isBankAdmin = CurrentUserRole == "BankAdmin";

            // Resolve the correct bankId/branchCode based on role
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

            return View("Create", vm);
        }

        // ═══════════════════════════════════════════════
        // EDIT POST
        // ═══════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AgentCreateEditVM vm)
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";
            bool isBankAdmin = CurrentUserRole == "BankAdmin";
            vm.IsSuperAdmin = isSuperAdmin;
            vm.IsBankAdmin = isBankAdmin;

            int bankId = isSuperAdmin ? (vm.SelectedBankID ?? CurrentBankID) : CurrentBankID;
            decimal branchCode = (isSuperAdmin || isBankAdmin) ? vm.BranchCode : (decimal)CurrentBranchID;

            if (!ModelState.IsValid)
            {
                if (isSuperAdmin) vm.BankList = await GetBankList(bankId);
                if (isSuperAdmin || isBankAdmin) vm.BranchList = await GetBranchList(bankId, branchCode);
                return View("Create", vm);
            }

            var agent = new Agent
            {
                BankID = bankId,
                brnc_code = branchCode,
                code = vm.Code,
                NAME = vm.NAME,
                MobileNo = vm.MobileNo,
                Block = vm.Block,
                NoOfHolidays = vm.NoOfHolidays,
                RadyToCash = vm.ReadyToCash ? "Y" : "N"
            };

            await _agentRepo.UpdateAsync(agent, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Agent profile updated.";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════
        // DELETE
        // ═══════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(decimal code, int bankId, decimal branchCode)
        {
            bool isSuperAdmin = CurrentUserRole == "SuperAdmin";

            int resolvedBankId = isSuperAdmin ? bankId : CurrentBankID;
            decimal resolvedBranchCode = (isSuperAdmin || CurrentUserRole == "BankAdmin") ? branchCode : (decimal)CurrentBranchID;

            await _agentRepo.DeleteAsync(resolvedBankId, resolvedBranchCode, code);
            TempData["Success"] = "Agent deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════
        // AJAX — branch list for SuperAdmin bank change
        // ═══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _branchRepo.GetAllByBankIdAsync(bankId);
            return Json(branches.Select(b => new { value = b.BranchID.ToString(), text = b.Name }));
        }

        // ═══════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════
        private async Task<IEnumerable<SelectListItem>> GetBankList(int? selectedBankId = null)
        {
            var banks = await _bankRepo.GetAllAsync();
            return banks.Select(b => new SelectListItem
            {
                Value = b.BankID.ToString(),
                Text = b.Name,
                Selected = b.BankID == selectedBankId
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetBranchList(int bankId, decimal? selectedBranchCode = null)
        {
            var branches = await _branchRepo.GetAllByBankIdAsync(bankId);
            return branches.Select(b => new SelectListItem
            {
                Value = b.BranchID.ToString(),
                Text = b.Name,
                Selected = selectedBranchCode.HasValue && b.BranchID == (int)selectedBranchCode.Value
            });
        }
    }
}