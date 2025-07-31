using Microsoft.AspNetCore.Mvc;

namespace   Market.Controllers;

public class MaintenanceController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}