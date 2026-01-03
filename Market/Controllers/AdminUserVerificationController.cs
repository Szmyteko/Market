using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize(Roles="Admin,Moderator")]
public class AdminUserVerificationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _um;
    public AdminUserVerificationController(ApplicationDbContext db, UserManager<IdentityUser> um){ _db=db; _um=um; }

    public async Task<IActionResult> Index(string status="Pending")
    {
        var st = Enum.Parse<VerificationStatus>(status, true);
        var list = await _db.VerificationRequests
            .Include(v=>v.User)
            .Where(v=>v.Status==st)
            .OrderBy(v=>v.SubmittedUtc)
            .AsNoTracking()
            .ToListAsync();
        ViewBag.Filter = status;
        return View(list);
    }

    public async Task<IActionResult> Review(Guid id)
    {
        var req = await _db.VerificationRequests.Include(v=>v.User).FirstOrDefaultAsync(v=>v.Id==id);
        if (req is null) return NotFound();
        return View(req);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(Guid id, bool approve, string? reason)
    {
        var req = await _db.VerificationRequests.FirstOrDefaultAsync(v=>v.Id==id);
        if (req is null) return NotFound();

        req.ReviewedUtc = DateTime.UtcNow;
        req.ReviewedById = _um.GetUserId(User);

        var uv = await _db.UserVerification.FindAsync(req.UserId) ?? new UserVerification{ UserId=req.UserId };
        if (approve)
        {
            req.Status = VerificationStatus.Verified;
            uv.Status = VerificationStatus.Verified;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reason))
                return RedirectToAction(nameof(Review), new { id });
            req.Status = VerificationStatus.Rejected;
            req.RejectReason = reason;
            uv.Status = VerificationStatus.Rejected;
        }
        uv.LastRequestId = req.Id;
        _db.UserVerification.Attach(uv).State = uv.LastRequestId==req.Id && _db.Entry(uv).IsKeySet ? EntityState.Modified : EntityState.Added;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
