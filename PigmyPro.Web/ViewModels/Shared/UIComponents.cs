using System;

namespace PigmyPro.Web.ViewModels.Shared
{
    public class StatCardVM
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        
        // Variants: default, brand, success, warning, info, premium-amber, premium-emerald
        public string Variant { get; set; } = "default";
        
        // If true, the card uses a more compact vertical rhythm
        public bool IsCompact { get; set; } = false;
    }

    public class PageHeaderVM
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        
        // HTML string for action buttons (e.g. Refresh Data, Create New)
        public string ActionsHtml { get; set; } = string.Empty;
    }
}
