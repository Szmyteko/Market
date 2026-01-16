using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public enum PaymentStatus { Pending = 0, Paid = 1, Overdue = 2, Cancelled = 3 }

public class Payment : IValidatableObject
{
    public int Id { get; set; }

    // Powiązania
    [Required] public int PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    [Required] public int RentalAgreementId { get; set; }
    public RentalAgreement RentalAgreement { get; set; } = default!;

    [Required] public string TenantId { get; set; } = default!;
    public IdentityUser Tenant { get; set; } = default!;

    [Required] public string UserId { get; set; } = default!; 
    public IdentityUser User { get; set; } = default!;

    // Kwoty i terminy
    [Required, Range(0, double.MaxValue, ErrorMessage = "Kwota musi być nieujemna.")]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "PLN";

    [DataType(DataType.Date)]
    public DateOnly DueDate { get; set; }        // termin płatności

    public DateTime? PaidUtc { get; set; }       // kiedy opłacono

    // Status i meta
    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(80)]
    public string? Reference { get; set; }      
    [MaxLength(240)]
    public string? Title { get; set; }           
    // Walidacja modelu
    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Amount < 0)
            yield return new ValidationResult("Kwota musi być nieujemna.", new[] { nameof(Amount) });
    }
}
