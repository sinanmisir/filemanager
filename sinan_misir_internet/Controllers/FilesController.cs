using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sinan_misir_internet.DAL;

namespace sinan_misir_internet.Controllers
{
    public class FilesController : Controller
    {
        private readonly DosyayonetimdbContext _context;

        public FilesController(DosyayonetimdbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var files = _context.Files
                .Include(f => f.Folder)
                .Include(f => f.User);
            return View(await files.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var file = await _context.Files
                .Include(f => f.Folder)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (file == null) return NotFound();

            return View(file);
        }

        // GET: Files/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["FolderId"] = new SelectList(_context.Folders, "Id", "Name");
            return View();
        }


        // POST: Files/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile uploadedFile, int? FolderId, int UserId)
        {
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = Path.GetFileName(uploadedFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                var newFile = new sinan_misir_internet.DAL.File
                {
                    FileName = fileName,
                    FilePath = "/uploads/" + fileName,
                    Size = uploadedFile.Length,
                    UploadedAt = DateTime.Now,
                    FolderId = FolderId,
                    UserId = UserId
                };

                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["FolderId"] = new SelectList(_context.Folders, "Id", "Name", FolderId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", UserId);
            ModelState.AddModelError("", "Dosya seçilmedi.");
            return View();
        }



        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var file = await _context.Files.FindAsync(id);
            if (file == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", file.UserId);
            ViewData["FolderId"] = new SelectList(_context.Folders, "Id", "Name", file.FolderId);
            return View(file);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? newFile, int UserId, int? FolderId)
        {
            var existingFile = await _context.Files.FindAsync(id);
            if (existingFile == null)
                return NotFound();

            if (newFile != null && newFile.Length > 0)
            {
                // Eski dosya varsa sil
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingFile.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                // Yeni dosyayı yükle
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var newFileName = Path.GetFileName(newFile.FileName);
                var newPath = Path.Combine(uploadsPath, newFileName);

                using (var stream = new FileStream(newPath, FileMode.Create))
                {
                    await newFile.CopyToAsync(stream);
                }

                // Dosya bilgilerini güncelle
                existingFile.FileName = newFileName;
                existingFile.FilePath = "/uploads/" + newFileName;
                existingFile.Size = newFile.Length;
                existingFile.UploadedAt = DateTime.Now;
            }

            // Diğer alanları güncelle
            existingFile.UserId = UserId;
            existingFile.FolderId = FolderId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Files.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var file = await _context.Files
                .Include(f => f.Folder)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (file == null) return NotFound();

            return View(file);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var file = await _context.Files
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file != null)
            {
                // 1. Silinen dosyayı DeletedFiles tablosuna ekle
                var deletedFile = new sinan_misir_internet.DAL.DeletedFile
                {
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    DeletedAt = DateTime.Now,
                    UserId = file.UserId
                };
                _context.DeletedFiles.Add(deletedFile);

                // 2. Asıl dosyayı sistemden kaldır
                _context.Files.Remove(file);

                // 3. Fiziksel dosyayı sil (isteğe bağlı)
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool FileExists(int id)
        {
            return _context.Files.Any(e => e.Id == id);
        }
    }
}
