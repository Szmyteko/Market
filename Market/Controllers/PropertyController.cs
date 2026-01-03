using Market.Data;
using Market.Models;
using Market.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace Market.Controllers;

[Authorize]
public class PropertyController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public PropertyController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // LISTA OGŁOSZEŃ (publiczna) - tylko zatwierdzone
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var properties = await _context.Property
            .Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Approved)
            .Include(p => p.User)
            .AsNoTracking()
            .ToListAsync();
      
        var ownerIds = properties.Select(p => p.UserId).Where(x => x != null).Distinct()!.ToList();
        var verMap = await _context.UserVerification.AsNoTracking()
            .Where(x => ownerIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.Status);

        ViewBag.OwnerVerification = verMap; // IDictionary<string, VerificationStatus>


        var activeToday = await _context.RentalAgreement
            .Where(ra =>
                (ra.EndDate == null && ra.StartDate <= today) ||
                (ra.EndDate != null && ra.StartDate <= today && today <= ra.EndDate))
            .GroupBy(ra => ra.PropertyId)
            .Select(g => new { PropertyId = g.Key, MaxEnd = g.Max(a => a.EndDate) })
            .ToListAsync();

        var availableMap = properties.ToDictionary(p => p.Id, _ => true);
        var nextFreeMap = new Dictionary<int, DateOnly?>();

        foreach (var a in activeToday)
        {
            availableMap[a.PropertyId] = false;
            nextFreeMap[a.PropertyId] = a.MaxEnd.HasValue ? a.MaxEnd.Value.AddDays(1) : (DateOnly?)null;
        }

        ViewBag.AvailableToday = availableMap; // IDictionary<int,bool>
        ViewBag.NextFreeFrom = nextFreeMap;    // IDictionary<int,DateOnly?>
        return View(properties);
    }

    // SZCZEGÓŁY
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var property = await _context.Property
            .Include(p => p.User)
            .Include(p => p.ServiceRequests)
            .Include(p => p.RentalAgreements).ThenInclude(ra => ra.Tenant)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        ViewBag.IsOwner = currentUserId != null && property.UserId == currentUserId;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var occupiedToday = property.RentalAgreements.Any(ra =>
            (ra.EndDate == null && ra.StartDate <= today) ||
            (ra.EndDate != null && ra.StartDate <= today && today <= ra.EndDate.Value));
        ViewBag.AvailableToday = !occupiedToday && !property.IsDeleted;

        // zakresy do zablokowania w kalendarzu
        var farFuture = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(10);
        var busy = property.RentalAgreements
            .Select(ra => new
            {
                from = ra.StartDate.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd"),
                to = (ra.EndDate ?? farFuture).ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd")
            })
            .ToList();

        ViewBag.BusyJson = JsonSerializer.Serialize(busy);
        return View(property);
    }

    // UTWÓRZ OGŁOSZENIE (GET)
    [Authorize]
    public IActionResult Create() => View(new Property());

    // MOJE LOKALE
    [Authorize]
    public async Task<IActionResult> MyListings(string filter = "all")
    {
        var userId = _userManager.GetUserId(User);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Property> q = _context.Property.Where(p => p.UserId == userId);

        q = filter switch
        {
            "available" => q.Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Approved),
            "pending" => q.Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Pending),
            "rejected" => q.Where(p => !p.IsDeleted && p.ApprovalStatus == ListingApprovalStatus.Rejected),
            "archived" => q.Where(p => p.IsDeleted),
            _ => q
        };

        var properties = await q
            .OrderByDescending(p => p.UpdatedUtc ?? DateTime.MinValue)
            .ThenByDescending(p => p.Id)
            .AsNoTracking()
            .ToListAsync();

        var propIds = properties.Select(p => p.Id).ToList();

        var busyIds = await _context.RentalAgreement
            .Where(ra => propIds.Contains(ra.PropertyId) &&
                ((ra.EndDate == null && ra.StartDate <= today) ||
                 (ra.EndDate != null && ra.StartDate <= today && today <= ra.EndDate.Value)))
            .Select(ra => ra.PropertyId)
            .Distinct()
            .ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.Availability = properties.ToDictionary(p => p.Id, p => !busyIds.Contains(p.Id));

        return View(properties);
    }

    // DODAJ OGŁOSZENIE (POST)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Property model, List<IFormFile>? files)
    {
        if (!ModelState.IsValid) return View(model);

        model.UserId = _userManager.GetUserId(User);
        model.IsAvailable = true;
        model.IsDeleted = false;
        model.ApprovalStatus = ListingApprovalStatus.Pending;

        _context.Property.Add(model);
        await _context.SaveChangesAsync(); // potrzebne model.Id

        // zapisz zdjęcia (te same reguły co w UploadImages)
        var okTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };
        var okExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "properties", model.Id.ToString());
        Directory.CreateDirectory(root);

        int saved = 0;
        foreach (var f in files ?? Enumerable.Empty<IFormFile>())
        {
            if (f.Length == 0 || f.Length > 5 * 1024 * 1024) continue;
            if (!okTypes.Contains(f.ContentType)) continue;

            var ext = Path.GetExtension(f.FileName);
            if (!okExt.Contains(ext)) continue;

            var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var rel = $"/uploads/properties/{model.Id}/{name}";
            var abs = Path.Combine(root, name);

            using (var fs = System.IO.File.Create(abs))
                await f.CopyToAsync(fs);

            _context.PropertyImage.Add(new PropertyImage { PropertyId = model.Id, Url = rel });
            saved++;
        }
        if (saved > 0) model.UpdatedUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"Ogłoszenie wysłane do akceptacji. {(saved > 0 ? $"Dodano {saved} zdjęć." : "")}";
        return RedirectToAction(nameof(MyListings));
    }

    // EDYCJA (GET)  // [NOWE] wariant 1
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<Property> q = _context.Property.Where(p => p.Id == id); // [NOWE]
        if (!isAdmin) q = q.Where(p => p.UserId == userId);                // [NOWE]

        var property = await q
            .Include(p => p.RentalAgreements.Where(ra => ra.EndDate == null))
                .ThenInclude(ra => ra.Tenant)
            .FirstOrDefaultAsync();

        if (property == null) return NotFound();

        var dto = new PropertyUpdateDto
        {
            Id = property.Id,
            Description = property.Description,
            RentPrice = property.RentPrice ?? 0m
        };

        ViewBag.ActiveAgreement = property.RentalAgreements.SingleOrDefault();

        ViewBag.Images = await _context.PropertyImage
            .Where(i => i.PropertyId == id)
            .OrderBy(i => i.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return View(dto);
    }

    // EDYCJA (POST)  // [NOWE] sprawdzenie własności
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyUpdateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        var property = await _context.Property.FirstOrDefaultAsync(p => p.Id == dto.Id);
        if (property == null) return NotFound();
        if (!isAdmin && property.UserId != userId) return Forbid(); // [NOWE]

        var descriptionChanged = (property.Description ?? string.Empty) != (dto.Description ?? string.Empty);
        var priceChanged = property.RentPrice != dto.RentPrice;

        property.Description = dto.Description;
        property.RentPrice = dto.RentPrice;

        if (property.ApprovalStatus == ListingApprovalStatus.Approved && descriptionChanged)
        {
            property.ApprovalStatus = ListingApprovalStatus.Pending;
            property.ModerationNote = null;
            property.ApprovedUtc = null;
            property.ApprovedByUserId = null;
            TempData["Ok"] = "Zmiana opisu. Ogłoszenie wysłane ponownie do akceptacji.";
        }
        else
        {
            TempData["Ok"] = priceChanged ? "Zmieniono cenę." : "Zapisano.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyListings));
    }

    // ZAKOŃCZ AKTYWNY NAJEM  // [NOWE] sprawdzenie własności
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseActiveRental(int propertyId)
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        var property = await _context.Property
            .Include(p => p.RentalAgreements)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null) return NotFound("Nie znaleziono lokalu.");
        if (!isAdmin && property.UserId != userId) return Forbid(); // [NOWE]

        var active = property.RentalAgreements.FirstOrDefault(ra => ra.EndDate == null);
        if (active == null) return NotFound("Brak aktywnej umowy.");

        active.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        property.IsAvailable = true;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = propertyId });
    }

    // USUŃ OGŁOSZENIE (soft delete)  // [NOWE] wariant 1
    [HttpPost, ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<Property> q = _context.Property.Where(p => p.Id == id); // [NOWE]
        if (!isAdmin) q = q.Where(p => p.UserId == userId);               // [NOWE]

        var property = await q.FirstOrDefaultAsync();
        if (property == null) return NotFound();

        property.IsDeleted = true;
        property.DeletedUtc = DateTime.UtcNow;
        property.IsAvailable = false;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyListings));
    }

    // PRZYWRÓĆ OGŁOSZENIE  // [NOWE] wariant 1
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var uid = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<Property> q = _context.Property.Where(p => p.Id == id && p.IsDeleted); // [NOWE]
        if (!isAdmin) q = q.Where(p => p.UserId == uid);                                   // [NOWE]

        var property = await q.FirstOrDefaultAsync();
        if (property == null) return NotFound();

        property.IsDeleted = false;
        property.DeletedUtc = null;

        property.ApprovalStatus = ListingApprovalStatus.Pending;
        property.ModerationNote = null;
        property.ApprovedUtc = null;
        property.ApprovedByUserId = null;

        property.IsAvailable = false;
        property.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Ok"] = "Ogłoszenie przywrócone i wysłane do akceptacji.";

        return RedirectToAction(nameof(MyListings), new { filter = "pending" });
    }

    // ZGŁOSZENIE SERWISOWE (tworzenie)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateServiceRequest(int propertyId, string description)
    {
        var property = await _context.Property.FindAsync(propertyId);
        if (property == null) return NotFound();

        var request = new MaintenanceRequest
        {
            PropertyId = propertyId,
            Description = description,
            CreatedUtc = DateTime.UtcNow,
            Status = "new",
            UserId = _userManager.GetUserId(User)!
        };

        _context.MaintenanceRequest.Add(request);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = propertyId });
    }

    // ZMIANA STATUSU ZGŁOSZENIA SERWISOWEGO  // [NOWE] kontrola właściciela/admina
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestStatus(int requestId, string status)
    {
        var uid = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        var request = await _context.MaintenanceRequest
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return NotFound();
        if (!isAdmin && request.Property.UserId != uid) return Forbid(); // [NOWE]

        request.Status = status;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(MyListings));
    }

    // ARCHIWUM (alias)  // [NOWE] usunięcie duplikatu widoku
    [Authorize]
    [HttpGet]
    public IActionResult ArchivedListings()
        => RedirectToAction(nameof(MyListings), new { filter = "archived" });

    // ZAJĘTE ZAKRESY DLA KALENDARZA
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> BlockedRanges(int id)
    {
        var agr = await _context.RentalAgreement
            .Where(a => a.PropertyId == id)
            .Select(a => new { a.StartDate, End = a.EndDate ?? new DateOnly(2099, 12, 31) })
            .ToListAsync();

        var req = await _context.RentalRequest
            .Where(r => r.PropertyId == id && r.Status != RentalRequestStatus.Rejected)
            .Select(r => new { r.StartDate, r.EndDate })
            .ToListAsync();

        var all = agr.Select(x => new { start = x.StartDate.ToString("yyyy-MM-dd"), end = x.End.ToString("yyyy-MM-dd") })
                     .Concat(req.Select(x => new { start = x.StartDate.ToString("yyyy-MM-dd"), end = x.EndDate.ToString("yyyy-MM-dd") }));

        return Json(all);
    }

    // UPLOAD ZDJĘĆ
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile>? files)
    {
        var userId = _userManager.GetUserId(User)!;

        var prop = await _context.Property.FirstOrDefaultAsync(p => p.Id == id);
        if (prop == null) return NotFound();
        if (prop.UserId != userId && !User.IsInRole("Admin")) return Forbid();

        var okTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };
        var okExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "properties", id.ToString());
        Directory.CreateDirectory(root);

        int saved = 0;
        foreach (var f in files ?? Enumerable.Empty<IFormFile>())
        {
            if (f.Length == 0 || f.Length > 5 * 1024 * 1024) continue;
            if (!okTypes.Contains(f.ContentType)) continue;

            var ext = Path.GetExtension(f.FileName);
            if (!okExt.Contains(ext)) continue;

            var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var rel = $"/uploads/properties/{id}/{name}";
            var abs = Path.Combine(root, name);

            using (var fs = System.IO.File.Create(abs))
                await f.CopyToAsync(fs);

            _context.PropertyImage.Add(new PropertyImage { PropertyId = id, Url = rel });
            saved++;
        }

        if (saved > 0)
        {
            prop.ApprovalStatus = ListingApprovalStatus.Pending; // „Oczekujące”
            prop.UpdatedUtc = DateTime.UtcNow;
            TempData["RequestOk"] = $"Dodano {saved} plik(ów). Ogłoszenie ustawiono na Oczekujące.";
        }
        else
        {
            TempData["RequestError"] = "Nie dodano żadnego pliku. Sprawdź format i rozmiar (max 5 MB).";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id });
    }

    // USUNIĘCIE ZDJĘCIA  // [NOWE] wariant 1
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var uid = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<PropertyImage> q = _context.PropertyImage
            .Include(i => i.Property)
            .Where(i => i.Id == imageId && i.PropertyId == id); // [NOWE]

        if (!isAdmin) q = q.Where(i => i.Property.UserId == uid); // [NOWE]

        var img = await q.FirstOrDefaultAsync();
        if (img == null) return NotFound();

        var abs = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(abs)) System.IO.File.Delete(abs);

        _context.PropertyImage.Remove(img);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id });
    }

    // PONOWNE WYSŁANIE DO MODERACJI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resubmit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var prop = await _context.Property.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && !p.IsDeleted);
        if (prop == null) return NotFound();

        prop.ApprovalStatus = ListingApprovalStatus.Pending;
        prop.ModerationNote = null;
        prop.ApprovedUtc = null;
        prop.ApprovedByUserId = null;

        await _context.SaveChangesAsync();
        TempData["Ok"] = "Ogłoszenie wysłane ponownie do akceptacji.";
        return RedirectToAction(nameof(MyListings));
    }

    // DATY (lista próśb dla ogłoszenia)  // [NOWE] wariant 1
    [Authorize]
    public async Task<IActionResult> Dates(int id)
    {
        var uid = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin"); // [NOWE]

        IQueryable<Property> pq = _context.Property.Where(p => p.Id == id); // [NOWE]
        if (!isAdmin) pq = pq.Where(p => p.UserId == uid);                 // [NOWE]

        var property = await pq.Include(p => p.User).FirstOrDefaultAsync();
        if (property == null) return NotFound();

        var items = await _context.RentalRequest
            .Where(r => r.PropertyId == id)
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new PropertyDatesVM.Row
            {
                RequestId = r.Id,
                UserId = r.RequesterId,
                UserName = (r.Requester != null ? (r.Requester.UserName ?? r.Requester.Email) : null) ?? r.RequesterId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                Status = r.Status,
                CreatedUtc = r.CreatedUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var vm = new PropertyDatesVM
        {
            PropertyId = property.Id,
            Address = property.Address ?? "",
            Items = items
        };
        return View(vm);
    }
}
