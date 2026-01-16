using System;
using System.Linq;
using System.Threading.Tasks;
using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class RentalRequestController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public RentalRequestController(ApplicationDbContext ctx, UserManager<IdentityUser> um)
    {
        _context = ctx;
        _userManager = um;
    }

    // Najemca: utworzenie prośby z zakresem dat
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int propertyId, DateOnly? startDate, DateOnly? endDate)
    {
        // brak dat → komunikat
        if (!startDate.HasValue || !endDate.HasValue)
        {
            TempData["RequestError"] = "Wybierz datę rozpoczęcia i zakończenia.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }

        var start = startDate.Value;
        var end = endDate.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (start < today || end < today)
        {
            TempData["RequestError"] = "Daty nie mogą być w przeszłości.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }

        if (end < start)
        {
            TempData["RequestError"] = "Data zakończenia musi być po dacie rozpoczęcia.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }

        var userId = _userManager.GetUserId(User)!;

        var prop = await _context.Property
            .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsDeleted);
        if (prop == null)
        {
            TempData["RequestError"] = "Nie znaleziono lokalu.";
            return RedirectToAction("Index", "Property");
        }
        if (prop.ApprovalStatus != ListingApprovalStatus.Approved)
        {
            TempData["RequestError"] = "Ogłoszenie oczekuje na akceptację administratora.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }
        if (prop.UserId == userId)
        {
            TempData["RequestError"] = "Nie możesz wynająć własnego lokalu.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }

        var hasPending = await _context.RentalRequest.AnyAsync(r =>
            r.PropertyId == propertyId &&
            r.RequesterId == userId &&
            r.Status == RentalRequestStatus.Pending);
        if (hasPending)
        {
            TempData["RequestError"] = "Masz już oczekujące zapytanie dla tego lokalu.";
            return RedirectToAction(nameof(MyRentals));
        }

        var agreementOverlap = await _context.RentalAgreement.AnyAsync(ra =>
            ra.PropertyId == propertyId &&
            ((ra.EndDate == null && ra.StartDate <= end) ||
             (ra.EndDate != null && ra.StartDate <= end && start <= ra.EndDate)));
        var requestOverlap = await _context.RentalRequest.AnyAsync(rr =>
            rr.PropertyId == propertyId &&
            rr.Status != RentalRequestStatus.Rejected &&
            rr.StartDate <= end && start <= rr.EndDate);
        if (agreementOverlap || requestOverlap)
        {
            TempData["RequestError"] = "Lokal niedostępny w podanym zakresie dat.";
            return RedirectToAction("Details", "Property", new { id = propertyId });
        }

        _context.RentalRequest.Add(new RentalRequest
        {
            PropertyId = propertyId,
            RequesterId = userId,
            StartDate = start,
            EndDate = end,
            Status = RentalRequestStatus.Pending
        });
        await _context.SaveChangesAsync();

        TempData["RequestOk"] = "Wysłano prośbę o najem.";
        return RedirectToAction(nameof(MyRentals));
    }


    // Najemca: moje prośby
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyRentals()
    {
        var userId = _userManager.GetUserId(User)!;
        var reqs = await _context.RentalRequest
            .Include(r => r.Property)
            .Where(r => r.RequesterId == userId)
            .OrderByDescending(r => r.CreatedUtc)
            .AsNoTracking()
            .ToListAsync();

        return View(reqs);
    }

    // Właściciel: skrzynka z prośbami
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> OwnerInbox()
    {
        var ownerId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); 

        IQueryable<RentalRequest> q = _context.RentalRequest
            .Include(r => r.Property)
            .Include(r => r.Requester)
            .Where(r => r.Status == RentalRequestStatus.Pending); 

        if (!isAdmin) 
            q = q.Where(r => r.Property.UserId == ownerId);

        var reqs = await q
            .OrderBy(r => r.CreatedUtc)
            .AsNoTracking()
            .ToListAsync();

        return View(reqs);
    }

    // Właściciel/Admin: akceptacja + generowanie płatności
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? note)
    {
        var ownerId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); 

        IQueryable<RentalRequest> q = _context.RentalRequest
            .Where(r => r.Id == id)
            .Include(r => r.Property);

        if (!isAdmin) 
            q = q.Where(r => r.Property.UserId == ownerId);

        var rr = await q.FirstOrDefaultAsync();
        if (rr == null) return NotFound();

        // kolizje z istniejącymi umowami
        var overlap = await _context.RentalAgreement.AnyAsync(ra =>
            ra.PropertyId == rr.PropertyId &&
            (
                (ra.EndDate == null && ra.StartDate <= rr.EndDate) ||
                (ra.EndDate != null && ra.StartDate <= rr.EndDate && rr.StartDate <= ra.EndDate)
            ));
        if (overlap)
        {
            TempData["ApproveError"] = "Zakres koliduje z istniejącą umową.";
            return RedirectToAction(nameof(OwnerInbox));
        }

        // utwórz umowę
        var agr = new RentalAgreement
        {
            PropertyId = rr.PropertyId,
            TenantId = rr.RequesterId,
            StartDate = rr.StartDate,
            EndDate = rr.EndDate,
            UserId = rr.Property!.UserId,
            MonthlyRent = rr.Property.RentPrice ?? 0m 
        };
        _context.RentalAgreement.Add(agr);
        await _context.SaveChangesAsync(); 

        // harmonogram płatności (Start..End, co miesiąc)
        var amount = rr.Property.RentPrice ?? 0m;
        int i = 0;
        for (var due = rr.StartDate; due <= rr.EndDate; due = AddMonthsSafe(due, 1), i++)
        {
            _context.Payment.Add(new Payment
            {
                PropertyId = rr.PropertyId,
                RentalAgreementId = agr.Id,
                TenantId = rr.RequesterId,
                UserId = rr.Property.UserId,
                Amount = amount,
                Currency = "PLN",
                DueDate = due,
                Status = PaymentStatus.Pending,
                Title = $"Najem {rr.Property.Address} — rata {i + 1}",
                Reference = $"AGR{agr.Id:D6}-{i + 1:00}"
            });
        }

        // status prośby
        rr.Status = RentalRequestStatus.Approved;
        rr.OwnerDecisionNote = note;

        // dostępność na dziś (EndDate w RR jest wymagane)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeToday = rr.StartDate <= today && today <= rr.EndDate; 
        rr.Property.IsAvailable = !activeToday && !rr.Property.IsDeleted;

        // odrzuć inne nakładające się prośby
        var conflicting = await _context.RentalRequest
            .Where(x => x.PropertyId == rr.PropertyId &&
                        x.Id != rr.Id &&
                        x.Status == RentalRequestStatus.Pending &&
                        x.StartDate <= rr.EndDate && rr.StartDate <= x.EndDate)
            .ToListAsync();
        foreach (var c in conflicting) c.Status = RentalRequestStatus.Rejected;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(OwnerInbox));
    }

    // helper
    private static DateOnly AddMonthsSafe(DateOnly d, int months)
    {
        int y = d.Year;
        int m = d.Month + months;
        y += (m - 1) / 12;
        m = ((m - 1) % 12) + 1;
        int day = Math.Min(d.Day, DateTime.DaysInMonth(y, m));
        return new DateOnly(y, m, day);
    }

    // Właściciel/Admin: odrzucenie
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var ownerId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); 

        IQueryable<RentalRequest> q = _context.RentalRequest
            .Where(r => r.Id == id)
            .Include(r => r.Property);

        if (!isAdmin) 
            q = q.Where(r => r.Property.UserId == ownerId);

        var rr = await q.FirstOrDefaultAsync();
        if (rr == null) return NotFound();

        rr.Status = RentalRequestStatus.Rejected;
        rr.OwnerDecisionNote = note;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(OwnerInbox));
    }

    // Najemca: wycofanie prośby Pending
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = _userManager.GetUserId(User)!;
        var rr = await _context.RentalRequest
            .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == uid && r.Status == RentalRequestStatus.Pending);
        if (rr == null) return NotFound();

        rr.Status = RentalRequestStatus.Rejected;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyRentals));
    }
}
