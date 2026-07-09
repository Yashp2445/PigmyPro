using System;
using System.Collections.Generic;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.Services
{
    public interface IReportExportService
    {
        byte[] GenerateDailyCollectionExcel(IEnumerable<DailyCollectionRow> data, string userRole, DateTime dateFrom, DateTime dateTo);
        byte[] GenerateDailyCollectionText(IEnumerable<DailyCollectionRow> data, string bankName, DateTime dateFrom, DateTime dateTo);
        byte[] GenerateAgentSummaryExcel(IEnumerable<AgentSummaryRow> data, DateTime dateFrom, DateTime dateTo);
        byte[] GenerateAgentSummaryText(IEnumerable<AgentSummaryRow> data, string bankName, DateTime dateFrom, DateTime dateTo);
    }
}
