using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Account
{
    public class ResetPasswordViewModel
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string token {  get; set; }
        [Required,MinLength(6)]
        public string password { get; set; }
        [Required,Compare(nameof(password))]
        public string ConfirmPassword { get; set; }
    }
}
