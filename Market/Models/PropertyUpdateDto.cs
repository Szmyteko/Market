using System.ComponentModel.DataAnnotations;

namespace Market.Models;

public class PropertyUpdateDto
{
    public int Id { get; set; }
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal RentPrice { get; set; }
}