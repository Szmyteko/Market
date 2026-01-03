using Market.Data;
using Market.Models;
using Market.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class VerificationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _um;
    private readonly IFileStorage _fs;
    public VerificationController(ApplicationDbContext db, UserManager<IdentityUser> um, IFileStorage fs)
    { _db = db; _um = um; _fs = fs; }

    [HttpGet] public IActionResult New() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(IFormFile front, IFormFile back, CancellationToken ct)
    {
        if (front is null || back is null) { ModelState.AddModelError("", "Dodaj przód i tył."); return View("New"); }
        static bool Valid(IFormFile f) => f.Length>0 && f.Length<=5_000_000 &&
            (f.ContentType is "image/jpeg" or "image/png" or "image/webp");
        if (!Valid(front) || !Valid(back)) { ModelState.AddModelError("", "JPG/PNG/WebP do 5 MB."); return View("New"); }

        var uid = _um.GetUserId(User)!;

        var hasPending = await _db.VerificationRequests.AnyAsync(v => v.UserId==uid && v.Status==VerificationStatus.Pending, ct);
        if (hasPending) { TempData["Err"]="Masz wniosek w toku."; return RedirectToAction(nameof(Status)); }

        var frontPath = await _fs.SaveUserDocAsync(uid, front, "front", ct);
        var backPath  = await _fs.SaveUserDocAsync(uid, back,  "back",  ct);

        var req = new VerificationRequest{
            UserId=uid, FrontPath=frontPath, BackPath=backPath,
            MimeFront=front.ContentType, MimeBack=back.ContentType
        };
        _db.VerificationRequests.Add(req);

        var uv = await _db.UserVerification.FindAsync(uid);
        if (uv is null) _db.UserVerification.Add(new UserVerification{ UserId=uid, Status=VerificationStatus.Pending, LastRequestId=req.Id });
        else { uv.Status = VerificationStatus.Pending; uv.LastRequestId = req.Id; }

        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Status));
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var uid = _um.GetUserId(User)!;
        var last = await _db.VerificationRequests
            .Where(v=>v.UserId==uid)
            .OrderByDescending(v=>v.SubmittedUtc)
            .FirstOrDefaultAsync();
        return View(last);
    }

    [HttpGet]
    public async Task<IActionResult> Doc(Guid id, string side, CancellationToken ct)
    {
        var req = await _db.VerificationRequests.FirstOrDefaultAsync(v=>v.Id==id, ct);
        if (req is null) return NotFound();
        var uid = _um.GetUserId(User)!;
        if (req.UserId!=uid && !User.IsInRole("Admin") && !User.IsInRole("Moderator")) return Forbid();
        var path = side=="front" ? req.FrontPath : req.BackPath;
        if (!_fs.Exists(path)) return NotFound();
        var mime = side=="front" ? req.MimeFront : req.MimeBack ?? "image/jpeg";
        var stream = await _fs.OpenAsync(path, ct);
        return File(stream, mime);
    }
}
