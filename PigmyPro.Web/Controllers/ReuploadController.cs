using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain;
using PigmyPro.Web.ViewModels.MobileImport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.BankAdmin + "," + AppRoles.BranchAdmin)]
    public class ReuploadController : BaseController
    {
        private readonly IReuploadRepository _repo;
        private readonly IBranchRepository _branchRepo;

        public ReuploadController(IReuploadRepository repo, IBranchRepository branchRepo)
        {
            _repo = repo;
            _branchRepo = branchRepo;
        }

        private async Task PopulateReuploadDropdowns(ReuploadVM vm)
        {
            if (CurrentUserRole == AppRoles.BankAdmin)
            {
                var branches = await _branchRepo.GetAllByBankIdAsync(CurrentBankID);
                vm.Branches = branches
                    .Select(b => new SelectListItem(b.Name, b.BranchID.ToString()))
                    .ToList();
            }

            decimal branchCode = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : (vm.SelectedBranchCode ?? 0);

            if (branchCode > 0)
            {
                var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchCode);
                vm.Agents = agents
                    .Select(a => new SelectListItem(a.Name, a.Code.ToString()))
                    .ToList();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = new ReuploadVM();
            await PopulateReuploadDropdowns(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents(int branchId)
        {
            var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchId);
            return Json(agents.Select(a => new { code = a.Code, name = a.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> CheckEligibility(decimal branchCode, decimal agentCode)
        {
            int bankId = CurrentBankID;
            decimal resolvedBranch = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : branchCode;

            if (resolvedBranch == 0 || agentCode == 0)
                return Json(new { eligible = false });

            var agent = await _repo.GetAgentDetailsAsync(bankId, resolvedBranch, agentCode);
            if (agent == null)
                return Json(new { eligible = false });

            // Re-upload primary condition: No pending mobile transactions
            bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(bankId, resolvedBranch, agentCode);
            bool eligible = !hasPendingData;

            return Json(new { eligible, hasPendingData });
        }

        [HttpPost]
        public async Task<IActionResult> ParseFile(IFormFile uploadedFile, int? SelectedBranchCode, decimal? SelectedAgentCode)
        {
            var vm = new ReuploadVM();
            vm.SelectedBranchCode = SelectedBranchCode;
            vm.SelectedAgentCode = SelectedAgentCode;
            await PopulateReuploadDropdowns(vm);

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                vm.ErrorMessage = "Please select a .DAT file to upload.";
                return View("Index", vm);
            }

            try
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            lines.Add(line.TrimEnd());
                    }
                }

                if (lines.Count == 0)
                {
                    vm.ErrorMessage = "The uploaded file is empty.";
                    return View("Index", vm);
                }

                var hParts = lines[0].Split(',');
                if (hParts.Length < 6)
                {
                    vm.ErrorMessage = "Invalid file: header row is malformed.";
                    return View("Index", vm);
                }

                string agentBranchField = hParts[3].Trim();
                if (agentBranchField.Length != 6)
                {
                    vm.ErrorMessage = "Invalid Agent/Branch code in file header. Expected 6 digits.";
                    return View("Index", vm);
                }

                decimal branchCode = decimal.Parse(agentBranchField.Substring(0, 3));
                decimal agentCode = decimal.Parse(agentBranchField.Substring(3, 3));

                vm.AgentCode = agentCode;
                vm.BranchCode = branchCode;
                vm.TotalRecords = int.TryParse(hParts[1].Trim(), out int tr) ? tr : 0;
                vm.TotalAmount = decimal.TryParse(hParts[2].Trim(), out decimal ta) ? ta : 0;

                if (DateTime.TryParseExact(hParts[4].Trim(), "dd-MM-yy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    vm.Date = parsedDate;
                else
                    vm.Date = DateTime.Today;

                bool branchValid = await _repo.ValidateBranchAsync(CurrentBankID, branchCode);
                if (!branchValid)
                {
                    vm.ErrorMessage = $"Branch code {branchCode} does not exist in your bank.";
                    return View("Index", vm);
                }

                var agentDetails = await _repo.ValidateAgentAsync(CurrentBankID, branchCode, agentCode);
                if (agentDetails == null)
                {
                    vm.ErrorMessage = $"Agent code {agentCode} does not exist in branch {branchCode}.";
                    return View("Index", vm);
                }

                vm.AgentName = agentDetails.AgentName;
                vm.BranchCode = branchCode;

                bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(CurrentBankID, branchCode, agentCode);
                if (hasPendingData)
                {
                    vm.ErrorMessage = "Agent already has pending collection data in the system — cannot re-upload.";
                    return View("Index", vm);
                }

                // *** NEW RE-UPLOAD LOGIC ***
                // Temporarily/initially make the required state values equivalent to the state expected at the beginning of normal upload
                await _repo.SetAgentStateForReuploadAsync(CurrentBankID, branchCode, agentCode);
                // ****************************

                double totalAmtCheck = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var p = lines[i].Split(',');
                    if (p.Length < 6) continue;

                    if (!long.TryParse(p[0].Trim(), out long code2)) continue;

                    string nm = p[2].Trim();
                    decimal amt = decimal.TryParse(p[3].Trim(), out decimal a) ? a : 0;

                    DateTime dt = vm.Date;
                    if (DateTime.TryParseExact(p[4].Trim(), "dd-MM-yy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime rowDate))
                        dt = rowDate;

                    string? mobile = p.Length >= 6 ? p[5].Trim() : null;
                    if (mobile == "0") mobile = null;

                    if (code2 > 0)
                    {
                        vm.ParsedRows.Add(new ImportAccountRowVM
                        {
                            Code2 = code2,
                            Name = nm,
                            Balance = amt,
                            OpnDate = dt,
                            Amount = amt,
                            MobileNumber = mobile
                        });
                        totalAmtCheck += (double)amt;
                    }
                }

                if (!vm.ParsedRows.Any())
                {
                    vm.ErrorMessage = "No valid data rows found in the file.";
                    return View("Index", vm);
                }

                vm.TotalRecords = vm.ParsedRows.Count;
                vm.TotalAmount = (decimal)totalAmtCheck;
                vm.HasParsedData = true;
                vm.ParsedRowsJson = JsonSerializer.Serialize(vm.ParsedRows);

                decimal expectedBranchCode = CurrentUserRole == AppRoles.BranchAdmin 
                    ? (decimal)CurrentBranchID 
                    : (vm.SelectedBranchCode ?? 0);

                if (expectedBranchCode != 0 && expectedBranchCode != branchCode)
                    vm.WarningMessage = "Selected branch does not match the file contents.";
                else if (vm.SelectedAgentCode.HasValue && vm.SelectedAgentCode.Value != agentCode)
                    vm.WarningMessage = "Selected agent does not match the file contents.";

                return View("Index", vm);
            }
            catch (Exception ex)
            {
                vm.ErrorMessage = "Error reading file: " + ex.Message;
                return View("Index", vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CommitUpload(ReuploadVM vm)
        {
            try
            {
                if (string.IsNullOrEmpty(vm.ParsedRowsJson))
                {
                    TempData["Error"] = "Session expired or invalid data. Please re-upload.";
                    return RedirectToAction("Index");
                }

                var rows = JsonSerializer.Deserialize<List<ReuploadAccountRow>>(vm.ParsedRowsJson);
                if (rows == null || rows.Count == 0)
                {
                    TempData["Error"] = "No rows to import.";
                    return RedirectToAction("Index");
                }

                int bankId = CurrentBankID;
                string userId = User.Identity?.Name ?? "system";
                string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                await _repo.CommitImportAsync(bankId, vm.BranchCode, vm.AgentCode, userId, clientIp, vm.TotalRecords, rows);

                TempData["Success"] = $"Master Data Re-uploaded successfully for Agent {vm.AgentCode}. {vm.TotalRecords} records committed.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to commit data: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
