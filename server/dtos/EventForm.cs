using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos;

public class EventForm
{
    [Required]
    public required int eventId { get; set; }
    [Required]
    public required List<string> FormData { get; set; }
}
