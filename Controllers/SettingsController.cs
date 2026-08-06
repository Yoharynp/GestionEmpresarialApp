using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Helpers;
using GestionEmpresarialApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool IsAdmin()         => SessionPermissions.IsAdmin(HttpContext.Session);

        private AuditLog BuildLog(string action, string result, string? target = null) => new AuditLog
        {
            UserId    = HttpContext.Session.GetInt32("UserId"),
            Username  = HttpContext.Session.GetString("Username") ?? "",
            Action    = action,
            Target    = target,
            Result    = result,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())         return RedirectToAction("Index", "Home");

            var settings = await _context.Settings.OrderBy(s => s.Key).ToListAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(string key, string value)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())         return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return BadRequest();

            var setting = await _context.Settings.FindAsync(key);
            if (setting == null) return NotFound();

            var oldValue = setting.Value;
            setting.Value     = value.Trim();
            setting.UpdatedAt = DateTime.UtcNow;

            _context.Settings.Update(setting);
            _context.AuditLogs.Add(BuildLog("EDIT_SETTING", "SUCCESS",
                $"clave: {key}, valor: {oldValue} → {value.Trim()}"));
            await _context.SaveChangesAsync();

            TempData["SettingsSaved"] = "true";
            return RedirectToAction("Index");
        }
    }
}
