using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.MobileImport;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.BankAdmin + "," + AppRoles.BranchAdmin)]
    public class MobileImportController : BaseController
    {
        private readonly IMobileImportRepository _repo;
        private readonly IBranchRepository _branchRepo;

        public MobileImportController(IMobileImportRepository repo, IBranchRepository branchRepo)
        {
            _repo = repo;
            _branchRepo = branchRepo;
        }


        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var vm = new ExportVM();
            await PopulateExportDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Export(ExportVM vm)
        {
            int bankId = CurrentBankID;
            decimal branchCode = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : (vm.BranchCode.HasValue ? (decimal)vm.BranchCode.Value : 0);

            if (branchCode == 0)
            {
                vm.ErrorMessage = "Please select a branch.";
                await PopulateExportDropdowns(vm);
                return View(vm);
            }

            if (!vm.AgentCode.HasValue)
            {
                vm.ErrorMessage = "Please select an agent.";
                await PopulateExportDropdowns(vm);
                return View(vm);
            }

            var agent = await _repo.GetAgentDetailsAsync(bankId, branchCode, vm.AgentCode.Value);
            if (agent == null)
            {
                vm.ErrorMessage = "Agent not found.";
                await PopulateExportDropdowns(vm);
                return View(vm);
            }

            if (agent.RadyToCash != "Y")
            {
                vm.ErrorMessage = "Agent must be marked Ready to Cash before exporting.";
                await PopulateExportDropdowns(vm);
                vm.HasSearched = true;
                return View(vm);
            }

            bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(bankId, branchCode, vm.AgentCode.Value);
            if (!hasPendingData)
            {
                vm.ErrorMessage = "No pending collection data found for this agent — nothing to export.";
                await PopulateExportDropdowns(vm);
                vm.HasSearched = true;
                return View(vm);
            }

            var rows = (await _repo.GetPendingCollectionsAsync(bankId, branchCode, vm.AgentCode.Value)).ToList();

            vm.Rows = rows.Select(r => new ExportRowVM
            {
                SrNo = r.SrNo,
                CollectionDate = r.CollectionDate,
                Code1 = r.Code1,
                Code2 = r.Code2,
                CustomerName = r.CustomerName,
                Amount = r.Amount,
                Balance = r.Balance
            }).ToList();

            vm.TotalRecords = vm.Rows.Count;
            vm.TotalAmount = vm.Rows.Sum(x => x.Amount);
            vm.HasSearched = true;

            await PopulateExportDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DownloadExport(int? branchCode, decimal? agentCode)
        {
            int bankId = CurrentBankID;
            decimal resolvedBranch = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : (branchCode.HasValue ? (decimal)branchCode.Value : 0);

            if (resolvedBranch == 0 || !agentCode.HasValue)
                return RedirectToAction("Export");

            var agent = await _repo.GetAgentDetailsAsync(bankId, resolvedBranch, agentCode.Value);
            if (agent == null || agent.RadyToCash != "Y")
                return RedirectToAction("Export");

            var rows = (await _repo.GetPendingCollectionsAsync(bankId, resolvedBranch, agentCode.Value)).ToList();
            if (!rows.Any())
                return RedirectToAction("Export");

            int outOfSyncCount = await _repo.ReconcileDownloadAsync(bankId, resolvedBranch, agentCode.Value);
            if (outOfSyncCount > 0)
            {
                TempData["Warning"] = $"{outOfSyncCount} record(s) were out of sync and have now been archived.";
            }

            int totalRecords = rows.Count;
            decimal totalAmount = rows.Sum(x => x.Amount);

            var sb = new StringBuilder();

            string agentStr = agentCode.Value.ToString("0").PadLeft(3, '0');
            string branchStr = resolvedBranch.ToString("0").PadLeft(3, '0');
            string recordsStr = totalRecords.ToString().PadLeft(6, '0');
            string amountStr = ((long)totalAmount).ToString().PadLeft(6, '0');

            sb.AppendLine($"000000,{recordsStr},{amountStr}          ,{agentStr}{branchStr},{DateTime.Today:dd-MM-yy},00000000");

            foreach (var r in rows)
            {
                string code2Str = r.Code2.ToString().PadLeft(6, '0');
                string rowAmtStr = ((long)r.Amount).ToString().PadLeft(6, '0');
                string name = (r.CustomerName ?? string.Empty);
                if (name.Length > 16) name = name.Substring(0, 16);
                name = name.PadRight(16);
                string balStr = ((long)r.Balance).ToString().PadLeft(6, '0');
                string dateStr = r.CollectionDate.ToString("dd-MM-yy");

                sb.AppendLine($"{code2Str},{rowAmtStr},{name},{balStr},{dateStr},{rowAmtStr}");
            }

            await _repo.LogExportAsync(bankId, resolvedBranch, agentCode.Value,
                User.Identity?.Name ?? "", totalRecords, totalAmount);
            await _repo.SetAgentDownloadFlagAsync(bankId, resolvedBranch, agentCode.Value, "Y");

            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            return File(bytes, "text/plain", "PCRX.DAT");
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents(int branchId)
        {
            var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchId);
            return Json(agents.Select(a => new { code = a.Code, name = a.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> CheckUploadEligibility(decimal branchCode, decimal agentCode)
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

            bool readyToCash = agent.RadyToCash == "Y";
            bool hasAcmasterRecords = await _repo.HasAcmasterRecordsAsync(bankId, resolvedBranch, agentCode);
            bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(bankId, resolvedBranch, agentCode);

            bool eligible = readyToCash && hasAcmasterRecords && !hasPendingData;

            return Json(new { eligible, readyToCash, hasAcmasterRecords, hasPendingData });
        }

        [HttpGet]
        public async Task<IActionResult> CheckExportEligibility(decimal branchCode, decimal agentCode)
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

            bool readyToCash = agent.RadyToCash == "Y";
            bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(bankId, resolvedBranch, agentCode);

            // Export requires RadyToCash == "Y" and hasPendingData == true
            bool eligible = readyToCash && hasPendingData;

            return Json(new { eligible, readyToCash, hasPendingData });
        }

        private async Task PopulateExportDropdowns(ExportVM vm)
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
                : (vm.BranchCode.HasValue ? (decimal)vm.BranchCode.Value : 0);

            if (branchCode > 0)
            {
                var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchCode);
                vm.Agents = agents
                    .Select(a => new SelectListItem(a.Name, a.Code.ToString()))
                    .ToList();
            }
        }

        private async Task PopulateUploadDropdowns(UploadVM vm)
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
                : (vm.SelectedBranchCode.HasValue ? (decimal)vm.SelectedBranchCode.Value : 0);

            if (branchCode > 0)
            {
                var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchCode);
                vm.Agents = agents
                    .Select(a => new SelectListItem(a.Name, a.Code.ToString()))
                    .ToList();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var vm = new UploadVM();
            await PopulateUploadDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ParseFile(IFormFile uploadedFile, int? SelectedBranchCode, decimal? SelectedAgentCode)
        {
            var vm = new UploadVM();
            vm.SelectedBranchCode = SelectedBranchCode;
            vm.SelectedAgentCode = SelectedAgentCode;
            await PopulateUploadDropdowns(vm);

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                vm.ErrorMessage = "Please select a .DAT file to upload.";
                return View("Upload", vm);
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
                    return View("Upload", vm);
                }

                // Parse header
                var hParts = lines[0].Split(',');
                if (hParts.Length < 6)
                {
                    vm.ErrorMessage = "Invalid file: header row is malformed.";
                    return View("Upload", vm);
                }

                // field[3] is AgentBranchCode — 6 chars: first 3 = agent, last 3 = branch
                string agentBranchField = hParts[3].Trim();
                if (agentBranchField.Length != 6)
                {
                    vm.ErrorMessage = "Invalid Agent/Branch code in file header. Expected 6 digits.";
                    return View("Upload", vm);
                }

                decimal agentCode = decimal.Parse(agentBranchField.Substring(0, 3));
                decimal branchCode = decimal.Parse(agentBranchField.Substring(3, 3));

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

                // Validate branch belongs to current bank
                bool branchValid = await _repo.ValidateBranchAsync(CurrentBankID, branchCode);
                if (!branchValid)
                {
                    vm.ErrorMessage = $"Branch code {branchCode} does not exist in your bank.";
                    return View("Upload", vm);
                }

                // Validate agent
                var agentDetails = await _repo.ValidateAgentAsync(CurrentBankID, branchCode, agentCode);
                if (agentDetails == null)
                {
                    vm.ErrorMessage = $"Agent code {agentCode} does not exist in branch {branchCode}.";
                    return View("Upload", vm);
                }

                vm.AgentName = agentDetails.AgentName;
                vm.BranchCode = branchCode;

                if (agentDetails.RadyToCash != "Y")
                {
                    vm.ErrorMessage = "Ready To Cash Not Set On Mobile.";
                    return View("Upload", vm);
                }

                if (agentDetails.Down_Load_YN != "Y")
                {
                    vm.ErrorMessage = "Pigmy Download Not Done. Please export first before uploading.";
                    return View("Upload", vm);
                }

                bool hasPendingData = await _repo.HasPendingMobileTransactionsAsync(CurrentBankID, branchCode, agentCode);
                if (hasPendingData)
                {
                    vm.ErrorMessage = "Agent already has pending collection data in the system — please export and clear existing data before uploading a new master file.";
                    return View("Upload", vm);
                }

                // Parse data rows (index 1 onward)
                // DAT format per row: Code2, Amount, Name, Balance, Date, Amount(again)
                double totalAmtCheck = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var p = lines[i].Split(',');
                    if (p.Length < 6) continue;

                    long code2 = 0;
                    if (!long.TryParse(p[0].Trim(), out code2)) continue;

                    // Index 1 is ignored exactly as it was in the original code

                    string nm = p[2].Trim(); // Index 2: Customer Name
                    decimal amt = decimal.TryParse(p[3].Trim(), out decimal a) ? a : 0; // Index 3: Amount

                    DateTime dt = vm.Date;
                    if (DateTime.TryParseExact(p[4].Trim(), "dd-MM-yy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime rowDate))
                        dt = rowDate; // Index 4: Date

                    if (code2 > 0)
                    {
                        vm.ParsedRows.Add(new ImportAccountRowVM
                        {
                            Code2 = code2,
                            Name = nm,
                            Balance = amt,   // WebForms uses Amount as Balance on import
                            OpnDate = dt,
                            Amount = amt
                        });
                        totalAmtCheck += (double)amt;
                    }
                }

                if (!vm.ParsedRows.Any())
                {
                    vm.ErrorMessage = "No valid data rows found in the file.";
                    return View("Upload", vm);
                }

                vm.TotalRecords = vm.ParsedRows.Count;
                vm.TotalAmount = (decimal)totalAmtCheck;
                vm.HasParsedData = true;
                vm.ParsedRowsJson = JsonSerializer.Serialize(vm.ParsedRows);

                // Cross-check with dropdown selections
                decimal expectedBranchCode = CurrentUserRole == AppRoles.BranchAdmin 
                    ? (decimal)CurrentBranchID 
                    : (vm.SelectedBranchCode.HasValue ? (decimal)vm.SelectedBranchCode.Value : 0);

                if (expectedBranchCode != 0 && expectedBranchCode != branchCode)
                {
                    vm.WarningMessage = "Selected branch does not match the file contents.";
                }
                else if (vm.SelectedAgentCode.HasValue && vm.SelectedAgentCode.Value != agentCode)
                {
                    vm.WarningMessage = "Selected agent does not match the file contents.";
                }

                return View("Upload", vm);
            }
            catch (Exception ex)
            {
                vm.ErrorMessage = "Error reading file: " + ex.Message;
                return View("Upload", vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CommitUpload(UploadVM vm)
        {
            try
            {
                if (string.IsNullOrEmpty(vm.ParsedRowsJson))
                {
                    TempData["Error"] = "No data to commit. Please upload a file first.";
                    return RedirectToAction("Upload");
                }

                var parsedRows = JsonSerializer.Deserialize<List<ImportAccountRowVM>>(vm.ParsedRowsJson);
                if (parsedRows == null || !parsedRows.Any())
                {
                    TempData["Error"] = "No valid data to commit.";
                    return RedirectToAction("Upload");
                }

                var domainRows = parsedRows.Select(r => new ImportAccountRow
                {
                    Code2 = r.Code2,
                    Name = r.Name,
                    Balance = r.Balance,
                    OpnDate = r.OpnDate
                }).ToList();

                string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                await _repo.CommitImportAsync(
                    CurrentBankID,
                    vm.BranchCode,
                    vm.AgentCode,
                    User.Identity?.Name ?? "",
                    clientIp,
                    vm.TotalRecords,
                    domainRows);

                TempData["Success"] = $"Upload successful. {vm.TotalRecords} account records imported.";
                return RedirectToAction("Upload");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Upload failed: " + ex.Message;
                return RedirectToAction("Upload");
            }
        }
        // ==========================================
        // REDOWNLOAD FLOW
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Redownload()
        {
            var vm = new RedownloadVM();
            // We can reuse PopulateExportDropdowns by casting or changing its signature,
            // or just write a small helper for Redownload
            await PopulateRedownloadDropdowns(vm);
            return View(vm);
        }

        private async Task PopulateRedownloadDropdowns(RedownloadVM vm)
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
                : (vm.BranchCode.HasValue ? (decimal)vm.BranchCode.Value : 0);

            if (branchCode > 0)
            {
                var agents = await _repo.GetAgentsByBranchAsync(CurrentBankID, branchCode);
                vm.Agents = agents
                    .Select(a => new SelectListItem(a.Name, a.Code.ToString()))
                    .ToList();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxDownloadDate(decimal branchCode, decimal agentCode)
        {
            int bankId = CurrentBankID;
            decimal resolvedBranch = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : branchCode;

            if (resolvedBranch == 0 || agentCode == 0)
                return Json(null);

            var maxDate = await _repo.GetMaxDownloadDateAsync(bankId, resolvedBranch, agentCode);
            if (maxDate.HasValue)
            {
                return Json(new { date = maxDate.Value.ToString("yyyy-MM-dd") });
            }
            
            return Json(null);
        }

        [HttpPost]
        public async Task<IActionResult> PreviewRedownload(RedownloadVM vm)
        {
            await PopulateRedownloadDropdowns(vm);

            if (!vm.BranchCode.HasValue && CurrentUserRole != AppRoles.BranchAdmin)
            {
                vm.ErrorMessage = "Please select a branch.";
                return View("Redownload", vm);
            }
            if (!vm.AgentCode.HasValue)
            {
                vm.ErrorMessage = "Please select an agent.";
                return View("Redownload", vm);
            }
            if (!vm.SelectedDate.HasValue)
            {
                vm.ErrorMessage = "Please select a date.";
                return View("Redownload", vm);
            }

            decimal resolvedBranch = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : (decimal)vm.BranchCode.Value;

            int bankId = CurrentBankID;
            
            var rows = await _repo.GetArchivedCollectionsAsync(bankId, resolvedBranch, vm.AgentCode.Value, vm.SelectedDate.Value);
            
            vm.HasSearched = true;
            vm.Rows = rows.Select(r => new ExportRowVM
            {
                SrNo = r.SrNo,
                CollectionDate = r.CollectionDate,
                Code1 = r.Code1,
                Code2 = r.Code2,
                CustomerName = r.CustomerName ?? "",
                Amount = r.Amount,
                Balance = r.Balance
            }).ToList();
            
            vm.TotalRecords = vm.Rows.Count;
            vm.TotalAmount = vm.Rows.Sum(r => r.Amount);

            return View("Redownload", vm);
        }

        [HttpPost]
        public async Task<IActionResult> DownloadRedownload(int? branchCode, decimal? agentCode, DateTime? selectedDate)
        {
            if (!agentCode.HasValue || !selectedDate.HasValue)
                return RedirectToAction("Redownload");

            decimal resolvedBranch = CurrentUserRole == AppRoles.BranchAdmin
                ? (decimal)CurrentBranchID
                : (branchCode ?? 0);
            
            if (resolvedBranch == 0)
                return RedirectToAction("Redownload");

            int bankId = CurrentBankID;
            var rows = (await _repo.GetArchivedCollectionsAsync(bankId, resolvedBranch, agentCode.Value, selectedDate.Value)).ToList();
            if (!rows.Any())
                return RedirectToAction("Redownload");

            int totalRecords = rows.Count;
            decimal totalAmount = rows.Sum(x => x.Amount);

            var sb = new StringBuilder();

            string agentStr = agentCode.Value.ToString("0").PadLeft(3, '0');
            string branchStr = resolvedBranch.ToString("0").PadLeft(3, '0');
            string recordsStr = totalRecords.ToString().PadLeft(6, '0');
            string amountStr = ((long)totalAmount).ToString().PadLeft(6, '0');

            // Header matching DownloadExport exactly
            sb.AppendLine($"000000,{recordsStr},{amountStr}          ,{agentStr}{branchStr},{DateTime.Today:dd-MM-yy},00000000");

            foreach (var r in rows)
            {
                string code2Str = r.Code2.ToString().PadLeft(6, '0');
                string rowAmtStr = ((long)r.Amount).ToString().PadLeft(6, '0');
                string name = (r.CustomerName ?? string.Empty);
                if (name.Length > 16) name = name.Substring(0, 16);
                name = name.PadRight(16);
                string balStr = ((long)r.Balance).ToString().PadLeft(6, '0');
                string dateStr = r.CollectionDate.ToString("dd-MM-yy");

                sb.AppendLine($"{code2Str},{rowAmtStr},{name},{balStr},{dateStr},{rowAmtStr}");
            }

            // User explicitly requested to skip LogExportAsync and SetAgentDownloadFlagAsync.
            // (DataExportLog logging is omitted per user confirmation).

            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            return File(bytes, "text/plain", "PCRX.DAT");
        }
    }
}