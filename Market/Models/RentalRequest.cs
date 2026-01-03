using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public enum RentalRequestStatus { Pending = 0, Approved = 1, Rejected = 2 }

public class RentalRequest : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    [Required]
    public string RequesterId { get; set; } = default!;
    public IdentityUser Requester { get; set; } = default!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Required]
    public RentalRequestStatus Status { get; set; } = RentalRequestStatus.Pending;

    public string? OwnerDecisionNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "Data zakończenia musi być nie wcześniejsza niż data rozpoczęcia.",
                new[] { nameof(EndDate), nameof(StartDate) });
        }
    }
}
