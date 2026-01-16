using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public MaintenanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var list = await _context.MaintenanceRequest
            .Include(m => m.Property)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync();

        return View(list);
    }


    public IActionResult Create(int propertyId)
    {
        var model = new MaintenanceRequest
        {
            PropertyId = propertyId
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceRequest vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = _userManager.GetUserId(User)!;
        vm.UserId = userId;
        vm.CreatedUtc = DateTime.UtcNow;

        _context.MaintenanceRequest.Add(vm);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Zg³oszenia do moich nieruchomoœci 
    public async Task<IActionResult> OwnerIndex()
    {
        var ownerId = _userManager.GetUserId(User)!;

        var list = await _context.MaintenanceRequest
            .Include(m => m.Property)
            .Include(m => m.User)
            .Where(m => m.Property.UserId == ownerId)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync();

        return View(list);
    }

    // Szczegó³y zg³oszenia
    public async Task<IActionResult> Details(int id)
    {
        var req = await _context.MaintenanceRequest
            .Include(m => m.Property)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (req == null) return NotFound();

        return View(req);
    }
}
