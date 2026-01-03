using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Market.Models;

public class MaintenanceRequest
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // Data utworzenia zg³oszenia
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // U¿ytkownik, który zg³asza problem (najemca)
    [Required]
    public string UserId { get; set; } = default!;
    public IdentityUser User { get; set; } = default!;

    // Status zg³oszenia np. Oczekuj¹ce / W trakcie / Zakoñczone
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Oczekuj¹ce";
}
