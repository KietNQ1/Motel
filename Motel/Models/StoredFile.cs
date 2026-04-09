using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class StoredFile
{
    public int StoredFileId { get; set; }

    public int LandlordId { get; set; }

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public string StoragePath { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public int UploadedByUserId { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual ICollection<StoredFileReference> StoredFileReferences { get; set; } = new List<StoredFileReference>();

    public virtual ApplicationUser UploadedByUser { get; set; } = null!;
}
