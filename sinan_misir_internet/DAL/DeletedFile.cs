using System;
using System.Collections.Generic;

namespace sinan_misir_internet.DAL;

public partial class DeletedFile
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public DateTime DeletedAt { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
