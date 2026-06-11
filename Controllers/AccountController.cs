using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Username") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor, ingrese su nombre de usuario y contraseña.";
                return View();
            }

            int intentos = HttpContext.Session.GetInt32("Intentos_" + username) ?? 0;
            if (intentos >= 3)
            {
                ViewBag.Error = "Has superado el número máximo de intentos. Inténtalo más tarde.";
                return View();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var usuarioDb = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (usuarioDb != null)
            {
                var perms = await _context.RolePermissions
                    .Where(rp => rp.RolId == usuarioDb.RolId)
                    .Select(rp => rp.Permission)
                    .ToListAsync();

                HttpContext.Session.Remove("Intentos_" + username);
                HttpContext.Session.SetString("Username", usuarioDb.Username);
                HttpContext.Session.SetString("Role", usuarioDb.Rol.NombreRol);
                HttpContext.Session.SetInt32("UserId", usuarioDb.UsuarioId);
                HttpContext.Session.SetString("Permissions", string.Join(",", perms));

                usuarioDb.LastLoginAt = DateTime.UtcNow;
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId    = usuarioDb.UsuarioId,
                    Username  = usuarioDb.Username,
                    Action    = "LOGIN",
                    Result    = "SUCCESS",
                    IpAddress = ip
                });
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            intentos++;
            HttpContext.Session.SetInt32("Intentos_" + username, intentos);

            _context.AuditLogs.Add(new AuditLog
            {
                Username  = username,
                Action    = "LOGIN_FAILED",
                Result    = "FAILED",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            ViewBag.Error    = $"Credenciales incorrectas. Intentos restantes: {3 - intentos}";
            ViewBag.Username = username;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            var userId   = HttpContext.Session.GetInt32("UserId");

            if (username != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId    = userId,
                    Username  = username,
                    Action    = "LOGOUT",
                    Result    = "SUCCESS",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
