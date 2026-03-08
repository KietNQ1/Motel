using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.User
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string FullName { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsLocked { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }
    }
}
