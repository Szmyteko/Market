using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Market.Models;

public class MaintenanceRequest
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public string Status { get; set; }
    public Property Property { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }
}