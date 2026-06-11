using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GestionEmpresarialApp.Controllers
{
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "audit.view")   || SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanExport()       => SessionPermissions.Has(HttpContext.Session, "audit.export") || SessionPermissions.IsAdmin(HttpContext.Session);

        public async Task<IActionResult> Index(
            [FromQuery(Name = "action")] string? filterAction,
            [FromQuery(Name = "result")] string? filterResult,
            [FromQuery(Name = "from")]   DateTime? from,
            [FromQuery(Name = "to")]     DateTime? to)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filterAction)) query = query.Where(l => l.Action == filterAction);
            if (!string.IsNullOrEmpty(filterResult)) query = query.Where(l => l.Result == filterResult);
            if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value.ToUniversalTime());
            if (to.HasValue)   query = query.Where(l => l.CreatedAt <= to.Value.ToUniversalTime().AddDays(1));

            var logs = await query.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();

            ViewBag.FilterAction = filterAction;
            ViewBag.FilterResult = filterResult;
            ViewBag.FilterFrom   = from?.ToString("yyyy-MM-dd");
            ViewBag.FilterTo     = to?.ToString("yyyy-MM-dd");

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Export(
            [FromQuery(Name = "action")] string? filterAction,
            [FromQuery(Name = "result")] string? filterResult,
            [FromQuery(Name = "from")]   DateTime? from,
            [FromQuery(Name = "to")]     DateTime? to)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filterAction)) query = query.Where(l => l.Action == filterAction);
            if (!string.IsNullOrEmpty(filterResult)) query = query.Where(l => l.Result == filterResult);
            if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value.ToUniversalTime());
            if (to.HasValue)   query = query.Where(l => l.CreatedAt <= to.Value.ToUniversalTime().AddDays(1));

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Fecha/Hora,Usuario,Accion,Resultado,IP,Detalle");
            foreach (var log in logs)
            {
                sb.AppendLine(string.Join(",",
                    log.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    log.Username,
                    log.Action,
                    log.Result,
                    log.IpAddress ?? "",
                    log.Target ?? ""
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"audit_logs_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
