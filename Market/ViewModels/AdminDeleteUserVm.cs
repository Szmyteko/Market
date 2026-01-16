using Microsoft.AspNetCore.Identity;

namespace Market.ViewModels;

public sealed class AdminDeleteUserVm
{
    public IdentityUser User { get; set; } = default!;

 
    public int PropertiesOwned { get; set; }
    public int PropertiesApprovedAsModerator { get; set; }

    
    public int RentalRequestsAsRequester { get; set; }

   
    public int RentalAgreementsAsOwner { get; set; }
    public int RentalAgreementsAsTenant { get; set; }

  
    public int PaymentsAsOwner { get; set; }
    public int PaymentsAsTenant { get; set; }

 
    public int MaintenanceRequests { get; set; }


    public int ConversationMemberships { get; set; }
    public int MessagesSent { get; set; }


    public int VerificationRequests { get; set; }
    public bool HasUserVerificationRow { get; set; }

    public bool HasAnyRelations =>
        PropertiesOwned > 0 ||
        PropertiesApprovedAsModerator > 0 ||
        RentalRequestsAsRequester > 0 ||
        RentalAgreementsAsOwner > 0 ||
        RentalAgreementsAsTenant > 0 ||
        PaymentsAsOwner > 0 ||
        PaymentsAsTenant > 0 ||
        MaintenanceRequests > 0 ||
        ConversationMemberships > 0 ||
        MessagesSent > 0 ||
        VerificationRequests > 0 ||
        HasUserVerificationRow;
}
