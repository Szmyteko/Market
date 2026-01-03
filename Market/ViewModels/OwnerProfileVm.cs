using System.Collections.Generic;
using Market.Models;

namespace Market.ViewModels;

public class OwnerProfileVm
{
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public IEnumerable<Property> Listings { get; set; } = new List<Property>();
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
}