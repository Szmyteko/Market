using System.ComponentModel.DataAnnotations;

namespace Market.Models;
public class PropertyImage
{
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    [Required, MaxLength(300)]
    public string Url { get; set; } = default!;      
    [MaxLength(300)]
    public string? ThumbUrl { get; set; }

    public int SortOrder { get; set; } = 0;
}
