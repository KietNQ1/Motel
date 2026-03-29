namespace Motel.ViewModels.Tenant
{
    public class CT01ViewModel
    {
        public int TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string IdentityNo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PermanentAddress { get; set; } = string.Empty;
        
        public string LandlordFullName { get; set; } = string.Empty;
        public string LandlordIdentityNo { get; set; } = string.Empty;
        
        public string CurrentAddress { get; set; } = string.Empty;
    }
}
