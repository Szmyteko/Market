using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace   Market.Models;

public class Tenant
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Generowanie klucza głównego jako GUID
    [Required(ErrorMessage = "Imię i nazwisko są wymagane.")]
    public string FullName { get; set; }
    [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
    [RegularExpression(@"^\+?[0-9]{9,11}$", ErrorMessage = "Nieprawidłowy numer telefonu.")]
    public string PhoneNumber { get; set; }
    [Required(ErrorMessage = "Adres email jest wymagany.")]
    [RegularExpression(@"^[^@\s]+@[^@\s].[^@\s]+$", ErrorMessage = "Nieprawidłowy format adresu email.")]
    public string Email { get; set; }
    public ICollection<RentalAgreement> RentalAgreements { get; set; } = new List<RentalAgreement>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }
}