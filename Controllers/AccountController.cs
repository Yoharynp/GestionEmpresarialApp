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

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Primero vamos a validar campos vacios para tener un codigo que sirva
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor, ingrese su nombre de usuario y contraseña.";
                return View();
            }

            // Luego vamos a implementar un control de intentos pero básico
            int intentos = HttpContext.Session.GetInt32("Intentos_" + username) ?? 0;
            if (intentos >= 3)
            {
                ViewBag.Error = "Has superado el número máximo de intentos. Por favor, inténtalo de nuevo más tarde.";
                return View();
            }

            // Validamos el usuario desde la base de datos, y pasamos su rol tambien
            var usuarioDb = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (usuarioDb != null)
            {
                HttpContext.Session.Remove("Intentos_" + username);

                HttpContext.Session.SetString("Username", usuarioDb.Username);
                HttpContext.Session.SetString("Role", usuarioDb.Rol.NombreRol);

                return RedirectToAction("Index", "Home");
            } else
            {
                intentos++;
                HttpContext.Session.SetInt32("Intentos_" + username, intentos);

                ViewBag.Error = "Nombre de usuario o contraseña incorrectos. Intentos restantes: " + (3 - intentos);
                return View();
            }
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}