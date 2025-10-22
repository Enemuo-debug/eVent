using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos;

public class CreateAccount
{
    [Required]
    [Range(5, 15)]
    public required string UserName { get; set; }
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }
    [Required]
    [Range(5, 100)]
    public required string OrganizationName { get; set; }
}
