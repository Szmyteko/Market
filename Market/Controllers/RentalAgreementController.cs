using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

public class RentalAgreementController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public RentalAgreementController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Lista wszystkich umów (np. dla admina)
    public async Task<IActionResult> Index()
    {
        var agreements = await _context.RentalAgreement
            .Include(r => r.Property)
            .Include(r => r.Tenant)
            .Include(r => r.User) // właściciel
            .ToListAsync();

        return View(agreements);
    }

    // GET: formularz do wynajęcia lokalu
    public async Task<IActionResult> Rent(int id)
    {
        var property = await _context.Property.FindAsync(id);
        if (property == null || !property.IsAvailable)
        {
            return BadRequest("Lokal jest niedostępny.");
        }

        var model = new RentalAgreement
        {
            PropertyId = property.Id,
            Property = property
        };

        return View(model);
    }

    // POST: zapisanie nowej umowy najmu
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rent(RentalAgreement rentalAgreement)
    {
        var property = await _context.Property.FindAsync(rentalAgreement.PropertyId);
        if (property == null || !property.IsAvailable)
            return BadRequest("Lokal jest niedostępny.");

        if (property.ApprovalStatus != ListingApprovalStatus.Approved)
            return BadRequest("Lokal oczekuje na akceptację administratora.");

        var userId = _userManager.GetUserId(User); // najemca = zalogowany użytkownik

        var newRentalAgreement = new RentalAgreement
        {
            PropertyId = rentalAgreement.PropertyId,
            TenantId = userId,
            StartDate = rentalAgreement.StartDate == default
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : rentalAgreement.StartDate,
            EndDate = rentalAgreement.EndDate,
            MonthlyRent = property.RentPrice ?? 0,
            UserId = property.UserId // właściciel lokalu
        };

        property.IsAvailable = false; // lokal zajęty

        _context.RentalAgreement.Add(newRentalAgreement);
        await _context.SaveChangesAsync();

        // przekierowanie do listy najemów zalogowanego użytkownika
        return RedirectToAction("MyRentals", "RentalRequest");
    }
    [Authorize]
    public async Task<IActionResult> ArchivedRentals()
    {
        var userId = _userManager.GetUserId(User);

        var archivedRentals = await _context.RentalAgreement
            .Include(r => r.Property)
            .Include(r => r.User) // właściciel
            .Where(r => r.TenantId == userId &&
                       (r.EndDate != null || r.Property.IsDeleted))
            .ToListAsync();

        return View(archivedRentals);
    }
}
