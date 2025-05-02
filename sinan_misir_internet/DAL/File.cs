using System;
using System.Collections.Generic;

namespace sinan_misir_internet.DAL;

public partial class File
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public long Size { get; set; }

    public DateTime UploadedAt { get; set; }

    public int UserId { get; set; }

    public int? FolderId { get; set; }

    public virtual Folder? Folder { get; set; }

    public virtual User User { get; set; } = null!;
}
