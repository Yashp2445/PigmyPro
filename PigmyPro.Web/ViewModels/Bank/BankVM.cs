using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PigmyPro.Web.ViewModels.Bank
{
    public class BankCreateEditVM
    {
        public int BankID { get; set; }

        [Required(ErrorMessage = "Bank Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4, ErrorMessage = "Prefix must be at most 4 characters")]
        [Display(Name = "App Login Prefix")]
        public string? AppLoginPrefix { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(15)]
        [Display(Name = "Contact No")]
        public string? ContactNo { get; set; }

        [StringLength(50)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [StringLength(50)]
        [Display(Name = "Email ID")]
        public string? EmailID { get; set; }

        public bool ActiveYN { get; set; } = true;
        public bool HasCBS { get; set; }

        [Display(Name = "No of Holidays")]
        public int? No_of_Holidays { get; set; }

        public bool IsPigmy { get; set; } = true; 
        public bool IsLoan { get; set; }
        public bool IsRecurring { get; set; }

        public long SelectedCollectionGLCode { get; set; } = 1;

        [Display(Name = "Bank Logo")]
        public IFormFile? LogoFile { get; set; }

        public bool HasExistingLogo { get; set; }

        public bool RemoveLogo { get; set; }
    }

    public class BankListVM
    {
        public int BankID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ActiveYN { get; set; }
        public bool HasCBS { get; set; }
        public bool HasLogo { get; set; }

        public int DependentBranchCount { get; set; }
        public bool CanDelete => DependentBranchCount == 0;
    }
}