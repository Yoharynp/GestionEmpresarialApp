using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "roles.view") || SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanEdit()         => SessionPermissions.Has(HttpContext.Session, "roles.edit") || SessionPermissions.IsAdmin(HttpContext.Session);

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            var roles = await _context.Roles
                .Include(r => r.Usuarios)
                .Include(r => r.RolePermissions)
                .ToListAsync();

            ViewBag.CanEdit  = CanEdit();
            ViewBag.IsAdmin  = SessionPermissions.IsAdmin(HttpContext.Session);
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions(int rolId, List<string>? permissions)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanEdit())         return RedirectToAction("Index", "Home");

            var targetRol = await _context.Roles.FindAsync(rolId);
            if (targetRol?.NombreRol == "Administrador" && !SessionPermissions.IsAdmin(HttpContext.Session))
                return RedirectToAction("Index");

            var existing = _context.RolePermissions.Where(rp => rp.RolId == rolId);
            _context.RolePermissions.RemoveRange(existing);

            foreach (var perm in permissions ?? new List<string>())
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RolId      = rolId,
                    Permission = perm
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
