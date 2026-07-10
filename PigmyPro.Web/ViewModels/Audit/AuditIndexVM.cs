using System;
using System.Collections.Generic;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Audit
{
    public class AuditIndexVM
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public IEnumerable<AuditLogRow> Logs { get; set; } = new List<AuditLogRow>();
    }
}
