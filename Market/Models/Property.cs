using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public class Property : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Musisz podać adres lokalu.")]
    public string? Address { get; set; }

    // --- POCZĄTEK ZMIAN ---
    // Zmieniamy typ na 'int?', aby umożliwić obsłużenie pustego pola w formularzu.
    // Atrybut [Required] nadal będzie pilnował, aby użytkownik podał wartość.
    [Required(ErrorMessage = "Należy podać cenę najmu.")]
    public int? RentPrice { get; set; }

    [Required(ErrorMessage = "Należy podać metraż lokalu.")]
    public int? Size { get; set; }
    // --- KONIEC ZMIAN ---

    public RentalAgreement? RentalAgreement { get; set; }
    public List<Payment> Payments { get; set; } = new List<Payment>();
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;
    public List<MaintenanceRequest>? ServiceRequests { get; set; } = new List<MaintenanceRequest>();
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Sprawdzamy, czy wartość została podana, zanim wykonamy na niej operacje
        if (RentPrice.HasValue && RentPrice.Value < 0)
        {
            yield return new ValidationResult("Cena najmu nie może być ujemna.", new[] { nameof(RentPrice) });
        }

        if (Size.HasValue && Size.Value <= 0)
        {
            yield return new ValidationResult("Metraż musi być większy od zera.", new[] { nameof(Size) });
        }
    }
}
