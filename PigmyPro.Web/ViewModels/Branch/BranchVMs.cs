using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PigmyPro.Web.ViewModels.Branch
{
    public class BranchListVM
    {
        public int BranchID { get; set; }
        public int BankID { get; set; }
        public string? Name { get; set; }
        public string? IsActive { get; set; }
        public DateTime EntryDate { get; set; }

        public bool CanDelete { get; set; } = true;
        public string? DependencyMessage { get; set; }
    }

    public class BranchCreateEditVM
    {
        public int BranchID { get; set; }

        public int BankID { get; set; }

        [Required(ErrorMessage = "Branch name is required")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Branch Name")]
        public string? Name { get; set; }

        [Required]
        public string Active { get; set; } = "Y";

        public List<SelectListItem>? BankList { get; set; }

        public bool IsSuperAdmin { get; set; }
    }
}
