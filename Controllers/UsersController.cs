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

        private async Task PopulateViewBag()
        {
            ViewBag.IsAdmin   = IsAdmin();
            ViewBag.CanCreate = CanCreate();
            ViewBag.CanEdit   = CanEdit();
            ViewBag.CanDelete = CanDelete();
            ViewBag.Roles     = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            await PopulateViewBag();

            var users = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Username)
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Report()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            var users = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Username)
                .ToListAsync();

            ViewBag.GeneratedAt = DateTime.Now;
            ViewBag.GeneratedBy = HttpContext.Session.GetString("Username") ?? "";
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            var users = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Username)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Usuario,Nombre Completo,Email,Rol,Estado,Fecha Registro");
            foreach (var u in users)
            {
                sb.AppendLine(string.Join(",",
                    CsvField(u.Username),
                    CsvField(u.FullName),
                    CsvField(u.Email),
                    CsvField(u.Rol?.NombreRol),
                    CsvField(u.Status),
                    CsvField(u.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"))));
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var filename = $"usuarios_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

        private static string CsvField(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
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
