using Market.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

public class TenantController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TenantController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [Authorize(Roles = "Najemca, Admin")]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        // Pobierz wszystkie wynajmy dla użytkownika
        var rentalAgreements = await _context.RentalAgreement
            .Include(ra => ra.Property)
            .Where(ra => ra.Tenant.UserId == userId)
            .ToListAsync();

        return View(rentalAgreements); // Przekazujemy listę RentalAgreement
    }


}