using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
       public string? Email { get; set; }
    }
}
