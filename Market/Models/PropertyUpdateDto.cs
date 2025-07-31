namespace Market.Models;

public class PropertyUpdateDto
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public int RentPrice { get; set; }
    public RentalAgreement? RentalAgreement { get; set; }
    public List<MaintenanceRequest>? MaintenanceRequest { get; set; }
}