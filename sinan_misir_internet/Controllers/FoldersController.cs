using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using sinan_misir_internet.DAL;

namespace sinan_misir_internet.Controllers
{
    public class FoldersController : Controller
    {
        private readonly DosyayonetimdbContext _context;

        public FoldersController(DosyayonetimdbContext context)
        {
            _context = context;
        }

        // GET: Folders
        public async Task<IActionResult> Index()
        {
            var dosyayonetimdbContext = _context.Folders.Include(f => f.ParentFolder).Include(f => f.User);
            return View(await dosyayonetimdbContext.ToListAsync());
        }

        // GET: Folders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders
                .Include(f => f.ParentFolder)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }

        // GET: Folders/Create
        // GET: Folders/Create
        public IActionResult Create()
        {
            ViewData["ParentFolderId"] = new SelectList(_context.Folders, "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }


        // POST: Folders/Create
        // POST: Folders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,UserId,ParentFolderId")] Folder folder)
        {
            if (folder.UserId <= 0)
            {
                ModelState.AddModelError("UserId", "Kullanıcı seçilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(folder.Name))
            {
                ModelState.AddModelError("Name", "Klasör adı zorunludur.");
            }

            if (ModelState.IsValid)
            {
                folder.CreatedAt = DateTime.Now;
                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users.ToList(), "Id", "UserName", folder.UserId);
            ViewData["ParentFolderId"] = new SelectList(_context.Folders, "Id", "Name", folder.ParentFolderId);
            return View(folder);
        }



        // GET: Folders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
            {
                return NotFound();
            }
            ViewData["ParentFolderId"] = new SelectList(_context.Folders, "Id", "Id", folder.ParentFolderId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", folder.UserId);
            return View(folder);
        }

        // POST: Folders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,UserId,ParentFolderId")] Folder folder)
        {
            if (id != folder.Id)
                return NotFound();

            var existingFolder = await _context.Folders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (existingFolder == null)
                return NotFound();

            folder.CreatedAt = existingFolder.CreatedAt; // orijinal zaman korunmalı

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(folder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FolderExists(folder.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ParentFolderId"] = new SelectList(_context.Folders, "Id", "Name", folder.ParentFolderId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", folder.UserId);
            return View(folder);
        }


        // GET: Folders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var folder = await _context.Folders
                .Include(f => f.ParentFolder)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.InverseParentFolder)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                return NotFound();

            // 👇 ALT KLASÖR ve DOSYA kontrolü
            if (folder.Files.Any() || folder.InverseParentFolder.Any())
            {
                TempData["Error"] = "Bu klasörde içerik var, önce silinmelidir.";
                return RedirectToAction(nameof(Index));
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool FolderExists(int id)
        {
            return _context.Folders.Any(e => e.Id == id);
        }
    }
}
