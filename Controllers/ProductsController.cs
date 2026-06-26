using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool IsAdmin()         => SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "products.view")   || IsAdmin();
        private bool CanCreate()       => SessionPermissions.Has(HttpContext.Session, "products.create") || IsAdmin();
        private bool CanEdit()         => SessionPermissions.Has(HttpContext.Session, "products.edit")   || IsAdmin();
        private bool CanDelete()       => SessionPermissions.Has(HttpContext.Session, "products.delete") || IsAdmin();

        private AuditLog BuildLog(string action, string result, string? target = null) => new AuditLog
        {
            UserId    = HttpContext.Session.GetInt32("UserId"),
            Username  = HttpContext.Session.GetString("Username") ?? "",
            Action    = action,
            Target    = target,
            Result    = result,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        private async Task PopulateViewBag(string? search = null)
        {
            ViewBag.Search     = search;
            ViewBag.IsAdmin    = IsAdmin();
            ViewBag.CanCreate  = CanCreate();
            ViewBag.CanEdit    = CanEdit();
            ViewBag.CanDelete  = CanDelete();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            await PopulateViewBag(search);

            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Code.Contains(search) ||
                    (p.Category != null && p.Category.Name.Contains(search)));

            return View(await query.OrderBy(p => p.Name).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create() => RedirectToAction("Index");

        [HttpGet]
        public IActionResult Edit(int id) => RedirectToAction("Index");

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanCreate())       return RedirectToAction("Index");

            if (await _context.Products.AnyAsync(p => p.Code == product.Code))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El código '{product.Code}' ya está registrado.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync());
            }

            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            _context.AuditLogs.Add(BuildLog("CREATE_PRODUCT", "SUCCESS", $"producto: {product.Name}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanEdit())         return RedirectToAction("Index");

            var existing = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

            if (existing == null) return NotFound();

            if (await _context.Products.AnyAsync(p => p.Code == product.Code && p.ProductId != product.ProductId))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El código '{product.Code}' ya está en uso por otro producto.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync());
            }

            product.CreatedAt = existing.CreatedAt;
            _context.Products.Update(product);
            _context.AuditLogs.Add(BuildLog("EDIT_PRODUCT", "SUCCESS", $"producto: {product.Name}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanDelete())       return RedirectToAction("Index");

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.AuditLogs.Add(BuildLog("DELETE_PRODUCT", "SUCCESS", $"producto: {product.Name}"));
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
