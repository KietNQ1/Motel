using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Motel.ViewModels.Contract
{
    public class ContractCreateViewModel
    {
        [Required]
        public int RoomId { get; set; }

        public int PropertyId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn người thuê chính")]
        public int TenantId { get; set; } // người đại diện trong Contract

        [Required]
        [Range(0, 999999999999)]
        public decimal DepositAmount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required]
        [DataType(DataType.Date)]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(12));

        // ở ghép: chọn thêm người ở
        public List<int> OccupantTenantIds { get; set; } = new();

        // hiển thị dropdown/checkbox
        public List<SelectListItem> TenantOptions { get; set; } = new();

        // để validate UI
        public int MaxOccupants { get; set; }
    }
}