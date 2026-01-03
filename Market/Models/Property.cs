using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public enum ListingApprovalStatus { Pending = 0, Approved = 1, Rejected = 2 }

public class Property : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Musisz podać adres lokalu.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Należy podać cenę najmu.")]
    public decimal? RentPrice { get; set; }   // było int? -> decimal? dla walut

    [Required(ErrorMessage = "Należy podać metraż lokalu.")]
    public int? Size { get; set; }

    public string? Description { get; set; }

    public bool IsAvailable { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedUtc { get; set; }

    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }

    //Historia zmian 
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // opcjonalne, ale polecam
    public DateTime? UpdatedUtc { get; set; }

    // Moderacja ogłoszeń
    [Required]
    public ListingApprovalStatus ApprovalStatus { get; set; } = ListingApprovalStatus.Pending;
    public string? ModerationNote { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public string? ApprovedByUserId { get; set; }
    public IdentityUser? ApprovedByUser { get; set; }

    // Powiązania
    public ICollection<RentalAgreement> RentalAgreements { get; set; } = new List<RentalAgreement>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<MaintenanceRequest> ServiceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();

    //Udogodnienia
    public bool HasWifi { get; set; }
    public bool HasPrivateBathroom { get; set; }
    public bool HasBalcony { get; set; }
    public string? AmenitiesNote { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (RentPrice.HasValue && RentPrice.Value < 0)
            yield return new ValidationResult("Cena najmu nie może być ujemna.", new[] { nameof(RentPrice) });

        if (Size.HasValue && Size.Value <= 0)
            yield return new ValidationResult("Metraż musi być większy od zera.", new[] { nameof(Size) });
    }
}
