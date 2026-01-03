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
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<IdentityUser> _um;

    public PaymentController(ApplicationDbContext ctx, UserManager<IdentityUser> um)
    {
        _ctx = ctx;
        _um = um;
    }

    // Najemca: moje płatności
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var uid = _um.GetUserId(User)!;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await _ctx.Payment
            .Where(p => p.TenantId == uid && p.Status == PaymentStatus.Pending && p.DueDate < today)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Overdue));

        var items = await _ctx.Payment
            .Where(p => p.TenantId == uid)
            .Include(p => p.Property)
            .Include(p => p.RentalAgreement)
            .OrderBy(p => p.Status).ThenBy(p => p.DueDate)
            .AsNoTracking()
            .ToListAsync();

        return View(items);
    }

    // Właściciel: płatności dla moich lokali
    [Authorize]
    public async Task<IActionResult> Owner()
    {
        var uid = _um.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); // [NOWE] rola poza LINQ

        IQueryable<Payment> q = _ctx.Payment;

        if (!isAdmin)
            q = q.Where(p => p.UserId == uid); // [NOWE] filtr własności tylko dla nie-admina

        var items = await q
            .Include(p => p.Property)
            .Include(p => p.Tenant)
            .OrderBy(p => p.Status).ThenBy(p => p.DueDate)
            .AsNoTracking()
            .ToListAsync();

        return View(items);
    }

    // Właściciel/Admin: oznacz jako opłacone
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var uid = _um.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<Payment> q = _ctx.Payment.Where(p => p.Id == id); // [NOWE]
        if (!isAdmin)
            q = q.Where(p => p.UserId == uid); // [NOWE]

        var pay = await q.FirstOrDefaultAsync();
        if (pay == null) return NotFound();

        pay.Status = PaymentStatus.Paid;
        pay.PaidUtc = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();

        return RedirectToAction(nameof(Owner));
    }

    // Najemca: opłać (symulacja)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id)
    {
        var uid = _um.GetUserId(User)!;

        var pay = await _ctx.Payment
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == uid && p.Status == PaymentStatus.Pending);
        if (pay == null) return NotFound();

        // TODO: integracja bramki płatniczej
        pay.Status = PaymentStatus.Paid;
        pay.PaidUtc = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
