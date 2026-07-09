using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain;

namespace PigmyPro.Web.Services
{
    public class ReportExportService : IReportExportService
    {
        public ReportExportService()
        {
            ExcelPackage.License.SetNonCommercialOrganization("PigmyPro");
        }

        public byte[] GenerateDailyCollectionExcel(IEnumerable<DailyCollectionRow> data, string userRole, DateTime dateFrom, DateTime dateTo)
        {
            var rows = data.ToList();
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Daily Collection");

            var col = 1;
            if (userRole == AppRoles.SuperAdmin)
                ws.Cells[1, col++].Value = "Bank Name";
            ws.Cells[1, col++].Value = "Date";
            ws.Cells[1, col++].Value = "Branch Name";
            ws.Cells[1, col++].Value = "Agent Name";
            ws.Cells[1, col++].Value = "Ac. No";
            ws.Cells[1, col++].Value = "Customer Name";
            ws.Cells[1, col++].Value = "Type";
            ws.Cells[1, col++].Value = "Amount";

            var headerRange = ws.Cells[1, 1, 1, col - 1];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(26, 122, 110));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

            var row = 2;
            foreach (var r in rows)
            {
                col = 1;
                if (userRole == AppRoles.SuperAdmin)
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

            col = 1;
            ws.Cells[row, col].Value = $"Total Records: {rows.Count}";
            ws.Cells[row, col].Style.Font.Bold = true;
            var lastCol = userRole == AppRoles.SuperAdmin ? 8 : 7;
            ws.Cells[row, lastCol].Value = rows.Sum(x => x.Amount);
            ws.Cells[row, lastCol].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, lastCol].Style.Font.Bold = true;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        public byte[] GenerateDailyCollectionText(IEnumerable<DailyCollectionRow> data, string bankName, DateTime dateFrom, DateTime dateTo)
        {
            var rows = data.ToList();
            var sb = new StringBuilder();
            var line = new string('=', 100);
            var dash = new string('-', 100);

            sb.AppendLine(line);
            sb.AppendLine($"  {bankName.ToUpper()}");
            sb.AppendLine($"  Daily Collection Report");
            sb.AppendLine($"  Date: {dateFrom:dd-MMM-yyyy} to {dateTo:dd-MMM-yyyy}");
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

            foreach (var r in rows)
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
            sb.AppendLine($"  Total Records: {rows.Count,-20} Total Amount: Rs. {rows.Sum(x => x.Amount):N2}");
            sb.AppendLine(line);

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GenerateAgentSummaryExcel(IEnumerable<AgentSummaryRow> data, DateTime dateFrom, DateTime dateTo)
        {
            var rows = data.ToList();
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Agent Summary");

            ws.Cells[1, 1].Value = "Branch";
            ws.Cells[1, 2].Value = "Agent";
            ws.Cells[1, 3].Value = "A/c Collected";
            ws.Cells[1, 4].Value = "Avg Amount/Ac";
            ws.Cells[1, 5].Value = "Total Amount";

            var headerRange = ws.Cells[1, 1, 1, 5];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(59, 130, 246));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

            var row = 2;
            foreach (var r in rows)
            {
                ws.Cells[row, 1].Value = r.BranchName;
                ws.Cells[row, 2].Value = r.AgentName;
                ws.Cells[row, 3].Value = r.TotalAccounts;
                ws.Cells[row, 4].Value = r.AverageAmount;
                ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 5].Value = r.TotalAmount;
                ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            ws.Cells[row, 1].Value = "TOTAL";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 3].Value = rows.Sum(x => x.TotalAccounts);
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 5].Value = rows.Sum(x => x.TotalAmount);
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 5].Style.Font.Bold = true;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        public byte[] GenerateAgentSummaryText(IEnumerable<AgentSummaryRow> data, string bankName, DateTime dateFrom, DateTime dateTo)
        {
            var rows = data.ToList();
            var sb = new StringBuilder();
            var line = new string('=', 80);
            var dash = new string('-', 80);

            sb.AppendLine(line);
            sb.AppendLine($"  {bankName.ToUpper()}");
            sb.AppendLine($"  Agent Collection Summary");
            sb.AppendLine($"  Date: {dateFrom:dd-MMM-yyyy} to {dateTo:dd-MMM-yyyy}");
            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine(
                "Branch".PadRight(20) +
                "Agent".PadRight(25) +
                "A/c Col.".PadRight(10) +
                "Avg.Amt".PadLeft(10) +
                "Total Amt".PadLeft(12));
            sb.AppendLine(dash);

            foreach (var r in rows)
            {
                sb.AppendLine(
                    Truncate(r.BranchName, 18).PadRight(20) +
                    Truncate(r.AgentName, 23).PadRight(25) +
                    r.TotalAccounts.ToString().PadRight(10) +
                    r.AverageAmount.ToString("N2").PadLeft(10) +
                    r.TotalAmount.ToString("N2").PadLeft(12));
            }

            sb.AppendLine(dash);
            sb.AppendLine($"TOTALS:".PadRight(45) +
                          $"{rows.Sum(x => x.TotalAccounts).ToString().PadRight(10)}" +
                          $"{"".PadLeft(10)}" +
                          $"{rows.Sum(x => x.TotalAmount):N2}".PadLeft(12));
            sb.AppendLine(line);

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 2) + "..";
        }
    }
}
