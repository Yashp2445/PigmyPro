using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.ViewModels.User
{
    public class UserListVM
    {
        public int UserID { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? BranchName { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserCreateEditVM
    {
        public int UserID { get; set; }
        public int BankID { get; set; }

        public bool IsSuperAdmin { get; set; }

        public bool IsBranchAdmin => Role == "BranchAdmin";

        public IEnumerable<SelectListItem> FilteredRoles =>
            IsSuperAdmin
                ? Roles
                : Roles.Where(r => r.Value == "BranchAdmin");

        [Display(Name = "Bank")]
        public int? SelectedBankID { get; set; } 

        [Display(Name = "Assigned Branch")]
        public int? BranchID { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = "BranchAdmin";

        public string? Code { get; set; }
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<SelectListItem>? BranchList { get; set; }
        public IEnumerable<SelectListItem>? BankList { get; set; } 


        public List<SelectListItem> Roles { get; set; } = new()
    {
        new SelectListItem { Value = "SuperAdmin", Text = "Super Admin" },
        new SelectListItem { Value = "BankAdmin", Text = "Bank Admin" },
        new SelectListItem { Value = "BranchAdmin", Text = "Branch Admin" }
    };
    }
}
