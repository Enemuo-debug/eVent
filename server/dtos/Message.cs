using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos;

public class Message
{
    [Required]
    public required string Subject { get; set; }
    [Required]
    public required string Body{ get; set; }
}
