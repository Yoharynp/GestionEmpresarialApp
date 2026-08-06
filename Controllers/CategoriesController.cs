using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool IsAdmin()         => SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "categories.view")   || IsAdmin();
        private bool CanCreate()       => SessionPermissions.Has(HttpContext.Session, "categories.create") || IsAdmin();
        private bool CanEdit()         => SessionPermissions.Has(HttpContext.Session, "categories.edit")   || IsAdmin();
        private bool CanDelete()       => SessionPermissions.Has(HttpContext.Session, "categories.delete") || IsAdmin();

        private AuditLog BuildLog(string action, string result, string? target = null) => new AuditLog
        {
            UserId    = HttpContext.Session.GetInt32("UserId"),
            Username  = HttpContext.Session.GetString("Username") ?? "",
            Action    = action,
            Target    = target,
            Result    = result,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        private void PopulateViewBag()
        {
            ViewBag.IsAdmin   = IsAdmin();
            ViewBag.CanCreate = CanCreate();
            ViewBag.CanEdit   = CanEdit();
            ViewBag.CanDelete = CanDelete();
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            PopulateViewBag();
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create() => RedirectToAction("Index");

        [HttpGet]
        public IActionResult Edit(int id) => RedirectToAction("Index");

        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanCreate())       return RedirectToAction("Index");

            if (await _context.Categories.AnyAsync(c => c.Name == category.Name))
            {
                PopulateViewBag();
                ViewBag.FormError = $"Ya existe una categoría con el nombre '{category.Name}'.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync());
            }

            _context.Categories.Add(category);
            _context.AuditLogs.Add(BuildLog("CREATE_CATEGORY", "SUCCESS", $"categoría: {category.Name}"));
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanEdit())         return RedirectToAction("Index");

            if (await _context.Categories.AnyAsync(c => c.Name == category.Name && c.CategoryId != category.CategoryId))
            {
                PopulateViewBag();
                ViewBag.FormError = $"Ya existe una categoría con el nombre '{category.Name}'.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync());
            }

            _context.Categories.Update(category);
            _context.AuditLogs.Add(BuildLog("EDIT_CATEGORY", "SUCCESS", $"categoría: {category.Name}"));
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanDelete())       return RedirectToAction("Index");

            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category != null)
            {
                if (category.Products.Any())
                {
                    PopulateViewBag();
                    ViewBag.FormError = $"No se puede eliminar '{category.Name}' porque tiene {category.Products.Count} producto(s) asignado(s).";
                    return View("Index", await _context.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync());
                }
                _context.AuditLogs.Add(BuildLog("DELETE_CATEGORY", "SUCCESS", $"categoría: {category.Name}"));
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
