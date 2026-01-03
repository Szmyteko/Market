using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Market.Models;

public enum VerificationStatus { Unverified=0, Pending=1, Rejected=2, Verified=3 }

public class VerificationRequest
{
    public Guid Id { get; set; }
    [Required] public string UserId { get; set; } = default!;
    public IdentityUser? User { get; set; }

    [Required] public string FrontPath { get; set; } = default!;
    [Required] public string BackPath  { get; set; } = default!;
    public string? MimeFront { get; set; }
    public string? MimeBack  { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTime SubmittedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedUtc { get; set; }
    public string? ReviewedById { get; set; }
    public string? RejectReason { get; set; }
}

// szybki lookup do badge
public class UserVerification
{
    [Key] public string UserId { get; set; } = default!;
    public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;
    public Guid? LastRequestId { get; set; }
}