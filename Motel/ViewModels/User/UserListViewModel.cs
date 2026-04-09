using Motel.Models;

namespace Motel.ViewModels.User
{
    public class UserListViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();

        public string? Search { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int PageSize { get; set; } = 10;
    }
}