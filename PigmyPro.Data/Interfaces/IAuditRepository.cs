using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    public class AuditLogRow
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityID { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? ChangedBy { get; set; }
        public string? ChangeIP { get; set; }
        public DateTime? ChangeDate { get; set; }
        public string? Remarks { get; set; }
    }

    public interface IAuditRepository
    {
        Task<IEnumerable<AuditLogRow>> GetRecentActivityAsync(int bankId, decimal? branchId, DateTime dateFrom, DateTime dateTo);
    }
}
