using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos;

public class Message
{
    [Required]
    [MinLength(10, ErrorMessage = "Subject must be at least 10 charachters long")]
    public required string Subject { get; set; }
    [Required]
    [MinLength(10, ErrorMessage = "Body must be at least 10 charachters long")]
    public required string Body{ get; set; }
}
