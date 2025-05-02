using System;
using System.Collections.Generic;

namespace sinan_misir_internet.DAL;

public partial class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DeletedFile> DeletedFiles { get; set; } = new List<DeletedFile>();

    public virtual ICollection<File> Files { get; set; } = new List<File>();

    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
}
