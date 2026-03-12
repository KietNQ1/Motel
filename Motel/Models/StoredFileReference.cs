using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class StoredFileReference
{
    public int StoredFileRefId { get; set; }

    public int StoredFileId { get; set; }

    public string RefType { get; set; } = null!;

    public int RefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StoredFile StoredFile { get; set; } = null!;

    //public virtual RoomFurniture? RoomFurniture { get; set; }
}
