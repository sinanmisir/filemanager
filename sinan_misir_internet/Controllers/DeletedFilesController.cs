using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sinan_misir_internet.DAL;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace sinan_misir_internet.Controllers
{
    public class DeletedFilesController : Controller
    {
        private readonly DosyayonetimdbContext _context;

        public DeletedFilesController(DosyayonetimdbContext context)
        {
            _context = context;
        }

        // GET: DeletedFiles
        public async Task<IActionResult> Index()
        {
            var deletedFiles = await _context.DeletedFiles
                .Include(f => f.User)
                .OrderByDescending(f => f.DeletedAt)
                .ToListAsync();

            return View(deletedFiles);
        }

        // POST: DeletedFiles/Restore/5
        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var deletedFile = await _context.DeletedFiles
                .FirstOrDefaultAsync(f => f.Id == id);

            if (deletedFile == null)
                return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", deletedFile.FilePath.TrimStart('/'));

            long fileSize = 0;
            if (System.IO.File.Exists(fullPath))
            {
                fileSize = new FileInfo(fullPath).Length;
            }
            else
            {
                // İsteğe bağlı: Kullanıcıya uyarı göstermek istersen
                TempData["Error"] = "Fiziksel dosya bulunamadı, ancak kayıt geri yüklendi.";
            }

            var restoredFile = new sinan_misir_internet.DAL.File
            {
                FileName = deletedFile.FileName,
                FilePath = deletedFile.FilePath,
                Size = fileSize,
                UploadedAt = DateTime.Now,
                FolderId = null, // ya da varsayılan klasöre atanabilir
                UserId = deletedFile.UserId
            };

            _context.Files.Add(restoredFile);
            _context.DeletedFiles.Remove(deletedFile);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // POST: DeletedFiles/DeletePermanent/5
        [HttpPost]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            var deletedFile = await _context.DeletedFiles.FindAsync(id);
            if (deletedFile == null)
                return NotFound();

            // Fiziksel dosyayı da silelim:
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", deletedFile.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.DeletedFiles.Remove(deletedFile);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
