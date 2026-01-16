using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Market.Data;
using Market.Models;
using Market.Services;
using Market.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class AdminPanelController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public AdminPanelController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _fileStorage = fileStorage;
    }

    // Techniczny użytkownik, pod którego przepinamy historię (żeby nie psuć widoków innych osób)
    private const string DeletedUserEmail = "deleted-user@market.local";
    private const string DeletedUserUserName = "deleted_user";

    private async Task<IdentityUser> GetOrCreateDeletedUserAsync()
    {
        var existing = await _userManager.FindByEmailAsync(DeletedUserEmail);
        if (existing is not null)
            return existing;

        var u = new IdentityUser
        {
            UserName = DeletedUserUserName, // musi spełniać AllowedUserNameCharacters
            Email = DeletedUserEmail,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        // Hasło tylko do utworzenia (konto ma być zablokowane)
        var pwd = $"X!{Guid.NewGuid():N}a1";

        var res = await _userManager.CreateAsync(u, pwd);
        if (!res.Succeeded)
        {
            var msg = string.Join("; ", res.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Nie udało się utworzyć użytkownika technicznego [{DeletedUserUserName}]. {msg}");
        }

        // blokada konta (żeby nikt się nie logował)
        await _userManager.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);

        // U Ciebie jest tabela UserVerification (1 rekord per user) – zapewnij spójność
        if (!await _context.UserVerification.AnyAsync(x => x.UserId == u.Id))
        {
            _context.UserVerification.Add(new UserVerification
            {
                UserId = u.Id,
                Status = VerificationStatus.Unverified
            });
            await _context.SaveChangesAsync();
        }

        return u;
    }

    private async Task<AdminDeleteUserVm> BuildDeleteVmAsync(IdentityUser user)
    {
        var id = user.Id;
        var vm = new AdminDeleteUserVm { User = user };

        vm.PropertiesOwned = await _context.Property.CountAsync(p => p.UserId == id);
        vm.PropertiesApprovedAsModerator = await _context.Property.CountAsync(p => p.ApprovedByUserId == id);

        vm.RentalRequestsAsRequester = await _context.RentalRequest.CountAsync(r => r.RequesterId == id);

        vm.RentalAgreementsAsOwner = await _context.RentalAgreement.CountAsync(a => a.UserId == id);
        vm.RentalAgreementsAsTenant = await _context.RentalAgreement.CountAsync(a => a.TenantId == id);

        vm.PaymentsAsOwner = await _context.Payment.CountAsync(p => p.UserId == id);
        vm.PaymentsAsTenant = await _context.Payment.CountAsync(p => p.TenantId == id);

        vm.MaintenanceRequests = await _context.MaintenanceRequest.CountAsync(m => m.UserId == id);

        vm.ConversationMemberships = await _context.ConversationMembers.CountAsync(m => m.UserId == id);
        vm.MessagesSent = await _context.Messages.CountAsync(m => m.SenderId == id);

        vm.VerificationRequests = await _context.VerificationRequests.CountAsync(v => v.UserId == id);
        vm.HasUserVerificationRow = await _context.UserVerification.AnyAsync(v => v.UserId == id);

        return vm;
    }

    public async Task<IActionResult> AddRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && await _roleManager.RoleExistsAsync(roleName))
        {
            await _userManager.AddToRoleAsync(user, roleName);
            return Ok("Rola została dodana.");
        }
        return BadRequest("Nie udało się przypisać roli.");
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();

        var usersWithRoles = await Task.WhenAll(users.Select(async user => new
        {
            User = user,
            Roles = await _userManager.GetRolesAsync(user)
        }));

        return View(usersWithRoles);
    }

    public IActionResult Create()
    {
        var roles = _roleManager.Roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();

        ViewBag.Roles = roles;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string userName, string email, string phoneNumber, string password, string selectedRole)
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = phoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(selectedRole))
                {
                    await _userManager.AddToRoleAsync(user, selectedRole);
                }

                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
        return View();
    }

    // ====== DELETE (GET) – pokazuje VM z licznikami powiązań ======
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID użytkownika jest wymagane.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Użytkownik nie został znaleziony.");
        }

        var vm = await BuildDeleteVmAsync(user);
        return View(vm);
    }

    // ====== DELETE (POST) – świeży user usuwa się normalnie, a powiązany: przepinamy historię i usuwamy konto ======
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Użytkownik nie został znaleziony.");
        }

        var vm = await BuildDeleteVmAsync(user);

        // "Świeży" użytkownik – kasuj jak dotychczas
        if (!vm.HasAnyRelations)
        {
            var resultFresh = await _userManager.DeleteAsync(user);
            if (resultFresh.Succeeded)
            {
                return RedirectToAction("Index");
            }

            foreach (var error in resultFresh.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Delete", vm);
        }

        // Użytkownik ma powiązania: przepnij dane na [USUNIETY], usuń co trzeba i usuń konto
        var deletedUser = await GetOrCreateDeletedUserAsync();
        var nowUtc = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1) Weryfikacje: usuń pliki po ścieżkach z bazy + usuń rekordy
            var vreqs = await _context.VerificationRequests.Where(v => v.UserId == id).ToListAsync();
            foreach (var v in vreqs)
            {
                // u Ciebie ścieżki wyglądają na pełne ścieżki z dysku (LocalFileStorage zapisuje full path)
                if (!string.IsNullOrWhiteSpace(v.FrontPath) && System.IO.File.Exists(v.FrontPath))
                    System.IO.File.Delete(v.FrontPath);

                if (!string.IsNullOrWhiteSpace(v.BackPath) && System.IO.File.Exists(v.BackPath))
                    System.IO.File.Delete(v.BackPath);
            }

            if (vreqs.Count > 0)
                _context.VerificationRequests.RemoveRange(vreqs);

            var uv = await _context.UserVerification.FindAsync(id);
            if (uv is not null)
                _context.UserVerification.Remove(uv);

            await _context.SaveChangesAsync();

            // 2) Moderacja: ApprovedByUserId -> null (żeby nie blokowało delete)
            await _context.Property
                .Where(p => p.ApprovedByUserId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ApprovedByUserId, (string?)null));

            // 3) Ogłoszenia właściciela: archiwizuj + przepnij Ownera na [USUNIETY]
            await _context.Property
                .Where(p => p.UserId == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.UserId, deletedUser.Id)
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedUtc, nowUtc)
                    .SetProperty(p => p.IsAvailable, false));

            // 4) RentalRequest jako requester: usuń (po usunięciu konta i tak nie ma sensu)
            await _context.RentalRequest
                .Where(r => r.RequesterId == id)
                .ExecuteDeleteAsync();

            // 5) Umowy: przepnij (historia ma zostać dla drugiej strony)
            await _context.RentalAgreement
                .Where(a => a.UserId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.UserId, deletedUser.Id));

            await _context.RentalAgreement
                .Where(a => a.TenantId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TenantId, deletedUser.Id));

            // 6) Płatności: przepnij
            await _context.Payment
                .Where(p => p.UserId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, deletedUser.Id));

            await _context.Payment
                .Where(p => p.TenantId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.TenantId, deletedUser.Id));

            // 7) Zgłoszenia serwisowe: przepnij
            await _context.MaintenanceRequest
                .Where(m => m.UserId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.UserId, deletedUser.Id));

            // 8) Wiadomości: przepnij nadawcę
            await _context.Messages
                .Where(m => m.SenderId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.SenderId, deletedUser.Id));

            // 9) Członkostwo w rozmowach: usuń (żeby nie zostawiać “martwego” członka)
            await _context.ConversationMembers
                .Where(m => m.UserId == id)
                .ExecuteDeleteAsync();

            // 10) Usuń konto Identity (AspNetUsers + powiązane identity tables)
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await tx.RollbackAsync();
                return View("Delete", vm);
            }

            await tx.CommitAsync();
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, $"Błąd podczas usuwania użytkownika: {ex.Message}");
            return View("Delete", vm);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID użytkownika jest wymagane.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Użytkownik nie został znaleziony.");
        }

        var properties = _context.Property.Where(p => p.UserId == id).ToList();
        var rentalAgreements = _context.RentalAgreement
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Where(ra => ra.UserId == id)
            .ToList();
        var payments = _context.Payment.Where(p => p.UserId == id).ToList();

        var roles = _roleManager.Roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();

        var currentRoles = await _userManager.GetRolesAsync(user);

        ViewBag.Properties = properties;
        ViewBag.RentalAgreements = rentalAgreements;
        ViewBag.Payments = payments;
        ViewBag.Roles = roles;
        ViewBag.CurrentRole = currentRoles.FirstOrDefault();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string userName, string email, string phoneNumber, string selectedRole)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID użytkownika jest wymagane.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Użytkownik nie został znaleziony.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        if (ModelState.IsValid)
        {
            user.UserName = userName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            if (!string.IsNullOrEmpty(selectedRole) && selectedRole != currentRole)
            {
                if ((currentRole == "Wynajmujący" && selectedRole == "Najemca") ||
                    (currentRole == "Najemca" && selectedRole == "Wynajmujący"))
                {
                    ModelState.AddModelError(string.Empty, "Nie można zmienić roli z najemcy na wynajmującego ani odwrotnie.");
                }
                else
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (removeRolesResult.Succeeded)
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, selectedRole);
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                    else
                    {
                        foreach (var error in removeRolesResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
        }

        ViewBag.Properties = _context.Property.Where(p => p.UserId == id).ToList();
        ViewBag.RentalAgreements = _context.RentalAgreement
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Where(ra => ra.UserId == id)
            .ToList();
        ViewBag.Payments = _context.Payment.Where(p => p.UserId == id).ToList();
        ViewBag.Roles = _roleManager.Roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();
        ViewBag.CurrentRole = currentRole;

        return View(user);
    }
}
