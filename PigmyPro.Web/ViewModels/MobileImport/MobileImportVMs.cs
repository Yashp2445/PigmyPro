using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Web.ViewModels.MobileImport
{
    public class MobileImportListVM
    {
        public IEnumerable<MobileTransaction> Transactions { get; set; } = new List<MobileTransaction>();
        
        // Filtering
        public decimal? AgentFilter { get; set; }
        public DateTime? DateFilter { get; set; } = DateTime.Today;
        
        // Dropdowns
        public IEnumerable<SelectListItem>? Agents { get; set; }
        
        // Summary Stats
        public decimal TotalAmount => Transactions.Sum(t => t.Amount);
        public int TotalCount => Transactions.Count();
    }
}
