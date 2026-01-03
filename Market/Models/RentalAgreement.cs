using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public class RentalAgreement : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Id lokalu jest wymagane.")]
    public int PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    // Najemca = IdentityUser
    [Required(ErrorMessage = "Id najemcy jest wymagane.")]
    public string TenantId { get; set; } = default!;
    public IdentityUser Tenant { get; set; } = default!;

    [Display(Name = "Początek najmu")]
    [Required(ErrorMessage = "Data początku najmu jest wymagana.")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "Koniec najmu")]
    public DateOnly? EndDate { get; set; }

    [Required(ErrorMessage = "Wysokość czynszu jest wymagana.")]
    [Range(0, int.MaxValue, ErrorMessage = "Czynsz musi być większy lub równy zero.")]
    public decimal MonthlyRent { get; set; }

    // Właściciel lokalu (IdentityUser)
    [Required]
    public string UserId { get; set; } = default!;
    public IdentityUser User { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.HasValue && StartDate > EndDate.Value)
        {
            yield return new ValidationResult(
                "Data zakończenia najmu nie może być wcześniejsza niż data rozpoczęcia.",
                new[] { nameof(EndDate) });
        }

        if (MonthlyRent < 0)
        {
            yield return new ValidationResult(
                "Czynsz musi być większy lub równy zero.",
                new[] { nameof(MonthlyRent) });
        }
    }
}
