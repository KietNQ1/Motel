using Microsoft.AspNetCore.Identity;

namespace Motel.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FullName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public virtual Landlord? Landlord { get; set; }

        public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

        public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
