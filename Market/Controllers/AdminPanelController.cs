using System.Security.Claims;
using  Market.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace   Market.Controllers;

[Authorize]
public class AdminPanelController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AdminPanelController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
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
        var users = _userManager.Users.ToList();
        var usersWithRoles = users.Select(async user => new
        {
            User = user,
            Roles = await _userManager.GetRolesAsync(user)
        }).Select(task => task.Result);

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

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Użytkownik nie został znaleziony.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View("Delete", user);
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

        // Pobranie bieżących danych
        var properties = _context.Property.Where(p => p.UserId == id).ToList();
        var rentalAgreements = _context.RentalAgreement
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Where(ra => ra.UserId == id)
            .ToList();
        var payments = _context.Payment.Where(p => p.UserId == id).ToList();

        // Pobranie roli
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

    var currentRoles = await _userManager.GetRolesAsync(user); // Pobranie ról użytkownika
    var currentRole = currentRoles.FirstOrDefault(); // Zakładamy, że użytkownik ma tylko jedną rolę

    if (ModelState.IsValid)
    {
        // Aktualizacja podstawowych danych użytkownika
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
        

        // Walidacja zmiany roli
        if (!string.IsNullOrEmpty(selectedRole) && selectedRole != currentRole)
        {
            // Ograniczenie zmiany ról między "Tenant" i "Owner"
            if ((currentRole == "Wynajmujący" && selectedRole == "Najemca") || 
                (currentRole == "Najemca" && selectedRole == "Wynajmujący"))
            {
                ModelState.AddModelError(string.Empty, "Nie można zmienić roli z najemcy na wynajmującego ani odwrotnie.");
            }
            else
            {
                // Usunięcie obecnych ról i dodanie nowej
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

        // Jeśli brak błędów, przekieruj do listy użytkowników
        if (ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }
    }

    
    // Jeśli walidacja się nie powiedzie, przekazujemy ponownie dane
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
