using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool IsAdmin()         => SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "users.view")   || IsAdmin();
        private bool CanCreate()       => SessionPermissions.Has(HttpContext.Session, "users.create") || IsAdmin();
        private bool CanEdit()         => SessionPermissions.Has(HttpContext.Session, "users.edit")   || IsAdmin();
        private bool CanDelete()       => SessionPermissions.Has(HttpContext.Session, "users.delete") || IsAdmin();

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
            ViewBag.Search    = search;
            ViewBag.IsAdmin   = IsAdmin();
            ViewBag.CanCreate = CanCreate();
            ViewBag.CanEdit   = CanEdit();
            ViewBag.CanDelete = CanDelete();
            ViewBag.Roles     = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            await PopulateViewBag(search);

            var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    u.Username.Contains(search) ||
                    (u.FullName != null && u.FullName.Contains(search)) ||
                    (u.Email    != null && u.Email.Contains(search))    ||
                    u.Rol.NombreRol.Contains(search));

            return View(await query.OrderBy(u => u.Username).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create() => RedirectToAction("Index");

        [HttpGet]
        public IActionResult Edit(int id) => RedirectToAction("Index");

        [HttpPost]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanCreate())       return RedirectToAction("Index");

            if (await _context.Usuarios.AnyAsync(u => u.Username == usuario.Username))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El usuario '{usuario.Username}' ya existe.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Usuarios.Include(u => u.Rol).OrderBy(u => u.Username).ToListAsync());
            }

            if (string.IsNullOrWhiteSpace(usuario.Password))
            {
                await PopulateViewBag();
                ViewBag.FormError = "La contraseña es requerida.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Usuarios.Include(u => u.Rol).OrderBy(u => u.Username).ToListAsync());
            }

            usuario.CreatedAt = DateTime.UtcNow;
            _context.Usuarios.Add(usuario);
            _context.AuditLogs.Add(BuildLog("CREATE_USER", "SUCCESS", $"usuario: {usuario.Username}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Usuario usuario)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanEdit())         return RedirectToAction("Index");

            var existing = await _context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);

            if (existing == null) return NotFound();

            if (await _context.Usuarios.AnyAsync(u => u.Username == usuario.Username && u.UsuarioId != usuario.UsuarioId))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El usuario '{usuario.Username}' ya existe.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Usuarios.Include(u => u.Rol).OrderBy(u => u.Username).ToListAsync());
            }

            if (string.IsNullOrWhiteSpace(usuario.Password))
                usuario.Password = existing.Password;

            usuario.CreatedAt = existing.CreatedAt;

            _context.Usuarios.Update(usuario);
            _context.AuditLogs.Add(BuildLog("EDIT_USER", "SUCCESS", $"usuario: {existing.Username}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanDelete())       return RedirectToAction("Index");

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.AuditLogs.Add(BuildLog("DELETE_USER", "SUCCESS", $"usuario: {usuario.Username}"));
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
