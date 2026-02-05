using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null;
        [Required, MinLength(6)]
        public string Password { get; set; } = null;
        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = null!;
        [Required]
        public string Fullname { get; set; }=null!;
        [Required,Phone]
        public string? PhoneNumber { get; set; }
       

    }
}
