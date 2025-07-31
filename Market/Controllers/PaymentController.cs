using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public PaymentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(); // Użytkownik nie istnieje, przekieruj do logowania
        }
        var userRoles = await _userManager.GetRolesAsync(user);

        if (userRoles.Contains("Admin"))
        {
            // Admin: wszystkie aktywne wynajmy + historia płatności
            var activeRentals = await _context.RentalAgreement
                .Include(ra => ra.Property)
                .Include(ra => ra.Tenant)
                .Where(ra => ra.EndDate == null || ra.EndDate >= DateOnly.FromDateTime(DateTime.Now))
                .ToListAsync();

            // Poprawione zapytanie, aby załadować wszystkie potrzebne dane
            var allPayments = await _context.Payment
                .Include(p => p.Tenant) // Dla @payment.Tenant.FullName
                .Include(p => p.User)   // Dla @payment.User?.UserName
                .Include(p => p.RentalAgreement)
                    .ThenInclude(ra => ra.Property)
                        .ThenInclude(p => p.User) // Dla @payment.RentalAgreement.Property.User.UserName
                .ToListAsync();

            ViewBag.ActiveRentals = activeRentals;
            ViewBag.AllPayments = allPayments; // Możesz to zostawić, jeśli druga tabela tego wymaga

            // --- GŁÓWNA POPRAWKA ---
            // Przekazujemy listę płatności jako model do widoku
            return View("AdminIndex", allPayments);
        }

        if (userRoles.Contains("Najemca"))
        {
            // Najemca: szczegóły wynajmu + możliwość dokonania płatności
            var tenantAgreements = await _context.RentalAgreement
                .Include(ra => ra.Property)
                .Where(ra => ra.UserId == user.Id)
                .ToListAsync();

            var tenantPayments = await _context.Payment
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            ViewBag.TenantAgreements = tenantAgreements;
            ViewBag.TenantPayments = tenantPayments;

            return View("TenantIndex");
        }

        if (userRoles.Contains("Wynajmujący"))
        {
            // Wynajmujący: lista lokali + historia wpływów
            var ownerProperties = await _context.Property
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            var ownerIncome = await _context.Payment
                .Include(p => p.RentalAgreement)
                .Where(p => p.RentalAgreement.Property.UserId == user.Id)
                .ToListAsync();

            ViewBag.OwnerProperties = ownerProperties;
            ViewBag.OwnerIncome = ownerIncome;

            return View("OwnerIndex");
        }

        return Unauthorized(); // Jeśli użytkownik nie ma odpowiedniej roli
    }

    [Authorize(Roles = "Najemca")]
    public async Task<IActionResult> Pay(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        var rentalAgreement = await _context.RentalAgreement
            .Include(ra => ra.Property)
            .FirstOrDefaultAsync(ra => ra.Id == id && ra.UserId == user.Id);

        if (rentalAgreement == null)
        {
            return NotFound("Umowa najmu nie została znaleziona lub nie masz do niej dostępu.");
        }

        ViewBag.RentalAgreement = rentalAgreement;

        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Najemca")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int rentalAgreementId, int amount, DateTime paymentDate)
    {
        var user = await _userManager.GetUserAsync(User);

        var rentalAgreement = await _context.RentalAgreement
            .FirstOrDefaultAsync(ra => ra.Id == rentalAgreementId && ra.UserId == user.Id);

        if (rentalAgreement == null)
        {
            return NotFound("Umowa najmu nie została znaleziona lub nie masz do niej dostępu.");
        }

        var payment = new Payment
        {
            RentalAgreementId = rentalAgreementId,
            Amount = amount,
            Date = DateOnly.FromDateTime(paymentDate),
            Status = "Opłacona",
            UserId = user.Id
        };

        _context.Payment.Add(payment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Płatność została dokonana.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Wynajmujący")]
    public async Task<IActionResult> Remind(int propertyId)
    {
        var user = await _userManager.GetUserAsync(User);

        var property = await _context.Property
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == user.Id);

        if (property == null)
        {
            return NotFound("Lokal nie został znaleziony lub nie jesteś jego właścicielem.");
        }

        var rentalAgreement = await _context.RentalAgreement
            .Include(ra => ra.Tenant)
            .FirstOrDefaultAsync(ra => ra.PropertyId == propertyId && (ra.EndDate == null || ra.EndDate >= DateOnly.FromDateTime(DateTime.Now)));

        if (rentalAgreement == null)
        {
            return NotFound("Brak aktywnej umowy najmu dla tego lokalu.");
        }

        // Logika wysyłania przypomnienia
        var tenant = rentalAgreement.Tenant;
        string message = $"Przypomnienie: Proszę dokonać płatności za lokal przy {property.Address}. Kwota: {rentalAgreement.MonthlyRent} zł.";

        // Zakładamy, że istnieje usługa EmailService lub SmsService
        // await _emailService.SendEmailAsync(tenant.Email, "Przypomnienie o płatności", message);

        TempData["SuccessMessage"] = "Przypomnienie zostało wysłane.";
        return RedirectToAction("Index");
    }
}
