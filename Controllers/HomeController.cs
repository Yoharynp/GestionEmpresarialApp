using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;

namespace GestionEmpresarialApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = HttpContext.Session.GetString("Username");
        var rol  = HttpContext.Session.GetString("Role");
        if (string.IsNullOrEmpty(user))
            return RedirectToAction("Login", "Account");

        var today = DateTime.UtcNow.Date;

        ViewBag.Username     = user;
        ViewBag.Rol          = rol;
        ViewBag.TotalUsers   = await _context.Usuarios.CountAsync();
        ViewBag.TotalRoles   = await _context.Roles.CountAsync();
        ViewBag.AccessesToday = await _context.AuditLogs
            .CountAsync(l => l.Action == "LOGIN" && l.Result == "SUCCESS" && l.CreatedAt >= today);
        ViewBag.FailedToday  = await _context.AuditLogs
            .CountAsync(l => l.Result == "FAILED" && l.CreatedAt >= today);
        ViewBag.RecentLogs   = await _context.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
