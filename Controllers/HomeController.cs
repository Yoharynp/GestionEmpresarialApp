using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GestionEmpresarialApp.Models;

namespace GestionEmpresarialApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("Username");
        var rol = HttpContext.Session.GetString("Role");
        if (string.IsNullOrEmpty(user))
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.Username = user;
        ViewBag.Rol = rol;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
