using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace   Market.Models;

public class RentalAgreement : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Id lokalu jest wymagane.")]
    public int PropertyId { get; set; }
    public Property Property { get; set; }
    [Required(ErrorMessage = "Id wynajmującego jest wymagane.")]
    public string TenantId { get; set; }
    public Tenant Tenant { get; set; }
    [Display(Name = "Początek najmu")]
    [Required(ErrorMessage = "Data początku najmu jest wymagana.")]
    public DateOnly StartDate { get; set; }
    [Display(Name = "Koniec najmu")]
    public DateOnly? EndDate { get; set; }
    [Required(ErrorMessage = "Wysokość czynszu jest wymagana.")]
    public int MonthlyRent { get; set; }
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate > EndDate)
        {
            yield return new ValidationResult(
                "Data musi zostać poprawnie wprowadzona (zakończenie najmu nie może być przed jego rozpoczęciem)",
                new[] { "Zakończenie najmu" });
        }

        if (MonthlyRent < 0)
        {
            yield return new ValidationResult("Czynsz musi być większy od zera.", new[] { "Wysokość czynszu." });
        }
    }
}