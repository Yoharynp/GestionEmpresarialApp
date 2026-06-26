using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Data;
using GestionEmpresarialApp.Models;
using GestionEmpresarialApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarialApp.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated() => SessionPermissions.IsAuthenticated(HttpContext.Session);
        private bool IsAdmin()         => SessionPermissions.IsAdmin(HttpContext.Session);
        private bool CanView()         => SessionPermissions.Has(HttpContext.Session, "clients.view")   || IsAdmin();
        private bool CanCreate()       => SessionPermissions.Has(HttpContext.Session, "clients.create") || IsAdmin();
        private bool CanEdit()         => SessionPermissions.Has(HttpContext.Session, "clients.edit")   || IsAdmin();
        private bool CanDelete()       => SessionPermissions.Has(HttpContext.Session, "clients.delete") || IsAdmin();

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
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanView())         return RedirectToAction("Index", "Home");

            await PopulateViewBag(search);

            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c =>
                    c.FirstName.Contains(search) ||
                    c.LastName.Contains(search)  ||
                    (c.Email   != null && c.Email.Contains(search)) ||
                    (c.Phone   != null && c.Phone.Contains(search)));

            return View(await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create() => RedirectToAction("Index");

        [HttpGet]
        public IActionResult Edit(int id) => RedirectToAction("Index");

        [HttpPost]
        public async Task<IActionResult> Create(Client client)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanCreate())       return RedirectToAction("Index");

            if (!string.IsNullOrWhiteSpace(client.Email) &&
                await _context.Clients.AnyAsync(c => c.Email == client.Email))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El email '{client.Email}' ya está registrado.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Clients.OrderBy(c => c.LastName).ToListAsync());
            }

            client.CreatedAt = DateTime.UtcNow;
            _context.Clients.Add(client);
            _context.AuditLogs.Add(BuildLog("CREATE_CLIENT", "SUCCESS", $"cliente: {client.FirstName} {client.LastName}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Client client)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanEdit())         return RedirectToAction("Index");

            var existing = await _context.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            if (existing == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(client.Email) &&
                await _context.Clients.AnyAsync(c => c.Email == client.Email && c.ClientId != client.ClientId))
            {
                await PopulateViewBag();
                ViewBag.FormError = $"El email '{client.Email}' ya está registrado en otro cliente.";
                ViewBag.ShowForm  = true;
                return View("Index", await _context.Clients.OrderBy(c => c.LastName).ToListAsync());
            }

            client.CreatedAt = existing.CreatedAt;
            _context.Clients.Update(client);
            _context.AuditLogs.Add(BuildLog("EDIT_CLIENT", "SUCCESS", $"cliente: {client.FirstName} {client.LastName}"));
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!CanDelete())       return RedirectToAction("Index");

            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.AuditLogs.Add(BuildLog("DELETE_CLIENT", "SUCCESS", $"cliente: {client.FirstName} {client.LastName}"));
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
