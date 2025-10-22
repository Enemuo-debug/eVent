namespace e_Vent.dtos;
using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    [Required]
    public required string UserName { get; set; }
    [Required]
    public required string Password { get; set; }
}
