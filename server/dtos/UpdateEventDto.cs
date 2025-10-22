namespace e_Vent.dtos;
using System.ComponentModel.DataAnnotations;

public class UpdateEventDto
{
    [Required]
    public required string EventName { get; set; }

    [Required]
    public DateTime EventDate { get; set; }
    [Required]
    public required string Location { get; set; }

    public string EventDescription { get; set; } = "...";
    public string FormDescription { get; set; } = string.Empty;
}