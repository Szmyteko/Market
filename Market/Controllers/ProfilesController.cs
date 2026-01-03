using Market.Data;
using Market.Models;
using Market.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Market.Controllers;

[AllowAnonymous]
public class ProfilesController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<IdentityUser> _um;

    public ProfilesController(ApplicationDbContext ctx, UserManager<IdentityUser> um)
    { _ctx = ctx; _um = um; }

    // /u/{id}  → publiczny profil właściciela
    [HttpGet("/u/{id}")]
    public async Task<IActionResult> Owner(string id)
    {
        var user = await _um.FindByIdAsync(id);
        if (user == null) return NotFound();

        var allProps = await _ctx.Property
            .AsNoTracking()
            .Where(p => p.UserId == id)
            .ToListAsync();

        var listings = allProps
            .Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Approved)
            .OrderByDescending(p => p.Id)   // jeśli masz PropertyId, zmień na p.PropertyId
            .ToList();

        var ver = await _ctx.UserVerification.AsNoTracking()
            .Where(x => x.UserId == id)
            .Select(x => x.Status)
            .FirstOrDefaultAsync();

        var vm = new OwnerProfileVm
        {
            UserId = id,
            DisplayName = user.UserName ?? user.Email ?? "użytkownik",
            TotalListings = allProps.Count,                 // wszystkie
            ActiveListings = allProps.Count(p => !p.IsDeleted), // niezarchiwizowane
            Listings = listings,
            VerificationStatus = ver
        };

        ViewBag.ArchivedCount = allProps.Count(p => p.IsDeleted);
        return View(vm);
    }

}
