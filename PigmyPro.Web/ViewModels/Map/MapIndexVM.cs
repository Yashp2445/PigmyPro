using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.ViewModels.Map
{
    public class MapIndexVM
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public int? BranchCode { get; set; }
        public long? AgentCode { get; set; }
        public List<SelectListItem> Branches { get; set; } = new();
        public List<SelectListItem> Agents { get; set; } = new();
        public string UserRole { get; set; } = string.Empty;
    }
}
