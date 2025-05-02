using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sinan_misir_internet.DAL;
using System.Security.Claims;

namespace sinan_misir_internet.Controllers
{
    [Authorize]
    public class FileManagerController : Controller
    {
        private readonly DosyayonetimdbContext _context;

        public FileManagerController(DosyayonetimdbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? folderId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var folders = await _context.Folders
                .Where(f => f.ParentFolderId == folderId && f.UserId == userId)
                .ToListAsync();

            var files = await _context.Files
                .Where(f => f.FolderId == folderId && f.UserId == userId)
                .ToListAsync();

            ViewBag.CurrentFolderId = folderId;
            ViewBag.Folders = folders;

            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int? folderId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (file != null && file.Length > 0 && userId != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newFile = new sinan_misir_internet.DAL.File
                {
                    FileName = file.FileName,
                    FilePath = "/uploads/" + file.FileName,
                    Size = file.Length,
                    UploadedAt = DateTime.Now,
                    FolderId = folderId,
                    UserId = userId
                };

                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { folderId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder(string folderName, int? parentFolderId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!string.IsNullOrWhiteSpace(folderName) && userId != null)
            {
                var folder = new Folder
                {
                    Name = folderName,
                    CreatedAt = DateTime.Now,
                    ParentFolderId = parentFolderId,
                    UserId = userId
                };

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { folderId = parentFolderId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int id, int? folderId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (file != null)
            {
                var deletedFile = new DeletedFile
                {
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    DeletedAt = DateTime.Now,
                    UserId = file.UserId
                };

                _context.DeletedFiles.Add(deletedFile);
                _context.Files.Remove(file);

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { folderId });
        }

        public async Task<IActionResult> RecycleBin()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var expiredFiles = await _context.DeletedFiles
                .Where(f => f.DeletedAt < DateTime.Now.AddDays(-30) && f.UserId == userId)
                .ToListAsync();

            foreach (var file in expiredFiles)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.DeletedFiles.Remove(file);
            }

            await _context.SaveChangesAsync();

            // 🟢 DÜZELTİLMİŞ KISIM BURASI
            var deletedFiles = await _context.DeletedFiles
                .Where(f => f.UserId == userId)
                .Include(f => f.User) // <-- Burası eksikti
                .OrderByDescending(f => f.DeletedAt)
                .ToListAsync();

            return View(deletedFiles);
        }


        [HttpPost]
        public async Task<IActionResult> RestoreFile(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var deletedFile = await _context.DeletedFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (deletedFile != null)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", deletedFile.FilePath.TrimStart('/'));
                long size = System.IO.File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

                var restoredFile = new sinan_misir_internet.DAL.File
                {
                    FileName = deletedFile.FileName,
                    FilePath = deletedFile.FilePath,
                    Size = size,
                    UploadedAt = DateTime.Now,
                    FolderId = null,
                    UserId = deletedFile.UserId
                };

                _context.Files.Add(restoredFile);
                _context.DeletedFiles.Remove(deletedFile);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("RecycleBin");
        }

        [HttpPost]
        public async Task<IActionResult> PermanentlyDeleteFile(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var deletedFile = await _context.DeletedFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (deletedFile != null)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", deletedFile.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.DeletedFiles.Remove(deletedFile);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("RecycleBin");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var folder = await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.InverseParentFolder)
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (folder != null)
            {
                if (folder.Files.Any() || folder.InverseParentFolder.Any())
                {
                    TempData["Error"] = "Klasör boş değil, önce içindekileri silmelisiniz.";
                }
                else
                {
                    _context.Folders.Remove(folder);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index", new { folderId = folder?.ParentFolderId });
        }

        [HttpGet]
        public async Task<IActionResult> MoveFolder(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (folder == null)
                return NotFound();

            var allFolders = await _context.Folders
                .Where(f => f.UserId == userId)
                .ToListAsync();

            var invalidIds = GetAllDescendantIds(folder, allFolders).Append(folder.Id).ToList();
            var validFolders = allFolders.Where(f => !invalidIds.Contains(f.Id)).ToList();

            ViewBag.FolderToMove = folder;
            ViewBag.FolderList = validFolders;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MoveFolder(int id, int? newParentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (folder != null)
            {
                folder.ParentFolderId = newParentId;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { folderId = newParentId });
        }

        private List<int> GetAllDescendantIds(Folder parent, List<Folder> allFolders)
        {
            var result = new List<int>();
            var children = allFolders.Where(f => f.ParentFolderId == parent.Id).ToList();
            foreach (var child in children)
            {
                result.Add(child.Id);
                result.AddRange(GetAllDescendantIds(child, allFolders));
            }
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null)
                return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
            var mimeType = GetMimeType(file.FileName);
            var fileName = Path.GetFileName(file.FileName);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, mimeType, file.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null)
                return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
            var mimeType = GetMimeType(file.FileName);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, mimeType);
        }

        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

    }
}
