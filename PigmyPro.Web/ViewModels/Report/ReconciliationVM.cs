using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Report
{
    public class ReconciliationVM
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int? FilterBranchID { get; set; }
        public IEnumerable<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public IEnumerable<ReconciliationRow> Results { get; set; } = new List<ReconciliationRow>();
    }
}
