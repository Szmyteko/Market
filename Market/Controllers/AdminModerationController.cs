using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class AdminModerationController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<IdentityUser> _um;
    public AdminModerationController(ApplicationDbContext ctx, UserManager<IdentityUser> um)
    { _ctx = ctx; _um = um; }

    [HttpGet]
    public async Task<IActionResult> Queue()
    {
        var items = await _ctx.Property
            .Include(p => p.User)
            .Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Pending)
            .OrderByDescending(p => p.UpdatedUtc ?? DateTime.MinValue)
            .AsNoTracking()
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? note)
    {
        var uid = _um.GetUserId(User)!;
        var p = await _ctx.Property.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        p.ApprovalStatus = ListingApprovalStatus.Approved;
        p.ModerationNote = null;
        p.ApprovedUtc = DateTime.UtcNow;
        p.ApprovedByUserId = uid;
        p.IsAvailable = true;

        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Queue));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var p = await _ctx.Property.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        p.ApprovalStatus = ListingApprovalStatus.Rejected;
        p.ModerationNote = reason;
        p.IsAvailable = false;

        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Queue));
    }
}