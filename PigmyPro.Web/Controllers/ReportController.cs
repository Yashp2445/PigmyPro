using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Report;
using OfficeOpenXml;
using System.Text;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly IReportRepository _repo;

        public ReportController(IReportRepository repo)
        {
            _repo = repo;
        }

        // ============================================================
        // DAILY COLLECTION REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DailyCollection()
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Daily Collection</li>";

            var vm = new DailyCollectionReportVM
            {
                UserRole = CurrentUserRole,
                HasSearched = false
            };

            await PopulateDailyCollectionDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollection(DailyCollectionReportVM vm)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Daily Collection</li>";

            vm.UserRole = CurrentUserRole;

            // Enforce role scoping — never trust posted values
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            vm.Rows = rows.ToList();
            vm.HasSearched = true;

            await PopulateDailyCollectionDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollectionExcel(DailyCollectionReportVM vm)
        {
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            var data = rows.ToList();

            ExcelPackage.License.SetNonCommercialOrganization("PigmyPro");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Daily Collection");

            // Header row
            var col = 1;
            if (CurrentUserRole == "SuperAdmin")
                ws.Cells[1, col++].Value = "Bank Name";
            ws.Cells[1, col++].Value = "Date";
            ws.Cells[1, col++].Value = "Branch Name";
            ws.Cells[1, col++].Value = "Agent Name";
            ws.Cells[1, col++].Value = "Ac. No";
            ws.Cells[1, col++].Value = "Customer Name";
            ws.Cells[1, col++].Value = "Type";
            ws.Cells[1, col++].Value = "Amount";

            // Style header
            var headerRange = ws.Cells[1, 1, 1, col - 1];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(26, 122, 110));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

            // Data rows
            var row = 2;
            foreach (var r in data)
            {
                col = 1;
                if (CurrentUserRole == "SuperAdmin")
                    ws.Cells[row, col++].Value = r.BankName;
                ws.Cells[row, col++].Value = r.Date.ToString("dd-MMM-yyyy");
                ws.Cells[row, col++].Value = r.BranchName;
                ws.Cells[row, col++].Value = r.AgentName;
                ws.Cells[row, col++].Value = r.Code2;
                ws.Cells[row, col++].Value = r.CustomerName;
                ws.Cells[row, col++].Value = r.AccountType;
                ws.Cells[row, col++].Value = r.Amount;
                ws.Cells[row, col - 1].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            // Summary row
            col = 1;
            ws.Cells[row, col].Value = $"Total Records: {data.Count}";
            ws.Cells[row, col].Style.Font.Bold = true;
            var lastCol = CurrentUserRole == "SuperAdmin" ? 8 : 7;
            ws.Cells[row, lastCol].Value = data.Sum(x => x.Amount);
            ws.Cells[row, lastCol].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, lastCol].Style.Font.Bold = true;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            var fileName = $"DailyCollection_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> DailyCollectionText(DailyCollectionReportVM vm)
        {
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetDailyCollectionAsync(
                vm.DateFrom, vm.DateTo,
                bankId, branchId,
                vm.FilterAgentCode, vm.FilterCode1);

            var data = rows.ToList();

            // Get bank name for header
            string bankName = "ALL BANKS";
            int headerBankId = bankId ?? CurrentBankID;
            if (headerBankId > 0)
                bankName = await _repo.GetBankNameAsync(headerBankId);

            var sb = new StringBuilder();
            var line = new string('=', 100);
            var dash = new string('-', 100);

            sb.AppendLine(line);
            sb.AppendLine($"  {bankName.ToUpper()}");
            sb.AppendLine($"  Daily Collection Report");
            sb.AppendLine($"  Date: {vm.DateFrom:dd-MMM-yyyy} to {vm.DateTo:dd-MMM-yyyy}");
            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine(
                "Date".PadRight(12) +
                "Branch".PadRight(18) +
                "Agent".PadRight(16) +
                "Ac.No".PadRight(10) +
                "Customer Name".PadRight(22) +
                "Type".PadRight(12) +
                "Amount".PadLeft(10));
            sb.AppendLine(dash);

            foreach (var r in data)
            {
                sb.AppendLine(
                    r.Date.ToString("dd-MMM-yy").PadRight(12) +
                    Truncate(r.BranchName, 16).PadRight(18) +
                    Truncate(r.AgentName, 14).PadRight(16) +
                    r.Code2.ToString().PadRight(10) +
                    Truncate(r.CustomerName, 20).PadRight(22) +
                    r.AccountType.PadRight(12) +
                    r.Amount.ToString("N2").PadLeft(10));
            }

            sb.AppendLine(dash);
            sb.AppendLine($"  Total Records: {data.Count,-20} Total Amount: Rs. {data.Sum(x => x.Amount):N2}");
            sb.AppendLine(line);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"DailyCollection_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.txt";
            return File(bytes, "text/plain", fileName);
        }

        // ============================================================
        // AGENT SUMMARY REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AgentSummary()
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Agent Summary</li>";
            var vm = new AgentSummaryReportVM { UserRole = CurrentUserRole, HasSearched = false };
            await PopulateAgentSummaryDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummary(AgentSummaryReportVM vm)
        {
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Report/DailyCollection'>Reports</a></li><li class='breadcrumb-item active'>Agent Summary</li>";
            vm.UserRole = CurrentUserRole;
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;

            var rows = await _repo.GetAgentSummaryAsync(vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode);
            vm.Rows = rows.ToList();
            vm.HasSearched = true;

            await PopulateAgentSummaryDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummaryExcel(AgentSummaryReportVM vm)
        {
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;
            var rows = (await _repo.GetAgentSummaryAsync(vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode)).ToList();

            ExcelPackage.License.SetNonCommercialOrganization("PigmyPro");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Agent Summary");

            string[] headers = { "Agent Code", "Agent Name", "Branch", "Accounts", "Total Amount", "Avg Amount" };
            for (int i = 0; i < headers.Length; i++) {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(26, 122, 110));
                ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int rowIdx = 2;
            foreach (var r in rows) {
                ws.Cells[rowIdx, 1].Value = r.AgentCode;
                ws.Cells[rowIdx, 2].Value = r.AgentName;
                ws.Cells[rowIdx, 3].Value = r.BranchName;
                ws.Cells[rowIdx, 4].Value = r.TotalAccounts;
                ws.Cells[rowIdx, 5].Value = r.TotalAmount;
                ws.Cells[rowIdx, 6].Value = r.AverageAmount;
                ws.Cells[rowIdx, 5].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[rowIdx, 6].Style.Numberformat.Format = "#,##0.00";
                rowIdx++;
            }

            ws.Cells[rowIdx, 3].Value = "Grand Total:";
            ws.Cells[rowIdx, 4].Value = rows.Sum(x => x.TotalAccounts);
            ws.Cells[rowIdx, 5].Value = rows.Sum(x => x.TotalAmount);
            ws.Cells[rowIdx, 3, rowIdx, 5].Style.Font.Bold = true;
            ws.Cells[rowIdx, 5].Style.Numberformat.Format = "#,##0.00";

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AgentSummary_{vm.DateFrom:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> AgentSummaryText(AgentSummaryReportVM vm)
        {
            int? bankId = CurrentUserRole == "SuperAdmin" ? vm.FilterBankID : CurrentBankID;
            int? branchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : vm.FilterBranchID;
            var rows = (await _repo.GetAgentSummaryAsync(vm.DateFrom, vm.DateTo, bankId, branchId, vm.FilterAgentCode)).ToList();
            string bankName = bankId.HasValue ? await _repo.GetBankNameAsync(bankId.Value) : "ALL BANKS";

            var sb = new StringBuilder();
            var line = new string('=', 75);
            var dash = new string('-', 75);

            sb.AppendLine(line);
            sb.AppendLine($"  {bankName.ToUpper()}");
            sb.AppendLine($"  Agent-wise Collection Summary");
            sb.AppendLine($"  Period: {vm.DateFrom:dd-MMM-yyyy} to {vm.DateTo:dd-MMM-yyyy}");
            sb.AppendLine(line);
            sb.AppendLine("Code".PadRight(8) + "Agent Name".PadRight(20) + "Branch".PadRight(18) + "Accounts".PadLeft(10) + "Total Amt".PadLeft(12) + "Avg Amt".PadLeft(12));
            sb.AppendLine(dash);

            foreach (var r in rows) {
                sb.AppendLine(r.AgentCode.ToString().PadRight(8) + Truncate(r.AgentName, 18).PadRight(20) + Truncate(r.BranchName, 16).PadRight(18) + r.TotalAccounts.ToString().PadLeft(10) + r.TotalAmount.ToString("N2").PadLeft(12) + r.AverageAmount.ToString("N2").PadLeft(12));
            }
            sb.AppendLine(dash);
            sb.AppendLine($"Grand Total: {rows.Sum(x => x.TotalAccounts)} Accounts".PadRight(46) + rows.Sum(x => x.TotalAmount).ToString("N2").PadLeft(12));
            sb.AppendLine(line);

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain", $"AgentSummary_{vm.DateFrom:yyyyMMdd}.txt");
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private async Task PopulateDailyCollectionDropdowns(DailyCollectionReportVM vm)
        {
            if (CurrentUserRole == "SuperAdmin") {
                var banks = await _repo.GetBankDropdownAsync();
                vm.Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList();
            }
            if (CurrentUserRole != "BranchAdmin") {
                int bId = (CurrentUserRole == "SuperAdmin") ? (vm.FilterBankID ?? 0) : CurrentBankID;
                if (bId > 0) {
                    var branches = await _repo.GetBranchDropdownAsync(bId);
                    vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
                }
            }
            int abId = (CurrentUserRole == "SuperAdmin") ? (vm.FilterBankID ?? 0) : CurrentBankID;
            int abrId = (CurrentUserRole == "BranchAdmin") ? CurrentBranchID : (vm.FilterBranchID ?? 0);
            if (abId > 0 && abrId > 0) {
                var agents = await _repo.GetAgentDropdownAsync(abId, abrId);
                vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();
            }
        }

        private async Task PopulateAgentSummaryDropdowns(AgentSummaryReportVM vm)
        {
            if (CurrentUserRole == "SuperAdmin") {
                var banks = await _repo.GetBankDropdownAsync();
                vm.Banks = banks.Select(b => new SelectListItem(b.Name, b.BankID.ToString())).ToList();
            }
            if (CurrentUserRole != "BranchAdmin") {
                int bId = (CurrentUserRole == "SuperAdmin") ? (vm.FilterBankID ?? 0) : CurrentBankID;
                if (bId > 0) {
                    var branches = await _repo.GetBranchDropdownAsync(bId);
                    vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
                }
            }
            int abId = (CurrentUserRole == "SuperAdmin") ? (vm.FilterBankID ?? 0) : CurrentBankID;
            int abrId = (CurrentUserRole == "BranchAdmin") ? CurrentBranchID : (vm.FilterBranchID ?? 0);
            if (abId > 0 && abrId > 0) {
                var agents = await _repo.GetAgentDropdownAsync(abId, abrId);
                vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();
            }
        }



        // AJAX endpoint: populate branches when bank changes (SuperAdmin)
        [HttpGet]
        public async Task<IActionResult> GetBranches(int bankId)
        {
            var branches = await _repo.GetBranchDropdownAsync(bankId);
            return Json(branches);
        }

        // AJAX endpoint: populate agents when branch changes
        [HttpGet]
        public async Task<IActionResult> GetAgents(int bankId, int branchId)
        {
            int safeBankId = CurrentUserRole == "SuperAdmin" ? bankId : CurrentBankID;
            var agents = await _repo.GetAgentDropdownAsync(safeBankId, branchId);
            return Json(agents);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}

