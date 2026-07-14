using System.ComponentModel.DataAnnotations;

namespace PigmyPro.Web.ViewModels.Auth
{
    public class ChangePasswordVM
    {
        [Required]
        [DataType(DataType.Password)]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a reason for the password change.")]
        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
