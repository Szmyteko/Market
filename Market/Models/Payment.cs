using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <- dodane!
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public class Payment : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Proszę wprowadzić ID umowy najmu")]
    public int RentalAgreementId { get; set; }

    [ForeignKey("RentalAgreementId")]
    public RentalAgreement RentalAgreement { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [ForeignKey("PropertyId")]
    public Property Property { get; set; }

    public string? TenantId { get; set; }

    [ForeignKey("TenantId")]
    public Tenant? Tenant { get; set; }

    [Required(ErrorMessage = "Należy wprowadzić kwotę płatności.")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "Musisz wprowadzić datę płatności.")]
    public DateOnly Date { get; set; }

    public string Status { get; set; }

    public string? UserId { get; set; }

    [ForeignKey("UserId")]
    public IdentityUser? User { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount < 0)
        {
            yield return new ValidationResult("Kwota musi być dodatnia", new[] { nameof(Amount) });
        }
    }
}
